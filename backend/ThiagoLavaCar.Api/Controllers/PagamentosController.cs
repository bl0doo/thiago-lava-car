using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ThiagoLavaCar.Api.Infrastructure;
using ThiagoLavaCar.Application.DTOs;
using ThiagoLavaCar.Application.Services;
using ThiagoLavaCar.Domain.Enums;

namespace ThiagoLavaCar.Api.Controllers;

/// <summary>Recebimentos, estornos e taxas por forma de pagamento. Somente o Dono.</summary>
[ApiController]
[Route("api/v1")]
[Authorize(Roles = Perfis.Dono)]
[Produces("application/json")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
public sealed class PagamentosController(IPagamentoService pagamentos) : ControllerBase
{
    /// <summary>Registra um recebimento da OS; pode ser dividido em várias formas.</summary>
    /// <remarks>A taxa vigente é copiada para o pagamento. O valor não pode passar do saldo pendente e o caixa do dia não pode estar fechado.</remarks>
    [HttpPost("ordens-servico/{id:int}/pagamentos")]
    [ProducesResponseType<PagamentoRegistradoResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<PagamentoRegistradoResponse>> Registrar(int id, RegistrarPagamentoRequest request, CancellationToken ct)
    {
        var criado = await pagamentos.RegistrarAsync(id, request, ct);
        return Created($"/api/v1/pagamentos?data={criado.DataCaixa:yyyy-MM-dd}", criado);
    }

    /// <summary>Pagamentos de um dia de caixa (padrão: hoje em Brasília), incluindo os estornados.</summary>
    [HttpGet("pagamentos")]
    [ProducesResponseType<IReadOnlyList<PagamentoResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<PagamentoResponse>>> Listar([FromQuery] DateOnly? data, CancellationToken ct) =>
        Ok(await pagamentos.ListarAsync(data, ct));

    /// <summary>Estorna um pagamento de um dia com caixa ainda aberto; registra auditoria.</summary>
    [HttpPost("pagamentos/{id:int}/estorno")]
    [ProducesResponseType<PagamentoResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<PagamentoResponse>> Estornar(int id, EstornoRequest? request, CancellationToken ct) =>
        Ok(await pagamentos.EstornarAsync(id, request?.Motivo, ct));

    /// <summary>Taxas por forma de pagamento.</summary>
    [HttpGet("taxas")]
    [ProducesResponseType<IReadOnlyList<TaxaResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<TaxaResponse>>> Taxas(CancellationToken ct) =>
        Ok(await pagamentos.ListarTaxasAsync(ct));

    /// <summary>Altera a taxa de uma forma de pagamento; vale para pagamentos futuros e registra auditoria.</summary>
    [HttpPut("taxas/{forma}")]
    [ProducesResponseType<TaxaResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<TaxaResponse>> AlterarTaxa(FormaPagamento forma, AlterarTaxaRequest request, CancellationToken ct) =>
        Ok(await pagamentos.AlterarTaxaAsync(forma, request, ct));
}
