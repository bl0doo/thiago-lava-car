using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ThiagoLavaCar.Api.Infrastructure;
using ThiagoLavaCar.Application.DTOs;
using ThiagoLavaCar.Application.Services;

namespace ThiagoLavaCar.Api.Controllers;

/// <summary>Veículos e a Ficha do carro. O Cliente só enxerga os próprios veículos.</summary>
[ApiController]
[Route("api/v1/veiculos")]
[Produces("application/json")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
public sealed class VeiculosController(IVeiculoService veiculos) : ControllerBase
{
    /// <summary>Busca um veículo pela placa (com ou sem hífen, qualquer caixa).</summary>
    [HttpGet]
    [Authorize(Roles = Perfis.Dono)]
    [ProducesResponseType<VeiculoResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<VeiculoResponse>> BuscarPorPlaca([FromQuery] string placa, CancellationToken ct) =>
        Ok(await veiculos.BuscarPorPlacaAsync(placa, ct));

    /// <summary>Cadastra um veículo. A placa é gravada normalizada (ABC1D23).</summary>
    /// <remarks>O Dono informa o <c>clienteId</c>; o Cliente cadastra para si e o campo é ignorado.</remarks>
    [HttpPost]
    [Authorize(Roles = Perfis.DonoOuCliente)]
    [ProducesResponseType<VeiculoResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<VeiculoResponse>> Criar(CriarVeiculoRequest request, CancellationToken ct)
    {
        var criado = await veiculos.CriarAsync(request, ct);
        return CreatedAtAction(nameof(Ficha), new { id = criado.Id }, criado);
    }

    /// <summary>Edita placa, modelo, tipo e cor.</summary>
    [HttpPut("{id:int}")]
    [Authorize(Roles = Perfis.DonoOuCliente)]
    [ProducesResponseType<VeiculoResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<VeiculoResponse>> Atualizar(int id, AtualizarVeiculoRequest request, CancellationToken ct) =>
        Ok(await veiculos.AtualizarAsync(id, request, ct));

    /// <summary>Ficha do carro: dados do veículo e histórico de serviços.</summary>
    [HttpGet("{id:int}/ficha")]
    [Authorize(Roles = Perfis.DonoOuCliente)]
    [ProducesResponseType<FichaVeiculoResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<FichaVeiculoResponse>> Ficha(int id, CancellationToken ct) =>
        Ok(await veiculos.ObterFichaAsync(id, ct));
}
