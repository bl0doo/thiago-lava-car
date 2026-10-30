using System.Globalization;
using ThiagoLavaCar.Domain.Exceptions;

namespace ThiagoLavaCar.Application.Common;

internal static class Periodo
{
    public static DateOnly ResolverMes(string? mes, TimeProvider relogio)
    {
        if (string.IsNullOrWhiteSpace(mes))
        {
            var hoje = FusoBrasilia.DataDe(relogio.GetUtcNow());
            return new DateOnly(hoje.Year, hoje.Month, 1);
        }

        return DateOnly.TryParseExact(mes + "-01", "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var primeiro)
            ? primeiro
            : throw new RegraInvalidaException("Mês inválido. Use o formato AAAA-MM, por exemplo 2026-10.");
    }

    public static (DateOnly De, DateOnly Ate) Resolver(DateOnly? de, DateOnly? ate, TimeProvider relogio)
    {
        var hoje = FusoBrasilia.DataDe(relogio.GetUtcNow());
        var inicio = de ?? new DateOnly(hoje.Year, hoje.Month, 1);
        var fim = ate ?? (de is null ? hoje : inicio.AddMonths(1).AddDays(-1));
        if (fim < inicio)
        {
            throw new RegraInvalidaException("A data final não pode ser anterior à inicial.");
        }

        return (inicio, fim);
    }
}
