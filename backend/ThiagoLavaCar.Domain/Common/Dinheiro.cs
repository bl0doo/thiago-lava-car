namespace ThiagoLavaCar.Domain.Common;

/// <summary>Regras de arredondamento monetário: duas casas, meio para cima (Entrega 2, seção 3).</summary>
public static class Dinheiro
{
    public static decimal Arredondar(decimal valor) => Math.Round(valor, 2, MidpointRounding.AwayFromZero);
}
