using ThiagoLavaCar.Domain.Exceptions;

namespace ThiagoLavaCar.Domain.Entities;

/// <summary>Quem alterou o quê em ações sensíveis (ajuste de caixa, estorno, preço manual, reabertura, troca de taxa).</summary>
public sealed class LogAuditoria
{
    private LogAuditoria() { }

    public long Id { get; private set; }
    public int UsuarioId { get; private set; }
    public string Acao { get; private set; } = null!;
    public string Entidade { get; private set; } = null!;
    public int EntidadeId { get; private set; }

    /// <summary>JSON com os valores antes e depois da alteração.</summary>
    public string? Detalhes { get; private set; }
    public DateTimeOffset CriadoEm { get; private set; }

    public static LogAuditoria Registrar(int usuarioId, string acao, string entidade, int entidadeId, string? detalhes, DateTimeOffset agora)
    {
        if (string.IsNullOrWhiteSpace(acao) || string.IsNullOrWhiteSpace(entidade))
        {
            throw new RegraInvalidaException("A auditoria precisa da ação e da entidade alterada.");
        }

        return new LogAuditoria
        {
            UsuarioId = usuarioId,
            Acao = acao,
            Entidade = entidade,
            EntidadeId = entidadeId,
            Detalhes = detalhes,
            CriadoEm = agora,
        };
    }
}
