using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using ThiagoLavaCar.Application.Abstractions;

namespace ThiagoLavaCar.Infrastructure.Security;

/// <summary>PBKDF2 com sal pelo PasswordHasher do ASP.NET Core Identity, 100 mil iterações (Entrega 2, seção 4).</summary>
internal sealed class SenhaHasher : ISenhaHasher
{
    private readonly PasswordHasher<string> _hasher = new(Options.Create(new PasswordHasherOptions { IterationCount = 100_000 }));
    private readonly string _hashFalso;

    public SenhaHasher() => _hashFalso = _hasher.HashPassword(string.Empty, Guid.NewGuid().ToString("N"));

    public string Gerar(string segredo) => _hasher.HashPassword(string.Empty, segredo);

    public bool Verificar(string hash, string segredo) =>
        _hasher.VerifyHashedPassword(string.Empty, hash, segredo) != PasswordVerificationResult.Failed;

    public void SimularVerificacao(string segredo) => _hasher.VerifyHashedPassword(string.Empty, _hashFalso, segredo);
}
