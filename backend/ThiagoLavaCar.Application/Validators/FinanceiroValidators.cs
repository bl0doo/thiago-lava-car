using FluentValidation;
using ThiagoLavaCar.Application.DTOs;
using ThiagoLavaCar.Domain.Enums;

namespace ThiagoLavaCar.Application.Validators;

public sealed class SalvarDespesaRequestValidator : AbstractValidator<SalvarDespesaRequest>
{
    public SalvarDespesaRequestValidator()
    {
        RuleFor(x => x.Descricao).NotEmpty().WithMessage("Informe a descrição.")
            .MaximumLength(200).WithMessage("A descrição deve ter até 200 caracteres.");
        RuleFor(x => x.Categoria)
            .Must(c => Enum.TryParse<CategoriaDespesa>(c, out _) && !int.TryParse(c, out _))
            .WithMessage("Categoria inválida. Use Insumos, Aluguel, Agua, Energia, Manutencao ou Outros.");
        RuleFor(x => x.Valor).GreaterThan(0).WithMessage("O valor deve ser maior que zero.");
    }
}

public sealed class RegistrarRetiradaRequestValidator : AbstractValidator<RegistrarRetiradaRequest>
{
    public RegistrarRetiradaRequestValidator()
    {
        RuleFor(x => x.Valor).GreaterThan(0).WithMessage("O valor deve ser maior que zero.");
        RuleFor(x => x.Observacao).Must(o => o is null || o.Length <= 200).WithMessage("A observação deve ter até 200 caracteres.");
    }
}

public sealed class LancarDiariaRequestValidator : AbstractValidator<LancarDiariaRequest>
{
    public LancarDiariaRequestValidator()
    {
        RuleFor(x => x.LavadorId).GreaterThan(0).WithMessage("Informe o lavador.");
        RuleFor(x => x.Valor).Must(v => v is null || v > 0).WithMessage("O valor da diária deve ser maior que zero.");
    }
}

public sealed class LancarDiariasDoDiaRequestValidator : AbstractValidator<LancarDiariasDoDiaRequest>
{
    public LancarDiariasDoDiaRequestValidator()
    {
        RuleFor(x => x.LavadorIds).NotEmpty().WithMessage("Escolha ao menos um lavador.");
        RuleFor(x => x.Valor).Must(v => v is null || v > 0).WithMessage("O valor da diária deve ser maior que zero.");
    }
}

public sealed class EditarDiariaRequestValidator : AbstractValidator<EditarDiariaRequest>
{
    public EditarDiariaRequestValidator()
    {
        RuleFor(x => x.Valor).GreaterThan(0).WithMessage("O valor da diária deve ser maior que zero.");
    }
}
