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
        RuleForEach(x => x.Avarias).SetValidator(new AvariaRequestValidator());
        RuleFor(x => x.PertencesEntrada).Must(t => t is null || t.Length <= LimiteTexto)
            .WithMessage($"O texto deve ter até {LimiteTexto} caracteres.");
        RuleFor(x => x.NomeQuemRetira).Must(t => t is null || t.Length <= 100)
            .WithMessage("O nome deve ter até 100 caracteres.");
    }
}
