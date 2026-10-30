using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ThiagoLavaCar.Api.Infrastructure;
using ThiagoLavaCar.Application.DTOs;
using ThiagoLavaCar.Application.Services;

namespace ThiagoLavaCar.Api.Controllers;

/// <summary>Os números do dia para o Dono.</summary>
[ApiController]
[Route("api/v1/painel")]
[Authorize(Roles = Perfis.Dono)]
[Produces("application/json")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
public sealed class PainelController(IPainelService painel) : ControllerBase
{
    /// <summary>Quanto entrou hoje e quanto sobrou de verdade.</summary>
    /// <remarks>
    /// <c>entrouHoje</c> é o bruto dos pagamentos não estornados com data de caixa de hoje (o líquido vem em <c>liquidoHoje</c>).
    /// <c>sobrouDeVerdade</c> é o líquido de hoje menos despesas, diárias e insumos das OS pagas hoje; retiradas do dono não entram.
    /// <c>carrosFaltamParaPagarContas</c> é o ponto de equilíbrio do mês (nulo se ainda não há margem positiva).
    /// <c>pendencias</c> lista dias anteriores com pagamento e caixa não fechado.
    /// </remarks>
    [HttpGet("hoje")]
    [ProducesResponseType<PainelHojeResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PainelHojeResponse>> Hoje(CancellationToken ct) => Ok(await painel.ObterHojeAsync(ct));
}
