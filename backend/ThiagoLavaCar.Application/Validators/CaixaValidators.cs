using FluentValidation;
using ThiagoLavaCar.Application.DTOs;
using ThiagoLavaCar.Domain.Enums;

namespace ThiagoLavaCar.Application.Validators;

public sealed class FecharCaixaRequestValidator : AbstractValidator<FecharCaixaRequest>
{
    public FecharCaixaRequestValidator()
    {
        RuleFor(x => x.Conferido).NotEmpty().WithMessage("Informe o valor conferido de cada forma de pagamento.");
        RuleForEach(x => x.Conferido).ChildRules(linha =>
        {
            linha.RuleFor(l => l.Forma)
                .Must(f => Enum.TryParse<FormaPagamento>(f, out _) && !int.TryParse(f, out _))
                .WithMessage("Forma de pagamento inválida. Use Pix, Debito, Credito ou Dinheiro.");
            linha.RuleFor(l => l.Valor).GreaterThanOrEqualTo(0m).WithMessage("O valor conferido não pode ser negativo.");
        });
        RuleFor(x => x.Justificativa).Must(j => j is null || j.Length <= 500).WithMessage("A justificativa deve ter até 500 caracteres.");
    }
}

public sealed class ReabrirCaixaRequestValidator : AbstractValidator<ReabrirCaixaRequest>
{
    public ReabrirCaixaRequestValidator()
    {
        RuleFor(x => x.Justificativa).NotEmpty().WithMessage("Informe a justificativa da reabertura.")
            .Must(j => j is null || (j.Trim().Length is >= 10 and <= 500))
            .WithMessage("A justificativa deve ter de 10 a 500 caracteres.");
    }
}
