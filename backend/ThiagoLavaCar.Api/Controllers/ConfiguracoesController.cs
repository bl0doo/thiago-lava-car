using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ThiagoLavaCar.Api.Infrastructure;
using ThiagoLavaCar.Application.DTOs;
using ThiagoLavaCar.Application.Services;

namespace ThiagoLavaCar.Api.Controllers;

/// <summary>Dados do lava-jato, horário de funcionamento, diárias padrão e textos das mensagens. Somente o Dono.</summary>
[ApiController]
[Route("api/v1")]
[Authorize(Roles = Perfis.Dono)]
[Produces("application/json")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
public sealed class ConfiguracoesController(IConfiguracaoService configuracoes) : ControllerBase
{
    /// <summary>Configuração do lava-jato.</summary>
    [HttpGet("configuracoes")]
    [ProducesResponseType<ConfiguracaoResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ConfiguracaoResponse>> Obter(CancellationToken ct) => Ok(await configuracoes.ObterAsync(ct));

    /// <summary>Atualiza a configuração.</summary>
    /// <remarks>Horários em HH:mm; dias de 0 (domingo) a 6 (sábado). Muda a grade da agenda e a diária sugerida; a alteração vai para a auditoria.</remarks>
    [HttpPut("configuracoes")]
    [ProducesResponseType<ConfiguracaoResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<ConfiguracaoResponse>> Atualizar(AtualizarConfiguracaoRequest request, CancellationToken ct) =>
        Ok(await configuracoes.AtualizarAsync(request, ct));

    /// <summary>Modelos das mensagens de lavagem iniciada e concluída.</summary>
    [HttpGet("modelos-mensagem")]
    [ProducesResponseType<IReadOnlyList<ModeloMensagemResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ModeloMensagemResponse>>> ListarModelos(CancellationToken ct) =>
        Ok(await configuracoes.ListarModelosAsync(ct));

    /// <summary>Edita o texto de um modelo.</summary>
    /// <remarks>Variáveis aceitas: {nome}, {modelo}, {placa}, {serviço}, {duração estimada}, {valor} e {nome do lava-jato}.</remarks>
    /// <param name="tipo">lavagem-iniciada ou lavagem-concluida.</param>
    [HttpPut("modelos-mensagem/{tipo}")]
    [ProducesResponseType<ModeloMensagemResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<ModeloMensagemResponse>> AtualizarModelo(string tipo, AtualizarModeloMensagemRequest request, CancellationToken ct) =>
        Ok(await configuracoes.AtualizarModeloAsync(tipo, request, ct));
}
