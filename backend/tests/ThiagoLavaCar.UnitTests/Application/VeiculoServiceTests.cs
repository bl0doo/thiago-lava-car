using ThiagoLavaCar.Application.Abstractions;
using ThiagoLavaCar.Application.DTOs;
using ThiagoLavaCar.Application.Exceptions;
using ThiagoLavaCar.Application.Services;
using ThiagoLavaCar.Application.Validators;
using ThiagoLavaCar.Domain.Entities;
using ThiagoLavaCar.Domain.Enums;
using ThiagoLavaCar.Domain.Exceptions;

namespace ThiagoLavaCar.UnitTests.Application;

public class VeiculoServiceTests
{
    private readonly IVeiculoRepository _veiculos = Substitute.For<IVeiculoRepository>();
    private readonly IClienteRepository _clientes = Substitute.For<IClienteRepository>();
    private readonly IModeloVeiculoRepository _modelos = Substitute.For<IModeloVeiculoRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IUsuarioLogado _logado = Substitute.For<IUsuarioLogado>();
    private readonly VeiculoService _servico;

    public VeiculoServiceTests()
    {
        _servico = new VeiculoService(_veiculos, _clientes, _modelos, _unitOfWork, _logado);
    }

    private void Logar(Perfil perfil, int usuarioId = 1)
    {
        _logado.Perfil.Returns(perfil);
        _logado.Id.Returns(usuarioId);
    }

    private static CriarVeiculoRequest Novo(int? clienteId = 10, string placa = "abc-1d23") =>
        new(clienteId, placa, "Chevrolet Onix", "Carro", " Prata ");

    [Fact]
    public async Task Dono_cadastra_com_placa_normalizada_e_cor_aparada()
    {
        Logar(Perfil.Dono);
        _clientes.ObterPorIdAsync(10, Arg.Any<CancellationToken>())
            .Returns(Cliente.Criar("Renata", "44999998888", null, CanalContato.WhatsApp, true, Fabrica.Agora).ComId(10));

        var resposta = await _servico.CriarAsync(Novo(), CancellationToken.None);

        Assert.Equal("ABC1D23", resposta.Placa);
        Assert.Equal("Prata", resposta.Cor);
        Assert.Equal(10, resposta.ClienteId);
    }

    [Fact]
    public async Task Cliente_cadastra_para_si_e_o_clienteId_do_corpo_e_ignorado()
    {
        Logar(Perfil.Cliente, usuarioId: 5);
        _clientes.ObterIdPorUsuarioAsync(5, Arg.Any<CancellationToken>()).Returns(42);

        var resposta = await _servico.CriarAsync(Novo(clienteId: 999), CancellationToken.None);

        Assert.Equal(42, resposta.ClienteId);
    }

    [Fact]
    public async Task Dono_sem_clienteId_recebe_422_e_cliente_inexistente_404()
    {
        Logar(Perfil.Dono);

        await Assert.ThrowsAsync<RegraInvalidaException>(() => _servico.CriarAsync(Novo(clienteId: null), CancellationToken.None));
        await Assert.ThrowsAsync<NaoEncontradoException>(() => _servico.CriarAsync(Novo(clienteId: 77), CancellationToken.None));
    }

    [Fact]
    public async Task Placa_repetida_devolve_409()
    {
        Logar(Perfil.Dono);
        _clientes.ObterPorIdAsync(10, Arg.Any<CancellationToken>())
            .Returns(Cliente.Criar("Renata", "44999998888", null, CanalContato.WhatsApp, true, Fabrica.Agora).ComId(10));
        _veiculos.ObterPorPlacaAsync("ABC1D23", Arg.Any<CancellationToken>()).Returns(Fabrica.Onix());

        await Assert.ThrowsAsync<ConflitoException>(() => _servico.CriarAsync(Novo(), CancellationToken.None));
    }

    [Fact]
    public async Task Cliente_nao_acessa_ficha_de_veiculo_de_outro_cliente()
    {
        Logar(Perfil.Cliente, usuarioId: 5);
        _clientes.ObterIdPorUsuarioAsync(5, Arg.Any<CancellationToken>()).Returns(99);
        _veiculos.ObterPorIdAsync(30, Arg.Any<CancellationToken>()).Returns(Fabrica.Onix()); // dono atual: cliente 10

        await Assert.ThrowsAsync<NaoEncontradoException>(() => _servico.ObterFichaAsync(30, CancellationToken.None));
    }

    [Fact]
    public async Task Atualizar_mantendo_a_propria_placa_nao_conflita()
    {
        Logar(Perfil.Dono);
        var veiculo = Fabrica.Onix();
        _veiculos.ObterPorIdAsync(30, Arg.Any<CancellationToken>()).Returns(veiculo);

        var resposta = await _servico.AtualizarAsync(30, new AtualizarVeiculoRequest("ABC1D23", "Onix LT", "Carro", null), CancellationToken.None);

        Assert.Equal("Onix LT", resposta.Modelo);
        await _veiculos.DidNotReceive().ObterPorPlacaAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData("AB1234", "Carro", "placa")]
    [InlineData("ABC1D23", "Foguete", "tipoVeiculo")]
    [InlineData("ABC1D23", "0", "tipoVeiculo")]
    public void Validador_rejeita_dados_invalidos(string placa, string tipo, string campo)
    {
        var resultado = new CriarVeiculoRequestValidator().Validate(new CriarVeiculoRequest(1, placa, "Onix", tipo, null));

        Assert.Contains(resultado.Errors, e => string.Equals(e.PropertyName, campo, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Modelo_informado_preenche_descricao_e_tipo_quando_omitidos()
    {
        Logar(Perfil.Cliente, usuarioId: 5);
        _clientes.ObterIdPorUsuarioAsync(5, Arg.Any<CancellationToken>()).Returns(42);
        _modelos.ObterPorIdAsync(7, Arg.Any<CancellationToken>())
            .Returns(ModeloVeiculo.Criar("Honda", "CG 160", TipoVeiculo.Moto));

        var resposta = await _servico.CriarAsync(new CriarVeiculoRequest(null, "abc-1d23", null, null, null, 7), CancellationToken.None);

        Assert.Equal("Honda CG 160", resposta.Modelo);
        Assert.Equal("Moto", resposta.Tipo);
    }

    [Fact]
    public async Task Modelo_inexistente_devolve_404()
    {
        Logar(Perfil.Cliente, usuarioId: 5);
        _clientes.ObterIdPorUsuarioAsync(5, Arg.Any<CancellationToken>()).Returns(42);

        await Assert.ThrowsAsync<NaoEncontradoException>(() =>
            _servico.CriarAsync(new CriarVeiculoRequest(null, "abc-1d23", null, null, null, 99), CancellationToken.None));
    }

    [Fact]
    public async Task Busca_de_modelos_mapeia_o_resultado()
    {
        _modelos.BuscarAsync("onix", Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns([ModeloVeiculo.Criar("Chevrolet", "Onix", TipoVeiculo.Carro)]);

        var lista = await _servico.BuscarModelosAsync("onix", CancellationToken.None);

        var item = Assert.Single(lista);
        Assert.Equal("Chevrolet Onix", item.Descricao);
        Assert.Equal("Carro", item.TipoVeiculo);
    }

    [Fact]
    public void Sem_modelo_descricao_e_tipo_sao_obrigatorios_e_com_modelo_nao()
    {
        var validador = new CriarVeiculoRequestValidator();

        Assert.False(validador.Validate(new CriarVeiculoRequest(1, "ABC1D23", null, null, null)).IsValid);
        Assert.True(validador.Validate(new CriarVeiculoRequest(1, "ABC1D23", null, null, null, 3)).IsValid);
    }
}
