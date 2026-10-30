using Microsoft.Extensions.Time.Testing;
using ThiagoLavaCar.Application.Abstractions;
using ThiagoLavaCar.Application.DTOs;
using ThiagoLavaCar.Application.Exceptions;
using ThiagoLavaCar.Application.Services;
using ThiagoLavaCar.Application.Validators;
using ThiagoLavaCar.Domain.Entities;
using ThiagoLavaCar.Domain.Enums;

namespace ThiagoLavaCar.UnitTests.Application;

public class ClienteServiceTests
{
    private readonly IClienteRepository _clientes = Substitute.For<IClienteRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IUsuarioLogado _logado = Substitute.For<IUsuarioLogado>();
    private readonly ClienteService _servico;

    public ClienteServiceTests()
    {
        _servico = new ClienteService(_clientes, _unitOfWork, _logado, new FakeTimeProvider(Fabrica.Agora));
    }

    private static CriarClienteRequest Novo(string telefone = "44999998888") =>
        new("Renata Souza", telefone, " Renata@Email.com ", "WhatsApp", true);

    [Fact]
    public async Task Criar_grava_cliente_com_email_normalizado_e_consentimento_na_hora_do_servidor()
    {
        var resposta = await _servico.CriarAsync(Novo(), CancellationToken.None);

        Assert.Equal("renata@email.com", resposta.Email);
        Assert.Equal("WhatsApp", resposta.CanalPreferido);
        await _clientes.Received(1).AdicionarAsync(Arg.Is<Cliente>(c => c.ConsentimentoEm == Fabrica.Agora), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SalvarAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Criar_com_telefone_existente_devolve_o_id_do_cadastro_encontrado()
    {
        _clientes.ObterPorTelefoneAsync("44999998888", Arg.Any<CancellationToken>())
            .Returns(Cliente.Criar("Outra", "44999998888", null, CanalContato.Telefone, true, Fabrica.Agora).ComId(7));

        var erro = await Assert.ThrowsAsync<ClienteDuplicadoException>(() => _servico.CriarAsync(Novo(), CancellationToken.None));

        Assert.Equal(7, erro.ClienteId);
        await _unitOfWork.DidNotReceive().SalvarAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Atualizar_para_telefone_de_outro_cliente_falha_com_409()
    {
        var atual = Cliente.Criar("Renata", "44999998888", null, CanalContato.WhatsApp, true, Fabrica.Agora).ComId(1);
        _clientes.ObterPorIdAsync(1, Arg.Any<CancellationToken>()).Returns(atual);
        _clientes.ObterPorTelefoneAsync("44911112222", Arg.Any<CancellationToken>())
            .Returns(Cliente.Criar("Outro", "44911112222", null, CanalContato.WhatsApp, true, Fabrica.Agora).ComId(2));

        var erro = await Assert.ThrowsAsync<ClienteDuplicadoException>(() =>
            _servico.AtualizarAsync(1, new AtualizarClienteRequest("Renata", "44911112222", null, "WhatsApp"), CancellationToken.None));

        Assert.Equal(2, erro.ClienteId);
    }

    [Fact]
    public async Task Atualizar_mantendo_o_proprio_telefone_nao_consulta_duplicidade()
    {
        var atual = Cliente.Criar("Renata", "44999998888", null, CanalContato.WhatsApp, true, Fabrica.Agora).ComId(1);
        _clientes.ObterPorIdAsync(1, Arg.Any<CancellationToken>()).Returns(atual);

        var resposta = await _servico.AtualizarAsync(1, new AtualizarClienteRequest("Renata S.", "44999998888", null, "Email"), CancellationToken.None);

        Assert.Equal("Renata S.", resposta.Nome);
        Assert.Equal("Email", resposta.CanalPreferido);
        await _clientes.DidNotReceive().ObterPorTelefoneAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Cliente_sem_cadastro_vinculado_ao_login_devolve_404()
    {
        _logado.Id.Returns(99);

        await Assert.ThrowsAsync<NaoEncontradoException>(() => _servico.ObterMeuAsync(CancellationToken.None));
    }

    [Theory]
    [InlineData(1, 20, 1, 20)]
    [InlineData(0, 0, 1, 20)]
    [InlineData(3, 500, 3, 100)]
    public void Paginacao_segue_a_convencao(int pagina, int tamanho, int paginaEsperada, int tamanhoEsperado) =>
        Assert.Equal((paginaEsperada, tamanhoEsperado), Paginacao.Normalizar(pagina, tamanho));

    [Theory]
    [InlineData("A", "44999998888", "WhatsApp", true, "nome")]
    [InlineData("Renata", "4499999", "WhatsApp", true, "telefone")]
    [InlineData("Renata", "(44) 99999-8888", "WhatsApp", true, "telefone")]
    [InlineData("Renata", "44999998888", "Pombo", true, "canalPreferido")]
    [InlineData("Renata", "44999998888", "1", true, "canalPreferido")]
    [InlineData("Renata", "44999998888", "WhatsApp", false, "consentimentoLgpd")]
    public void Validador_rejeita_dados_invalidos(string nome, string telefone, string canal, bool lgpd, string campo)
    {
        var resultado = new CriarClienteRequestValidator().Validate(new CriarClienteRequest(nome, telefone, null, canal, lgpd));

        Assert.Contains(resultado.Errors, e => string.Equals(e.PropertyName, campo, StringComparison.OrdinalIgnoreCase));
    }

    [Theory]
    [InlineData("nao-e-email")]
    public void Validador_rejeita_email_invalido_mas_aceita_vazio(string email)
    {
        var v = new AtualizarClienteRequestValidator();
        Assert.False(v.Validate(new AtualizarClienteRequest("Renata", "44999998888", email, "Email")).IsValid);
        Assert.True(v.Validate(new AtualizarClienteRequest("Renata", "44999998888", null, "Email")).IsValid);
    }
}
