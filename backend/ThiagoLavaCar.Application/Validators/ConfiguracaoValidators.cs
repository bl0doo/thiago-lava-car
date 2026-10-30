using System.Globalization;
using FluentValidation;
using ThiagoLavaCar.Application.DTOs;
using ThiagoLavaCar.Domain.Entities;

namespace ThiagoLavaCar.Application.Validators;

public sealed class AtualizarConfiguracaoRequestValidator : AbstractValidator<AtualizarConfiguracaoRequest>
{
    public AtualizarConfiguracaoRequestValidator()
    {
        RuleFor(x => x.NomeLavaJato).NotEmpty().WithMessage("Informe o nome do lava-jato.")
            .MaximumLength(100).WithMessage("O nome deve ter até 100 caracteres.");
        RuleFor(x => x.Telefone).Must(t => t is null || t.Length <= 20).WithMessage("O telefone deve ter até 20 caracteres.");
        RuleFor(x => x.Abertura).Must(EhHora).WithMessage("Use o horário no formato HH:mm.");
        RuleFor(x => x.Fechamento).Must(EhHora).WithMessage("Use o horário no formato HH:mm.");
        RuleFor(x => x.DiasFuncionamento).NotEmpty().WithMessage("Informe ao menos um dia de funcionamento.")
            .Must(d => d is null || d.All(n => n is >= 0 and <= 6) && d.Distinct().Count() == d.Count)
            .WithMessage("Dias de funcionamento inválidos: use números de 0 (domingo) a 6 (sábado), sem repetir.");
        RuleFor(x => x.DiariaSemana).GreaterThan(0).WithMessage("A diária deve ser maior que zero.");
        RuleFor(x => x.DiariaSabado).GreaterThan(0).WithMessage("A diária deve ser maior que zero.");
        RuleFor(x => x.IntervaloAgendaMin).InclusiveBetween(5, 240).WithMessage("O intervalo da agenda deve ficar entre 5 e 240 minutos.");
    }

    internal static bool EhHora(string? valor) =>
        TimeOnly.TryParseExact(valor, "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out _);
}

public sealed class AtualizarModeloMensagemRequestValidator : AbstractValidator<AtualizarModeloMensagemRequest>
{
    public AtualizarModeloMensagemRequestValidator()
    {
        RuleFor(x => x.Texto).NotEmpty().WithMessage("Informe o texto da mensagem.")
            .MaximumLength(ModeloMensagem.TextoMax).WithMessage($"O texto deve ter até {ModeloMensagem.TextoMax} caracteres.");
    }
}
