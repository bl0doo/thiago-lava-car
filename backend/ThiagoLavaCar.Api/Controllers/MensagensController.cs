using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ThiagoLavaCar.Api.Infrastructure;
using ThiagoLavaCar.Application.DTOs;
using ThiagoLavaCar.Application.Services;

namespace ThiagoLavaCar.Api.Controllers;

/// <summary>Mensagens de acompanhamento que o Dono envia ao cliente (WhatsApp). Somente o Dono.</summary>
[ApiController]
[Route("api/v1/ordens-servico/{id:int}/mensagens")]
[Authorize(Roles = Perfis.Dono)]
[Produces("application/json")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
public sealed class MensagensController(IMensagemService mensagens) : ControllerBase
{
    /// <summary>Gera o texto da mensagem e o link do WhatsApp.</summary>
    /// <remarks>O modelo troca {nome}, {modelo}, {placa}, {serviço}, {duração estimada}, {valor} e {nome do lava-jato}. A frase com {valor} vira "Valor a pagar: R$ ..." e só aparece se houver saldo pendente. A mensagem de lavagem iniciada exige OS em andamento e a de lavagem concluída exige OS concluída (409).</remarks>
    /// <param name="id">Número da ordem de serviço.</param>
    /// <param name="tipo">lavagem-iniciada ou lavagem-concluida.</param>
    [HttpGet("{tipo}")]
    [ProducesResponseType<MensagemGeradaResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<MensagemGeradaResponse>> Gerar(int id, string tipo, CancellationToken ct) =>
        Ok(await mensagens.GerarAsync(id, tipo, ct));

    /// <summary>Registra que o Dono enviou a mensagem; guarda o texto final.</summary>
    /// <param name="id">Número da ordem de serviço.</param>
    /// <param name="tipo">lavagem-iniciada ou lavagem-concluida.</param>
    [HttpPost("{tipo}/enviada")]
    [ProducesResponseType<MensagemEnviadaResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<MensagemEnviadaResponse>> RegistrarEnvio(int id, string tipo, CancellationToken ct) =>
        Created($"/api/v1/ordens-servico/{id}/mensagens/{tipo}", await mensagens.RegistrarEnvioAsync(id, tipo, ct));
}
