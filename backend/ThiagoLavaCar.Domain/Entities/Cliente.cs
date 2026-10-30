using ThiagoLavaCar.Domain.Common;
using ThiagoLavaCar.Domain.Enums;
using ThiagoLavaCar.Domain.Exceptions;

namespace ThiagoLavaCar.Domain.Entities;

public sealed class Cliente : Entidade
{
    private Cliente() { }

    public int? UsuarioId { get; private set; }
    public string Nome { get; private set; } = null!;
    public string Telefone { get; private set; } = null!;
    public string? Email { get; private set; }
    public CanalContato CanalPreferido { get; private set; }
    public bool ConsentimentoLgpd { get; private set; }
    public DateTimeOffset ConsentimentoEm { get; private set; }

    public static Cliente Criar(string nome, string telefone, string? email, CanalContato canal, bool consentimentoLgpd, DateTimeOffset agora, int? usuarioId = null)
    {
        if (!consentimentoLgpd)
        {
            throw new RegraInvalidaException("O consentimento LGPD é obrigatório para cadastrar o cliente.");
        }

        return new Cliente
        {
            Nome = nome,
            Telefone = telefone,
            Email = email,
            CanalPreferido = canal,
            ConsentimentoLgpd = true,
            ConsentimentoEm = agora,
            UsuarioId = usuarioId,
        };
    }

    public void Atualizar(string nome, string telefone, string? email, CanalContato canal)
    {
        Nome = nome;
        Telefone = telefone;
        Email = email;
        CanalPreferido = canal;
    }
}
