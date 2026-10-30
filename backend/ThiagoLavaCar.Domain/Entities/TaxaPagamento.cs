using ThiagoLavaCar.Domain.Common;
using ThiagoLavaCar.Domain.Enums;
using ThiagoLavaCar.Domain.Exceptions;

namespace ThiagoLavaCar.Domain.Entities;

public sealed class TaxaPagamento : Entidade
{
    private TaxaPagamento() { }

    public FormaPagamento FormaPagamento { get; private set; }
    public decimal Percentual { get; private set; }
    public decimal ValorFixo { get; private set; }

    public static TaxaPagamento Criar(FormaPagamento forma, decimal percentual, decimal valorFixo)
    {
        var taxa = new TaxaPagamento { FormaPagamento = forma };
        taxa.Alterar(percentual, valorFixo);
        return taxa;
    }

    public void Alterar(decimal percentual, decimal valorFixo)
    {
        if (percentual is < 0 or > 100 || valorFixo < 0)
        {
            throw new RegraInvalidaException("Taxa inválida: percentual entre 0 e 100 e valor fixo não negativo.");
        }

        Percentual = percentual;
        ValorFixo = valorFixo;
    }

    /// <summary>valor bruto × percentual ÷ 100 + valor fixo, em duas casas.</summary>
    public decimal CalcularTaxa(decimal valorBruto) => Dinheiro.Arredondar(valorBruto * Percentual / 100m + ValorFixo);
}
