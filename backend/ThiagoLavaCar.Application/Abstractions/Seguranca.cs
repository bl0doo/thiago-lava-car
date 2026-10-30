using ThiagoLavaCar.Domain.Entities;
using ThiagoLavaCar.Domain.Enums;

namespace ThiagoLavaCar.Application.Abstractions;

public interface ISenhaHasher
{
    string Gerar(string segredo);
    bool Verificar(string hash, string segredo);

    /// <summary>Gasta o mesmo tempo de uma verificação real, para o login não revelar se o e-mail existe.</summary>
    void SimularVerificacao(string segredo);
}

public sealed record AccessToken(string Valor, DateTimeOffset ExpiraEm);

public sealed record RefreshTokenGerado(string Valor, string Hash, TimeSpan Validade);

public interface ITokenService
{
    AccessToken CriarAccessToken(Usuario usuario, int? clienteId, DateTimeOffset agora);
    /// <summary>Dono e Cliente: 7 dias, renovados a cada uso. Lavador: dura o turno (12 h).</summary>
    RefreshTokenGerado GerarRefreshToken(Perfil perfil);
    string CalcularHash(string refreshToken);
}

/// <summary>Quem está chamando, extraído do JWT validado.</summary>
public interface IUsuarioLogado
{
    int Id { get; }
    Perfil Perfil { get; }
}
