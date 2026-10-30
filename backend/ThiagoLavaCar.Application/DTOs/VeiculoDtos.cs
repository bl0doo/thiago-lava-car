namespace ThiagoLavaCar.Application.DTOs;

/// <summary>
/// <c>ClienteId</c> é obrigatório para o Dono; para o Cliente é ignorado e vem do token.
/// Informe <c>ModeloVeiculoId</c> (do autocompletar) ou <c>ModeloDescricao</c> e <c>TipoVeiculo</c>; com o id, descrição e tipo vêm do modelo, e o que for enviado prevalece.
/// </summary>
public sealed record CriarVeiculoRequest(int? ClienteId, string Placa, string? ModeloDescricao, string? TipoVeiculo, string? Cor, int? ModeloVeiculoId = null);

public sealed record ModeloVeiculoResponse(int Id, string Marca, string Nome, string Descricao, string TipoVeiculo);

public sealed record AtualizarVeiculoRequest(string Placa, string ModeloDescricao, string TipoVeiculo, string? Cor);

public sealed record VeiculoResponse(int Id, int ClienteId, string Placa, string Modelo, string Tipo, string? Cor);

/// <summary>Uma passagem do carro pelo lava-jato. Valores só para Dono e Cliente.</summary>
public sealed record FichaOrdemResponse(
    int Id, DateTimeOffset Data, string Status, IReadOnlyList<string> Servicos, decimal ValorTotal, int? DuracaoRealMin);

/// <summary>Ficha do carro: dados do veículo e histórico de serviços (metáfora central do projeto).</summary>
public sealed record FichaVeiculoResponse(VeiculoResponse Veiculo, int TotalServicos, IReadOnlyList<FichaOrdemResponse> Historico);
