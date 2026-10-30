using ThiagoLavaCar.Application.Abstractions;
using ThiagoLavaCar.Application.Common;
using ThiagoLavaCar.Application.DTOs;
using ThiagoLavaCar.Domain.Common;

namespace ThiagoLavaCar.Application.Services;

public interface IPainelService
{
    Task<PainelHojeResponse> ObterHojeAsync(CancellationToken ct);
}

public sealed class PainelService(
    IPagamentoRepository pagamentos,
    ICaixaRepository caixa,
    IRelatorioRepository relatorio,
    PontoEquilibrioCalculador pontoEquilibrio,
    TimeProvider relogio) : IPainelService
{
    private const int DiasDePendencia = 30;

    public async Task<PainelHojeResponse> ObterHojeAsync(CancellationToken ct)
    {
        var hoje = FusoBrasilia.DataDe(relogio.GetUtcNow());
        var primeiroDoMes = new DateOnly(hoje.Year, hoje.Month, 1);

        var doDia = (await pagamentos.ListarPorDataCaixaAsync(hoje, ct)).Where(p => !p.Estornado).ToList();
        var entrou = Dinheiro.Arredondar(doDia.Sum(p => p.ValorBruto));
        var liquido = Dinheiro.Arredondar(doDia.Sum(p => p.ValorLiquido));

        // "Sobrou de verdade": retiradas do dono não entram.
        var insumosDeHoje = (await relatorio.ListarOrdensPagasAsync(hoje, hoje, ct)).Sum(o => o.Insumos);
        var sobrou = Dinheiro.Arredondar(
            liquido - await caixa.SomarDespesasAsync(hoje, ct) - await caixa.SomarDiariasAsync(hoje, ct) - insumosDeHoje);

        var equilibrio = await pontoEquilibrio.CalcularAsync(primeiroDoMes, hoje, ct);
        return new PainelHojeResponse(entrou, liquido, sobrou, equilibrio.CarrosFeitos, equilibrio.Faltam, await PendenciasAsync(hoje, ct));
    }

    /// <summary>Dias anteriores, dos últimos 30, com pagamento e sem fechamento de caixa.</summary>
    private async Task<IReadOnlyList<string>> PendenciasAsync(DateOnly hoje, CancellationToken ct)
    {
        var de = hoje.AddDays(-DiasDePendencia);
        var ontem = hoje.AddDays(-1);
        var comMovimento = await relatorio.DiasComPagamentoAsync(de, ontem, ct);
        var fechados = (await caixa.ListarFechamentosAsync(de, ontem, ct)).Select(f => f.Data).ToHashSet();
        return comMovimento.Where(d => !fechados.Contains(d)).Select(d => $"Caixa de {d:dd/MM} não fechado").ToList();
    }
}
