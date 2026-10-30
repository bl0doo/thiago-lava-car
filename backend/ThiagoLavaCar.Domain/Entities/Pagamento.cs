using ThiagoLavaCar.Domain.Common;
using ThiagoLavaCar.Domain.Enums;
using ThiagoLavaCar.Domain.Exceptions;

namespace ThiagoLavaCar.Domain.Entities;

/// <summary>Recebimento com a taxa vigente copiada, para que mudanças na tabela não alterem vendas antigas.</summary>
public sealed class Pagamento : Entidade
{
    private Pagamento() { }

    public int OrdemServicoId { get; private set; }
    public int RegistradoPorId { get; private set; }
    public FormaPagamento FormaPagamento { get; private set; }
    public decimal ValorBruto { get; private set; }
    public decimal PercentualTaxa { get; private set; }
    public decimal ValorTaxa { get; private set; }
    public decimal ValorLiquido { get; private set; }
    public DateOnly DataCaixa { get; private set; }
    public DateTimeOffset CriadoEm { get; private set; }
    public DateTimeOffset? EstornadoEm { get; private set; }

    public bool Estornado => EstornadoEm is not null;

    public static Pagamento Criar(
        int ordemServicoId, int registradoPorId, decimal valorBruto, TaxaPagamento taxa, DateOnly dataCaixa, DateTimeOffset agora)
    {
        if (valorBruto <= 0)
        {
            throw new RegraInvalidaException("O valor do pagamento deve ser maior que zero.");
        }

        var bruto = Dinheiro.Arredondar(valorBruto);
        var valorTaxa = taxa.CalcularTaxa(bruto);
        return new Pagamento
        {
            OrdemServicoId = ordemServicoId,
            RegistradoPorId = registradoPorId,
            FormaPagamento = taxa.FormaPagamento,
            ValorBruto = bruto,
            PercentualTaxa = taxa.Percentual,
            ValorTaxa = valorTaxa,
            ValorLiquido = bruto - valorTaxa,
            DataCaixa = dataCaixa,
            CriadoEm = agora,
        };
    }

    public void Estornar(DateTimeOffset agora)
    {
        if (Estornado)
        {
            throw new ConflitoException("Este pagamento já foi estornado.");
        }

        EstornadoEm = agora;
    }
}
