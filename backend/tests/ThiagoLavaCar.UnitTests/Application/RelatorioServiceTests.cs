using Microsoft.Extensions.Time.Testing;
using ThiagoLavaCar.Application.Abstractions;
using ThiagoLavaCar.Application.Services;
using ThiagoLavaCar.Domain.Entities;
using ThiagoLavaCar.Domain.Enums;
using ThiagoLavaCar.Domain.Exceptions;

namespace ThiagoLavaCar.UnitTests.Application;

public class RelatorioServiceTests
{
    private static readonly DateOnly Hoje = new(2026, 10, 20);

    private static OrdemPaga OsComDoisItens(decimal liquido)
    {
        var lavagem = Fabrica.Lavagem(); // 90 min, insumos R$ 8, carro com cera R$ 100
        var motor = Servico.Criar("Motor", "Extras", 30, 2m).ComId(2);
        motor.DefinirPreco(TipoVeiculo.Carro, 40m, null);
        var os = OrdemServico.Registrar(
            Fabrica.Onix(), 1,
            [ItemOrdemServico.Criar(lavagem, true, 100m, false), ItemOrdemServico.Criar(motor, false, 40m, false)],
            true, [], true, null, null, Fabrica.Agora).ComId(7);
        return new OrdemPaga(os, Hoje, 140m, liquido);
    }

    [Fact] // T41
    public void Lucro_de_uma_OS_com_dois_itens_divide_receita_pelo_preco_e_mao_de_obra_pelo_tempo()
    {
        // 120 min estimados; diárias de R$ 240 = R$ 2 por minuto; receita líquida R$ 140
        var linhas = LucroPorServicoCalculo.Calcular([OsComDoisItens(140m)], 240m);

        var lavagem = linhas.Single(l => l.Servico == "Lavagem completa com cera");
        var motor = linhas.Single(l => l.Servico == "Motor");
        Assert.Equal((1, 100m, 8m, 180m, -88m, 90), (lavagem.Quantidade, lavagem.ReceitaLiquida, lavagem.Insumos, lavagem.MaoDeObra, lavagem.Lucro, lavagem.TempoMedioMin));
        Assert.Equal((1, 40m, 2m, 60m, -22m, 30), (motor.Quantidade, motor.ReceitaLiquida, motor.Insumos, motor.MaoDeObra, motor.Lucro, motor.TempoMedioMin));
        Assert.Equal("Motor", linhas[0].Servico); // ordenado por lucro, o menos negativo primeiro
    }

    [Fact]
    public void Ranking_agrupa_por_servico_e_cera_e_calcula_lucro_medio()
    {
        var a = Fabrica.OrdemAguardando(1);
        var b = Fabrica.OrdemAguardando(2);
        var ordens = new[] { new OrdemPaga(a, Hoje, 100m, 96.51m), new OrdemPaga(b, Hoje, 100m, 96.51m) };

        var linhas = LucroPorServicoCalculo.Calcular(ordens, 180m); // 180 min lavados: R$ 1 por minuto, R$ 90 por OS

        var linha = Assert.Single(linhas);
        Assert.Equal((2, 193.02m, 16m, 180m, -2.98m, -1.49m), (linha.Quantidade, linha.ReceitaLiquida, linha.Insumos, linha.MaoDeObra, linha.Lucro, linha.LucroMedio));
    }

    [Fact]
    public void Sem_ordens_o_ranking_e_vazio()
    {
        Assert.Empty(LucroPorServicoCalculo.Calcular([], 500m));
    }

    private static RelatorioService CriarServico(IRelatorioRepository relatorio)
    {
        var relogio = new FakeTimeProvider(Fabrica.Agora);
        var calculador = new PontoEquilibrioCalculador(relatorio, Substitute.For<IServicoRepository>(), Substitute.For<ITaxaRepository>());
        return new RelatorioService(relatorio, calculador, relogio);
    }

    [Fact]
    public async Task Periodo_invertido_ou_maior_que_366_dias_e_recusado()
    {
        var servico = CriarServico(Substitute.For<IRelatorioRepository>());

        await Assert.ThrowsAsync<RegraInvalidaException>(() => servico.LucroPorServicoAsync(Hoje, Hoje.AddDays(-1), CancellationToken.None));
        await Assert.ThrowsAsync<RegraInvalidaException>(() => servico.LucroPorServicoAsync(Hoje.AddDays(-366), Hoje, CancellationToken.None));
        await servico.LucroPorServicoAsync(Hoje.AddDays(-365), Hoje, CancellationToken.None); // 366 dias exatos
    }

    [Fact]
    public async Task Sem_datas_o_periodo_e_o_mes_atual_ate_hoje()
    {
        var relatorio = Substitute.For<IRelatorioRepository>();

        await CriarServico(relatorio).LucroPorServicoAsync(null, null, CancellationToken.None);

        await relatorio.Received(1).ListarOrdensPagasAsync(new DateOnly(2026, 10, 1), Hoje, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Ponto_de_equilibrio_de_mes_passado_vai_ate_o_ultimo_dia_e_recusa_mes_futuro_ou_invalido()
    {
        var relatorio = Substitute.For<IRelatorioRepository>();
        relatorio.SomarDespesasAsync(new DateOnly(2026, 9, 1), new DateOnly(2026, 9, 30), Arg.Any<CancellationToken>()).Returns(1000m);
        relatorio.SomarDiariasAsync(new DateOnly(2026, 9, 1), new DateOnly(2026, 9, 30), Arg.Any<CancellationToken>()).Returns(500m);
        var servico = CriarServico(relatorio);

        var setembro = await servico.PontoEquilibrioAsync("2026-09", CancellationToken.None);

        Assert.Equal(("2026-09", 1000m, 500m, 1500m), (setembro.Mes, setembro.Despesas, setembro.Diarias, setembro.Contas));
        await Assert.ThrowsAsync<RegraInvalidaException>(() => servico.PontoEquilibrioAsync("2026-11", CancellationToken.None));
        await Assert.ThrowsAsync<RegraInvalidaException>(() => servico.PontoEquilibrioAsync("10/2026", CancellationToken.None));
    }

    private static OrdemServico Cronometrada(int id, int minutos, string apelido)
    {
        var os = OsComDoisItens(140m).Ordem;
        typeof(OrdemServico).GetProperty(nameof(OrdemServico.DuracaoRealMin))!.SetValue(os, minutos);
        typeof(OrdemServico).GetProperty(nameof(OrdemServico.Lavador))!.SetValue(os, Fabrica.Lavador(2, apelido));
        return os.ComId(id);
    }

    [Fact]
    public void Produtividade_divide_o_tempo_real_pelo_tempo_estimado_de_cada_item_e_agrupa_por_lavador()
    {
        // Duas OS de 120 e 60 minutos reais, ambas com Lavagem (90 estimados) + Motor (30 estimados)
        var resposta = ProdutividadeCalculo.Calcular([Cronometrada(1, 120, "Diego"), Cronometrada(2, 60, "Marcos")]);

        var lavagem = resposta.PorServico.Single(s => s.Servico == "Lavagem completa");
        var motor = resposta.PorServico.Single(s => s.Servico == "Motor");
        Assert.Equal((2, 68, 90), (lavagem.Quantidade, lavagem.TempoMedioMin, lavagem.EstimadoMedioMin)); // (90 + 45) / 2 = 67,5
        Assert.Equal((2, 23, 30), (motor.Quantidade, motor.TempoMedioMin, motor.EstimadoMedioMin)); // (30 + 15) / 2 = 22,5
        Assert.Equal([("Diego", 1, 120), ("Marcos", 1, 60)], resposta.PorLavador.Select(l => (l.Lavador, l.Quantidade, l.TempoMedioMin)));
    }

    [Fact]
    public void Produtividade_sem_ordens_e_vazia()
    {
        var resposta = ProdutividadeCalculo.Calcular([]);

        Assert.Empty(resposta.PorServico);
        Assert.Empty(resposta.PorLavador);
    }

    [Fact]
    public async Task Faturamento_soma_por_dia_e_calcula_ticket_medio_das_OS_pagas()
    {
        var relatorio = Substitute.For<IRelatorioRepository>();
        var pix = TaxaPagamento.Criar(FormaPagamento.Pix, 0m, 0m);
        var credito = TaxaPagamento.Criar(FormaPagamento.Credito, 3.49m, 0m);
        var ontem = Hoje.AddDays(-1);
        relatorio.ListarPagamentosAsync(Arg.Any<DateOnly>(), Arg.Any<DateOnly>(), Arg.Any<CancellationToken>()).Returns(
        [
            Pagamento.Criar(1, 1, 100m, credito, ontem, Fabrica.Agora),
            Pagamento.Criar(2, 1, 60m, pix, Hoje, Fabrica.Agora),
            Pagamento.Criar(2, 1, 40m, pix, Hoje, Fabrica.Agora),
        ]);
        relatorio.ListarOrdensPagasAsync(Arg.Any<DateOnly>(), Arg.Any<DateOnly>(), Arg.Any<CancellationToken>()).Returns(
        [
            new OrdemPaga(Fabrica.OrdemAguardando(1), ontem, 100m, 96.51m),
            new OrdemPaga(Fabrica.OrdemAguardando(2), Hoje, 100m, 100m),
            new OrdemPaga(Fabrica.OrdemAguardando(3), Hoje, 140m, 140m),
        ]);

        var resposta = await CriarServico(relatorio).FaturamentoAsync(ontem, Hoje, CancellationToken.None);

        Assert.Equal([ontem, Hoje], resposta.Dias.Select(d => d.Data));
        Assert.Equal((100m, 96.51m, 1, 100m), (resposta.Dias[0].Bruto, resposta.Dias[0].Liquido, resposta.Dias[0].OsPagas, resposta.Dias[0].TicketMedio));
        Assert.Equal((100m, 100m, 2, 120m), (resposta.Dias[1].Bruto, resposta.Dias[1].Liquido, resposta.Dias[1].OsPagas, resposta.Dias[1].TicketMedio));
        Assert.Equal((200m, 196.51m, 3, 113.33m), (resposta.TotalBruto, resposta.TotalLiquido, resposta.OsPagas, resposta.TicketMedio));
    }

    [Fact]
    public async Task Faturamento_sem_movimento_devolve_zeros()
    {
        var resposta = await CriarServico(Substitute.For<IRelatorioRepository>()).FaturamentoAsync(null, null, CancellationToken.None);

        Assert.Empty(resposta.Dias);
        Assert.Equal((0m, 0m, 0, 0m), (resposta.TotalBruto, resposta.TotalLiquido, resposta.OsPagas, resposta.TicketMedio));
    }
}
