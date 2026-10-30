using ThiagoLavaCar.Application.Abstractions;
using ThiagoLavaCar.Application.Exceptions;
using ThiagoLavaCar.Application.Services;
using ThiagoLavaCar.Domain.Entities;
using ThiagoLavaCar.Domain.Enums;

namespace ThiagoLavaCar.UnitTests.Application;

public class OrdemDetalheServiceTests
{
    private static readonly DateOnly Hoje = new(2026, 10, 20);

    private readonly IOrdemServicoRepository _ordens = Substitute.For<IOrdemServicoRepository>();
    private readonly IPagamentoRepository _pagamentos = Substitute.For<IPagamentoRepository>();
    private readonly IUsuarioLogado _logado = Substitute.For<IUsuarioLogado>();
    private readonly OrdemDetalheService _servico;

    public OrdemDetalheServiceTests()
    {
        _logado.Perfil.Returns(Perfil.Dono);
        _servico = new OrdemDetalheService(_ordens, _pagamentos, _logado);
    }

    [Fact]
    public async Task Detalhe_soma_so_pagamentos_nao_estornados_e_calcula_o_saldo()
    {
        var os = Fabrica.OrdemAguardando(); // R$ 100,00
        typeof(OrdemServico).GetProperty(nameof(OrdemServico.Veiculo))!.SetValue(os, Fabrica.Onix());
        typeof(OrdemServico).GetProperty(nameof(OrdemServico.Cliente))!.SetValue(os, Cliente.Criar("Renata Souza", "44999998888", null, CanalContato.WhatsApp, true, Fabrica.Agora).ComId(10));
        _ordens.ObterComAvariasAsync(88, Arg.Any<CancellationToken>()).Returns(os);
        var pix = TaxaPagamento.Criar(FormaPagamento.Pix, 0m, 0m);
        var estornado = Pagamento.Criar(88, 1, 30m, pix, Hoje, Fabrica.Agora);
        estornado.Estornar(Fabrica.Agora);
        _pagamentos.ListarPorOrdemAsync(88, Arg.Any<CancellationToken>()).Returns([Pagamento.Criar(88, 1, 40m, pix, Hoje, Fabrica.Agora), estornado]);

        var detalhe = await _servico.ObterAsync(88, CancellationToken.None);

        Assert.Equal((40m, 60m), (detalhe.ValorPago, detalhe.SaldoPendente));
        Assert.Equal(2, detalhe.Pagamentos.Count);
        Assert.Contains(detalhe.Pagamentos, p => p.Estornado);
        Assert.Single(detalhe.Itens);
        Assert.Equal(os.ValorTotal, detalhe.Itens.Sum(i => i.PrecoFinal));
    }

    [Fact]
    public async Task Detalhe_de_ordem_inexistente_da_404()
    {
        _ordens.ObterComAvariasAsync(9, Arg.Any<CancellationToken>()).Returns((OrdemServico?)null);

        await Assert.ThrowsAsync<NaoEncontradoException>(() => _servico.ObterAsync(9, CancellationToken.None));
    }
}
