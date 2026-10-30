namespace ThiagoLavaCar.Application.DTOs;

public sealed record SalvarDespesaRequest(string Descricao, string Categoria, decimal Valor, DateOnly Data);

public sealed record DespesaResponse(int Id, string Descricao, string Categoria, decimal Valor, DateOnly Data);

public sealed record DespesasResponse(IReadOnlyList<DespesaResponse> Itens, decimal Total);

public sealed record RegistrarRetiradaRequest(decimal Valor, DateOnly Data, string? Observacao);

public sealed record RetiradaResponse(int Id, decimal Valor, DateOnly Data, string? Observacao);

public sealed record RetiradasResponse(IReadOnlyList<RetiradaResponse> Itens, decimal Total);

/// <summary>Quando <c>Valor</c> é omitido, vale a diária padrão da configuração (semana ou sábado).</summary>
public sealed record LancarDiariaRequest(int LavadorId, DateOnly Data, decimal? Valor);

public sealed record LancarDiariasDoDiaRequest(IReadOnlyList<int> LavadorIds, decimal? Valor);

public sealed record EditarDiariaRequest(decimal Valor, bool? Paga);

public sealed record DiariaResponse(int Id, int LavadorId, DateOnly Data, decimal Valor, bool Paga);

public sealed record DiariasResponse(IReadOnlyList<DiariaResponse> Itens, decimal Total);

public sealed record DiariasDoDiaResponse(IReadOnlyList<DiariaResponse> Criadas, IReadOnlyList<int> JaLancadas);
