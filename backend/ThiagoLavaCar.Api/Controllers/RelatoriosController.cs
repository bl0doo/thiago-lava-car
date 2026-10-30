using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ThiagoLavaCar.Api.Infrastructure;
using ThiagoLavaCar.Application.DTOs;
using ThiagoLavaCar.Application.Services;

namespace ThiagoLavaCar.Api.Controllers;

/// <summary>Relatórios gerenciais. Somente o Dono.</summary>
[ApiController]
[Route("api/v1/relatorios")]
[Authorize(Roles = Perfis.Dono)]
[Produces("application/json")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
public sealed class RelatoriosController(IRelatorioService relatorios) : ControllerBase
{
    /// <summary>Quanto cada serviço rende no período, do mais para o menos lucrativo.</summary>
    /// <remarks>
    /// Considera as OS totalmente pagas no período (pela data do último pagamento). Lucro = receita líquida − insumos − mão de obra rateada.
    /// A mão de obra é o total de diárias do período dividido pelos minutos lavados, vezes os minutos de cada OS (duração real, ou a estimada se o temporizador não foi usado).
    /// Numa OS com vários itens, a receita segue a proporção do preço e o tempo, a proporção do tempo estimado de cada item.
    /// Serviço com cera aparece em linha própria.
    /// </remarks>
    /// <param name="inicio">Primeiro dia (AAAA-MM-DD). Padrão: dia 1 do mês atual.</param>
    /// <param name="fim">Último dia (AAAA-MM-DD). Padrão: hoje. O período vai até 366 dias.</param>
    [HttpGet("lucro-por-servico")]
    [ProducesResponseType<IReadOnlyList<LucroPorServicoResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<IReadOnlyList<LucroPorServicoResponse>>> LucroPorServico(
        [FromQuery] DateOnly? inicio, [FromQuery] DateOnly? fim, CancellationToken ct) =>
        Ok(await relatorios.LucroPorServicoAsync(inicio, fim, ct));

    /// <summary>Quantos carros faltam para pagar as contas do mês.</summary>
    /// <remarks>Carros necessários = contas (despesas + diárias) ÷ margem por carro, arredondado para cima; faltam = máximo entre 0 e (necessários − OS pagas no mês). A margem usa os últimos 30 dias, ou o catálogo quando não há histórico (<c>margemEstimada</c>).</remarks>
    /// <param name="mes">Mês no formato AAAA-MM. Padrão: o mês atual.</param>
    [HttpGet("ponto-equilibrio")]
    [ProducesResponseType<PontoEquilibrioResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<PontoEquilibrioResponse>> PontoEquilibrio([FromQuery] string? mes, CancellationToken ct) =>
        Ok(await relatorios.PontoEquilibrioAsync(mes, ct));

    /// <summary>Faturamento e ticket médio por dia.</summary>
    /// <remarks>Só dias com pagamento. Bruto e líquido somam os pagamentos não estornados com data de caixa no dia; o ticket médio é o valor das OS totalmente pagas no dia dividido pelo número delas.</remarks>
    /// <param name="inicio">Primeiro dia (AAAA-MM-DD). Padrão: dia 1 do mês atual.</param>
    /// <param name="fim">Último dia (AAAA-MM-DD). Padrão: hoje. O período vai até 366 dias.</param>
    [HttpGet("faturamento")]
    [ProducesResponseType<FaturamentoResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<FaturamentoResponse>> Faturamento([FromQuery] DateOnly? inicio, [FromQuery] DateOnly? fim, CancellationToken ct) =>
        Ok(await relatorios.FaturamentoAsync(inicio, fim, ct));

    /// <summary>Tempo médio real por serviço e por lavador.</summary>
    /// <remarks>Considera OS concluídas ou entregues no período que usaram o temporizador. Numa OS com vários itens, a duração se divide pela proporção do tempo estimado de cada item; <c>estimadoMedioMin</c> permite comparar com o previsto.</remarks>
    /// <param name="inicio">Primeiro dia (AAAA-MM-DD). Padrão: dia 1 do mês atual.</param>
    /// <param name="fim">Último dia (AAAA-MM-DD). Padrão: hoje. O período vai até 366 dias.</param>
    [HttpGet("produtividade")]
    [ProducesResponseType<ProdutividadeResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<ProdutividadeResponse>> Produtividade([FromQuery] DateOnly? inicio, [FromQuery] DateOnly? fim, CancellationToken ct) =>
        Ok(await relatorios.ProdutividadeAsync(inicio, fim, ct));
}
