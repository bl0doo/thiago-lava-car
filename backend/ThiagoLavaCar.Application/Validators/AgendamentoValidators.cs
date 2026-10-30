using FluentValidation;
using ThiagoLavaCar.Application.DTOs;

namespace ThiagoLavaCar.Application.Validators;

public sealed class CriarAgendamentoRequestValidator : AbstractValidator<CriarAgendamentoRequest>
{
    public CriarAgendamentoRequestValidator()
    {
        RuleFor(x => x.VeiculoId).GreaterThan(0).WithMessage("Informe o veículo.");
        RuleFor(x => x.ServicoId).GreaterThan(0).WithMessage("Informe o serviço.");
        RuleFor(x => x.Inicio).NotEqual(default(DateTimeOffset)).WithMessage("Informe o horário.");
    }
}

public sealed class CheckInRequestValidator : AbstractValidator<CheckInRequest>
{
    private const int LimiteTexto = 500;

    public CheckInRequestValidator()
    {
        RuleFor(x => x.AceiteCliente).Equal(true).WithMessage("O cliente precisa aceitar a vistoria de entrada.");
        RuleFor(x => x.Avarias).Must((x, avarias) => !x.SemAvarias || avarias is null || avarias.Count == 0)
            .WithMessage("Vistoria inconsistente: marcada sem avarias, mas há avarias listadas.");
        RuleFor(x => x.Avarias).Must((x, avarias) => x.SemAvarias || avarias is { Count: > 0 })
            .WithMessage("Marque \"Sem avarias\" ou registre ao menos uma avaria.");
        RuleForEach(x => x.Avarias).ChildRules(avaria =>
        {
            avaria.RuleFor(a => a.Local).NotEmpty().WithMessage("Informe o local da avaria.")
                .MaximumLength(100).WithMessage("O local deve ter até 100 caracteres.");
            avaria.RuleFor(a => a.Tipo).NotEmpty().WithMessage("Informe o tipo da avaria.")
                .MaximumLength(50).WithMessage("O tipo deve ter até 50 caracteres.");
            avaria.RuleFor(a => a.Descricao).Must(d => d is null || d.Length <= LimiteTexto)
                .WithMessage($"A descrição deve ter até {LimiteTexto} caracteres.");
        });
        RuleFor(x => x.PertencesEntrada).Must(t => t is null || t.Length <= LimiteTexto)
            .WithMessage($"O texto deve ter até {LimiteTexto} caracteres.");
        RuleFor(x => x.NomeQuemRetira).Must(t => t is null || t.Length <= 100)
            .WithMessage("O nome deve ter até 100 caracteres.");
    }
}
