using FluentValidation;
using ThiagoLavaCar.Application.DTOs;
using ThiagoLavaCar.Domain.Enums;

namespace ThiagoLavaCar.Application.Validators;

public sealed class SalvarServicoRequestValidator : AbstractValidator<SalvarServicoRequest>
{
    public SalvarServicoRequestValidator()
    {
        RuleFor(x => x.Nome).NotEmpty().WithMessage("Informe o nome do serviço.")
            .MaximumLength(100).WithMessage("O nome deve ter até 100 caracteres.");
        RuleFor(x => x.Categoria).NotEmpty().WithMessage("Informe a categoria.")
            .MaximumLength(50).WithMessage("A categoria deve ter até 50 caracteres.");
        RuleFor(x => x.TempoEstimadoMin).GreaterThan(0).WithMessage("O tempo estimado deve ser maior que zero.");
        RuleFor(x => x.CustoInsumos).GreaterThanOrEqualTo(0).WithMessage("O custo de insumos não pode ser negativo.");
        RuleFor(x => x.Precos).NotEmpty().WithMessage("Informe ao menos um preço.")
            .Must(p => p is null || p.Select(x => x.TipoVeiculo).Distinct().Count() == p.Count)
            .WithMessage("Há tipos de veículo repetidos na tabela de preços.");
        RuleForEach(x => x.Precos).ChildRules(preco =>
        {
            preco.RuleFor(p => p.TipoVeiculo)
                .Must(t => Enum.TryParse<TipoVeiculo>(t, out _) && !int.TryParse(t, out _))
                .WithMessage("Tipo inválido. Use Moto, Carro ou Caminhonete.");
            preco.RuleFor(p => p.Preco).GreaterThan(0).WithMessage("O preço deve ser maior que zero.");
            preco.RuleFor(p => p.PrecoComCera).Must(c => c is null || c > 0).WithMessage("O preço com cera deve ser maior que zero.");
        });
    }
}
