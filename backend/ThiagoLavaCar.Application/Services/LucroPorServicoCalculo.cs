using ThiagoLavaCar.Application.Abstractions;
using ThiagoLavaCar.Application.DTOs;
using ThiagoLavaCar.Domain.Common;

namespace ThiagoLavaCar.Application.Services;

/// <summary>
/// Lucro por serviço: receita líquida − insumos − mão de obra rateada.
/// A mão de obra do período é rateada por minuto lavado; numa OS com vários itens, a receita segue a proporção do preço
/// e o tempo e a mão de obra seguem a proporção do tempo estimado de cada item.
/// </summary>
public static class LucroPorServicoCalculo
{
    private sealed class Acumulado
    {
        public int Quantidade;
        public decimal Receita;
        public decimal Insumos;
        public decimal MaoDeObra;
        public decimal Minutos;
    }

    public static IReadOnlyList<LucroPorServicoResponse> Calcular(IReadOnlyList<OrdemPaga> ordens, decimal diariasDoPeriodo)
    {
        // Minutos reais, ou os estimados quando o temporizador não foi usado.
        static int Minutos(OrdemPaga o) => o.Ordem.DuracaoRealMin ?? o.Ordem.DuracaoEstimadaMin;

        var totalMinutos = ordens.Sum(Minutos);
        var custoPorMinuto = totalMinutos > 0 ? diariasDoPeriodo / totalMinutos : 0m;
        var grupos = new Dictionary<(int ServicoId, bool ComCera), (string Nome, Acumulado Dados)>();

        foreach (var paga in ordens)
        {
            var itens = paga.Ordem.Itens;
            var somaPrecos = itens.Sum(i => i.PrecoFinal);
            var somaTempos = itens.Sum(i => i.TempoEstimadoMin);
            var minutos = Minutos(paga);
            var maoDeObraDaOs = custoPorMinuto * minutos;

            foreach (var item in itens)
            {
                var parteDoPreco = somaPrecos > 0 ? item.PrecoFinal / somaPrecos : 1m / itens.Count;
                var parteDoTempo = somaTempos > 0 ? (decimal)item.TempoEstimadoMin / somaTempos : 1m / itens.Count;
                var chave = (item.ServicoId, item.ComCera);
                if (!grupos.TryGetValue(chave, out var grupo))
                {
                    grupo = (item.ComCera ? $"{item.NomeServico} com cera" : item.NomeServico, new Acumulado());
                    grupos[chave] = grupo;
                }

                grupo.Dados.Quantidade++;
                grupo.Dados.Receita += paga.Liquido * parteDoPreco;
                grupo.Dados.Insumos += item.CustoInsumos;
                grupo.Dados.MaoDeObra += maoDeObraDaOs * parteDoTempo;
                grupo.Dados.Minutos += minutos * parteDoTempo;
            }
        }

        return grupos.Values
            .Select(g =>
            {
                var receita = Dinheiro.Arredondar(g.Dados.Receita);
                var insumos = Dinheiro.Arredondar(g.Dados.Insumos);
                var maoDeObra = Dinheiro.Arredondar(g.Dados.MaoDeObra);
                var lucro = receita - insumos - maoDeObra;
                return new LucroPorServicoResponse(
                    g.Nome, g.Dados.Quantidade, receita, insumos, maoDeObra, lucro,
                    Dinheiro.Arredondar(lucro / g.Dados.Quantidade),
                    (int)Math.Round(g.Dados.Minutos / g.Dados.Quantidade, MidpointRounding.AwayFromZero));
            })
            .OrderByDescending(r => r.Lucro).ThenBy(r => r.Servico, StringComparer.Ordinal)
            .ToList();
    }
}
