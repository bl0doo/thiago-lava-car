using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ThiagoLavaCar.Api.Infrastructure;
using ThiagoLavaCar.Application.DTOs;
using ThiagoLavaCar.Application.Services;

namespace ThiagoLavaCar.Api.Controllers;

/// <summary>Conferência e fechamento do caixa do dia. Somente o Dono.</summary>
[ApiController]
[Route("api/v1/caixa")]
[Authorize(Roles = Perfis.Dono)]
[Produces("application/json")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
public sealed class CaixaController(ICaixaService caixa) : ControllerBase
{
    /// <summary>Resumo calculado do dia, para conferência.</summary>
    /// <remarks>Considera só pagamentos não estornados com data de caixa igual à data. Avisa quantas OS concluídas ou entregues ainda não foram totalmente pagas.</remarks>
    /// <param name="data">Dia (AAAA-MM-DD) em Brasília; não pode ser futuro.</param>
    [HttpGet("{data}/resumo")]
    [ProducesResponseType<CaixaResumoResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<CaixaResumoResponse>> Resumo(DateOnly data, CancellationToken ct) =>
        Ok(await caixa.ObterResumoAsync(data, ct));

    /// <summary>Fecha o caixa do dia depois da conferência.</summary>
    /// <remarks>O sistema recalcula o resumo no momento do fechamento. Se algum valor conferido diferir do calculado, a justificativa (10 a 500 caracteres) é obrigatória e o ajuste vai para a auditoria. Depois de fechado, os pagamentos do dia ficam bloqueados.</remarks>
    [HttpPost("{data}/fechamento")]
    [ProducesResponseType<FechamentoResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<FechamentoResponse>> Fechar(DateOnly data, FecharCaixaRequest request, CancellationToken ct)
    {
        var criado = await caixa.FecharAsync(data, request, ct);
        return Created($"/api/v1/caixa/fechamentos?mes={data:yyyy-MM}", criado);
    }

    /// <summary>Reabre um dia fechado, com justificativa; registra auditoria.</summary>
    [HttpPost("{data}/reabrir")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Reabrir(DateOnly data, ReabrirCaixaRequest request, CancellationToken ct)
    {
        await caixa.ReabrirAsync(data, request.Justificativa, ct);
        return NoContent();
    }

    /// <summary>Histórico de fechamentos de um mês.</summary>
    /// <param name="mes">Mês no formato AAAA-MM. Padrão: o mês atual.</param>
    [HttpGet("fechamentos")]
    [ProducesResponseType<IReadOnlyList<FechamentoResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<IReadOnlyList<FechamentoResponse>>> Fechamentos([FromQuery] string? mes, CancellationToken ct) =>
        Ok(await caixa.ListarFechamentosAsync(mes, ct));
}
