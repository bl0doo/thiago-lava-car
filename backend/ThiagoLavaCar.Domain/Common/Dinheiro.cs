namespace ThiagoLavaCar.Domain.Common;

/// <summary>Regras de arredondamento monetário: duas casas, meio para cima.</summary>
public static class Dinheiro
{
    public static decimal Arredondar(decimal valor) => Math.Round(valor, 2, MidpointRounding.AwayFromZero);
}
