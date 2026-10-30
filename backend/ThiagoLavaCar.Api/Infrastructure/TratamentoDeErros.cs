using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using ThiagoLavaCar.Application.Exceptions;
using ThiagoLavaCar.Domain.Exceptions;

namespace ThiagoLavaCar.Api.Infrastructure;

/// <summary>
/// Tratamento global de erros: exceções de negócio viram ProblemDetails (RFC 9457) em português com o código HTTP
/// certo; qualquer outra vira 500 sem detalhes internos, só o traceId (o detalhe fica no log do servidor).
/// </summary>
internal sealed class TratamentoDeErros(IProblemDetailsService problemas, ILogger<TratamentoDeErros> log) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken ct)
    {
        var (status, mensagem) = exception switch
        {
            NaoEncontradoException e => (StatusCodes.Status404NotFound, e.Message),
            NaoAutenticadoException e => (StatusCodes.Status401Unauthorized, e.Message),
            UsuarioBloqueadoException e => (StatusCodes.Status423Locked, e.Message),
            ClienteDuplicadoException e => (StatusCodes.Status409Conflict, e.Message),
            ConflitoException e => (StatusCodes.Status409Conflict, e.Message),
            RegraInvalidaException e => (StatusCodes.Status422UnprocessableEntity, e.Message),
            _ => (StatusCodes.Status500InternalServerError, "Algo deu errado. Tente de novo em instantes."),
        };

        if (status == StatusCodes.Status500InternalServerError)
        {
            log.LogError(exception, "Erro inesperado. TraceId {TraceId}", httpContext.TraceIdentifier);
        }

        var problema = new ProblemDetails { Status = status, Detail = mensagem };
        if (exception is ClienteDuplicadoException duplicado)
        {
            problema.Extensions["clienteId"] = duplicado.ClienteId;
        }

        httpContext.Response.StatusCode = status;
        return await problemas.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = problema,
        });
    }
}

internal static class ProblemasPadrao
{
    private static readonly Dictionary<int, string> Titulos = new()
    {
        [StatusCodes.Status400BadRequest] = "Requisição inválida",
        [StatusCodes.Status401Unauthorized] = "Não autenticado",
        [StatusCodes.Status403Forbidden] = "Sem permissão",
        [StatusCodes.Status404NotFound] = "Não encontrado",
        [StatusCodes.Status409Conflict] = "Conflito",
        [StatusCodes.Status422UnprocessableEntity] = "Dados inválidos",
        [StatusCodes.Status423Locked] = "Acesso bloqueado",
        [StatusCodes.Status429TooManyRequests] = "Muitas tentativas",
        [StatusCodes.Status500InternalServerError] = "Erro interno",
    };

    /// <summary>Completa todo ProblemDetails (também os de 401/403/404 sem corpo) com título, type e traceId.</summary>
    public static void Completar(ProblemDetailsContext contexto) => Completar(contexto.ProblemDetails, contexto.HttpContext);

    public static void Completar(ProblemDetails problema, HttpContext httpContext)
    {
        var status = problema.Status ?? httpContext.Response.StatusCode;
        problema.Status = status;
        problema.Type = $"https://httpstatuses.io/{status}";
        problema.Title = Titulos.GetValueOrDefault(status, problema.Title ?? "Erro");
        if (status is StatusCodes.Status401Unauthorized && problema.Detail is null)
        {
            problema.Detail = "Entre novamente para continuar.";
        }
        else if (status is StatusCodes.Status403Forbidden && problema.Detail is null)
        {
            problema.Detail = "Seu perfil não tem permissão para esta ação.";
        }

        problema.Extensions["traceId"] = System.Diagnostics.Activity.Current?.Id ?? httpContext.TraceIdentifier;
    }
}
