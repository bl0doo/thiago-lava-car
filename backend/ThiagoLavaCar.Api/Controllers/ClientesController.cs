using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ThiagoLavaCar.Api.Infrastructure;
using ThiagoLavaCar.Application.DTOs;
using ThiagoLavaCar.Application.Services;

namespace ThiagoLavaCar.Api.Controllers;

/// <summary>Cadastro e consulta de clientes. O Dono gerencia todos; o Cliente só vê e edita os próprios dados (/clientes/me).</summary>
[ApiController]
[Route("api/v1/clientes")]
[Produces("application/json")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
public sealed class ClientesController(IClienteService clientes) : ControllerBase
{
    /// <summary>Busca clientes por nome, telefone ou placa.</summary>
    /// <param name="busca">Trecho do nome, do telefone ou da placa de um veículo.</param>
    /// <param name="pagina">Página (padrão 1).</param>
    /// <param name="tamanho">Itens por página (padrão 20, máximo 100).</param>
    [HttpGet]
    [Authorize(Roles = Perfis.Dono)]
    [ProducesResponseType<PaginaResponse<ClienteResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PaginaResponse<ClienteResponse>>> Buscar(
        [FromQuery] string? busca, [FromQuery] int pagina = 1, [FromQuery] int tamanho = 20, CancellationToken ct = default) =>
        Ok(await clientes.BuscarAsync(busca, pagina, tamanho, ct));

    /// <summary>Detalhe do cliente com os veículos.</summary>
    [HttpGet("{id:int}")]
    [Authorize(Roles = Perfis.Dono)]
    [ProducesResponseType<ClienteDetalheResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ClienteDetalheResponse>> Obter(int id, CancellationToken ct) =>
        Ok(await clientes.ObterAsync(id, ct));

    /// <summary>Cadastra um cliente no balcão.</summary>
    /// <remarks>Telefone único e consentimento LGPD obrigatório. No 409, a resposta traz <c>clienteId</c> do cadastro existente.</remarks>
    [HttpPost]
    [Authorize(Roles = Perfis.Dono)]
    [ProducesResponseType<ClienteResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<ClienteResponse>> Criar(CriarClienteRequest request, CancellationToken ct)
    {
        var criado = await clientes.CriarAsync(request, ct);
        return CreatedAtAction(nameof(Obter), new { id = criado.Id }, criado);
    }

    /// <summary>Edita nome, telefone, e-mail e canal preferido.</summary>
    [HttpPut("{id:int}")]
    [Authorize(Roles = Perfis.Dono)]
    [ProducesResponseType<ClienteResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<ClienteResponse>> Atualizar(int id, AtualizarClienteRequest request, CancellationToken ct) =>
        Ok(await clientes.AtualizarAsync(id, request, ct));

    /// <summary>Dados do cliente logado.</summary>
    [HttpGet("me")]
    [Authorize(Roles = Perfis.Cliente)]
    [ProducesResponseType<ClienteDetalheResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ClienteDetalheResponse>> ObterMeu(CancellationToken ct) =>
        Ok(await clientes.ObterMeuAsync(ct));

    /// <summary>Edita os dados do cliente logado.</summary>
    [HttpPut("me")]
    [Authorize(Roles = Perfis.Cliente)]
    [ProducesResponseType<ClienteResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<ClienteResponse>> AtualizarMeu(AtualizarClienteRequest request, CancellationToken ct) =>
        Ok(await clientes.AtualizarMeuAsync(request, ct));

    /// <summary>Veículos do cliente logado.</summary>
    [HttpGet("me/veiculos")]
    [Authorize(Roles = Perfis.Cliente)]
    [ProducesResponseType<IReadOnlyList<VeiculoResumoResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<VeiculoResumoResponse>>> MeusVeiculos(CancellationToken ct) =>
        Ok(await clientes.ListarMeusVeiculosAsync(ct));
}
