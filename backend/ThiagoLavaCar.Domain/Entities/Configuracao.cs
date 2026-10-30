using ThiagoLavaCar.Domain.Common;
using ThiagoLavaCar.Domain.Exceptions;

namespace ThiagoLavaCar.Domain.Entities;

/// <summary>Linha única (id = 1) com os dados do lava-jato, o horário de funcionamento e as diárias padrão.</summary>
public sealed class Configuracao : Entidade
{
    public const int IdUnico = 1;

    private Configuracao() { }

    public string NomeLavaJato { get; private set; } = null!;
    public string? Telefone { get; private set; }
    public TimeOnly Abertura { get; private set; }
    public TimeOnly Fechamento { get; private set; }

    /// <summary>Dias da semana abertos, de 0 (domingo) a 6 (sábado), separados por vírgula. Ex.: "1,2,3,4,5,6".</summary>
    public string DiasFuncionamento { get; private set; } = null!;
    public decimal DiariaSemana { get; private set; }
    public decimal DiariaSabado { get; private set; }
    public int IntervaloAgendaMin { get; private set; }

    public static Configuracao Padrao(string nomeLavaJato) =>
        Criar(nomeLavaJato, null, new TimeOnly(8, 0), new TimeOnly(18, 0), "1,2,3,4,5,6", 100m, 120m, 30);

    public static Configuracao Criar(
        string nomeLavaJato, string? telefone, TimeOnly abertura, TimeOnly fechamento, string diasFuncionamento,
        decimal diariaSemana, decimal diariaSabado, int intervaloAgendaMin)
    {
        var c = new Configuracao();
        c.Atualizar(nomeLavaJato, telefone, abertura, fechamento, diasFuncionamento, diariaSemana, diariaSabado, intervaloAgendaMin);
        return c;
    }

    public void Atualizar(
        string nomeLavaJato, string? telefone, TimeOnly abertura, TimeOnly fechamento, string diasFuncionamento,
        decimal diariaSemana, decimal diariaSabado, int intervaloAgendaMin)
    {
        if (string.IsNullOrWhiteSpace(nomeLavaJato))
        {
            throw new RegraInvalidaException("Informe o nome do lava-jato.");
        }

        if (fechamento <= abertura)
        {
            throw new RegraInvalidaException("O horário de fechamento deve ser depois da abertura.");
        }

        var dias = diasFuncionamento.Split(',', StringSplitOptions.TrimEntries);
        if (dias.Length == 0 || dias.Any(d => !int.TryParse(d, out var n) || n is < 0 or > 6) || dias.Distinct().Count() != dias.Length)
        {
            throw new RegraInvalidaException("Dias de funcionamento inválidos: use números de 0 (domingo) a 6 (sábado), sem repetir.");
        }

        if (diariaSemana <= 0 || diariaSabado <= 0)
        {
            throw new RegraInvalidaException("As diárias padrão devem ser maiores que zero.");
        }

        if (intervaloAgendaMin is < 5 or > 240)
        {
            throw new RegraInvalidaException("O intervalo da agenda deve ficar entre 5 e 240 minutos.");
        }

        Id = IdUnico;
        NomeLavaJato = nomeLavaJato.Trim();
        Telefone = telefone;
        Abertura = abertura;
        Fechamento = fechamento;
        DiasFuncionamento = string.Join(',', dias);
        DiariaSemana = Dinheiro.Arredondar(diariaSemana);
        DiariaSabado = Dinheiro.Arredondar(diariaSabado);
        IntervaloAgendaMin = intervaloAgendaMin;
    }

    /// <summary>Diária sugerida para o dia: a de sábado aos sábados, a da semana nos demais.</summary>
    public decimal DiariaSugerida(DateOnly data) => data.DayOfWeek == DayOfWeek.Saturday ? DiariaSabado : DiariaSemana;
}
