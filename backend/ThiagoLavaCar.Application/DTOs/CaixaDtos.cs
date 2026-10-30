namespace ThiagoLavaCar.Application.DTOs;

public sealed record CaixaFormaResponse(string Forma, int Quantidade, decimal Bruto, decimal Taxas, decimal Liquido);

public sealed record CaixaResumoResponse(
    DateOnly Data,
    bool Fechado,
    IReadOnlyList<CaixaFormaResponse> PorForma,
    decimal TotalBruto,
    decimal TotalLiquido,
    decimal Despesas,
    decimal Diarias,
    int OsConcluidasSemPagamento);

public sealed record ValorConferidoDto(string Forma, decimal Valor);

public sealed record FecharCaixaRequest(IReadOnlyList<ValorConferidoDto> Conferido, string? Justificativa);

public sealed record ReabrirCaixaRequest(string Justificativa);

public sealed record FechamentoFormaResponse(string Forma, decimal Calculado, decimal Conferido);

public sealed record FechamentoResponse(
    DateOnly Data,
    decimal TotalBruto,
    decimal TotalLiquido,
    decimal TotalConferido,
    decimal Diferenca,
    string? Justificativa,
    IReadOnlyList<FechamentoFormaResponse> Formas,
    DateTimeOffset FechadoEm);

/// <summary>Os números do painel do Dono; <c>CarrosFaltamParaPagarContas</c> é nulo quando ainda não há margem positiva para calcular.</summary>
public sealed record PainelHojeResponse(
    decimal EntrouHoje,
    decimal LiquidoHoje,
    decimal SobrouDeVerdade,
    int CarrosNoMes,
    int? CarrosFaltamParaPagarContas,
    IReadOnlyList<string> Pendencias);
