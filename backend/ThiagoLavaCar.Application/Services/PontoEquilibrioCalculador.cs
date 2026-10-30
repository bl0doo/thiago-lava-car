using ThiagoLavaCar.Application.Abstractions;
using ThiagoLavaCar.Domain.Common;
using ThiagoLavaCar.Domain.Enums;

namespace ThiagoLavaCar.Application.Services;

/// <param name="Despesas">Despesas do período.</param>
/// <param name="Diarias">Diárias do período.</param>
/// <param name="Contas">Despesas mais diárias do período.</param>
/// <param name="MargemPorCarro">Líquido médio por OS menos insumos médios por OS nos últimos 30 dias; nulo se não for positiva.</param>
/// <param name="MargemEstimada">Verdadeiro quando não havia histórico e a margem veio do catálogo.</param>
/// <param name="CarrosNecessarios">Contas ÷ margem, arredondado para cima.</param>
/// <param name="CarrosFeitos">OS pagas no mês.</param>
/// <param name="Faltam">Máximo entre 0 e (necessários − feitos).</param>
public sealed record PontoEquilibrio(
    decimal Despesas, decimal Diarias, decimal Contas, decimal? MargemPorCarro, bool MargemEstimada, int? CarrosNecessarios, int CarrosFeitos, int? Faltam);

/// <summary>Ponto de equilíbrio do mês (Entrega 2, regras de cálculo), compartilhado pelo painel e pelo relatório.</summary>
public sealed class PontoEquilibrioCalculador(
    IRelatorioRepository relatorio,
    IServicoRepository servicos,
    ITaxaRepository taxas)
{
    private const int JanelaMargemDias = 30;

    public async Task<PontoEquilibrio> CalcularAsync(DateOnly primeiroDia, DateOnly ultimoDia, CancellationToken ct)
    {
        var despesas = Dinheiro.Arredondar(await relatorio.SomarDespesasAsync(primeiroDia, ultimoDia, ct));
        var diarias = Dinheiro.Arredondar(await relatorio.SomarDiariasAsync(primeiroDia, ultimoDia, ct));
        var contas = despesas + diarias;
        var feitas = (await relatorio.ListarOrdensPagasAsync(primeiroDia, ultimoDia, ct)).Count;

        var historico = await relatorio.ListarOrdensPagasAsync(ultimoDia.AddDays(1 - JanelaMargemDias), ultimoDia, ct);
        decimal? margem;
        var estimada = historico.Count == 0;
        if (estimada)
        {
            margem = await MargemDoCatalogoAsync(ct);
        }
        else
        {
            margem = historico.Average(o => o.Liquido) - historico.Average(o => o.Insumos);
        }

        if (margem is not > 0)
        {
            return new PontoEquilibrio(despesas, diarias, contas, null, estimada, null, feitas, null);
        }

        var margemArredondada = Dinheiro.Arredondar(margem.Value);
        var necessarios = (int)Math.Ceiling(contas / margemArredondada);
        return new PontoEquilibrio(despesas, diarias, contas, margemArredondada, estimada, necessarios, feitas, Math.Max(0, necessarios - feitas));
    }

    /// <summary>Sem histórico: preço da lavagem de carro do catálogo menos insumos e a taxa média.</summary>
    private async Task<decimal?> MargemDoCatalogoAsync(CancellationToken ct)
    {
        var candidatos = (await servicos.ListarAsync(somenteAtivos: true, ct))
            .Select(s => (Servico: s, Preco: s.Precos.FirstOrDefault(p => p.TipoVeiculo == TipoVeiculo.Carro)))
            .Where(x => x.Preco is not null)
            .ToList();
        var lavagem = candidatos.FirstOrDefault(x => x.Servico.Nome.StartsWith("Lavagem", StringComparison.OrdinalIgnoreCase));
        var escolhido = lavagem.Servico is null ? candidatos.FirstOrDefault() : lavagem;
        if (escolhido.Servico is null)
        {
            return null;
        }

        var lista = await taxas.ListarAsync(ct);
        var taxaMedia = lista.Count == 0 ? 0m : lista.Average(t => t.Percentual);
        var preco = escolhido.Preco!.Preco;
        return preco - preco * taxaMedia / 100m - escolhido.Servico.CustoInsumos;
    }
}
