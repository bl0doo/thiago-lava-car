using System.Globalization;
using System.Text.RegularExpressions;

namespace ThiagoLavaCar.Application.Services;

/// <summary>Dados que um modelo de mensagem pode usar.</summary>
public sealed record DadosDaMensagem(
    string NomeCliente, string Modelo, string Placa, string Servicos, int DuracaoEstimadaMin, decimal SaldoPendente, string NomeLavaJato);

/// <summary>Troca as variáveis do modelo pelos dados da OS. A frase com {valor} só aparece quando há saldo pendente.</summary>
public static partial class MensagemRenderizador
{
    public static string Renderizar(string modelo, DadosDaMensagem dados)
    {
        var texto = FraseDoValor().Replace(modelo, _ =>
            dados.SaldoPendente > 0 ? $" Valor a pagar: {Dinheiro(dados.SaldoPendente)}." : string.Empty);

        return texto
            .Replace("{nome}", PrimeiroNome(dados.NomeCliente), StringComparison.Ordinal)
            .Replace("{modelo}", dados.Modelo, StringComparison.Ordinal)
            .Replace("{placa}", dados.Placa, StringComparison.Ordinal)
            .Replace("{serviço}", dados.Servicos, StringComparison.Ordinal)
            .Replace("{duração estimada}", Duracao(dados.DuracaoEstimadaMin), StringComparison.Ordinal)
            .Replace("{nome do lava-jato}", dados.NomeLavaJato, StringComparison.Ordinal)
            .Replace("  ", " ", StringComparison.Ordinal)
            .Trim();
    }

    public static string Duracao(int minutos) => minutos < 60
        ? $"{minutos} min"
        : minutos % 60 == 0 ? $"{minutos / 60}h" : $"{minutos / 60}h{minutos % 60:00}";

    private static string Dinheiro(decimal valor) => "R$ " + valor.ToString("0.00", CultureInfo.InvariantCulture).Replace('.', ',');

    private static string PrimeiroNome(string nome) => nome.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? nome;

    [GeneratedRegex(@"[^.!?\n]*\{valor\}[^.!?\n]*[.!?]?")]
    private static partial Regex FraseDoValor();
}
