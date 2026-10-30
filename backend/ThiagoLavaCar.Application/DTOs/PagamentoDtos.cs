namespace ThiagoLavaCar.Application.DTOs;

public sealed record RegistrarPagamentoRequest(string FormaPagamento, decimal ValorBruto);

public sealed record PagamentoRegistradoResponse(
    int Id, decimal ValorBruto, decimal PercentualTaxa, decimal ValorTaxa, decimal ValorLiquido, decimal SaldoPendente, DateOnly DataCaixa);

public sealed record PagamentoResponse(
    int Id, int OrdemServicoId, string FormaPagamento, decimal ValorBruto, decimal PercentualTaxa, decimal ValorTaxa,
    decimal ValorLiquido, DateOnly DataCaixa, DateTimeOffset CriadoEm, bool Estornado);

public sealed record EstornoRequest(string? Motivo);

public sealed record TaxaResponse(string FormaPagamento, decimal Percentual, decimal ValorFixo);

public sealed record AlterarTaxaRequest(decimal Percentual, decimal ValorFixo);
