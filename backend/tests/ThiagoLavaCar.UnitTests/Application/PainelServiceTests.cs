using Microsoft.Extensions.Time.Testing;
using ThiagoLavaCar.Application.Abstractions;
using ThiagoLavaCar.Application.Services;
using ThiagoLavaCar.Domain.Entities;
using ThiagoLavaCar.Domain.Enums;

namespace ThiagoLavaCar.UnitTests.Application;

public class PainelServiceTests
{
    private static readonly DateOnly Hoje = new(2026, 10, 20);
    private static readonly DateOnly PrimeiroDoMes = new(2026, 10, 1);

    private readonly IPagamentoRepository _pagamentos = Substitute.For<IPagamentoRepository>();
    private readonly ICaixaRepository _caixa = Substitute.For<ICaixaRepository>();
    private readonly IRelatorioRepository _relatorio = Substitute.For<IRelatorioRepository>();
    private readonly IServicoRepository _servicos = Substitute.For<IServicoRepository>();
    private readonly ITaxaRepository _taxas = Substitute.For<ITaxaRepository>();
    private readonly FakeTimeProvider _relogio = new(Fabrica.Agora);
    private readonly PainelService _painel;

    public PainelServiceTests()
    {
        _painel = new PainelService(_pagamentos, _caixa, _relatorio, new PontoEquilibrioCalculador(_relatorio, _servicos, _taxas), _relogio);
        _relatorio.ListarOrdensPagasAsync(Arg.Any<DateOnly>(), Arg.Any<DateOnly>(), Arg.Any<CancellationToken>()).Returns([]);
        _relatorio.DiasComPagamentoAsync(Arg.Any<DateOnly>(), Arg.Any<DateOnly>(), Arg.Any<CancellationToken>()).Returns([]);
        _caixa.ListarFechamentosAsync(Arg.Any<DateOnly>(), Arg.Any<DateOnly>(), Arg.Any<CancellationToken>()).Returns([]);
        _servicos.ListarAsync(true, Arg.Any<CancellationToken>()).Returns([Fabrica.Lavagem()]);
        _taxas.ListarAsync(Arg.Any<CancellationToken>()).Returns([TaxaPagamento.Criar(FormaPagamento.Pix, 0m, 0m), TaxaPagamento.Criar(FormaPagamento.Credito, 3.49m, 0m)]);
    }

    private static OrdemPaga Paga(decimal liquido) => new(Fabrica.OrdemAguardando(), Hoje, 100m, liquido); // insumos R$ 8,00 pelo item

    [Fact] // T40
    public async Task Painel_calcula_entrou_liquido_e_sobrou_de_verdade()
    {
        var pix = TaxaPagamento.Criar(FormaPagamento.Pix, 0m, 0m);
        var credito = TaxaPagamento.Criar(FormaPagamento.Credito, 3.49m, 0m);
        var estornado = Pagamento.Criar(1, 1, 500m, pix, Hoje, Fabrica.Agora);
        estornado.Estornar(Fabrica.Agora);
        _pagamentos.ListarPorDataCaixaAsync(Hoje, Arg.Any<CancellationToken>()).Returns(
            [Pagamento.Criar(1, 1, 440m, pix, Hoje, Fabrica.Agora), Pagamento.Criar(1, 2, 200m, credito, Hoje, Fabrica.Agora), estornado]);
        _caixa.SomarDespesasAsync(Hoje, Arg.Any<CancellationToken>()).Returns(45m);
        _caixa.SomarDiariasAsync(Hoje, Arg.Any<CancellationToken>()).Returns(300m);
        _relatorio.ListarOrdensPagasAsync(Hoje, Hoje, Arg.Any<CancellationToken>()).Returns([Paga(96.51m)]);

        var painel = await _painel.ObterHojeAsync(CancellationToken.None);

        Assert.Equal(640m, painel.EntrouHoje);
        Assert.Equal(633.02m, painel.LiquidoHoje);
        Assert.Equal(280.02m, painel.SobrouDeVerdade); // 633,02 - 45 - 300 - 8 de insumos
    }

    [Fact] // T42: valores exatos da Entrega 2 (líquido R$ 633,02, despesas R$ 45, diárias R$ 300 e insumos R$ 32)
    public async Task Sobrou_de_verdade_com_os_valores_do_cenario_T42_da_Entrega_2()
    {
        var pix = TaxaPagamento.Criar(FormaPagamento.Pix, 0m, 0m);
        var credito = TaxaPagamento.Criar(FormaPagamento.Credito, 3.49m, 0m);
        _pagamentos.ListarPorDataCaixaAsync(Hoje, Arg.Any<CancellationToken>()).Returns(
            [Pagamento.Criar(1, 1, 440m, pix, Hoje, Fabrica.Agora), Pagamento.Criar(1, 2, 200m, credito, Hoje, Fabrica.Agora)]);
        _caixa.SomarDespesasAsync(Hoje, Arg.Any<CancellationToken>()).Returns(45m);
        _caixa.SomarDiariasAsync(Hoje, Arg.Any<CancellationToken>()).Returns(300m);
        _relatorio.ListarOrdensPagasAsync(Hoje, Hoje, Arg.Any<CancellationToken>()).Returns([Paga(0m), Paga(0m), Paga(0m), Paga(0m)]); // 4 x R$ 8 = R$ 32 de insumos

        var painel = await _painel.ObterHojeAsync(CancellationToken.None);

        Assert.Equal(633.02m, painel.LiquidoHoje);
        Assert.Equal(256.02m, painel.SobrouDeVerdade);
    }

    [Fact] // T43: valores exatos da Entrega 2 (custos R$ 9.000, margem média R$ 75 e 95 carros no mês)
    public async Task Faltam_25_carros_com_os_valores_do_cenario_T43_da_Entrega_2()
    {
        var noMes = Enumerable.Range(0, 95).Select(_ => Paga(83m)).ToList(); // margem 83 - 8 de insumos = 75
        _relatorio.SomarDespesasAsync(PrimeiroDoMes, Hoje, Arg.Any<CancellationToken>()).Returns(4000m);
        _relatorio.SomarDiariasAsync(PrimeiroDoMes, Hoje, Arg.Any<CancellationToken>()).Returns(5000m);
        _relatorio.ListarOrdensPagasAsync(PrimeiroDoMes, Hoje, Arg.Any<CancellationToken>()).Returns(noMes);
        _relatorio.ListarOrdensPagasAsync(Hoje.AddDays(-29), Hoje, Arg.Any<CancellationToken>()).Returns(noMes);

        var painel = await _painel.ObterHojeAsync(CancellationToken.None);

        Assert.Equal(95, painel.CarrosNoMes);
        Assert.Equal(25, painel.CarrosFaltamParaPagarContas); // ceil(9000 / 75) = 120, menos 95 feitos
    }

    [Fact]
    public async Task Ponto_de_equilibrio_usa_a_margem_dos_ultimos_30_dias()
    {
        _relatorio.SomarDespesasAsync(PrimeiroDoMes, Hoje, Arg.Any<CancellationToken>()).Returns(4000m);
        _relatorio.SomarDiariasAsync(PrimeiroDoMes, Hoje, Arg.Any<CancellationToken>()).Returns(5000m);
        _relatorio.ListarOrdensPagasAsync(PrimeiroDoMes, Hoje, Arg.Any<CancellationToken>()).Returns([Paga(83m), Paga(83m)]);
        _relatorio.ListarOrdensPagasAsync(Hoje.AddDays(-29), Hoje, Arg.Any<CancellationToken>()).Returns([Paga(83m), Paga(83m)]); // margem 83 - 8 = 75

        var painel = await _painel.ObterHojeAsync(CancellationToken.None);

        Assert.Equal(2, painel.CarrosNoMes);
        Assert.Equal(118, painel.CarrosFaltamParaPagarContas); // ceil(9000 / 75) = 120, menos 2 feitos
    }

    [Fact]
    public async Task Sem_historico_a_margem_vem_do_catalogo_menos_insumos_e_taxa_media()
    {
        _relatorio.SomarDespesasAsync(PrimeiroDoMes, Hoje, Arg.Any<CancellationToken>()).Returns(1000m);

        var painel = await _painel.ObterHojeAsync(CancellationToken.None);

        // Carro R$ 80 - 1,745% de taxa média (1,40) - R$ 8 de insumos = 70,60 → ceil(1000 / 70,60) = 15
        Assert.Equal(15, painel.CarrosFaltamParaPagarContas);
    }

    [Fact]
    public async Task Sem_margem_positiva_nao_calcula_quantos_faltam()
    {
        _servicos.ListarAsync(true, Arg.Any<CancellationToken>()).Returns([]);
        _relatorio.SomarDespesasAsync(PrimeiroDoMes, Hoje, Arg.Any<CancellationToken>()).Returns(1000m);

        var painel = await _painel.ObterHojeAsync(CancellationToken.None);

        Assert.Null(painel.CarrosFaltamParaPagarContas);
    }

    [Fact]
    public async Task Dias_anteriores_com_pagamento_e_sem_fechamento_viram_pendencia()
    {
        var dia18 = new DateOnly(2026, 10, 18);
        var dia19 = new DateOnly(2026, 10, 19);
        _relatorio.DiasComPagamentoAsync(Hoje.AddDays(-30), Hoje.AddDays(-1), Arg.Any<CancellationToken>()).Returns([dia18, dia19]);
        _caixa.ListarFechamentosAsync(Hoje.AddDays(-30), Hoje.AddDays(-1), Arg.Any<CancellationToken>())
            .Returns([FechamentoCaixa.Fechar(dia19, Hoje, 1, [new FechamentoCaixa.Linha(FormaPagamento.Pix, 10m, 10m)], 10m, null, Fabrica.Agora)]);

        var painel = await _painel.ObterHojeAsync(CancellationToken.None);

        Assert.Equal(["Caixa de 18/10 não fechado"], painel.Pendencias);
    }
}
