namespace ThiagoLavaCar.Application.DTOs;

/// <summary>Horários livres de um dia para um serviço, com o preço e a duração que valem para aquele tipo de veículo.</summary>
public sealed record HorariosResponse(decimal Preco, int DuracaoEstimadaMin, IReadOnlyList<string> Horarios);

public sealed record CriarAgendamentoRequest(int VeiculoId, int ServicoId, bool ComCera, DateTimeOffset Inicio);

public sealed record AgendamentoResponse(
    int Id,
    int ClienteId,
    int VeiculoId,
    string Placa,
    int ServicoId,
    string Servico,
    bool ComCera,
    DateTimeOffset Inicio,
    int DuracaoEstimadaMin,
    decimal PrecoEstimado,
    string Status,
    string? Cliente = null);

/// <summary>Vistoria de entrada do check-in; o veículo e o serviço vêm do agendamento.</summary>
public sealed record CheckInRequest(
    bool SemAvarias,
    IReadOnlyList<AvariaRequest>? Avarias,
    string? PertencesEntrada,
    string? NomeQuemRetira,
    bool AceiteCliente);

public sealed record AgendaDoDiaResponse(DateOnly Data, IReadOnlyList<AgendamentoResponse> Agendamentos);
