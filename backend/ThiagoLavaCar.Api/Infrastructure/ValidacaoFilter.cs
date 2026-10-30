using System.Text.Json;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace ThiagoLavaCar.Api.Infrastructure;

/// <summary>Roda os validators (FluentValidation) de cada argumento antes do Controller; falhas viram 422.</summary>
internal sealed class ValidacaoFilter(IServiceProvider servicos) : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var erros = new Dictionary<string, List<string>>();

        foreach (var argumento in context.ActionArguments.Values.Where(a => a is not null))
        {
            var tipoValidator = typeof(IValidator<>).MakeGenericType(argumento!.GetType());
            if (servicos.GetService(tipoValidator) is not IValidator validator)
            {
                continue;
            }

            var resultado = await validator.ValidateAsync(
                new ValidationContext<object>(argumento), context.HttpContext.RequestAborted);
            foreach (var falha in resultado.Errors)
            {
                var campo = JsonNamingPolicy.CamelCase.ConvertName(falha.PropertyName);
                if (!erros.TryGetValue(campo, out var lista))
                {
                    erros[campo] = lista = [];
                }

                lista.Add(falha.ErrorMessage);
            }
        }

        if (erros.Count == 0)
        {
            await next();
            return;
        }

        var problema = new ValidationProblemDetails(erros.ToDictionary(e => e.Key, e => e.Value.ToArray()))
        {
            Status = StatusCodes.Status422UnprocessableEntity,
            Title = "Dados inválidos",
            Detail = "Um ou mais campos não passaram na validação.",
        };
        ProblemasPadrao.Completar(problema, context.HttpContext);
        context.Result = new ObjectResult(problema) { StatusCode = StatusCodes.Status422UnprocessableEntity };
    }
}
