using ThiagoLavaCar.Domain.Common;
using ThiagoLavaCar.Domain.Exceptions;

namespace ThiagoLavaCar.Domain.Entities;

/// <summary>Diária de um lavador por dia trabalhado; base do rateio da mão de obra por ordem de serviço.</summary>
public sealed class Diaria : Entidade
{
    private Diaria() { }

    public int LavadorId { get; private set; }
    public DateOnly Data { get; private set; }
    public decimal Valor { get; private set; }
    public bool Paga { get; private set; }

    public static Diaria Criar(int lavadorId, DateOnly data, decimal valor)
    {
        if (valor <= 0)
        {
            throw new RegraInvalidaException("O valor da diária deve ser maior que zero.");
        }

        return new Diaria { LavadorId = lavadorId, Data = data, Valor = Dinheiro.Arredondar(valor) };
    }

    public void MarcarPaga() => Paga = true;

    public void Reajustar(decimal valor)
    {
        if (valor <= 0)
        {
            throw new RegraInvalidaException("O valor da diária deve ser maior que zero.");
        }

        Valor = Dinheiro.Arredondar(valor);
    }
}
