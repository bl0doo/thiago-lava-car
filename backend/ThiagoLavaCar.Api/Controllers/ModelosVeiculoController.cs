using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ThiagoLavaCar.Api.Infrastructure;
using ThiagoLavaCar.Application.DTOs;
using ThiagoLavaCar.Application.Services;

namespace ThiagoLavaCar.Api.Controllers;

/// <summary>Autocompletar de modelos de veículo no cadastro.</summary>
[ApiController]
[Route("api/v1/modelos-veiculo")]
[Authorize(Roles = Perfis.DonoOuCliente)]
[Produces("application/json")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
public sealed class ModelosVeiculoController(IVeiculoService veiculos) : ControllerBase
{
    /// <summary>Até 20 modelos cuja marca ou nome contém o texto, por marca e nome.</summary>
    /// <remarks>Sem <c>busca</c>, devolve os primeiros 20. O <c>id</c> pode ir em <c>modeloVeiculoId</c> no cadastro do veículo, que então herda descrição e tipo.</remarks>
    /// <param name="busca">Parte da marca ou do nome, sem diferenciar maiúsculas (ex.: "onix").</param>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<ModeloVeiculoResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ModeloVeiculoResponse>>> Buscar([FromQuery] string? busca, CancellationToken ct) =>
        Ok(await veiculos.BuscarModelosAsync(busca, ct));
}
