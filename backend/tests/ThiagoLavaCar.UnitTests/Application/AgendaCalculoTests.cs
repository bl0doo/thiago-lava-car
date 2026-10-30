using ThiagoLavaCar.Application.Abstractions;
using ThiagoLavaCar.Application.Common;
using ThiagoLavaCar.Application.Services;
using ThiagoLavaCar.Domain.Entities;

namespace ThiagoLavaCar.UnitTests.Application;

public class AgendaCalculoTests
{
    private static readonly Configuracao Padrao = Configuracao.Padrao("Thiago Lava Car");
    private static readonly DateOnly Quarta = new(2026, 10, 21);
    private static readonly DateTimeOffset MadrugadaDeQuarta = FusoBrasilia.ParaUtc(Quarta, new TimeOnly(5, 0));

    private static string[] Horas(IEnumerable<TimeOnly> horas) => horas.Select(h => h.ToString("HH:mm", System.Globalization.CultureInfo.InvariantCulture)).ToArray();

    [Fact]
    public void Grade_vai_de_30_em_30_e_o_ultimo_horario_ainda_termina_no_fechamento()
    {
        var grade = Horas(AgendaCalculo.Grade(Padrao, 90));

        Assert.Equal("08:00", grade[0]);
        Assert.Equal("08:30", grade[1]);
        Assert.Equal("16:30", grade[^1]);
    }

    [Fact]
    public void Domingo_esta_fechado()
    {
        Assert.False(AgendaCalculo.DiaAberto(Padrao, new DateOnly(2026, 10, 25)));
        Assert.True(AgendaCalculo.DiaAberto(Padrao, Quarta));
    }

    [Fact]
    public void Horario_lota_quando_os_agendamentos_sobrepostos_igualam_os_lavadores()
    {
        var ocupacao = new[] { new AgendaOcupada(FusoBrasilia.ParaUtc(Quarta, new TimeOnly(9, 0)), 90) };

        var comDoisLavadores = Horas(AgendaCalculo.HorariosLivres(Padrao, Quarta, 60, ocupacao, 2, MadrugadaDeQuarta));
        var comUmLavador = Horas(AgendaCalculo.HorariosLivres(Padrao, Quarta, 60, ocupacao, 1, MadrugadaDeQuarta));

        Assert.Contains("09:30", comDoisLavadores);
        Assert.DoesNotContain("09:00", comUmLavador);
        Assert.DoesNotContain("08:30", comUmLavador); // 08:30 + 60 min invade o agendamento das 09:00
        Assert.Contains("08:00", comUmLavador);
        Assert.Contains("10:30", comUmLavador); // o agendamento das 9h termina às 10:30
        Assert.DoesNotContain("10:00", comUmLavador);
    }

    [Fact]
    public void Horarios_com_menos_de_uma_hora_de_antecedencia_somem()
    {
        var agora = FusoBrasilia.ParaUtc(Quarta, new TimeOnly(9, 10));

        var horas = Horas(AgendaCalculo.HorariosLivres(Padrao, Quarta, 60, [], 1, agora));

        Assert.DoesNotContain("10:00", horas);
        Assert.Equal("10:30", horas[0]);
    }

    [Fact]
    public void Sem_lavadores_ativos_nao_ha_horario()
    {
        Assert.Empty(AgendaCalculo.HorariosLivres(Padrao, Quarta, 60, [], 0, MadrugadaDeQuarta));
    }
}
