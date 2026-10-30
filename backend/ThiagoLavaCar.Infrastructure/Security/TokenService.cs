using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using ThiagoLavaCar.Application.Abstractions;
using ThiagoLavaCar.Domain.Entities;
using ThiagoLavaCar.Domain.Enums;

namespace ThiagoLavaCar.Infrastructure.Security;

internal sealed class TokenService(IOptions<JwtOptions> opcoes) : ITokenService
{
    private readonly JwtOptions _opcoes = opcoes.Value;
    private readonly SigningCredentials _credenciais = new(
        new SymmetricSecurityKey(Encoding.UTF8.GetBytes(opcoes.Value.Key)), SecurityAlgorithms.HmacSha256);
    private readonly JsonWebTokenHandler _handler = new();

    public AccessToken CriarAccessToken(Usuario usuario, int? clienteId, DateTimeOffset agora)
    {
        var expira = agora + TimeSpan.FromMinutes(_opcoes.MinutosAccess);

        var claims = new Dictionary<string, object>
        {
            [JwtRegisteredClaimNames.Sub] = usuario.Id.ToString(System.Globalization.CultureInfo.InvariantCulture),
            [JwtRegisteredClaimNames.Name] = usuario.Apelido ?? usuario.Nome,
            [ClaimsNames.Role] = usuario.Perfil.ToString(),
            [JwtRegisteredClaimNames.Jti] = Guid.NewGuid().ToString("N"),
        };
        if (clienteId is { } id)
        {
            claims[ClaimsNames.ClienteId] = id;
        }

        var descritor = new SecurityTokenDescriptor
        {
            Issuer = _opcoes.Issuer,
            Audience = _opcoes.Audience,
            IssuedAt = agora.UtcDateTime,
            NotBefore = agora.UtcDateTime,
            Expires = expira.UtcDateTime,
            Claims = claims,
            SigningCredentials = _credenciais,
        };
        return new AccessToken(_handler.CreateToken(descritor), expira);
    }

    public RefreshTokenGerado GerarRefreshToken(Perfil perfil)
    {
        var valor = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        var validade = perfil == Perfil.Lavador ? TimeSpan.FromHours(_opcoes.HorasTurnoLavador) : TimeSpan.FromDays(_opcoes.DiasRefresh);
        return new RefreshTokenGerado(valor, CalcularHash(valor), validade);
    }

    public string CalcularHash(string refreshToken) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(refreshToken))).ToLowerInvariant();
}

/// <summary>Nomes dos claims usados no JWT (a API valida o token com os mesmos nomes).</summary>
public static class ClaimsNames
{
    public const string Role = "role";
    public const string ClienteId = "clienteId";
}
