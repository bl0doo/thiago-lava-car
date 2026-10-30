using ThiagoLavaCar.Domain.Common;

namespace ThiagoLavaCar.Domain.Entities;

/// <summary>Token de renovação da sessão. Só o hash SHA-256 é guardado; o valor bruto vai ao cliente uma vez.</summary>
public sealed class RefreshToken : Entidade
{
    private RefreshToken() { }

    public int UsuarioId { get; private set; }
    public string TokenHash { get; private set; } = null!;
    public DateTimeOffset CriadoEm { get; private set; }
    public DateTimeOffset ExpiraEm { get; private set; }
    public DateTimeOffset? RevogadoEm { get; private set; }

    public static RefreshToken Criar(int usuarioId, string tokenHash, DateTimeOffset agora, TimeSpan validade) =>
        new() { UsuarioId = usuarioId, TokenHash = tokenHash, CriadoEm = agora, ExpiraEm = agora + validade };

    public bool EstaAtivo(DateTimeOffset agora) => RevogadoEm is null && ExpiraEm > agora;

    public bool FoiRevogado => RevogadoEm is not null;

    public void Revogar(DateTimeOffset agora) => RevogadoEm ??= agora;
}
