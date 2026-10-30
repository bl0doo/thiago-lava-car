namespace ThiagoLavaCar.Application.DTOs;

public sealed record LoginRequest(string Email, string Senha);

public sealed record LoginPinRequest(int UsuarioId, string Pin);

public sealed record UsuarioResponse(int Id, string Nome, string Perfil);

public sealed record LavadorResumoResponse(int Id, string Apelido);

public sealed record AuthResponse(string AccessToken, DateTimeOffset ExpiraEm, UsuarioResponse Usuario);

/// <summary>Resultado interno do login: o refresh token vai para um cookie HttpOnly, nunca para o corpo.</summary>
public sealed record SessaoIniciada(AuthResponse Resposta, string RefreshToken, DateTimeOffset RefreshExpiraEm);

public sealed record CadastroClienteRequest(
    string Nome,
    string Telefone,
    string Email,
    string Senha,
    string? CanalPreferido,
    bool ConsentimentoLgpd);
