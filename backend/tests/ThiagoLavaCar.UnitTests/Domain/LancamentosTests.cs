using ThiagoLavaCar.Domain.Entities;
using ThiagoLavaCar.Domain.Enums;
using ThiagoLavaCar.Domain.Exceptions;

namespace ThiagoLavaCar.UnitTests.Domain;

public sealed class LancamentosTests
{
    private static readonly DateOnly Hoje = new(2026, 10, 20);

    [Fact]
    public void Despesa_valida_arredonda_o_valor_e_apara_a_descricao()
    {
        var d = Despesa.Criar(1, "  Shampoo  ", CategoriaDespesa.Insumos, 45.456m, Hoje, Hoje, Fabrica.Agora);

        Assert.Equal("Shampoo", d.Descricao);
        Assert.Equal(45.46m, d.Valor);
    }

    [Theory]
    [InlineData("", 10)]
    [InlineData("Água", 0)]
    [InlineData("Água", -5)]
    public void Despesa_sem_descricao_ou_com_valor_nao_positivo_e_recusada(string descricao, decimal valor) =>
        Assert.Throws<RegraInvalidaException>(() =>
            Despesa.Criar(1, descricao, CategoriaDespesa.Agua, valor, Hoje, Hoje, Fabrica.Agora));

    [Fact]
    public void Despesa_com_data_futura_e_recusada() =>
        Assert.Throws<RegraInvalidaException>(() =>
            Despesa.Criar(1, "Aluguel", CategoriaDespesa.Aluguel, 100m, Hoje.AddDays(1), Hoje, Fabrica.Agora));

    [Fact]
    public void Retirada_com_valor_zero_e_recusada() =>
        Assert.Throws<RegraInvalidaException>(() => RetiradaDono.Criar(1, 0m, Hoje, Hoje, null));

    [Fact]
    public void Diaria_nasce_nao_paga_e_pode_ser_marcada_como_paga()
    {
        var d = Diaria.Criar(2, Hoje, 100m);
        Assert.False(d.Paga);

        d.MarcarPaga();

        Assert.True(d.Paga);
    }
}
