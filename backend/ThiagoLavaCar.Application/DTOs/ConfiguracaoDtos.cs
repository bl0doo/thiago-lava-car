namespace ThiagoLavaCar.Application.DTOs;

/// <summary>Dados do lava-jato. Horários em "HH:mm" (Brasília); dias de 0 (domingo) a 6 (sábado).</summary>
public sealed record ConfiguracaoResponse(
    string NomeLavaJato,
    string? Telefone,
    string Abertura,
    string Fechamento,
    IReadOnlyList<int> DiasFuncionamento,
    decimal DiariaSemana,
    decimal DiariaSabado,
    int IntervaloAgendaMin);

public sealed record AtualizarConfiguracaoRequest(
    string NomeLavaJato,
    string? Telefone,
    string Abertura,
    string Fechamento,
    IReadOnlyList<int> DiasFuncionamento,
    decimal DiariaSemana,
    decimal DiariaSabado,
    int IntervaloAgendaMin);

/// <summary>Texto de um modelo de mensagem; <c>tipo</c> é "lavagem-iniciada" ou "lavagem-concluida".</summary>
public sealed record ModeloMensagemResponse(string Tipo, string Texto, DateTimeOffset AtualizadoEm);

public sealed record AtualizarModeloMensagemRequest(string Texto);
