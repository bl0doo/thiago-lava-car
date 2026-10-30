using ThiagoLavaCar.Application.Abstractions;
using ThiagoLavaCar.Application.DTOs;
using ThiagoLavaCar.Application.Exceptions;
using ThiagoLavaCar.Application.Validators;
using ThiagoLavaCar.Domain.Entities;
using ThiagoLavaCar.Domain.Enums;
using ThiagoLavaCar.Domain.Exceptions;

namespace ThiagoLavaCar.Application.Services;

public sealed class AuthService(
    IUsuarioRepository usuarios,
    IClienteRepository clientes,
    IRefreshTokenRepository refreshTokens,
    IUnitOfWork unitOfWork,
    ISenhaHasher hasher,
    ITokenService tokens,
    IUsuarioLogado usuarioLogado,
    TimeProvider relogio) : IAuthService
{
    private const string MensagemEmailOuSenha = "E-mail ou senha incorretos.";
    private const string MensagemPin = "PIN incorreto.";
    private const string MensagemSessao = "Sessão expirada. Entre novamente.";

    public async Task<SessaoIniciada> LoginAsync(LoginRequest request, CancellationToken ct)
    {
        var agora = relogio.GetUtcNow();
        var usuario = await usuarios.ObterPorEmailAsync(request.Email.Trim().ToLowerInvariant(), ct);

        if (usuario is null || usuario.Perfil == Perfil.Lavador || usuario.SenhaHash is null)
        {
            hasher.SimularVerificacao(request.Senha);
            throw new NaoAutenticadoException(MensagemEmailOuSenha);
        }

        await ConferirSegredoAsync(usuario, hasher.Verificar(usuario.SenhaHash, request.Senha), MensagemEmailOuSenha, agora, ct);
        return await CriarSessaoAsync(usuario, agora, fimDoTurno: null, ct);
    }

    public async Task<SessaoIniciada> LoginPinAsync(LoginPinRequest request, CancellationToken ct)
    {
        var agora = relogio.GetUtcNow();
        var usuario = await usuarios.ObterPorIdAsync(request.UsuarioId, ct);

        if (usuario is null || usuario.Perfil != Perfil.Lavador || usuario.PinHash is null)
        {
            hasher.SimularVerificacao(request.Pin);
            throw new NaoAutenticadoException(MensagemPin);
        }

        await ConferirSegredoAsync(usuario, hasher.Verificar(usuario.PinHash, request.Pin), MensagemPin, agora, ct);
        return await CriarSessaoAsync(usuario, agora, fimDoTurno: null, ct);
    }

    public async Task<SessaoIniciada> CadastrarClienteAsync(CadastroClienteRequest request, CancellationToken ct)
    {
        var agora = relogio.GetUtcNow();
        var email = request.Email.Trim().ToLowerInvariant();
        var telefone = Telefones.Normalizar(request.Telefone);

        if (await usuarios.ObterPorEmailAsync(email, ct) is not null)
        {
            throw new ConflitoException("Já existe uma conta com este e-mail. Entre com a senha ou use outro e-mail.");
        }

        if (await clientes.ObterPorTelefoneAsync(telefone, ct) is not null)
        {
            throw new ConflitoException("Já existe um cadastro com este telefone. Fale com o Thiago para vincular sua conta.");
        }

        var canal = request.CanalPreferido is null ? CanalContato.WhatsApp : Enum.Parse<CanalContato>(request.CanalPreferido, ignoreCase: true);
        var nome = request.Nome.Trim();
        var usuario = Usuario.CriarComSenha(nome, email, hasher.Gerar(request.Senha), Perfil.Cliente, agora);
        await usuarios.AdicionarAsync(usuario, ct);
        await unitOfWork.SalvarAsync(ct); // precisa do id do usuário para ligar a ficha do cliente
        await clientes.AdicionarAsync(Cliente.Criar(nome, telefone, email, canal, request.ConsentimentoLgpd, agora, usuario.Id), ct);
        await unitOfWork.SalvarAsync(ct);

        return await CriarSessaoAsync(usuario, agora, fimDoTurno: null, ct);
    }

    public async Task<SessaoIniciada> RenovarAsync(string? refreshToken, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            throw new NaoAutenticadoException(MensagemSessao);
        }

        var agora = relogio.GetUtcNow();
        var atual = await refreshTokens.ObterPorHashAsync(tokens.CalcularHash(refreshToken), ct)
            ?? throw new NaoAutenticadoException(MensagemSessao);

        if (atual.FoiRevogado)
        {
            // token já usado: alguém pode ter copiado o cookie, então a sessão inteira cai
            await refreshTokens.RevogarTodosDoUsuarioAsync(atual.UsuarioId, agora, ct);
            await unitOfWork.SalvarAsync(ct);
            throw new NaoAutenticadoException(MensagemSessao);
        }

        var usuario = await usuarios.ObterPorIdAsync(atual.UsuarioId, ct);
        if (!atual.EstaAtivo(agora) || usuario is not { Ativo: true } || usuario.EstaBloqueado(agora))
        {
            throw new NaoAutenticadoException(MensagemSessao);
        }

        atual.Revogar(agora);
        // a sessão do lavador acaba no fim do turno: renovar troca o token, mas não estende o prazo
        var fimDoTurno = usuario.Perfil == Perfil.Lavador ? atual.ExpiraEm : (DateTimeOffset?)null;
        return await CriarSessaoAsync(usuario, agora, fimDoTurno, ct);
    }

    public async Task SairAsync(string? refreshToken, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            return;
        }

        var atual = await refreshTokens.ObterPorHashAsync(tokens.CalcularHash(refreshToken), ct);
        if (atual is null)
        {
            return;
        }

        atual.Revogar(relogio.GetUtcNow());
        await unitOfWork.SalvarAsync(ct);
    }

    public async Task<UsuarioResponse> ObterUsuarioLogadoAsync(CancellationToken ct)
    {
        var usuario = await usuarios.ObterPorIdAsync(usuarioLogado.Id, ct)
            ?? throw new NaoAutenticadoException(MensagemSessao);
        return Mapear(usuario);
    }

    public async Task<IReadOnlyList<LavadorResumoResponse>> ListarLavadoresAsync(CancellationToken ct)
    {
        var lavadores = await usuarios.ListarLavadoresAtivosAsync(ct);
        return lavadores.Select(l => new LavadorResumoResponse(l.Id, l.Apelido ?? l.Nome)).ToList();
    }

    /// <summary>Bloqueio vem antes da conferência: durante os 15 minutos nem a senha certa entra.</summary>
    private async Task ConferirSegredoAsync(Usuario usuario, bool segredoCorreto, string mensagemErro, DateTimeOffset agora, CancellationToken ct)
    {
        if (usuario.EstaBloqueado(agora))
        {
            throw BloqueioDe(usuario);
        }

        if (!segredoCorreto)
        {
            usuario.RegistrarFalhaDeLogin(agora);
            await unitOfWork.SalvarAsync(ct);
            throw usuario.EstaBloqueado(agora) ? BloqueioDe(usuario) : new NaoAutenticadoException(mensagemErro);
        }

        // usuário inativo recebe a mesma resposta de credencial errada
        if (!usuario.Ativo)
        {
            throw new NaoAutenticadoException(mensagemErro);
        }

        usuario.RegistrarLoginComSucesso();
    }

    private static UsuarioBloqueadoException BloqueioDe(Usuario usuario) =>
        new($"Acesso bloqueado por {(int)Usuario.DuracaoBloqueio.TotalMinutes} minutos após várias tentativas.");

    private async Task<SessaoIniciada> CriarSessaoAsync(Usuario usuario, DateTimeOffset agora, DateTimeOffset? fimDoTurno, CancellationToken ct)
    {
        int? clienteId = usuario.Perfil == Perfil.Cliente ? await clientes.ObterIdPorUsuarioAsync(usuario.Id, ct) : null;
        var access = tokens.CriarAccessToken(usuario, clienteId, agora);

        var refresh = tokens.GerarRefreshToken(usuario.Perfil);
        var validade = fimDoTurno is { } fim && fim - agora < refresh.Validade ? fim - agora : refresh.Validade;
        var entidade = RefreshToken.Criar(usuario.Id, refresh.Hash, agora, validade);
        await refreshTokens.AdicionarAsync(entidade, ct);

        await unitOfWork.SalvarAsync(ct);
        return new SessaoIniciada(new AuthResponse(access.Valor, access.ExpiraEm, Mapear(usuario)), refresh.Valor, entidade.ExpiraEm);
    }

    private static UsuarioResponse Mapear(Usuario usuario) => new(usuario.Id, usuario.Nome, usuario.Perfil.ToString());
}
