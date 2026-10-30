using ThiagoLavaCar.Application.Abstractions;
using ThiagoLavaCar.Application.Common;
using ThiagoLavaCar.Domain.Entities;

namespace ThiagoLavaCar.Application.Services;

/// <summary>Regras da agenda, sem acesso a dados: grade de horários, antecedência mínima e lotação por lavador.</summary>
public static class AgendaCalculo
{
    public static readonly TimeSpan AntecedenciaMinima = TimeSpan.FromHours(1);
    public const int DiasDeAntecedenciaMaxima = 30;

    public static bool DiaAberto(Configuracao configuracao, DateOnly data) =>
        configuracao.DiasFuncionamento.Split(',').Contains(((int)data.DayOfWeek).ToString(System.Globalization.CultureInfo.InvariantCulture));

    /// <summary>Horários de início do dia, do funcionamento em passos do intervalo, em que o serviço ainda termina até o fechamento.</summary>
    public static IReadOnlyList<TimeOnly> Grade(Configuracao configuracao, int duracaoMin)
    {
        var grade = new List<TimeOnly>();
        var passo = TimeSpan.FromMinutes(configuracao.IntervaloAgendaMin);
        var duracao = TimeSpan.FromMinutes(duracaoMin);
        for (var inicio = configuracao.Abertura.ToTimeSpan(); inicio + duracao <= configuracao.Fechamento.ToTimeSpan(); inicio += passo)
        {
            grade.Add(TimeOnly.FromTimeSpan(inicio));
        }

        return grade;
    }

    /// <summary>Quantos agendamentos já ocupam algum lavador em qualquer ponto de [inicio, inicio + duração).</summary>
    public static int Sobrepostos(IEnumerable<AgendaOcupada> ocupacao, DateTimeOffset inicio, int duracaoMin)
    {
        var fim = inicio.AddMinutes(duracaoMin);
        return ocupacao.Count(o => o.Inicio < fim && o.Inicio.AddMinutes(o.DuracaoMin) > inicio);
    }

    public static IReadOnlyList<TimeOnly> HorariosLivres(
        Configuracao configuracao, DateOnly data, int duracaoMin, IReadOnlyCollection<AgendaOcupada> ocupacao, int lavadoresAtivos, DateTimeOffset agora)
    {
        if (!DiaAberto(configuracao, data))
        {
            return [];
        }

        return Grade(configuracao, duracaoMin)
            .Where(hora =>
            {
                var inicio = FusoBrasilia.ParaUtc(data, hora);
                return inicio >= agora + AntecedenciaMinima && Sobrepostos(ocupacao, inicio, duracaoMin) < lavadoresAtivos;
            })
            .ToList();
    }
}
