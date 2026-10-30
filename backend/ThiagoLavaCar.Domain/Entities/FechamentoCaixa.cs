using ThiagoLavaCar.Domain.Common;
using ThiagoLavaCar.Domain.Enums;
using ThiagoLavaCar.Domain.Exceptions;

namespace ThiagoLavaCar.Domain.Entities;

/// <summary>Fechamento do caixa de um dia: totais calculados pelo sistema, valores conferidos pelo dono e a diferença.</summary>
public sealed class FechamentoCaixa : Entidade
{
    public const int JustificativaMin = 10;
    public const int JustificativaMax = 500;

    private readonly List<FechamentoForma> _formas = [];

    private FechamentoCaixa() { }

    public DateOnly Data { get; private set; }
    public decimal TotalBruto { get; private set; }
    public decimal TotalLiquido { get; private set; }
    public decimal TotalConferido { get; private set; }
    public decimal Diferenca { get; private set; }
    public string? Justificativa { get; private set; }
    public int FechadoPorId { get; private set; }
    public DateTimeOffset FechadoEm { get; private set; }

    public IReadOnlyCollection<FechamentoForma> Formas => _formas;

    /// <summary>Linha de conferência de uma forma de pagamento: o que o sistema calculou (bruto) e o que o dono contou.</summary>
    public readonly record struct Linha(FormaPagamento Forma, decimal Calculado, decimal Conferido);

    public static FechamentoCaixa Fechar(
        DateOnly data, DateOnly hoje, int fechadoPorId, IReadOnlyCollection<Linha> linhas, decimal totalLiquido, string? justificativa, DateTimeOffset agora)
    {
        if (data > hoje)
        {
            throw new RegraInvalidaException("Não é possível fechar o caixa de uma data futura.");
        }

        if (linhas.Count == 0)
        {
            throw new RegraInvalidaException("Informe a conferência de ao menos uma forma de pagamento.");
        }

        if (linhas.Select(l => l.Forma).Distinct().Count() != linhas.Count)
        {
            throw new RegraInvalidaException("Informe cada forma de pagamento uma única vez.");
        }

        if (linhas.Any(l => l.Conferido < 0))
        {
            throw new RegraInvalidaException("Os valores conferidos não podem ser negativos.");
        }

        var formas = linhas.Select(l => FechamentoForma.Criar(l.Forma, l.Calculado, l.Conferido)).ToList();
        var totalBruto = Dinheiro.Arredondar(formas.Sum(f => f.ValorCalculado));
        var totalConferido = Dinheiro.Arredondar(formas.Sum(f => f.ValorConferido));
        var temDiferenca = formas.Any(f => f.TemDiferenca);

        var texto = justificativa?.Trim();
        if (temDiferenca && (texto is null || texto.Length < JustificativaMin || texto.Length > JustificativaMax))
        {
            throw new RegraInvalidaException("Informe a justificativa da diferença (de 10 a 500 caracteres).");
        }

        var fechamento = new FechamentoCaixa
        {
            Data = data,
            TotalBruto = totalBruto,
            TotalLiquido = Dinheiro.Arredondar(totalLiquido),
            TotalConferido = totalConferido,
            Diferenca = totalConferido - totalBruto,
            Justificativa = temDiferenca ? texto : null,
            FechadoPorId = fechadoPorId,
            FechadoEm = agora,
        };
        fechamento._formas.AddRange(formas);
        return fechamento;
    }
}
