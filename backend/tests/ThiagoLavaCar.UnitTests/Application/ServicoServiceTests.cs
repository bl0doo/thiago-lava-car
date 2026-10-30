using ThiagoLavaCar.Application.Abstractions;
using ThiagoLavaCar.Application.DTOs;
using ThiagoLavaCar.Application.Exceptions;
using ThiagoLavaCar.Application.Services;
using ThiagoLavaCar.Application.Validators;
using ThiagoLavaCar.Domain.Entities;
using ThiagoLavaCar.Domain.Enums;
using ThiagoLavaCar.Domain.Exceptions;

namespace ThiagoLavaCar.UnitTests.Application;

public class ServicoServiceTests
{
    private readonly IServicoRepository _servicos = Substitute.For<IServicoRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IUsuarioLogado _logado = Substitute.For<IUsuarioLogado>();
    private readonly ServicoService _servico;

    public ServicoServiceTests()
    {
        _servico = new ServicoService(_servicos, _unitOfWork, _logado);
    }

    private static SalvarServicoRequest Pedido(decimal carro = 80m, decimal? carroCera = 100m) => new(
        "Lavagem completa", "Lavagem", 90, 8m,
        [new PrecoServicoDto("Carro", carro, carroCera), new PrecoServicoDto("Moto", 40m, null)]);

    [Fact]
    public async Task Dono_cria_servico_com_precos_por_tipo()
    {
        _logado.Perfil.Returns(Perfil.Dono);

        var resposta = await _servico.CriarAsync(Pedido(), CancellationToken.None);

        Assert.Equal(8m, resposta.CustoInsumos);
        Assert.Equal(2, resposta.Precos.Count);
        await _unitOfWork.Received(1).SalvarAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Nome_repetido_devolve_409()
    {
        _logado.Perfil.Returns(Perfil.Dono);
        _servicos.ObterPorNomeAsync("Lavagem completa", Arg.Any<CancellationToken>()).Returns(Fabrica.Lavagem());

        await Assert.ThrowsAsync<ConflitoException>(() => _servico.CriarAsync(Pedido(), CancellationToken.None));
    }

    [Fact]
    public async Task Cliente_ve_so_ativos_e_sem_custo_de_insumos()
    {
        _logado.Perfil.Returns(Perfil.Cliente);
        _servicos.ListarAsync(true, Arg.Any<CancellationToken>()).Returns([Fabrica.Lavagem()]);

        var lista = await _servico.ListarAsync(CancellationToken.None);

        Assert.Null(Assert.Single(lista).CustoInsumos);
    }

    [Fact]
    public async Task Atualizar_substitui_precos_e_remove_tipos_que_sairam()
    {
        _logado.Perfil.Returns(Perfil.Dono);
        var existente = Fabrica.Lavagem(); // Carro e Moto
        _servicos.ObterPorIdAsync(1, Arg.Any<CancellationToken>()).Returns(existente);

        var resposta = await _servico.AtualizarAsync(1,
            new SalvarServicoRequest("Lavagem completa", "Lavagem", 60, 9m, [new PrecoServicoDto("Carro", 90m, null)]), CancellationToken.None);

        var preco = Assert.Single(resposta.Precos);
        Assert.Equal(90m, preco.Preco);
        Assert.Null(preco.PrecoComCera);
        Assert.Equal(60, resposta.TempoEstimadoMin);
    }

    [Fact]
    public async Task Preco_sem_opcao_com_cera_devolve_422()
    {
        _logado.Perfil.Returns(Perfil.Dono);
        _servicos.ObterPorIdAsync(1, Arg.Any<CancellationToken>()).Returns(Fabrica.Lavagem());

        await Assert.ThrowsAsync<RegraInvalidaException>(() => _servico.ObterPrecoAsync(1, TipoVeiculo.Moto, true, CancellationToken.None));
    }

    [Fact]
    public async Task Cliente_nao_consulta_preco_de_servico_inativo()
    {
        _logado.Perfil.Returns(Perfil.Cliente);
        var inativo = Fabrica.Lavagem();
        inativo.DefinirAtivo(false);
        _servicos.ObterPorIdAsync(1, Arg.Any<CancellationToken>()).Returns(inativo);

        await Assert.ThrowsAsync<NaoEncontradoException>(() => _servico.ObterPrecoAsync(1, TipoVeiculo.Carro, false, CancellationToken.None));
    }

    [Fact]
    public void Validador_rejeita_tipo_repetido_preco_zero_e_lista_vazia()
    {
        var v = new SalvarServicoRequestValidator();

        Assert.False(v.Validate(Pedido() with { Precos = [] }).IsValid);
        Assert.False(v.Validate(Pedido() with { Precos = [new("Carro", 50m, null), new("Carro", 60m, null)] }).IsValid);
        Assert.False(v.Validate(Pedido(carro: 0m)).IsValid);
        Assert.False(v.Validate(Pedido() with { Precos = [new("Foguete", 50m, null)] }).IsValid);
        Assert.True(v.Validate(Pedido()).IsValid);
    }
}
