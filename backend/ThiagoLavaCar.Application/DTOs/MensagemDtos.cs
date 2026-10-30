namespace ThiagoLavaCar.Application.DTOs;

/// <summary>Texto pronto para enviar ao cliente, o canal preferido dele e o link do WhatsApp já com o texto.</summary>
public sealed record MensagemGeradaResponse(string Texto, string Canal, string LinkWhatsApp);

public sealed record MensagemEnviadaResponse(int Id, string Tipo, string Texto, DateTimeOffset EnviadaEm);
