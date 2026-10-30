using ThiagoLavaCar.Domain.Enums;
using ThiagoLavaCar.Domain.Exceptions;

namespace ThiagoLavaCar.Application.Common;

/// <summary>Os tipos de mensagem aparecem nas rotas como "lavagem-iniciada" e "lavagem-concluida".</summary>
public static class TiposMensagem
{
    public static string Slug(TipoMensagem tipo) => tipo switch
    {
        TipoMensagem.LavagemIniciada => "lavagem-iniciada",
        TipoMensagem.LavagemConcluida => "lavagem-concluida",
        _ => throw new ArgumentOutOfRangeException(nameof(tipo)),
    };

    public static TipoMensagem Parse(string? slug) => slug?.Trim().ToLowerInvariant() switch
    {
        "lavagem-iniciada" => TipoMensagem.LavagemIniciada,
        "lavagem-concluida" => TipoMensagem.LavagemConcluida,
        _ => throw new RegraInvalidaException("Tipo de mensagem inválido. Use lavagem-iniciada ou lavagem-concluida."),
    };
}
