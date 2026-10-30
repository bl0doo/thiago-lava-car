using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ThiagoLavaCar.Api.Infrastructure;
using ThiagoLavaCar.Application.DTOs;
using ThiagoLavaCar.Application.Services;

namespace ThiagoLavaCar.Api.Controllers;

/// <summary>Quadro Pit Stop e temporizador das ordens de serviço. O Lavador recebe os cartões sem valores nem contato do cliente.</summary>
[ApiController]
[Route("api/v1/ordens-servico")]
[Authorize(Roles = Perfis.DonoOuLavador)]
[Produces("application/json")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
public sealed class OrdensServicoController(IOrdemServicoService ordens, IOrdemDetalheService detalhes) : ControllerBase
{
    /// <summary>Registra a entrada do carro com a vistoria; o cartão nasce em Aguardando.</summary>
    /// <remarks>Preço de cada item pela tabela do tipo do veículo, copiado para o item. Um veículo não pode ter duas OS abertas.</remarks>
    [HttpPost]
    [Authorize(Roles = Perfis.Dono)]
    [ProducesResponseType<OrdemCriadaResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<OrdemCriadaResponse>> Registrar(RegistrarEntradaRequest request, CancellationToken ct)
    {
        var criada = await ordens.RegistrarEntradaAsync(request, ct);
        return CreatedAtAction(nameof(Obter), new { id = criada.Id }, criada);
    }

    /// <summary>Edita os serviços e textos de uma OS em Aguardando; preços são recalculados pela tabela.</summary>
    [HttpPut("{id}")]
    [Authorize(Roles = Perfis.Dono)]
    [ProducesResponseType<CartaoResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<CartaoResponse>> Editar(int id, EditarOrdemRequest request, CancellationToken ct) =>
        Ok(await ordens.EditarAsync(id, request, ct));

    /// <summary>Registra a entrega do carro ao cliente (Concluído para Entregue).</summary>
    [HttpPost("{id}/entregar")]
    [Authorize(Roles = Perfis.Dono)]
    [ProducesResponseType<CartaoResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CartaoResponse>> Entregar(int id, EntregarRequest? request, CancellationToken ct) =>
        Ok(await ordens.EntregarAsync(id, request?.NomeQuemRetira, ct));

    /// <summary>Cancela uma OS que ainda está em Aguardando.</summary>
    [HttpPost("{id}/cancelar")]
    [Authorize(Roles = Perfis.Dono)]
    [ProducesResponseType<CartaoResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CartaoResponse>> Cancelar(int id, CancellationToken ct) =>
        Ok(await ordens.CancelarAsync(id, ct));

    /// <summary>Registra a nota de 1 a 5 dada pelo cliente.</summary>
    [HttpPatch("{id}/avaliacao")]
    [Authorize(Roles = Perfis.Dono)]
    [ProducesResponseType<CartaoResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<CartaoResponse>> Avaliar(int id, AvaliacaoRequest request, CancellationToken ct) =>
        Ok(await ordens.AvaliarAsync(id, request.Nota, ct));

    /// <summary>Registra pertences encontrados no carro durante a lavagem.</summary>
    [HttpPatch("{id}/pertences-encontrados")]
    [ProducesResponseType<CartaoResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<CartaoResponse>> PertencesEncontrados(int id, PertencesEncontradosRequest request, CancellationToken ct) =>
        Ok(await ordens.RegistrarPertencesEncontradosAsync(id, request.Texto, ct));

    /// <summary>Quadro do dia em três colunas: Aguardando, Em andamento e Concluído.</summary>
    /// <param name="data">Dia em Brasília (AAAA-MM-DD). Padrão: hoje. Cartões ainda abertos aparecem sempre.</param>
    [HttpGet("quadro")]
    [ProducesResponseType<QuadroResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<QuadroResponse>> Quadro([FromQuery] DateOnly? data, CancellationToken ct) =>
        Ok(await ordens.ObterQuadroAsync(data, ct));

    /// <summary>Detalhe de um cartão.</summary>
    [HttpGet("{id}")]
    [ProducesResponseType<CartaoResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CartaoResponse>> Obter(int id, CancellationToken ct) =>
        Ok(await ordens.ObterAsync(id, ct));

    /// <summary>Tela de detalhe da OS para o Dono: itens, vistoria, avaliação e pagamentos.</summary>
    /// <remarks>Reúne o cartão com itens (preço final de cada um), pertences de entrada, quem retira, avarias, nota, valor pago, saldo pendente e todos os pagamentos da OS (inclusive os estornados, que não entram no valor pago).</remarks>
    [HttpGet("{id:int}/detalhe")]
    [Authorize(Roles = Perfis.Dono)]
    [ProducesResponseType<DetalheOrdemResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DetalheOrdemResponse>> Detalhe(int id, CancellationToken ct) =>
        Ok(await detalhes.ObterAsync(id, ct));

    /// <summary>Inicia o temporizador (Aguardando para Em andamento). A hora é a do servidor.</summary>
    /// <param name="id">Número da ordem de serviço.</param>
    /// <param name="lavadorId">Só o dono pode indicar outro lavador como responsável.</param>
    [HttpPost("{id}/iniciar")]
    [ProducesResponseType<CartaoResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<CartaoResponse>> Iniciar(int id, [FromQuery] int? lavadorId, CancellationToken ct) =>
        Ok(await ordens.IniciarAsync(id, lavadorId, ct));

    /// <summary>Encerra o temporizador (Em andamento para Concluído) e calcula a duração real.</summary>
    [HttpPost("{id}/encerrar")]
    [ProducesResponseType<CartaoResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CartaoResponse>> Encerrar(int id, CancellationToken ct) =>
        Ok(await ordens.EncerrarAsync(id, ct));

    /// <summary>Desfaz o último passo (iniciar ou encerrar) em até 2 minutos.</summary>
    [HttpPost("{id}/desfazer")]
    [ProducesResponseType<CartaoResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CartaoResponse>> Desfazer(int id, CancellationToken ct) =>
        Ok(await ordens.DesfazerAsync(id, ct));
}
