namespace ThiagoLavaCar.Api.Infrastructure;

/// <summary>Valores para <c>[Authorize(Roles = ...)]</c>; iguais aos do enum Perfil.</summary>
public static class Perfis
{
    public const string Dono = "Dono";
    public const string Lavador = "Lavador";
    public const string Cliente = "Cliente";
    public const string DonoOuLavador = Dono + "," + Lavador;
    public const string DonoOuCliente = Dono + "," + Cliente;
}
