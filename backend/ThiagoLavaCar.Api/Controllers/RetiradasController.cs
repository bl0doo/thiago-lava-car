using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ThiagoLavaCar.Api.Infrastructure;
using ThiagoLavaCar.Application.DTOs;
using ThiagoLavaCar.Application.Services;

namespace ThiagoLavaCar.Api.Controllers;

/// <summary>Pró-labore e retiradas pessoais do dono, separados das despesas. Somente o Dono.</summary>
[ApiController]
[Route("api/v1/retiradas")]
[Authorize(Roles = Perfis.Dono)]
[Produces("application/json")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
public sealed class RetiradasController(IRetiradaService retiradas) : ControllerBase
{
    /// <summary>Lista as retiradas de um período, com o total.</summary>
    /// <param name="de">Primeiro dia (AAAA-MM-DD). Padrão: dia 1 do mês atual.</param>
    /// <param name="ate">Último dia (AAAA-MM-DD). Padrão: hoje (ou o fim do mês de <paramref name="de"/>).</param>
    [HttpGet]
    [ProducesResponseType<RetiradasResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<RetiradasResponse>> Listar([FromQuery] DateOnly? de, [FromQuery] DateOnly? ate, CancellationToken ct) =>
        Ok(await retiradas.ListarAsync(de, ate, ct));

    /// <summary>Registra uma retirada do dono.</summary>
    /// <remarks>Não entra no custo do negócio nem no lucro por serviço. Valor maior que zero, data não futura, observação até 200 caracteres.</remarks>
    [HttpPost]
    [ProducesResponseType<RetiradaResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<RetiradaResponse>> Registrar(RegistrarRetiradaRequest request, CancellationToken ct)
    {
        var criada = await retiradas.RegistrarAsync(request, ct);
        return Created($"/api/v1/retiradas?de={criada.Data:yyyy-MM-dd}&ate={criada.Data:yyyy-MM-dd}", criada);
    }

    /// <summary>Exclui uma retirada e registra a exclusão na auditoria.</summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Excluir(int id, CancellationToken ct)
    {
        await retiradas.ExcluirAsync(id, ct);
        return NoContent();
    }
}
