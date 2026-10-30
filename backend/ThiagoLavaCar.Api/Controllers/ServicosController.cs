using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ThiagoLavaCar.Api.Infrastructure;
using ThiagoLavaCar.Application.DTOs;
using ThiagoLavaCar.Application.Services;
using ThiagoLavaCar.Domain.Enums;

namespace ThiagoLavaCar.Api.Controllers;

/// <summary>Catálogo de serviços com preço por tipo de veículo. Só o Dono altera; o Cliente consulta os ativos.</summary>
[ApiController]
[Route("api/v1/servicos")]
[Produces("application/json")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
public sealed class ServicosController(IServicoService servicos) : ControllerBase
{
    /// <summary>Lista o catálogo. O Cliente recebe só os ativos e sem o custo de insumos.</summary>
    [HttpGet]
    [Authorize(Roles = Perfis.DonoOuCliente)]
    [ProducesResponseType<IReadOnlyList<ServicoResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ServicoResponse>>> Listar(CancellationToken ct) =>
        Ok(await servicos.ListarAsync(ct));

    /// <summary>Cria um serviço com a tabela de preços por tipo de veículo.</summary>
    [HttpPost]
    [Authorize(Roles = Perfis.Dono)]
    [ProducesResponseType<ServicoResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<ServicoResponse>> Criar(SalvarServicoRequest request, CancellationToken ct)
    {
        var criado = await servicos.CriarAsync(request, ct);
        return CreatedAtAction(nameof(Preco), new { id = criado.Id }, criado);
    }

    /// <summary>Edita o serviço e substitui a tabela de preços. Vendas antigas não mudam: o preço é copiado para cada item.</summary>
    [HttpPut("{id:int}")]
    [Authorize(Roles = Perfis.Dono)]
    [ProducesResponseType<ServicoResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<ServicoResponse>> Atualizar(int id, SalvarServicoRequest request, CancellationToken ct) =>
        Ok(await servicos.AtualizarAsync(id, request, ct));

    /// <summary>Ativa ou desativa o serviço (serviços com histórico nunca são apagados).</summary>
    [HttpPatch("{id:int}/ativo")]
    [Authorize(Roles = Perfis.Dono)]
    [ProducesResponseType<ServicoResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ServicoResponse>> AlterarAtivo(int id, AlterarAtivoRequest request, CancellationToken ct) =>
        Ok(await servicos.AlterarAtivoAsync(id, request.Ativo, ct));

    /// <summary>Preço e duração do serviço para um tipo de veículo, com ou sem cera.</summary>
    [HttpGet("{id:int}/preco")]
    [Authorize(Roles = Perfis.DonoOuCliente)]
    [ProducesResponseType<PrecoCalculadoResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<PrecoCalculadoResponse>> Preco(
        int id, [FromQuery] TipoVeiculo tipoVeiculo, [FromQuery] bool comCera = false, CancellationToken ct = default) =>
        Ok(await servicos.ObterPrecoAsync(id, tipoVeiculo, comCera, ct));
}
