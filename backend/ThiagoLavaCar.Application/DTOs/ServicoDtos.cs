namespace ThiagoLavaCar.Application.DTOs;

public sealed record PrecoServicoDto(string TipoVeiculo, decimal Preco, decimal? PrecoComCera);

public sealed record SalvarServicoRequest(
    string Nome, string Categoria, int TempoEstimadoMin, decimal CustoInsumos, IReadOnlyList<PrecoServicoDto> Precos);

public sealed record AlterarAtivoRequest(bool Ativo);

/// <summary><c>CustoInsumos</c> só aparece para o Dono; o Cliente vê o catálogo sem custos internos.</summary>
public sealed record ServicoResponse(
    int Id, string Nome, string Categoria, int TempoEstimadoMin, bool Ativo,
    [property: System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)] decimal? CustoInsumos,
    IReadOnlyList<PrecoServicoDto> Precos);

public sealed record PrecoCalculadoResponse(int ServicoId, string TipoVeiculo, bool ComCera, decimal Preco, int DuracaoEstimadaMin);
