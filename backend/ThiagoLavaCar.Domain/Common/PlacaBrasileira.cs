using System.Text.RegularExpressions;

namespace ThiagoLavaCar.Domain.Common;

/// <summary>Placa no padrão antigo (ABC1234) ou Mercosul (ABC1D23), gravada em maiúsculas e sem hífen.</summary>
public static partial class PlacaBrasileira
{
    public const string Padrao = "^[A-Z]{3}[0-9][A-Z0-9][0-9]{2}$";

    [GeneratedRegex(Padrao, RegexOptions.CultureInvariant)]
    private static partial Regex PadraoRegex();

    public static string Normalizar(string? valor) =>
        (valor ?? string.Empty).Replace("-", "", StringComparison.Ordinal).Replace(" ", "", StringComparison.Ordinal).ToUpperInvariant();

    public static bool EhValida(string? valor) => PadraoRegex().IsMatch(Normalizar(valor));
}
