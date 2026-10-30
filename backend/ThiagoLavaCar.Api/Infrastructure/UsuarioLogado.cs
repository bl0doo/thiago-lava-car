using ThiagoLavaCar.Application.Abstractions;
using ThiagoLavaCar.Domain.Enums;
using ThiagoLavaCar.Infrastructure.Security;

namespace ThiagoLavaCar.Api.Infrastructure;

/// <summary>Lê o usuário dos claims do JWT já validado. Só deve ser usado em rotas autenticadas.</summary>
internal sealed class UsuarioLogado(IHttpContextAccessor acessor) : IUsuarioLogado
{
    private System.Security.Claims.ClaimsPrincipal Principal =>
        acessor.HttpContext?.User ?? throw new InvalidOperationException("Sem contexto HTTP.");

    public int Id => int.Parse(
        Principal.FindFirst("sub")?.Value ?? throw new InvalidOperationException("Token sem claim sub."),
        System.Globalization.CultureInfo.InvariantCulture);

    public Perfil Perfil => Enum.Parse<Perfil>(
        Principal.FindFirst(ClaimsNames.Role)?.Value ?? throw new InvalidOperationException("Token sem claim role."));
}
