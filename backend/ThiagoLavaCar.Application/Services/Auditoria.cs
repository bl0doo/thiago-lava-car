using System.Text.Json;
using ThiagoLavaCar.Application.Abstractions;
using ThiagoLavaCar.Domain.Entities;

namespace ThiagoLavaCar.Application.Services;

/// <summary>Registra em logs_auditoria quem fez uma ação sensível; grava junto com a ação, na mesma transação do SaveChanges.</summary>
public sealed class Auditoria(IAuditoriaRepository logs, IUsuarioLogado usuarioLogado, TimeProvider relogio)
{
    private static readonly JsonSerializerOptions OpcoesJson = new(JsonSerializerDefaults.Web);

    public Task RegistrarAsync(string acao, string entidade, int entidadeId, object? detalhes, CancellationToken ct) =>
        logs.AdicionarAsync(
            LogAuditoria.Registrar(usuarioLogado.Id, acao, entidade, entidadeId,
                detalhes is null ? null : JsonSerializer.Serialize(detalhes, OpcoesJson), relogio.GetUtcNow()),
            ct);
}
