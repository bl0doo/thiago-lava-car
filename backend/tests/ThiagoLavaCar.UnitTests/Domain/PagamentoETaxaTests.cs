using ThiagoLavaCar.Domain.Entities;
using ThiagoLavaCar.Domain.Enums;
using ThiagoLavaCar.Domain.Exceptions;

namespace ThiagoLavaCar.UnitTests.Domain;

public class PagamentoETaxaTests
{
    private static readonly DateOnly Dia = new(2026, 10, 20);

    [Fact] // T33
    public void Credito_de_100_com_taxa_de_349_percentual_da_taxa_349_e_liquido_9651()
    {
        var taxa = TaxaPagamento.Criar(FormaPagamento.Credito, 3.49m, 0m);

        var pagamento = Pagamento.Criar(88, 1, 100m, taxa, Dia, Fabrica.Agora);

        Assert.Equal(3.49m, pagamento.ValorTaxa);
        Assert.Equal(96.51m, pagamento.ValorLiquido);
        Assert.Equal(3.49m, pagamento.PercentualTaxa);
    }

    [Fact]
    public void Taxa_arredonda_meio_para_cima_em_duas_casas()
    {
        var taxa = TaxaPagamento.Criar(FormaPagamento.Debito, 1.5m, 0m);

        // 33,33 × 1,5% = 0,49995 → 0,50
        Assert.Equal(0.50m, taxa.CalcularTaxa(33.33m));
        // 0,50 × 1% = 0,005 → 0,01 (meio para cima, e não para o par mais próximo)
        Assert.Equal(0.01m, TaxaPagamento.Criar(FormaPagamento.Debito, 1m, 0m).CalcularTaxa(0.50m));
    }

    [Fact]
    public void Taxa_soma_o_valor_fixo_da_forma()
    {
        var taxa = TaxaPagamento.Criar(FormaPagamento.Credito, 2m, 0.50m);

        Assert.Equal(2.50m, taxa.CalcularTaxa(100m));
    }

    [Fact]
    public void Pix_sem_taxa_tem_liquido_igual_ao_bruto()
    {
        var pagamento = Pagamento.Criar(88, 1, 80m, TaxaPagamento.Criar(FormaPagamento.Pix, 0m, 0m), Dia, Fabrica.Agora);

        Assert.Equal(80m, pagamento.ValorLiquido);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void Pagamento_com_valor_nao_positivo_falha(double valor)
    {
        var taxa = TaxaPagamento.Criar(FormaPagamento.Pix, 0m, 0m);

        Assert.Throws<RegraInvalidaException>(() => Pagamento.Criar(88, 1, (decimal)valor, taxa, Dia, Fabrica.Agora));
    }

    [Theory]
    [InlineData(-1, 0)]
    [InlineData(101, 0)]
    [InlineData(1, -0.01)]
    public void Taxa_fora_dos_limites_falha(double percentual, double fixo)
    {
        Assert.Throws<RegraInvalidaException>(() => TaxaPagamento.Criar(FormaPagamento.Pix, (decimal)percentual, (decimal)fixo));
    }
}
