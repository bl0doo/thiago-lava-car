using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ThiagoLavaCar.Api.Infrastructure;
using ThiagoLavaCar.Application.DTOs;
using ThiagoLavaCar.Application.Services;

namespace ThiagoLavaCar.Api.Controllers;

/// <summary>Diária de cada lavador por dia trabalhado; base do rateio da mão de obra. Somente o Dono.</summary>
[ApiController]
[Route("api/v1/diarias")]
[Authorize(Roles = Perfis.Dono)]
[Produces("application/json")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
public sealed class DiariasController(IDiariaService diarias) : ControllerBase
{
    /// <summary>Lista as diárias de um mês, com o total.</summary>
    /// <param name="mes">Mês no formato AAAA-MM. Padrão: o mês atual.</param>
    [HttpGet]
    [ProducesResponseType<DiariasResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<DiariasResponse>> Listar([FromQuery] string? mes, CancellationToken ct) =>
        Ok(await diarias.ListarAsync(mes, ct));

    /// <summary>Lança a diária de um lavador em um dia.</summary>
    /// <remarks>Uma por lavador por dia (409 se já existir). Sem <c>valor</c>, vale a diária padrão da configuração: R$ 100 na semana e R$ 120 aos sábados.</remarks>
    [HttpPost]
    [ProducesResponseType<DiariaResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<DiariaResponse>> Lancar(LancarDiariaRequest request, CancellationToken ct)
    {
        var criada = await diarias.LancarAsync(request, ct);
        return Created($"/api/v1/diarias?mes={criada.Data:yyyy-MM}", criada);
    }

    /// <summary>Lança as diárias de todos os lavadores escolhidos em um dia.</summary>
    /// <remarks>Quem já tem diária no dia não é duplicado: vem em <c>jaLancadas</c>.</remarks>
    /// <param name="data">Dia (AAAA-MM-DD); não pode ser futuro.</param>
    [HttpPost("lancar-dia")]
    [ProducesResponseType<DiariasDoDiaResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<DiariasDoDiaResponse>> LancarDia([FromQuery] DateOnly data, LancarDiariasDoDiaRequest request, CancellationToken ct) =>
        Ok(await diarias.LancarDoDiaAsync(data, request, ct));

    /// <summary>Ajusta o valor de uma diária ou a marca como paga.</summary>
    /// <remarks>Depois de paga, o valor não pode mais mudar (409). Mudanças de valor vão para a auditoria.</remarks>
    [HttpPut("{id:int}")]
    [ProducesResponseType<DiariaResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<DiariaResponse>> Editar(int id, EditarDiariaRequest request, CancellationToken ct) =>
        Ok(await diarias.EditarAsync(id, request, ct));

    /// <summary>Exclui uma diária ainda não paga e registra a exclusão na auditoria.</summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Excluir(int id, CancellationToken ct)
    {
        await diarias.ExcluirAsync(id, ct);
        return NoContent();
    }
}
