using System.Globalization;
using ThiagoLavaCar.Application.Abstractions;
using ThiagoLavaCar.Application.Common;
using ThiagoLavaCar.Application.DTOs;
using ThiagoLavaCar.Application.Exceptions;
using ThiagoLavaCar.Domain.Common;
using ThiagoLavaCar.Domain.Entities;
using ThiagoLavaCar.Domain.Enums;
using ThiagoLavaCar.Domain.Exceptions;

namespace ThiagoLavaCar.Application.Services;

public interface ICaixaService
{
    Task<CaixaResumoResponse> ObterResumoAsync(DateOnly data, CancellationToken ct);
    Task<FechamentoResponse> FecharAsync(DateOnly data, FecharCaixaRequest request, CancellationToken ct);
    Task ReabrirAsync(DateOnly data, string justificativa, CancellationToken ct);
    Task<IReadOnlyList<FechamentoResponse>> ListarFechamentosAsync(string? mes, CancellationToken ct);
}

public sealed class CaixaService(
    ICaixaRepository caixa,
    IPagamentoRepository pagamentos,
    Auditoria auditoria,
    IUnitOfWork unitOfWork,
    IUsuarioLogado usuarioLogado,
    TimeProvider relogio) : ICaixaService
{
    public async Task<CaixaResumoResponse> ObterResumoAsync(DateOnly data, CancellationToken ct)
    {
        GarantirNaoFuturo(data);
        var (formas, _) = await CalcularFormasAsync(data, ct);
        var (inicio, fim) = FusoBrasilia.IntervaloDoDia(data);
        return new CaixaResumoResponse(
            data,
            await caixa.ExisteFechamentoAsync(data, ct),
            formas.Select(f => new CaixaFormaResponse(f.Forma.ToString(), f.Quantidade, f.Bruto, f.Taxas, f.Liquido)).ToList(),
            formas.Sum(f => f.Bruto),
            formas.Sum(f => f.Liquido),
            await caixa.SomarDespesasAsync(data, ct),
            await caixa.SomarDiariasAsync(data, ct),
            await caixa.ContarOrdensSemPagamentoCompletoAsync(inicio, fim, ct));
    }

    public async Task<FechamentoResponse> FecharAsync(DateOnly data, FecharCaixaRequest request, CancellationToken ct)
    {
        GarantirNaoFuturo(data);
        if (await caixa.ExisteFechamentoAsync(data, ct))
        {
            throw new ConflitoException("O caixa deste dia já foi fechado.");
        }

        // O sistema recalcula o resumo no momento do fechamento; o que a tela mostrou antes não vale.
        var (formas, totalLiquido) = await CalcularFormasAsync(data, ct);
        if (request.Conferido.Select(c => c.Forma).Distinct().Count() != request.Conferido.Count)
        {
            throw new RegraInvalidaException("Informe cada forma de pagamento uma única vez.");
        }

        var conferido = request.Conferido.ToDictionary(c => Enum.Parse<FormaPagamento>(c.Forma), c => c.Valor);

        var faltando = formas.Where(f => !conferido.ContainsKey(f.Forma)).Select(f => f.Forma.ToString()).ToList();
        if (faltando.Count > 0)
        {
            throw new RegraInvalidaException($"Informe o valor conferido de: {string.Join(", ", faltando)}.");
        }

        var linhas = conferido
            .Select(c => new FechamentoCaixa.Linha(c.Key, formas.FirstOrDefault(f => f.Forma == c.Key)?.Bruto ?? 0m, c.Value))
            .ToList();
        var agora = relogio.GetUtcNow();
        var fechamento = FechamentoCaixa.Fechar(data, FusoBrasilia.DataDe(agora), usuarioLogado.Id, linhas, totalLiquido, request.Justificativa, agora);

        await caixa.AdicionarFechamentoAsync(fechamento, ct);
        if (fechamento.Diferenca != 0 || fechamento.Formas.Any(f => f.TemDiferenca))
        {
            await auditoria.RegistrarAsync("AjusteCaixa", nameof(FechamentoCaixa), 0, new
            {
                data = data.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                fechamento.TotalBruto,
                fechamento.TotalConferido,
                fechamento.Diferenca,
                fechamento.Justificativa,
            }, ct);
        }

        await unitOfWork.SalvarAsync(ct);
        return Mapear(fechamento);
    }

    public async Task ReabrirAsync(DateOnly data, string justificativa, CancellationToken ct)
    {
        var fechamento = await caixa.ObterFechamentoAsync(data, ct)
            ?? throw new NaoEncontradoException("Não há fechamento para esta data.");
        caixa.RemoverFechamento(fechamento);
        await auditoria.RegistrarAsync("ReaberturaCaixa", nameof(FechamentoCaixa), fechamento.Id, new
        {
            data = data.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            fechamento.TotalBruto,
            fechamento.TotalConferido,
            fechamento.Diferenca,
            fechadoEm = fechamento.FechadoEm,
            justificativaDoFechamento = fechamento.Justificativa,
            justificativaDaReabertura = justificativa.Trim(),
        }, ct);
        await unitOfWork.SalvarAsync(ct);
    }

    public async Task<IReadOnlyList<FechamentoResponse>> ListarFechamentosAsync(string? mes, CancellationToken ct)
    {
        var referencia = relogio.GetUtcNow();
        var ano = FusoBrasilia.DataDe(referencia).Year;
        var numero = FusoBrasilia.DataDe(referencia).Month;
        if (!string.IsNullOrWhiteSpace(mes))
        {
            if (!DateOnly.TryParseExact(mes + "-01", "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var primeiro))
            {
                throw new RegraInvalidaException("Mês inválido. Use o formato AAAA-MM, por exemplo 2026-10.");
            }

            (ano, numero) = (primeiro.Year, primeiro.Month);
        }

        var inicio = new DateOnly(ano, numero, 1);
        var fim = inicio.AddMonths(1).AddDays(-1);
        return (await caixa.ListarFechamentosAsync(inicio, fim, ct)).Select(Mapear).ToList();
    }

    private void GarantirNaoFuturo(DateOnly data)
    {
        if (data > FusoBrasilia.DataDe(relogio.GetUtcNow()))
        {
            throw new RegraInvalidaException("Não é possível consultar ou fechar o caixa de uma data futura.");
        }
    }

    private sealed record TotalDaForma(FormaPagamento Forma, int Quantidade, decimal Bruto, decimal Taxas, decimal Liquido);

    /// <summary>Só pagamentos não estornados com data de caixa igual ao dia; uma linha por forma com movimento.</summary>
    private async Task<(List<TotalDaForma> Formas, decimal TotalLiquido)> CalcularFormasAsync(DateOnly data, CancellationToken ct)
    {
        var doDia = (await pagamentos.ListarPorDataCaixaAsync(data, ct)).Where(p => !p.Estornado).ToList();
        var formas = doDia
            .GroupBy(p => p.FormaPagamento)
            .OrderBy(g => g.Key)
            .Select(g => new TotalDaForma(
                g.Key, g.Count(), Dinheiro.Arredondar(g.Sum(p => p.ValorBruto)),
                Dinheiro.Arredondar(g.Sum(p => p.ValorTaxa)), Dinheiro.Arredondar(g.Sum(p => p.ValorLiquido))))
            .ToList();
        return (formas, formas.Sum(f => f.Liquido));
    }

    private static FechamentoResponse Mapear(FechamentoCaixa f) => new(
        f.Data, f.TotalBruto, f.TotalLiquido, f.TotalConferido, f.Diferenca, f.Justificativa,
        f.Formas.OrderBy(x => x.FormaPagamento).Select(x => new FechamentoFormaResponse(x.FormaPagamento.ToString(), x.ValorCalculado, x.ValorConferido)).ToList(),
        f.FechadoEm);
}
