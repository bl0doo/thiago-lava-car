using System.Globalization;
using ThiagoLavaCar.Application.Abstractions;
using ThiagoLavaCar.Application.Common;
using ThiagoLavaCar.Application.DTOs;
using ThiagoLavaCar.Domain.Common;
using ThiagoLavaCar.Domain.Exceptions;

namespace ThiagoLavaCar.Application.Services;

public interface IRelatorioService
{
    Task<IReadOnlyList<LucroPorServicoResponse>> LucroPorServicoAsync(DateOnly? inicio, DateOnly? fim, CancellationToken ct);
    Task<PontoEquilibrioResponse> PontoEquilibrioAsync(string? mes, CancellationToken ct);
    Task<FaturamentoResponse> FaturamentoAsync(DateOnly? inicio, DateOnly? fim, CancellationToken ct);
    Task<ProdutividadeResponse> ProdutividadeAsync(DateOnly? inicio, DateOnly? fim, CancellationToken ct);
}

public sealed class RelatorioService(IRelatorioRepository relatorio, PontoEquilibrioCalculador pontoEquilibrio, TimeProvider relogio) : IRelatorioService
{
    private const int MaximoDiasDoPeriodo = 366;

    public async Task<IReadOnlyList<LucroPorServicoResponse>> LucroPorServicoAsync(DateOnly? inicio, DateOnly? fim, CancellationToken ct)
    {
        var (de, ate) = ResolverPeriodo(inicio, fim);
        var ordens = await relatorio.ListarOrdensPagasAsync(de, ate, ct);
        return LucroPorServicoCalculo.Calcular(ordens, await relatorio.SomarDiariasAsync(de, ate, ct));
    }

    public async Task<FaturamentoResponse> FaturamentoAsync(DateOnly? inicio, DateOnly? fim, CancellationToken ct)
    {
        var (de, ate) = ResolverPeriodo(inicio, fim);
        var pagamentos = await relatorio.ListarPagamentosAsync(de, ate, ct);
        var pagas = (await relatorio.ListarOrdensPagasAsync(de, ate, ct)).ToLookup(o => o.DataPaga);

        var dias = pagamentos
            .GroupBy(p => p.DataCaixa)
            .OrderBy(g => g.Key)
            .Select(g => new FaturamentoDiaResponse(
                g.Key, Dinheiro.Arredondar(g.Sum(p => p.ValorBruto)), Dinheiro.Arredondar(g.Sum(p => p.ValorLiquido)),
                pagas[g.Key].Count(), TicketMedio(pagas[g.Key])))
            .ToList();
        var todas = pagas.SelectMany(g => g).ToList();
        return new FaturamentoResponse(
            dias, dias.Sum(d => d.Bruto), dias.Sum(d => d.Liquido), todas.Count, TicketMedio(todas));
    }

    public async Task<ProdutividadeResponse> ProdutividadeAsync(DateOnly? inicio, DateOnly? fim, CancellationToken ct)
    {
        var (de, ate) = ResolverPeriodo(inicio, fim);
        return ProdutividadeCalculo.Calcular(await relatorio.ListarOrdensCronometradasAsync(de, ate, ct));
    }

    private static decimal TicketMedio(IEnumerable<OrdemPaga> ordens)
    {
        var lista = ordens.ToList();
        return lista.Count == 0 ? 0m : Dinheiro.Arredondar(lista.Sum(o => o.Bruto) / lista.Count);
    }

    public async Task<PontoEquilibrioResponse> PontoEquilibrioAsync(string? mes, CancellationToken ct)
    {
        var hoje = FusoBrasilia.DataDe(relogio.GetUtcNow());
        var primeiro = new DateOnly(hoje.Year, hoje.Month, 1);
        if (!string.IsNullOrWhiteSpace(mes) &&
            !DateOnly.TryParseExact(mes + "-01", "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out primeiro))
        {
            throw new RegraInvalidaException("Mês inválido. Use o formato AAAA-MM, por exemplo 2026-10.");
        }

        if (primeiro > hoje)
        {
            throw new RegraInvalidaException("Não há ponto de equilíbrio para um mês futuro.");
        }

        // O mês corrente só vai até hoje; os anteriores, até o último dia.
        var ultimo = DateOnly.FromDayNumber(primeiro.AddMonths(1).DayNumber - 1);
        var ponto = await pontoEquilibrio.CalcularAsync(primeiro, ultimo > hoje ? hoje : ultimo, ct);
        return new PontoEquilibrioResponse(
            primeiro.ToString("yyyy-MM", CultureInfo.InvariantCulture), ponto.Despesas, ponto.Diarias, ponto.Contas,
            ponto.MargemPorCarro, ponto.MargemEstimada, ponto.CarrosNecessarios, ponto.CarrosFeitos, ponto.Faltam);
    }

    /// <summary>Sem datas, o período é o mês atual até hoje; vale inicio ≤ fim e até 366 dias.</summary>
    private (DateOnly De, DateOnly Ate) ResolverPeriodo(DateOnly? inicio, DateOnly? fim)
    {
        var hoje = FusoBrasilia.DataDe(relogio.GetUtcNow());
        var de = inicio ?? new DateOnly(hoje.Year, hoje.Month, 1);
        var ate = fim ?? hoje;
        if (de > ate)
        {
            throw new RegraInvalidaException("A data inicial não pode ser posterior à final.");
        }

        if (ate.DayNumber - de.DayNumber + 1 > MaximoDiasDoPeriodo)
        {
            throw new RegraInvalidaException($"O período pode ter no máximo {MaximoDiasDoPeriodo} dias.");
        }

        return (de, ate);
    }
}
