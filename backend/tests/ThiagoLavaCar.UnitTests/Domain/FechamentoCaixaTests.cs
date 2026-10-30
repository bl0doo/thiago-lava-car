using ThiagoLavaCar.Domain.Entities;
using ThiagoLavaCar.Domain.Enums;
using ThiagoLavaCar.Domain.Exceptions;

namespace ThiagoLavaCar.UnitTests.Domain;

public sealed class FechamentoCaixaTests
{
    private static readonly DateOnly Hoje = new(2026, 10, 20);

    private static FechamentoCaixa.Linha[] Conferido(decimal dinheiro) =>
    [
        new(FormaPagamento.Pix, 360m, 360m),
        new(FormaPagamento.Credito, 200m, 200m),
        new(FormaPagamento.Dinheiro, 80m, dinheiro),
    ];

    [Fact] // T38
    public void Sem_diferenca_nao_exige_justificativa()
    {
        var f = FechamentoCaixa.Fechar(Hoje, Hoje, 1, Conferido(80m), 633.02m, null, Fabrica.Agora);

        Assert.Equal(0m, f.Diferenca);
        Assert.Equal(640m, f.TotalBruto);
        Assert.Equal(633.02m, f.TotalLiquido);
        Assert.Null(f.Justificativa);
    }

    [Fact] // T39
    public void Diferenca_sem_justificativa_e_recusada() =>
        Assert.Throws<RegraInvalidaException>(() => FechamentoCaixa.Fechar(Hoje, Hoje, 1, Conferido(70m), 633.02m, null, Fabrica.Agora));

    [Fact]
    public void Justificativa_curta_demais_e_recusada() =>
        Assert.Throws<RegraInvalidaException>(() => FechamentoCaixa.Fechar(Hoje, Hoje, 1, Conferido(70m), 633.02m, "troco", Fabrica.Agora));

    [Fact] // T40
    public void Diferenca_de_menos_10_com_justificativa_e_aceita()
    {
        var f = FechamentoCaixa.Fechar(Hoje, Hoje, 1, Conferido(70m), 633.02m, "Troco de R$ 10 dado a mais pela manhã", Fabrica.Agora);

        Assert.Equal(-10m, f.Diferenca);
        Assert.Equal(630m, f.TotalConferido);
        Assert.Equal(3, f.Formas.Count);
    }

    [Fact]
    public void Data_futura_e_recusada() =>
        Assert.Throws<RegraInvalidaException>(() => FechamentoCaixa.Fechar(Hoje.AddDays(1), Hoje, 1, Conferido(80m), 633.02m, null, Fabrica.Agora));

    [Fact]
    public void Forma_repetida_e_valor_negativo_sao_recusados()
    {
        Assert.Throws<RegraInvalidaException>(() => FechamentoCaixa.Fechar(
            Hoje, Hoje, 1, [new(FormaPagamento.Pix, 10m, 10m), new(FormaPagamento.Pix, 5m, 5m)], 15m, null, Fabrica.Agora));
        Assert.Throws<RegraInvalidaException>(() => FechamentoCaixa.Fechar(
            Hoje, Hoje, 1, [new(FormaPagamento.Pix, 10m, -1m)], 10m, "x conferido errado", Fabrica.Agora));
    }
}
