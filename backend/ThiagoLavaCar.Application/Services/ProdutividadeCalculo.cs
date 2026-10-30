using ThiagoLavaCar.Application.DTOs;
using ThiagoLavaCar.Domain.Entities;

namespace ThiagoLavaCar.Application.Services;

/// <summary>
/// Tempo médio real por serviço e por lavador. Numa OS com vários itens, a duração real se divide pela proporção
/// do tempo estimado de cada item (a mesma regra do lucro por serviço).
/// </summary>
public static class ProdutividadeCalculo
{
    public static ProdutividadeResponse Calcular(IReadOnlyList<OrdemServico> ordens)
    {
        var porServico = new Dictionary<int, (string Nome, int Quantidade, decimal Minutos, decimal Estimado)>();
        foreach (var ordem in ordens)
        {
            var real = ordem.DuracaoRealMin ?? 0;
            var somaTempos = ordem.Itens.Sum(i => i.TempoEstimadoMin);
            foreach (var item in ordem.Itens)
            {
                var parte = somaTempos > 0 ? (decimal)item.TempoEstimadoMin / somaTempos : 1m / ordem.Itens.Count;
                var atual = porServico.TryGetValue(item.ServicoId, out var existente) ? existente : (Nome: item.NomeServico, Quantidade: 0, Minutos: 0m, Estimado: 0m);
                porServico[item.ServicoId] = (atual.Nome, atual.Quantidade + 1, atual.Minutos + real * parte, atual.Estimado + item.TempoEstimadoMin);
            }
        }

        var servicos = porServico.Values
            .Select(s => new TempoPorServicoResponse(s.Nome, s.Quantidade, Media(s.Minutos, s.Quantidade), Media(s.Estimado, s.Quantidade)))
            .OrderBy(s => s.Servico, StringComparer.Ordinal)
            .ToList();

        var lavadores = ordens
            .GroupBy(o => o.Lavador?.Apelido ?? o.Lavador?.Nome ?? "Sem lavador")
            .Select(g => new TempoPorLavadorResponse(g.Key, g.Count(), Media(g.Sum(o => o.DuracaoRealMin ?? 0), g.Count())))
            .OrderBy(l => l.Lavador, StringComparer.Ordinal)
            .ToList();

        return new ProdutividadeResponse(servicos, lavadores);
    }

    private static int Media(decimal total, int quantidade) => (int)Math.Round(total / quantidade, MidpointRounding.AwayFromZero);
}
