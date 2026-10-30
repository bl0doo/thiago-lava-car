using FluentValidation;
using ThiagoLavaCar.Application.DTOs;
using ThiagoLavaCar.Domain.Common;
using ThiagoLavaCar.Domain.Enums;

namespace ThiagoLavaCar.Application.Validators;

internal static class RegrasDeVeiculo
{
    public static IRuleBuilderOptions<T, string> Placa<T>(this IRuleBuilder<T, string> regra) => regra
        .NotEmpty().WithMessage("Informe a placa.")
        .Must(PlacaBrasileira.EhValida).WithMessage("Placa inválida. Use o formato ABC1234 ou ABC1D23.");

    public static IRuleBuilderOptions<T, string> Modelo<T>(this IRuleBuilder<T, string> regra) => regra
        .NotEmpty().WithMessage("Informe o modelo do veículo.")
        .MaximumLength(100).WithMessage("O modelo deve ter até 100 caracteres.");

    public static IRuleBuilderOptions<T, string> Tipo<T>(this IRuleBuilder<T, string> regra) => regra
        .Must(t => Enum.TryParse<TipoVeiculo>(t, out _) && !int.TryParse(t, out _))
        .WithMessage("Tipo inválido. Use Moto, Carro ou Caminhonete.");

    public static IRuleBuilderOptions<T, string?> Cor<T>(this IRuleBuilder<T, string?> regra) => regra
        .Must(c => c is null || c.Length <= 30).WithMessage("A cor deve ter até 30 caracteres.");
}

public sealed class CriarVeiculoRequestValidator : AbstractValidator<CriarVeiculoRequest>
{
    public CriarVeiculoRequestValidator()
    {
        RuleFor(x => x.Placa).Placa();
        When(x => x.ModeloVeiculoId is null, () =>
        {
            RuleFor(x => x.ModeloDescricao!).Modelo().OverridePropertyName(nameof(CriarVeiculoRequest.ModeloDescricao));
            RuleFor(x => x.TipoVeiculo!).Tipo().OverridePropertyName(nameof(CriarVeiculoRequest.TipoVeiculo));
        }).Otherwise(() =>
        {
            RuleFor(x => x.ModeloDescricao).MaximumLength(100).WithMessage("O modelo deve ter até 100 caracteres.");
            RuleFor(x => x.TipoVeiculo).Must(t => t is null || (Enum.TryParse<TipoVeiculo>(t, out _) && !int.TryParse(t, out _)))
                .WithMessage("Tipo inválido. Use Moto, Carro ou Caminhonete.");
        });
        RuleFor(x => x.Cor).Cor();
    }
}

public sealed class AtualizarVeiculoRequestValidator : AbstractValidator<AtualizarVeiculoRequest>
{
    public AtualizarVeiculoRequestValidator()
    {
        RuleFor(x => x.Placa).Placa();
        RuleFor(x => x.ModeloDescricao).Modelo();
        RuleFor(x => x.TipoVeiculo).Tipo();
        RuleFor(x => x.Cor).Cor();
    }
}
