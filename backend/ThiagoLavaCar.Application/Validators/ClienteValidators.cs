using System.Text.RegularExpressions;
using FluentValidation;
using ThiagoLavaCar.Application.DTOs;
using ThiagoLavaCar.Domain.Enums;

namespace ThiagoLavaCar.Application.Validators;

internal static class RegrasDeCliente
{
    public static IRuleBuilderOptions<T, string> Nome<T>(this IRuleBuilder<T, string> regra) => regra
        .NotEmpty().WithMessage("Informe o nome.")
        .Length(2, 100).WithMessage("O nome deve ter de 2 a 100 caracteres.");

    public static IRuleBuilderOptions<T, string> Telefone<T>(this IRuleBuilder<T, string> regra) => regra
        .NotEmpty().WithMessage("Informe o telefone.")
        .Must(t => t is not null && Regex.IsMatch(t, @"^\d{10,11}$")).WithMessage("O telefone deve ter 10 ou 11 dígitos (DDD + número), só números.");

    public static IRuleBuilderOptions<T, string?> EmailOpcional<T>(this IRuleBuilder<T, string?> regra) => regra
        .Must(e => string.IsNullOrWhiteSpace(e) || new System.ComponentModel.DataAnnotations.EmailAddressAttribute().IsValid(e.Trim()))
        .WithMessage("E-mail inválido.")
        .Must(e => e is null || e.Length <= 254).WithMessage("E-mail muito longo.");

    public static IRuleBuilderOptions<T, string> Canal<T>(this IRuleBuilder<T, string> regra) => regra
        .Must(c => Enum.TryParse<CanalContato>(c, out _) && !int.TryParse(c, out _))
        .WithMessage("Canal inválido. Use WhatsApp, Telefone ou Email.");
}

public sealed class CriarClienteRequestValidator : AbstractValidator<CriarClienteRequest>
{
    public CriarClienteRequestValidator()
    {
        RuleFor(x => x.Nome).Nome();
        RuleFor(x => x.Telefone).Telefone();
        RuleFor(x => x.Email).EmailOpcional();
        RuleFor(x => x.CanalPreferido).Canal();
        RuleFor(x => x.ConsentimentoLgpd).Equal(true).WithMessage("O consentimento LGPD é obrigatório para cadastrar o cliente.");
    }
}

public sealed class AtualizarClienteRequestValidator : AbstractValidator<AtualizarClienteRequest>
{
    public AtualizarClienteRequestValidator()
    {
        RuleFor(x => x.Nome).Nome();
        RuleFor(x => x.Telefone).Telefone();
        RuleFor(x => x.Email).EmailOpcional();
        RuleFor(x => x.CanalPreferido).Canal();
    }
}
