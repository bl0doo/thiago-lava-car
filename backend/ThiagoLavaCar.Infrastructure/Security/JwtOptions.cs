using System.ComponentModel.DataAnnotations;

namespace ThiagoLavaCar.Infrastructure.Security;

public sealed class JwtOptions
{
    public const string Secao = "Jwt";

    /// <summary>Chave HS256 com pelo menos 256 bits. Vem de variável de ambiente ou User Secrets, nunca do repositório.</summary>
    [Required, MinLength(32, ErrorMessage = "A chave JWT precisa ter pelo menos 32 caracteres (256 bits).")]
    public string Key { get; set; } = string.Empty;

    [Required]
    public string Issuer { get; set; } = "thiagolavacar-api";

    [Required]
    public string Audience { get; set; } = "thiagolavacar-web";

    [Range(1, 120)]
    public int MinutosAccess { get; set; } = 30;

    /// <summary>Sessão do lavador: o cookie de renovação vale um turno e não é estendido pelo uso.</summary>
    [Range(1, 24)]
    public int HorasTurnoLavador { get; set; } = 12;

    [Range(1, 30)]
    public int DiasRefresh { get; set; } = 7;
}
