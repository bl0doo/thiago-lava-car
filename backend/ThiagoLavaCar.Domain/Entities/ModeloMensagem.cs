using ThiagoLavaCar.Domain.Common;
using ThiagoLavaCar.Domain.Enums;
using ThiagoLavaCar.Domain.Exceptions;

namespace ThiagoLavaCar.Domain.Entities;

/// <summary>Texto personalizável das mensagens de lavagem iniciada e concluída (um modelo por tipo).</summary>
public sealed class ModeloMensagem : Entidade
{
    public const int TextoMax = 500;

    private ModeloMensagem() { }

    public TipoMensagem Tipo { get; private set; }
    public string Texto { get; private set; } = null!;
    public DateTimeOffset AtualizadoEm { get; private set; }

    public static ModeloMensagem Criar(TipoMensagem tipo, string texto, DateTimeOffset agora)
    {
        var modelo = new ModeloMensagem { Tipo = tipo };
        modelo.Editar(texto, agora);
        return modelo;
    }

    public void Editar(string texto, DateTimeOffset agora)
    {
        if (string.IsNullOrWhiteSpace(texto) || texto.Length > TextoMax)
        {
            throw new RegraInvalidaException($"O texto da mensagem deve ter de 1 a {TextoMax} caracteres.");
        }

        Texto = texto.Trim();
        AtualizadoEm = agora;
    }
}
