using FluentValidation;
using ThiagoLavaCar.Application.DTOs;
using ThiagoLavaCar.Domain.Enums;

namespace ThiagoLavaCar.Application.Validators;

public sealed class RegistrarPagamentoRequestValidator : AbstractValidator<RegistrarPagamentoRequest>
{
    public RegistrarPagamentoRequestValidator()
    {
        RuleFor(x => x.FormaPagamento)
            .Must(f => Enum.TryParse<FormaPagamento>(f, out _) && !int.TryParse(f, out _))
            .WithMessage("Forma de pagamento inválida. Use Pix, Debito, Credito ou Dinheiro.");
        RuleFor(x => x.ValorBruto).GreaterThan(0).WithMessage("O valor deve ser maior que zero.");
    }
}

public sealed class EstornoRequestValidator : AbstractValidator<EstornoRequest>
{
    public EstornoRequestValidator()
    {
        RuleFor(x => x.Motivo).Must(m => m is null || m.Length <= 200).WithMessage("O motivo deve ter até 200 caracteres.");
    }
}

public sealed class AlterarTaxaRequestValidator : AbstractValidator<AlterarTaxaRequest>
{
    public AlterarTaxaRequestValidator()
    {
        RuleFor(x => x.Percentual).InclusiveBetween(0m, 100m).WithMessage("O percentual deve estar entre 0 e 100.");
        RuleFor(x => x.ValorFixo).GreaterThanOrEqualTo(0m).WithMessage("O valor fixo não pode ser negativo.");
    }
}
