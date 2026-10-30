using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ThiagoLavaCar.Api.Infrastructure;
using ThiagoLavaCar.Application.DTOs;
using ThiagoLavaCar.Application.Services;

namespace ThiagoLavaCar.Api.Controllers;

/// <summary>Horários reservados pelo cliente (ou pelo Dono, no balcão).</summary>
[ApiController]
[Route("api/v1/agendamentos")]
[Authorize(Roles = Perfis.DonoOuCliente)]
[Produces("application/json")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
public sealed class AgendamentosController(IAgendamentoService agendamentos) : ControllerBase
{
    /// <summary>Horários livres de um dia, com o preço e a duração do serviço.</summary>
    /// <remarks>Grade de 30 em 30 minutos dentro do funcionamento. Um horário está livre enquanto os agendamentos que o cobrem forem menos que os lavadores ativos. O serviço precisa terminar até o fechamento e horários com menos de 1 hora de antecedência não aparecem.</remarks>
    /// <param name="data">Dia (AAAA-MM-DD), de hoje até 30 dias à frente.</param>
    /// <param name="servicoId">Serviço escolhido.</param>
    /// <param name="tipoVeiculo">Moto, Carro ou Caminhonete.</param>
    /// <param name="comCera">Com cera, quando o serviço tem essa opção para o tipo.</param>
    [HttpGet("horarios")]
    [ProducesResponseType<HorariosResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<HorariosResponse>> ListarHorarios(
        [FromQuery] DateOnly data, [FromQuery] int servicoId, [FromQuery] string tipoVeiculo, [FromQuery] bool comCera, CancellationToken ct) =>
        Ok(await agendamentos.ListarHorariosAsync(data, servicoId, tipoVeiculo, comCera, ct));

    /// <summary>Reserva um horário.</summary>
    /// <remarks>O horário é conferido de novo dentro de uma transação. O veículo precisa ser do cliente logado (404 se for de outro). Máximo de 2 agendamentos futuros por cliente.</remarks>
    [HttpPost]
    [ProducesResponseType<AgendamentoResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<AgendamentoResponse>> Criar(CriarAgendamentoRequest request, CancellationToken ct)
    {
        var criado = await agendamentos.CriarAsync(request, ct);
        return Created($"/api/v1/agendamentos/{criado.Id}", criado);
    }

    /// <summary>Agenda do dia, por horário, com todos os status.</summary>
    /// <param name="data">Dia (AAAA-MM-DD). Padrão: hoje em Brasília.</param>
    [HttpGet]
    [Authorize(Roles = Perfis.Dono)]
    [ProducesResponseType<AgendaDoDiaResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<AgendaDoDiaResponse>> ListarDoDia([FromQuery] DateOnly? data, CancellationToken ct) =>
        Ok(await agendamentos.ListarDoDiaAsync(data, ct));

    /// <summary>Agendamentos do cliente logado, próximos e passados, do mais recente para o mais antigo.</summary>
    [HttpGet("meus")]
    [Authorize(Roles = Perfis.Cliente)]
    [ProducesResponseType<IReadOnlyList<AgendamentoResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<AgendamentoResponse>>> ListarMeus(CancellationToken ct) =>
        Ok(await agendamentos.ListarMeusAsync(ct));

    /// <summary>Cancela um agendamento que ainda está agendado.</summary>
    /// <remarks>O cliente só cancela os próprios; o de outro cliente devolve 404. Agendamento já atendido, cancelado ou com falta devolve 409.</remarks>
    [HttpPatch("{id:int}/cancelar")]
    [ProducesResponseType<AgendamentoResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AgendamentoResponse>> Cancelar(int id, CancellationToken ct) =>
        Ok(await agendamentos.CancelarAsync(id, ct));

    /// <summary>Check-in: transforma o agendamento em uma OS e abre a vistoria. Somente o Dono.</summary>
    /// <remarks>O veículo e o serviço (com ou sem cera) vêm do agendamento, com o preço vigente da tabela. A OS nasce em Aguardando e o agendamento passa a Atendido. 409 se o agendamento não está mais agendado ou o veículo já está no pátio.</remarks>
    [HttpPost("{id:int}/check-in")]
    [Authorize(Roles = Perfis.Dono)]
    [ProducesResponseType<OrdemCriadaResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<OrdemCriadaResponse>> FazerCheckIn(int id, CheckInRequest request, CancellationToken ct)
    {
        var criada = await agendamentos.FazerCheckInAsync(id, request, ct);
        return Created($"/api/v1/ordens-servico/{criada.Id}", criada);
    }

    /// <summary>Marca que o cliente não compareceu ao horário. Somente o Dono.</summary>
    [HttpPatch("{id:int}/nao-compareceu")]
    [Authorize(Roles = Perfis.Dono)]
    [ProducesResponseType<AgendamentoResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AgendamentoResponse>> NaoCompareceu(int id, CancellationToken ct) =>
        Ok(await agendamentos.MarcarNaoCompareceuAsync(id, ct));
}
