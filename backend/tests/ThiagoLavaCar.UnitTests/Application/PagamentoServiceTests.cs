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

public class PagamentoServiceTests
{
    private readonly IPagamentoRepository _pagamentos = Substitute.For<IPagamentoRepository>();
    private readonly IOrdemServicoRepository _ordens = Substitute.For<IOrdemServicoRepository>();
    private readonly ITaxaRepository _taxas = Substitute.For<ITaxaRepository>();
    private readonly ICaixaRepository _caixa = Substitute.For<ICaixaRepository>();
    private readonly IAuditoriaRepository _logs = Substitute.For<IAuditoriaRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IUsuarioLogado _logado = Substitute.For<IUsuarioLogado>();
    private readonly FakeTimeProvider _relogio = new(Fabrica.Agora);
    private readonly PagamentoService _servico;

    public PagamentoServiceTests()
    {
        _logado.Id.Returns(1);
        _logado.Perfil.Returns(Perfil.Dono);
        _taxas.ObterPorFormaAsync(FormaPagamento.Credito, Arg.Any<CancellationToken>()).Returns(TaxaPagamento.Criar(FormaPagamento.Credito, 3.49m, 0m));
        _taxas.ObterPorFormaAsync(FormaPagamento.Pix, Arg.Any<CancellationToken>()).Returns(TaxaPagamento.Criar(FormaPagamento.Pix, 0m, 0m));
        _servico = new PagamentoService(_pagamentos, _ordens, _taxas, _caixa, new Auditoria(_logs, _logado, _relogio), _unitOfWork, _logado, _relogio);
    }

    private OrdemServico OsDe220()
    {
        var os = Fabrica.OrdemAguardando(88);
        var servico = Fabrica.Lavagem();
        os.Editar([ItemOrdemServico.Criar(servico, true, 100m, false), ItemOrdemServico.Criar(servico, true, 120m, false)], null, null);
        _ordens.ObterPorIdAsync(88, Arg.Any<CancellationToken>()).Returns(os);
        return os;
    }

    [Fact] // T33
    public async Task Pagamento_no_credito_calcula_taxa_liquido_e_saldo()
    {
        OsDe220();

        var resposta = await _servico.RegistrarAsync(88, new RegistrarPagamentoRequest("Credito", 100m), CancellationToken.None);

        Assert.Equal(3.49m, resposta.ValorTaxa);
        Assert.Equal(96.51m, resposta.ValorLiquido);
        Assert.Equal(120m, resposta.SaldoPendente);
        Assert.Equal(new DateOnly(2026, 10, 20), resposta.DataCaixa);
        await _unitOfWork.Received(1).SalvarAsync(Arg.Any<CancellationToken>());
    }

    [Fact] // T35
    public async Task Valor_maior_que_o_saldo_devolve_422_com_o_saldo_em_reais()
    {
        OsDe220();
        _pagamentos.SomarPagoAsync(88, Arg.Any<CancellationToken>()).Returns(100m);

        var erro = await Assert.ThrowsAsync<RegraInvalidaException>(() =>
            _servico.RegistrarAsync(88, new RegistrarPagamentoRequest("Pix", 150m), CancellationToken.None));

        Assert.Contains("120,00", erro.Message);
        await _unitOfWork.DidNotReceive().SalvarAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Caixa_fechado_e_os_cancelada_devolvem_409()
    {
        var os = OsDe220();
        _caixa.ExisteFechamentoAsync(new DateOnly(2026, 10, 20), Arg.Any<CancellationToken>()).Returns(true);
        var fechado = await Assert.ThrowsAsync<ConflitoException>(() =>
            _servico.RegistrarAsync(88, new RegistrarPagamentoRequest("Pix", 10m), CancellationToken.None));
        Assert.Equal("O caixa de hoje já foi fechado.", fechado.Message);

        os.Cancelar();
        await Assert.ThrowsAsync<ConflitoException>(() =>
            _servico.RegistrarAsync(88, new RegistrarPagamentoRequest("Pix", 10m), CancellationToken.None));
    }

    [Fact]
    public async Task Os_inexistente_devolve_404()
    {
        await Assert.ThrowsAsync<NaoEncontradoException>(() =>
            _servico.RegistrarAsync(1, new RegistrarPagamentoRequest("Pix", 10m), CancellationToken.None));
    }

    [Fact]
    public async Task Estorno_marca_o_pagamento_e_registra_auditoria()
    {
        var pagamento = Pagamento.Criar(88, 1, 100m, TaxaPagamento.Criar(FormaPagamento.Pix, 0m, 0m), new DateOnly(2026, 10, 20), Fabrica.Agora);
        _pagamentos.ObterPorIdAsync(5, Arg.Any<CancellationToken>()).Returns(pagamento);

        var resposta = await _servico.EstornarAsync(5, " cliente desistiu ", CancellationToken.None);

        Assert.True(resposta.Estornado);
        await _logs.Received(1).AdicionarAsync(
            Arg.Is<LogAuditoria>(l => l.Acao == "EstornoPagamento" && l.UsuarioId == 1 && l.Detalhes!.Contains("cliente desistiu")), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Estorno_com_caixa_do_dia_fechado_ou_repetido_devolve_409()
    {
        var pagamento = Pagamento.Criar(88, 1, 100m, TaxaPagamento.Criar(FormaPagamento.Pix, 0m, 0m), new DateOnly(2026, 10, 20), Fabrica.Agora);
        _pagamentos.ObterPorIdAsync(5, Arg.Any<CancellationToken>()).Returns(pagamento);
        _caixa.ExisteFechamentoAsync(new DateOnly(2026, 10, 20), Arg.Any<CancellationToken>()).Returns(true);
        await Assert.ThrowsAsync<ConflitoException>(() => _servico.EstornarAsync(5, null, CancellationToken.None));

        _caixa.ExisteFechamentoAsync(new DateOnly(2026, 10, 20), Arg.Any<CancellationToken>()).Returns(false);
        await _servico.EstornarAsync(5, null, CancellationToken.None);
        await Assert.ThrowsAsync<ConflitoException>(() => _servico.EstornarAsync(5, null, CancellationToken.None));
    }

    [Fact]
    public async Task Alterar_taxa_registra_antes_e_depois_na_auditoria()
    {
        var taxa = TaxaPagamento.Criar(FormaPagamento.Debito, 1m, 0m);
        _taxas.ObterPorFormaAsync(FormaPagamento.Debito, Arg.Any<CancellationToken>()).Returns(taxa);

        var resposta = await _servico.AlterarTaxaAsync(FormaPagamento.Debito, new AlterarTaxaRequest(1.99m, 0.5m), CancellationToken.None);

        Assert.Equal(1.99m, resposta.Percentual);
        await _logs.Received(1).AdicionarAsync(Arg.Is<LogAuditoria>(l => l.Acao == "AlteracaoTaxa" && l.Detalhes!.Contains("1.99")), Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData("Boleto", 10, "formaPagamento")]
    [InlineData("Pix", 0, "valorBruto")]
    [InlineData("Pix", -5, "valorBruto")]
    public void Validador_rejeita_forma_desconhecida_e_valor_nao_positivo(string forma, double valor, string campo)
    {
        var resultado = new RegistrarPagamentoRequestValidator().Validate(new RegistrarPagamentoRequest(forma, (decimal)valor));

        Assert.Contains(resultado.Errors, e => string.Equals(e.PropertyName, campo, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Validador_de_taxa_limita_percentual_a_100_e_fixo_nao_negativo()
    {
        var v = new AlterarTaxaRequestValidator();
        Assert.False(v.Validate(new AlterarTaxaRequest(101m, 0m)).IsValid);
        Assert.False(v.Validate(new AlterarTaxaRequest(1m, -1m)).IsValid);
        Assert.True(v.Validate(new AlterarTaxaRequest(100m, 0m)).IsValid);
    }
}
