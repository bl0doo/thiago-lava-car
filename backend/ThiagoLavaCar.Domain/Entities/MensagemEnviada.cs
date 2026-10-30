using ThiagoLavaCar.Domain.Common;
using ThiagoLavaCar.Domain.Enums;
using ThiagoLavaCar.Domain.Exceptions;

namespace ThiagoLavaCar.Domain.Entities;

/// <summary>Registro de uma mensagem gerada e enviada ao cliente; guarda o texto final, já com os dados da OS.</summary>
public sealed class MensagemEnviada : Entidade
{
    private MensagemEnviada() { }

    public int OrdemServicoId { get; private set; }
    public int EnviadaPorId { get; private set; }
    public TipoMensagem Tipo { get; private set; }
    public string Texto { get; private set; } = null!;
    public DateTimeOffset EnviadaEm { get; private set; }

    public static MensagemEnviada Registrar(int ordemServicoId, int enviadaPorId, TipoMensagem tipo, string texto, DateTimeOffset agora)
    {
        if (string.IsNullOrWhiteSpace(texto))
        {
            throw new RegraInvalidaException("O texto da mensagem enviada não pode ficar vazio.");
        }

        return new MensagemEnviada
        {
            OrdemServicoId = ordemServicoId,
            EnviadaPorId = enviadaPorId,
            Tipo = tipo,
            Texto = texto,
            EnviadaEm = agora,
        };
    }
}
