using ThiagoLavaCar.Domain.Common;
using ThiagoLavaCar.Domain.Exceptions;

namespace ThiagoLavaCar.Domain.Entities;

/// <summary>Pró-labore e retiradas pessoais do dono: não entram no custo do negócio nem no lucro por serviço.</summary>
public sealed class RetiradaDono : Entidade
{
    private RetiradaDono() { }

    public int RegistradoPorId { get; private set; }
    public decimal Valor { get; private set; }
    public DateOnly Data { get; private set; }
    public string? Observacao { get; private set; }

    public static RetiradaDono Criar(int registradoPorId, decimal valor, DateOnly data, DateOnly hoje, string? observacao)
    {
        Validacao.Lancamento("retirada", valor, data, hoje);
        if (observacao is { Length: > 200 })
        {
            throw new RegraInvalidaException("A observação deve ter no máximo 200 caracteres.");
        }

        return new RetiradaDono { RegistradoPorId = registradoPorId, Valor = Dinheiro.Arredondar(valor), Data = data, Observacao = observacao };
    }
}
