using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ThiagoLavaCar.Api.Infrastructure;
using ThiagoLavaCar.Application.DTOs;
using ThiagoLavaCar.Application.Services;

namespace ThiagoLavaCar.Api.Controllers;

/// <summary>Gastos da empresa por categoria. Somente o Dono.</summary>
[ApiController]
[Route("api/v1/despesas")]
[Authorize(Roles = Perfis.Dono)]
[Produces("application/json")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
public sealed class DespesasController(IDespesaService despesas) : ControllerBase
{
    /// <summary>Lista as despesas de um período, da mais recente para a mais antiga, com o total.</summary>
    /// <param name="de">Primeiro dia (AAAA-MM-DD). Padrão: dia 1 do mês atual.</param>
    /// <param name="ate">Último dia (AAAA-MM-DD). Padrão: hoje (ou o fim do mês de <paramref name="de"/>).</param>
    [HttpGet]
    [ProducesResponseType<DespesasResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<DespesasResponse>> Listar([FromQuery] DateOnly? de, [FromQuery] DateOnly? ate, CancellationToken ct) =>
        Ok(await despesas.ListarAsync(de, ate, ct));

    /// <summary>Registra uma despesa.</summary>
    /// <remarks>Descrição obrigatória, valor maior que zero e data não futura. Categorias: Insumos, Aluguel, Agua, Energia, Manutencao, Outros.</remarks>
    [HttpPost]
    [ProducesResponseType<DespesaResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<DespesaResponse>> Criar(SalvarDespesaRequest request, CancellationToken ct)
    {
        var criada = await despesas.CriarAsync(request, ct);
        return Created($"/api/v1/despesas?de={criada.Data:yyyy-MM-dd}&ate={criada.Data:yyyy-MM-dd}", criada);
    }

    /// <summary>Edita uma despesa.</summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType<DespesaResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<DespesaResponse>> Editar(int id, SalvarDespesaRequest request, CancellationToken ct) =>
        Ok(await despesas.EditarAsync(id, request, ct));

    /// <summary>Exclui uma despesa e registra a exclusão na auditoria.</summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Excluir(int id, CancellationToken ct)
    {
        await despesas.ExcluirAsync(id, ct);
        return NoContent();
    }
}
