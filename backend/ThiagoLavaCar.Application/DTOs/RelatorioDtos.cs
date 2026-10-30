namespace ThiagoLavaCar.Application.DTOs;

/// <summary>Uma linha do ranking: o serviço (com "com cera" quando for o caso) e o que ele rendeu no período.</summary>
public sealed record LucroPorServicoResponse(
    string Servico,
    int Quantidade,
    decimal ReceitaLiquida,
    decimal Insumos,
    decimal MaoDeObra,
    decimal Lucro,
    decimal LucroMedio,
    int TempoMedioMin);

public sealed record PontoEquilibrioResponse(
    string Mes,
    decimal Despesas,
    decimal Diarias,
    decimal Contas,
    decimal? MargemPorCarro,
    bool MargemEstimada,
    int? CarrosNecessarios,
    int CarrosFeitos,
    int? Faltam);

public sealed record FaturamentoDiaResponse(DateOnly Data, decimal Bruto, decimal Liquido, int OsPagas, decimal TicketMedio);

/// <summary>Só os dias com movimento; o ticket médio é o valor das OS pagas dividido pelo número delas.</summary>
public sealed record FaturamentoResponse(
    IReadOnlyList<FaturamentoDiaResponse> Dias, decimal TotalBruto, decimal TotalLiquido, int OsPagas, decimal TicketMedio);

public sealed record TempoPorServicoResponse(string Servico, int Quantidade, int TempoMedioMin, int EstimadoMedioMin);

public sealed record TempoPorLavadorResponse(string Lavador, int Quantidade, int TempoMedioMin);

public sealed record ProdutividadeResponse(
    IReadOnlyList<TempoPorServicoResponse> PorServico, IReadOnlyList<TempoPorLavadorResponse> PorLavador);
