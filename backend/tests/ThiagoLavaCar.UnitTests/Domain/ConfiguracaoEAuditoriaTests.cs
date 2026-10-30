using ThiagoLavaCar.Domain.Entities;
using ThiagoLavaCar.Domain.Exceptions;

namespace ThiagoLavaCar.UnitTests.Domain;

public sealed class ConfiguracaoEAuditoriaTests
{
    [Fact]
    public void Padrao_segue_a_Entrega_2_e_sugere_diaria_de_R120_no_sabado()
    {
        var c = Configuracao.Padrao("Thiago Lava Car");

        Assert.Equal(Configuracao.IdUnico, c.Id);
        Assert.Equal(new TimeOnly(8, 0), c.Abertura);
        Assert.Equal(new TimeOnly(18, 0), c.Fechamento);
        Assert.Equal("1,2,3,4,5,6", c.DiasFuncionamento);
        Assert.Equal(100m, c.DiariaSugerida(new DateOnly(2026, 10, 21)));
        Assert.Equal(120m, c.DiariaSugerida(new DateOnly(2026, 10, 24)));
    }

    [Theory]
    [InlineData("")]
    [InlineData("7")]
    [InlineData("1,1")]
    [InlineData("seg")]
    public void Dias_de_funcionamento_invalidos_sao_recusados(string dias) =>
        Assert.Throws<RegraInvalidaException>(() =>
            Configuracao.Criar("Lava", null, new TimeOnly(8, 0), new TimeOnly(18, 0), dias, 100m, 120m, 30));

    [Fact]
    public void Fechamento_antes_da_abertura_e_recusado() =>
        Assert.Throws<RegraInvalidaException>(() =>
            Configuracao.Criar("Lava", null, new TimeOnly(18, 0), new TimeOnly(8, 0), "1,2", 100m, 120m, 30));

    [Fact]
    public void Auditoria_exige_acao_e_entidade() =>
        Assert.Throws<RegraInvalidaException>(() => LogAuditoria.Registrar(1, "", "pagamentos", 1, null, Fabrica.Agora));
}
