using System.Text.Json.Serialization;

namespace ThiagoLavaCar.Application.DTOs;

/// <summary>
/// Cartão do quadro Pit Stop. Para o Lavador, <see cref="ValorTotal"/>, <see cref="Cliente"/> e
/// <see cref="Telefone"/> ficam nulos e nem aparecem no JSON.
/// </summary>
public sealed record CartaoResponse(
    int Id,
    string Status,
    string Placa,
    string Modelo,
    IReadOnlyList<string> Servicos,
    int DuracaoEstimadaMin,
    DateTimeOffset? InicioEm,
    DateTimeOffset? FimEm,
    int? DuracaoRealMin,
    bool RevisaoNecessaria,
    string? Lavador,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] decimal? ValorTotal,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? Cliente,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? Telefone);

public sealed record QuadroResponse(
    DateOnly Data,
    IReadOnlyList<CartaoResponse> Aguardando,
    IReadOnlyList<CartaoResponse> EmAndamento,
    IReadOnlyList<CartaoResponse> Concluido);

public sealed record ItemEntradaRequest(int ServicoId, bool ComCera, decimal? PrecoManual);

public sealed record AvariaRequest(string Local, string Tipo, string? Descricao);

/// <summary>Entrada do carro com a vistoria (POST /ordens-servico).</summary>
public sealed record RegistrarEntradaRequest(
    int VeiculoId,
    IReadOnlyList<ItemEntradaRequest> Itens,
    bool SemAvarias,
    IReadOnlyList<AvariaRequest>? Avarias,
    string? PertencesEntrada,
    string? NomeQuemRetira,
    bool AceiteCliente);

public sealed record OrdemCriadaResponse(
    int Id, string Status, decimal ValorTotal, int DuracaoEstimadaMin, DateTimeOffset PrevisaoConclusao);

public sealed record PertencesEncontradosRequest(string? Texto);

public sealed record EditarOrdemRequest(IReadOnlyList<ItemEntradaRequest> Itens, string? PertencesEntrada, string? NomeQuemRetira);

public sealed record EntregarRequest(string? NomeQuemRetira);

public sealed record AvaliacaoRequest(int Nota);

public sealed record ItemDetalheResponse(int ServicoId, string Nome, bool ComCera, decimal PrecoFinal);

public sealed record AvariaDetalheResponse(string Local, string Tipo, string? Descricao);

/// <summary>Cartão completo da OS para o Dono; <c>Avaliacao</c> é a nota de 1 a 5 ou nulo.</summary>
public sealed record DetalheOrdemResponse(
    CartaoResponse Cartao,
    IReadOnlyList<ItemDetalheResponse> Itens,
    string? PertencesEntrada,
    string? PertencesEncontrados,
    string? NomeQuemRetira,
    bool SemAvarias,
    IReadOnlyList<AvariaDetalheResponse> Avarias,
    int? Avaliacao,
    decimal ValorPago,
    decimal SaldoPendente,
    IReadOnlyList<PagamentoResponse> Pagamentos);
