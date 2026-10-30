using ThiagoLavaCar.Domain.Common;

namespace ThiagoLavaCar.Domain.Entities;

public sealed class Avaria : Entidade
{
    private Avaria() { }

    public int OrdemServicoId { get; private set; }
    public string Local { get; private set; } = null!;
    public string Tipo { get; private set; } = null!;
    public string? Descricao { get; private set; }

    public static Avaria Criar(string local, string tipo, string? descricao) =>
        new() { Local = local, Tipo = tipo, Descricao = descricao };
}
