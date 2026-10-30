namespace ThiagoLavaCar.Application.DTOs;

public sealed record PaginaResponse<T>(IReadOnlyList<T> Itens, int Total, int Pagina, int Tamanho);

public sealed record CriarClienteRequest(string Nome, string Telefone, string? Email, string CanalPreferido, bool ConsentimentoLgpd);

public sealed record AtualizarClienteRequest(string Nome, string Telefone, string? Email, string CanalPreferido);

public sealed record VeiculoResumoResponse(int Id, string Placa, string Modelo, string Tipo, string? Cor);

public sealed record ClienteResponse(int Id, string Nome, string Telefone, string? Email, string CanalPreferido, bool ConsentimentoLgpd);

public sealed record ClienteDetalheResponse(
    int Id, string Nome, string Telefone, string? Email, string CanalPreferido, bool ConsentimentoLgpd,
    IReadOnlyList<VeiculoResumoResponse> Veiculos);
