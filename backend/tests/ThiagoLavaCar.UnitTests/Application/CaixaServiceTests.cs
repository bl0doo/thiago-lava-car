using Microsoft.Extensions.Time.Testing;
using ThiagoLavaCar.Application.Abstractions;
using ThiagoLavaCar.Application.DTOs;
using ThiagoLavaCar.Application.Exceptions;
using ThiagoLavaCar.Application.Services;
using ThiagoLavaCar.Application.Validators;
using ThiagoLavaCar.Domain.Entities;
using ThiagoLavaCar.Domain.Enums;
using ThiagoLavaCar.Domain.Exceptions;

namespace ThiagoLavaCar.UnitTests.Application;

public class CaixaServiceTests
{
    private static readonly DateOnly Hoje = new(2026, 10, 20);

    private readonly ICaixaRepository _caixa = Substitute.For<ICaixaRepository>();
    private readonly IPagamentoRepository _pagamentos = Substitute.For<IPagamentoRepository>();
    private readonly IAuditoriaRepository _logs = Substitute.For<IAuditoriaRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IUsuarioLogado _logado = Substitute.For<IUsuarioLogado>();
    private readonly FakeTimeProvider _relogio = new(Fabrica.Agora);
    private readonly CaixaService _servico;

    public CaixaServiceTests()
    {
        _logado.Id.Returns(1);
        _logado.Perfil.Returns(Perfil.Dono);
        _servico = new CaixaService(_caixa, _pagamentos, new Auditoria(_logs, _logado, _relogio), _unitOfWork, _logado, _relogio);
        // 4 Pix de 90 = 360, 2 créditos de 100 = 200 (taxa 3,49%), 1 dinheiro de 80, 1 pix estornado que não entra
        var pix = TaxaPagamento.Criar(FormaPagamento.Pix, 0m, 0m);
        var credito = TaxaPagamento.Criar(FormaPagamento.Credito, 3.49m, 0m);
        var dinheiro = TaxaPagamento.Criar(FormaPagamento.Dinheiro, 0m, 0m);
        var estornado = Pagamento.Criar(1, 1, 500m, pix, Hoje, Fabrica.Agora);
        estornado.Estornar(Fabrica.Agora);
        _pagamentos.ListarPorDataCaixaAsync(Hoje, Arg.Any<CancellationToken>()).Returns(
        [
            .. Enumerable.Range(0, 4).Select(_ => Pagamento.Criar(1, 1, 90m, pix, Hoje, Fabrica.Agora)),
            .. Enumerable.Range(0, 2).Select(_ => Pagamento.Criar(1, 1, 100m, credito, Hoje, Fabrica.Agora)),
            Pagamento.Criar(1, 1, 80m, dinheiro, Hoje, Fabrica.Agora),
            estornado,
        ]);
        _caixa.SomarDespesasAsync(Hoje, Arg.Any<CancellationToken>()).Returns(45m);
        _caixa.SomarDiariasAsync(Hoje, Arg.Any<CancellationToken>()).Returns(300m);
        _caixa.ContarOrdensSemPagamentoCompletoAsync(Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>()).Returns(1);
    }

    [Fact] // T36
    public async Task Resumo_soma_so_pagamentos_nao_estornados_por_forma()
    {
        var resumo = await _servico.ObterResumoAsync(Hoje, CancellationToken.None);

        Assert.False(resumo.Fechado);
        Assert.Equal(3, resumo.PorForma.Count);
        var credito = resumo.PorForma.Single(f => f.Forma == "Credito");
        Assert.Equal((2, 200m, 6.98m, 193.02m), (credito.Quantidade, credito.Bruto, credito.Taxas, credito.Liquido));
        Assert.Equal(640m, resumo.TotalBruto);
        Assert.Equal(633.02m, resumo.TotalLiquido);
        Assert.Equal((45m, 300m, 1), (resumo.Despesas, resumo.Diarias, resumo.OsConcluidasSemPagamento));
    }

    [Fact]
    public async Task Resumo_de_data_futura_devolve_422()
    {
        await Assert.ThrowsAsync<RegraInvalidaException>(() => _servico.ObterResumoAsync(Hoje.AddDays(1), CancellationToken.None));
    }

    [Fact] // T37
    public async Task Fechamento_com_diferenca_exige_justificativa_e_grava_auditoria()
    {
        var conferido = new ValorConferidoDto[] { new("Pix", 360m), new("Credito", 200m), new("Dinheiro", 70m) };

        await Assert.ThrowsAsync<RegraInvalidaException>(() =>
            _servico.FecharAsync(Hoje, new FecharCaixaRequest(conferido, null), CancellationToken.None));

        var fechamento = await _servico.FecharAsync(Hoje, new FecharCaixaRequest(conferido, "Troco de R$ 10 dado a mais pela manhã"), CancellationToken.None);

        Assert.Equal(-10m, fechamento.Diferenca);
        Assert.Equal(640m, fechamento.TotalBruto);
        Assert.Equal(630m, fechamento.TotalConferido);
        await _logs.Received(1).AdicionarAsync(Arg.Is<LogAuditoria>(l => l.Acao == "AjusteCaixa"), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SalvarAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Fechamento_sem_diferenca_nao_pede_justificativa_nem_gera_auditoria()
    {
        var conferido = new ValorConferidoDto[] { new("Pix", 360m), new("Credito", 200m), new("Dinheiro", 80m) };

        var fechamento = await _servico.FecharAsync(Hoje, new FecharCaixaRequest(conferido, null), CancellationToken.None);

        Assert.Equal(0m, fechamento.Diferenca);
        await _logs.DidNotReceive().AdicionarAsync(Arg.Any<LogAuditoria>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Fechamento_exige_todas_as_formas_com_movimento_e_so_uma_vez_cada()
    {
        var faltando = await Assert.ThrowsAsync<RegraInvalidaException>(() =>
            _servico.FecharAsync(Hoje, new FecharCaixaRequest([new("Pix", 360m)], null), CancellationToken.None));
        Assert.Contains("Credito", faltando.Message);

        await Assert.ThrowsAsync<RegraInvalidaException>(() => _servico.FecharAsync(Hoje,
            new FecharCaixaRequest([new("Pix", 360m), new("Pix", 360m), new("Credito", 200m), new("Dinheiro", 80m)], null), CancellationToken.None));
    }

    [Fact]
    public async Task Caixa_ja_fechado_devolve_409()
    {
        _caixa.ExisteFechamentoAsync(Hoje, Arg.Any<CancellationToken>()).Returns(true);

        await Assert.ThrowsAsync<ConflitoException>(() =>
            _servico.FecharAsync(Hoje, new FecharCaixaRequest([new("Pix", 1m)], null), CancellationToken.None));
    }

    [Fact]
    public async Task Reabrir_remove_o_fechamento_e_registra_as_duas_justificativas()
    {
        var fechamento = FechamentoCaixa.Fechar(Hoje, Hoje, 1, [new(FormaPagamento.Pix, 100m, 100m)], 100m, null, Fabrica.Agora);
        _caixa.ObterFechamentoAsync(Hoje, Arg.Any<CancellationToken>()).Returns(fechamento);

        await _servico.ReabrirAsync(Hoje, " esqueci um pagamento ", CancellationToken.None);

        _caixa.Received(1).RemoverFechamento(fechamento);
        await _logs.Received(1).AdicionarAsync(
            Arg.Is<LogAuditoria>(l => l.Acao == "ReaberturaCaixa" && l.Detalhes!.Contains("esqueci um pagamento")), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Reabrir_dia_sem_fechamento_devolve_404()
    {
        await Assert.ThrowsAsync<NaoEncontradoException>(() => _servico.ReabrirAsync(Hoje, "motivo qualquer longo", CancellationToken.None));
    }

    [Theory]
    [InlineData("2026-13")]
    [InlineData("outubro")]
    [InlineData("2026-1")]
    public async Task Historico_com_mes_invalido_devolve_422(string mes)
    {
        await Assert.ThrowsAsync<RegraInvalidaException>(() => _servico.ListarFechamentosAsync(mes, CancellationToken.None));
    }

    [Fact]
    public async Task Historico_sem_mes_usa_o_mes_atual_em_Brasilia()
    {
        await _servico.ListarFechamentosAsync(null, CancellationToken.None);

        await _caixa.Received(1).ListarFechamentosAsync(new DateOnly(2026, 10, 1), new DateOnly(2026, 10, 31), Arg.Any<CancellationToken>());
    }

    [Fact]
    public void Validadores_checam_forma_valor_e_justificativa_da_reabertura()
    {
        Assert.False(new FecharCaixaRequestValidator().Validate(new FecharCaixaRequest([new("Boleto", 1m)], null)).IsValid);
        Assert.False(new FecharCaixaRequestValidator().Validate(new FecharCaixaRequest([new("Pix", -1m)], null)).IsValid);
        Assert.False(new FecharCaixaRequestValidator().Validate(new FecharCaixaRequest([], null)).IsValid);
        Assert.False(new ReabrirCaixaRequestValidator().Validate(new ReabrirCaixaRequest("curta")).IsValid);
        Assert.True(new ReabrirCaixaRequestValidator().Validate(new ReabrirCaixaRequest("Esqueci um pagamento")).IsValid);
    }
}
