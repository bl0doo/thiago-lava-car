using FluentValidation;
using ThiagoLavaCar.Application.DTOs;
using ThiagoLavaCar.Domain.Enums;

namespace ThiagoLavaCar.Application.Validators;

public sealed class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Informe o e-mail.")
            .EmailAddress().WithMessage("E-mail inválido.")
            .MaximumLength(254).WithMessage("E-mail muito longo.");
        RuleFor(x => x.Senha).NotEmpty().WithMessage("Informe a senha.");
    }
}

public sealed class LoginPinRequestValidator : AbstractValidator<LoginPinRequest>
{
    public LoginPinRequestValidator()
    {
        RuleFor(x => x.UsuarioId).GreaterThan(0).WithMessage("Escolha o lavador.");
        RuleFor(x => x.Pin)
            .NotEmpty().WithMessage("Informe o PIN.")
            .Matches(@"^\d{4}$").WithMessage("O PIN deve ter 4 dígitos.");
    }
}

public sealed class CadastroClienteRequestValidator : AbstractValidator<CadastroClienteRequest>
{
    public CadastroClienteRequestValidator()
    {
        RuleFor(x => x.Nome)
            .NotEmpty().WithMessage("Informe o nome.")
            .Length(2, 100).WithMessage("O nome deve ter de 2 a 100 caracteres.");
        RuleFor(x => x.Telefone)
            .NotEmpty().WithMessage("Informe o telefone.")
            .Must(t => Telefones.Normalizar(t) is { Length: 10 or 11 }).WithMessage("Telefone inválido. Use DDD + número (10 ou 11 dígitos).");
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Informe o e-mail.")
            .EmailAddress().WithMessage("E-mail inválido.")
            .MaximumLength(254).WithMessage("E-mail muito longo.");
        RuleFor(x => x.Senha).SenhaForte();
        RuleFor(x => x.CanalPreferido)
            .Must(c => c is null || Enum.TryParse<CanalContato>(c, ignoreCase: true, out _) && !int.TryParse(c, out _))
            .WithMessage("Canal inválido. Use WhatsApp, Telefone ou Email.");
        RuleFor(x => x.ConsentimentoLgpd).Equal(true).WithMessage("Para criar a conta, aceite o uso dos seus dados (LGPD).");
    }
}

internal static class PinRegras
{
    private static readonly string[] Obvios = ["0000", "1111", "2222", "3333", "4444", "5555", "6666", "7777", "8888", "9999", "1234", "4321", "0123", "3210"];

    /// <summary>PIN de 4 dígitos que não seja uma sequência óbvia.</summary>
    public static IRuleBuilderOptions<T, string> PinValido<T>(this IRuleBuilder<T, string> regra) =>
        regra.NotEmpty().WithMessage("Informe o PIN.")
            .Matches(@"^\d{4}$").WithMessage("O PIN deve ter 4 dígitos.")
            .Must(p => !Obvios.Contains(p)).WithMessage("PIN fácil demais. Evite sequências como 1234 ou 0000.");
}

public sealed class RedefinirPinRequestValidator : AbstractValidator<RedefinirPinRequest>
{
    public RedefinirPinRequestValidator()
    {
        RuleFor(x => x.Pin).PinValido();
    }
}

public sealed class CriarLavadorRequestValidator : AbstractValidator<CriarLavadorRequest>
{
    public CriarLavadorRequestValidator()
    {
        RuleFor(x => x.Nome).NotEmpty().WithMessage("Informe o nome.").Length(2, 100).WithMessage("O nome deve ter de 2 a 100 caracteres.");
        RuleFor(x => x.Apelido).NotEmpty().WithMessage("Informe o apelido.").MaximumLength(30).WithMessage("O apelido deve ter até 30 caracteres.");
        RuleFor(x => x.Pin).PinValido();
    }
}

public sealed class EditarUsuarioRequestValidator : AbstractValidator<EditarUsuarioRequest>
{
    public EditarUsuarioRequestValidator()
    {
        RuleFor(x => x.Nome).NotEmpty().WithMessage("Informe o nome.").Length(2, 100).WithMessage("O nome deve ter de 2 a 100 caracteres.");
        RuleFor(x => x.Apelido).MaximumLength(30).WithMessage("O apelido deve ter até 30 caracteres.");
    }
}

internal static class SenhaRegras
{
    public static IRuleBuilderOptions<T, string> SenhaForte<T>(this IRuleBuilder<T, string> regra) =>
        regra.NotEmpty().WithMessage("Informe a senha.")
            .MinimumLength(8).WithMessage("A senha deve ter pelo menos 8 caracteres.")
            .MaximumLength(128).WithMessage("A senha deve ter até 128 caracteres.")
            .Must(s => s.Any(char.IsLetter) && s.Any(char.IsDigit)).WithMessage("A senha deve ter letras e números.");
}

internal static class Telefones
{
    public static string Normalizar(string? telefone) => new((telefone ?? string.Empty).Where(char.IsAsciiDigit).ToArray());
}
