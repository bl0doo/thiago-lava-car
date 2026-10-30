namespace ThiagoLavaCar.Application.Common;

/// <summary>O banco guarda UTC; "o dia" do lava-jato é sempre o dia em Brasília.</summary>
public static class FusoBrasilia
{
    private static readonly TimeZoneInfo Fuso = TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo");

    public static DateOnly DataDe(DateTimeOffset instante) =>
        DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(instante, Fuso).DateTime);

    /// <summary>Intervalo UTC [início, fim) que corresponde ao dia inteiro em Brasília.</summary>
    public static (DateTimeOffset Inicio, DateTimeOffset Fim) IntervaloDoDia(DateOnly data)
    {
        var inicioLocal = data.ToDateTime(TimeOnly.MinValue);
        var inicio = new DateTimeOffset(TimeZoneInfo.ConvertTimeToUtc(inicioLocal, Fuso), TimeSpan.Zero);
        var fim = new DateTimeOffset(TimeZoneInfo.ConvertTimeToUtc(inicioLocal.AddDays(1), Fuso), TimeSpan.Zero);
        return (inicio, fim);
    }

    public static TimeOnly HoraDe(DateTimeOffset instante) =>
        TimeOnly.FromDateTime(TimeZoneInfo.ConvertTime(instante, Fuso).DateTime);

    /// <summary>O instante UTC em que o relógio de Brasília marca <paramref name="hora"/> no dia <paramref name="data"/>.</summary>
    public static DateTimeOffset ParaUtc(DateOnly data, TimeOnly hora) =>
        new(TimeZoneInfo.ConvertTimeToUtc(data.ToDateTime(hora), Fuso), TimeSpan.Zero);
}
