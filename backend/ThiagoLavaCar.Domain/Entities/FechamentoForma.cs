using ThiagoLavaCar.Domain.Common;
using ThiagoLavaCar.Domain.Enums;

namespace ThiagoLavaCar.Domain.Entities;

/// <summary>Valor calculado (soma dos brutos) e conferido de uma forma de pagamento no fechamento.</summary>
public sealed class FechamentoForma : Entidade
{
    private FechamentoForma() { }

    public int FechamentoId { get; private set; }
    public FormaPagamento FormaPagamento { get; private set; }
    public decimal ValorCalculado { get; private set; }
    public decimal ValorConferido { get; private set; }

    public bool TemDiferenca => ValorCalculado != ValorConferido;

    internal static FechamentoForma Criar(FormaPagamento forma, decimal calculado, decimal conferido) =>
        new() { FormaPagamento = forma, ValorCalculado = Dinheiro.Arredondar(calculado), ValorConferido = Dinheiro.Arredondar(conferido) };
}
