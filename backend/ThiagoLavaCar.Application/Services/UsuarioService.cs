using ThiagoLavaCar.Application.Abstractions;
using ThiagoLavaCar.Application.DTOs;
using ThiagoLavaCar.Application.Exceptions;
using ThiagoLavaCar.Domain.Enums;
using ThiagoLavaCar.Domain.Exceptions;

namespace ThiagoLavaCar.Application.Services;

public interface IUsuarioService
{
    Task<IReadOnlyList<UsuarioDetalheResponse>> ListarAsync(string? perfil, CancellationToken ct);
    Task<UsuarioDetalheResponse> CriarLavadorAsync(CriarLavadorRequest request, CancellationToken ct);
    Task<UsuarioDetalheResponse> EditarAsync(int id, EditarUsuarioRequest request, CancellationToken ct);
    Task AlterarAtivoAsync(int id, bool ativo, CancellationToken ct);
    Task RedefinirPinAsync(int id, string pin, CancellationToken ct);
}

public sealed class UsuarioService(
    IUsuarioRepository usuarios,
    IRefreshTokenRepository refreshTokens,
    IUnitOfWork unitOfWork,
    ISenhaHasher hasher,
    IUsuarioLogado usuarioLogado,
    TimeProvider relogio) : IUsuarioService
{
    public async Task<IReadOnlyList<UsuarioDetalheResponse>> ListarAsync(string? perfil, CancellationToken ct)
    {
        Perfil? filtro = null;
        if (!string.IsNullOrWhiteSpace(perfil))
        {
            if (!Enum.TryParse<Perfil>(perfil, ignoreCase: true, out var valor) || int.TryParse(perfil, out _))
            {
                throw new RegraInvalidaException("Perfil inválido. Use Dono, Lavador ou Cliente.");
            }

            filtro = valor;
        }

        return (await usuarios.ListarAsync(filtro, ct)).Select(Mapear).ToList();
    }

    public async Task<UsuarioDetalheResponse> CriarLavadorAsync(CriarLavadorRequest request, CancellationToken ct)
    {
        var apelido = request.Apelido.Trim();
        if (await usuarios.ApelidoDeLavadorExisteAsync(apelido, ct))
        {
            throw new ConflitoException("Já existe um lavador com esse apelido.");
        }

        var lavador = Domain.Entities.Usuario.CriarLavador(request.Nome.Trim(), apelido, hasher.Gerar(request.Pin), relogio.GetUtcNow());
        await usuarios.AdicionarAsync(lavador, ct);
        await unitOfWork.SalvarAsync(ct);
        return Mapear(lavador);
    }

    public async Task<UsuarioDetalheResponse> EditarAsync(int id, EditarUsuarioRequest request, CancellationToken ct)
    {
        var usuario = await ObterAsync(id, ct);
        usuario.AlterarNomes(request.Nome.Trim(), request.Apelido?.Trim());
        await unitOfWork.SalvarAsync(ct);
        return Mapear(usuario);
    }

    /// <summary>Desativar derruba as sessões na hora: revoga os refresh tokens e a API confere o lavador a cada requisição.</summary>
    public async Task AlterarAtivoAsync(int id, bool ativo, CancellationToken ct)
    {
        var usuario = await ObterAsync(id, ct);
        if (!ativo && usuario.Id == usuarioLogado.Id)
        {
            throw new RegraInvalidaException("Você não pode desativar o próprio acesso.");
        }

        if (ativo)
        {
            usuario.Ativar();
        }
        else
        {
            usuario.Desativar();
            await refreshTokens.RevogarTodosDoUsuarioAsync(usuario.Id, relogio.GetUtcNow(), ct);
        }

        await unitOfWork.SalvarAsync(ct);
    }

    public async Task RedefinirPinAsync(int id, string pin, CancellationToken ct)
    {
        var usuario = await ObterAsync(id, ct);
        if (usuario.Perfil != Perfil.Lavador)
        {
            throw new RegraInvalidaException("Só o lavador tem PIN.");
        }

        usuario.RedefinirPin(hasher.Gerar(pin));
        await refreshTokens.RevogarTodosDoUsuarioAsync(usuario.Id, relogio.GetUtcNow(), ct);
        await unitOfWork.SalvarAsync(ct);
    }

    private async Task<Domain.Entities.Usuario> ObterAsync(int id, CancellationToken ct) =>
        await usuarios.ObterPorIdAsync(id, ct) ?? throw new NaoEncontradoException("Usuário não encontrado.");

    private static UsuarioDetalheResponse Mapear(Domain.Entities.Usuario u) => new(u.Id, u.Nome, u.Apelido, u.Perfil.ToString(), u.Ativo);
}
