namespace ThiagoLavaCar.Application.DTOs;

public sealed record RedefinirPinRequest(string Pin);

public sealed record EditarUsuarioRequest(string Nome, string? Apelido);

public sealed record UsuarioDetalheResponse(int Id, string Nome, string? Apelido, string Perfil, bool Ativo);

public sealed record CriarLavadorRequest(string Nome, string Apelido, string Pin);
