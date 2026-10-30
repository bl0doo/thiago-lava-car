using Microsoft.Extensions.Time.Testing;
using ThiagoLavaCar.Application.Abstractions;
using ThiagoLavaCar.Application.DTOs;
using ThiagoLavaCar.Application.Exceptions;
using ThiagoLavaCar.Application.Services;
using ThiagoLavaCar.Domain.Entities;
using ThiagoLavaCar.Domain.Enums;
using ThiagoLavaCar.Domain.Exceptions;

namespace ThiagoLavaCar.UnitTests.Application;

public class AuthServiceTests
{
    private readonly IUsuarioRepository _usuarios = Substitute.For<IUsuarioRepository>();
    private readonly IClienteRepository _clientes = Substitute.For<IClienteRepository>();
    private readonly IRefreshTokenRepository _refreshTokens = Substitute.For<IRefreshTokenRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ISenhaHasher _hasher = Substitute.For<ISenhaHasher>();
    private readonly ITokenService _tokens = Substitute.For<ITokenService>();
    private readonly IUsuarioLogado _usuarioLogado = Substitute.For<IUsuarioLogado>();
    private readonly FakeTimeProvider _relogio = new(Fabrica.Agora);
    private readonly AuthService _servico;

    public AuthServiceTests()
    {
        _servico = new AuthService(_usuarios, _clientes, _refreshTokens, _unitOfWork, _hasher, _tokens, _usuarioLogado, _relogio);
        _tokens.CriarAccessToken(Arg.Any<Usuario>(), Arg.Any<int?>(), Arg.Any<DateTimeOffset>())
            .Returns(ci => new AccessToken("jwt", ci.Arg<DateTimeOffset>().AddMinutes(30)));
        _tokens.GerarRefreshToken(Arg.Any<Perfil>()).Returns(ci => new RefreshTokenGerado(
            "refresh-bruto", "refresh-hash", ci.Arg<Perfil>() == Perfil.Lavador ? TimeSpan.FromHours(12) : TimeSpan.FromDays(7)));
        _tokens.CalcularHash(Arg.Any<string>()).Returns(ci => "hash:" + ci.Arg<string>());
    }

    private Usuario DonoCadastrado()
    {
        var dono = Fabrica.Dono();
        _usuarios.ObterPorEmailAsync("thiago@lavacar.com", Arg.Any<CancellationToken>()).Returns(dono);
        _hasher.Verificar("hash-senha", "certa").Returns(true);
        return dono;
    }

    [Fact] // T01
    public async Task Login_do_dono_com_credenciais_corretas_devolve_token_e_refresh()
    {
        DonoCadastrado();

        var sessao = await _servico.LoginAsync(new LoginRequest("Thiago@LavaCar.com", "certa"), default);

        Assert.Equal("jwt", sessao.Resposta.AccessToken);
        Assert.Equal("Dono", sessao.Resposta.Usuario.Perfil);
        Assert.Equal("refresh-bruto", sessao.RefreshToken);
        await _refreshTokens.Received(1).AdicionarAsync(Arg.Is<RefreshToken>(t => t.TokenHash == "refresh-hash"), Arg.Any<CancellationToken>());
        await _unitOfWork.Received().SalvarAsync(Arg.Any<CancellationToken>());
    }

    [Fact] // T02
    public async Task Senha_errada_devolve_401_e_conta_uma_falha()
    {
        var dono = DonoCadastrado();

        var erro = await Assert.ThrowsAsync<NaoAutenticadoException>(
            () => _servico.LoginAsync(new LoginRequest("thiago@lavacar.com", "errada"), default));

        Assert.Equal("E-mail ou senha incorretos.", erro.Message);
        Assert.Equal(1, dono.FalhasLogin);
        await _unitOfWork.Received(1).SalvarAsync(Arg.Any<CancellationToken>());
    }

    [Fact] // T03
    public async Task Quinta_senha_errada_bloqueia_e_a_senha_certa_tambem_e_recusada_por_15_minutos()
    {
        var dono = DonoCadastrado();

        for (var i = 0; i < 4; i++)
        {
            await Assert.ThrowsAsync<NaoAutenticadoException>(() => _servico.LoginAsync(new LoginRequest("thiago@lavacar.com", "errada"), default));
        }

        await Assert.ThrowsAsync<UsuarioBloqueadoException>(() => _servico.LoginAsync(new LoginRequest("thiago@lavacar.com", "errada"), default));
        await Assert.ThrowsAsync<UsuarioBloqueadoException>(() => _servico.LoginAsync(new LoginRequest("thiago@lavacar.com", "certa"), default));

        _relogio.Advance(TimeSpan.FromMinutes(15));
        var sessao = await _servico.LoginAsync(new LoginRequest("thiago@lavacar.com", "certa"), default);
        Assert.NotNull(sessao.RefreshToken);
        Assert.Equal(0, dono.FalhasLogin);
    }

    [Fact] // T04
    public async Task Usuario_desativado_com_senha_certa_recebe_401()
    {
        var dono = DonoCadastrado();
        dono.Desativar();

        await Assert.ThrowsAsync<NaoAutenticadoException>(() => _servico.LoginAsync(new LoginRequest("thiago@lavacar.com", "certa"), default));
    }

    [Fact]
    public async Task Email_inexistente_recebe_a_mesma_mensagem_e_gasta_o_mesmo_tempo()
    {
        var erro = await Assert.ThrowsAsync<NaoAutenticadoException>(
            () => _servico.LoginAsync(new LoginRequest("ninguem@lavacar.com", "qualquer"), default));

        Assert.Equal("E-mail ou senha incorretos.", erro.Message);
        _hasher.Received(1).SimularVerificacao("qualquer");
    }

    [Fact]
    public async Task Lavador_nao_entra_pelo_login_de_senha()
    {
        var lavador = Fabrica.Lavador();
        _usuarios.ObterPorEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(lavador);

        await Assert.ThrowsAsync<NaoAutenticadoException>(() => _servico.LoginAsync(new LoginRequest("x@y.com", "certa"), default));
    }

    [Fact] // T05
    public async Task Login_por_PIN_correto_abre_sessao_que_dura_o_turno()
    {
        _usuarios.ObterPorIdAsync(2, Arg.Any<CancellationToken>()).Returns(Fabrica.Lavador());
        _hasher.Verificar("hash-pin", "4821").Returns(true);

        var sessao = await _servico.LoginPinAsync(new LoginPinRequest(2, "4821"), default);

        Assert.Equal("Lavador", sessao.Resposta.Usuario.Perfil);
        Assert.Equal(Fabrica.Agora.AddHours(12), sessao.RefreshExpiraEm);
    }

    [Fact]
    public async Task Renovar_a_sessao_do_lavador_nao_estende_o_fim_do_turno()
    {
        var fimDoTurno = Fabrica.Agora.AddHours(4);
        var atual = RefreshToken.Criar(2, "hash:cookie", fimDoTurno.AddHours(-12), TimeSpan.FromHours(12));
        _refreshTokens.ObterPorHashAsync("hash:cookie", Arg.Any<CancellationToken>()).Returns(atual);
        _usuarios.ObterPorIdAsync(2, Arg.Any<CancellationToken>()).Returns(Fabrica.Lavador());

        var sessao = await _servico.RenovarAsync("cookie", default);

        Assert.Equal(fimDoTurno, sessao.RefreshExpiraEm);
    }

    [Fact]
    public async Task Renovar_a_sessao_do_dono_estende_por_mais_7_dias()
    {
        var atual = RefreshToken.Criar(1, "hash:cookie", Fabrica.Agora.AddDays(-6), TimeSpan.FromDays(7));
        _refreshTokens.ObterPorHashAsync("hash:cookie", Arg.Any<CancellationToken>()).Returns(atual);
        _usuarios.ObterPorIdAsync(1, Arg.Any<CancellationToken>()).Returns(Fabrica.Dono());

        var sessao = await _servico.RenovarAsync("cookie", default);

        Assert.Equal(Fabrica.Agora.AddDays(7), sessao.RefreshExpiraEm);
    }

    [Fact]
    public async Task Login_por_PIN_de_um_dono_e_recusado()
    {
        _usuarios.ObterPorIdAsync(1, Arg.Any<CancellationToken>()).Returns(Fabrica.Dono());

        var erro = await Assert.ThrowsAsync<NaoAutenticadoException>(() => _servico.LoginPinAsync(new LoginPinRequest(1, "4821"), default));
        Assert.Equal("PIN incorreto.", erro.Message);
    }

    [Fact] // T07
    public async Task Refresh_valido_revoga_o_atual_e_devolve_um_novo_par()
    {
        var dono = Fabrica.Dono();
        var atual = RefreshToken.Criar(1, "hash:cookie", Fabrica.Agora.AddMinutes(-40), TimeSpan.FromDays(7));
        _refreshTokens.ObterPorHashAsync("hash:cookie", Arg.Any<CancellationToken>()).Returns(atual);
        _usuarios.ObterPorIdAsync(1, Arg.Any<CancellationToken>()).Returns(dono);

        var sessao = await _servico.RenovarAsync("cookie", default);

        Assert.True(atual.FoiRevogado);
        Assert.Equal("refresh-bruto", sessao.RefreshToken);
    }

    [Fact] // T08
    public async Task Refresh_ja_usado_revoga_todos_os_tokens_do_usuario()
    {
        var usado = RefreshToken.Criar(1, "hash:cookie", Fabrica.Agora.AddMinutes(-40), TimeSpan.FromDays(7));
        usado.Revogar(Fabrica.Agora.AddMinutes(-10));
        _refreshTokens.ObterPorHashAsync("hash:cookie", Arg.Any<CancellationToken>()).Returns(usado);

        await Assert.ThrowsAsync<NaoAutenticadoException>(() => _servico.RenovarAsync("cookie", default));

        await _refreshTokens.Received(1).RevogarTodosDoUsuarioAsync(1, Fabrica.Agora, Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SalvarAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Refresh_expirado_ou_ausente_e_recusado()
    {
        var expirado = RefreshToken.Criar(1, "hash:cookie", Fabrica.Agora.AddDays(-8), TimeSpan.FromDays(7));
        _refreshTokens.ObterPorHashAsync("hash:cookie", Arg.Any<CancellationToken>()).Returns(expirado);
        _usuarios.ObterPorIdAsync(1, Arg.Any<CancellationToken>()).Returns(Fabrica.Dono());

        await Assert.ThrowsAsync<NaoAutenticadoException>(() => _servico.RenovarAsync("cookie", default));
        await Assert.ThrowsAsync<NaoAutenticadoException>(() => _servico.RenovarAsync(null, default));
    }

    [Fact]
    public async Task Refresh_de_usuario_desativado_e_recusado()
    {
        var dono = Fabrica.Dono();
        dono.Desativar();
        _refreshTokens.ObterPorHashAsync("hash:cookie", Arg.Any<CancellationToken>())
            .Returns(RefreshToken.Criar(1, "hash:cookie", Fabrica.Agora, TimeSpan.FromDays(7)));
        _usuarios.ObterPorIdAsync(1, Arg.Any<CancellationToken>()).Returns(dono);

        await Assert.ThrowsAsync<NaoAutenticadoException>(() => _servico.RenovarAsync("cookie", default));
    }

    [Fact]
    public async Task Lista_de_lavadores_usa_o_apelido()
    {
        _usuarios.ListarLavadoresAtivosAsync(Arg.Any<CancellationToken>()).Returns([Fabrica.Lavador(2, "Diego"), Fabrica.Lavador(3, "Marcos")]);

        var lista = await _servico.ListarLavadoresAsync(default);

        Assert.Equal(["Diego", "Marcos"], lista.Select(l => l.Apelido));
    }

    private static CadastroClienteRequest NovoCadastro(string email = "Renata@Email.com", string telefone = "(44) 99999-8888") =>
        new("  Renata Souza ", telefone, email, "Senha123", "WhatsApp", true);

    [Fact]
    public async Task Cadastro_de_cliente_cria_usuario_Cliente_e_ficha_com_telefone_so_de_digitos()
    {
        _hasher.Gerar("Senha123").Returns("hash-senha");

        var sessao = await _servico.CadastrarClienteAsync(NovoCadastro(), default);

        Assert.Equal("Cliente", sessao.Resposta.Usuario.Perfil);
        await _usuarios.Received(1).AdicionarAsync(
            Arg.Is<Usuario>(u => u.Email == "renata@email.com" && u.Perfil == Perfil.Cliente && u.SenhaHash == "hash-senha" && u.Nome == "Renata Souza"),
            Arg.Any<CancellationToken>());
        await _clientes.Received(1).AdicionarAsync(
            Arg.Is<Cliente>(c => c.Telefone == "44999998888" && c.ConsentimentoLgpd), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Cadastro_com_email_ja_usado_devolve_conflito_e_nao_grava()
    {
        _usuarios.ObterPorEmailAsync("renata@email.com", Arg.Any<CancellationToken>()).Returns(Fabrica.Dono());

        await Assert.ThrowsAsync<ConflitoException>(() => _servico.CadastrarClienteAsync(NovoCadastro(), default));

        await _usuarios.DidNotReceive().AdicionarAsync(Arg.Any<Usuario>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Cadastro_com_telefone_ja_usado_devolve_conflito()
    {
        _clientes.ObterPorTelefoneAsync("44999998888", Arg.Any<CancellationToken>()).Returns(Cliente.Criar("Outra", "44999998888", null, CanalContato.WhatsApp, true, Fabrica.Agora));

        await Assert.ThrowsAsync<ConflitoException>(() => _servico.CadastrarClienteAsync(NovoCadastro(), default));

        await _clientes.DidNotReceive().AdicionarAsync(Arg.Any<Cliente>(), Arg.Any<CancellationToken>());
    }
}
