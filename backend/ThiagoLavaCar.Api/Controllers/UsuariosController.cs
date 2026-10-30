using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ThiagoLavaCar.Api.Infrastructure;
using ThiagoLavaCar.Application.DTOs;
using ThiagoLavaCar.Application.Services;

namespace ThiagoLavaCar.Api.Controllers;

/// <summary>Gestão de acessos feita pelo Dono: editar usuário, ativar/desativar e redefinir o PIN do lavador.</summary>
[ApiController]
[Route("api/v1/usuarios")]
[Authorize(Roles = Perfis.Dono)]
[Produces("application/json")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
public sealed class UsuariosController(IUsuarioService usuarios) : ControllerBase
{
    /// <summary>Lista os usuários, ativos ou não, por nome.</summary>
    /// <param name="perfil">Filtra por Dono, Lavador ou Cliente. Sem filtro, devolve todos.</param>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<UsuarioDetalheResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<IReadOnlyList<UsuarioDetalheResponse>>> Listar([FromQuery] string? perfil, CancellationToken ct) =>
        Ok(await usuarios.ListarAsync(perfil, ct));

    /// <summary>Cadastra um lavador com apelido e PIN de 4 dígitos.</summary>
    /// <remarks>O apelido é o botão que o lavador toca para entrar; não pode repetir (409). PIN óbvio, como 1234 ou 0000, é recusado.</remarks>
    [HttpPost]
    [ProducesResponseType<UsuarioDetalheResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<UsuarioDetalheResponse>> CriarLavador(CriarLavadorRequest request, CancellationToken ct)
    {
        var criado = await usuarios.CriarLavadorAsync(request, ct);
        return Created($"/api/v1/usuarios?perfil=Lavador", criado);
    }

    /// <summary>Edita nome e apelido (o apelido só vale para lavador).</summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType<UsuarioDetalheResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<UsuarioDetalheResponse>> Editar(int id, EditarUsuarioRequest request, CancellationToken ct) =>
        Ok(await usuarios.EditarAsync(id, request, ct));

    /// <summary>Ativa ou desativa o acesso. Desativar revoga as sessões do usuário.</summary>
    [HttpPatch("{id:int}/ativo")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> AlterarAtivo(int id, AlterarAtivoRequest request, CancellationToken ct)
    {
        await usuarios.AlterarAtivoAsync(id, request.Ativo, ct);
        return NoContent();
    }

    /// <summary>Redefine o PIN (4 dígitos) do lavador, zera o bloqueio e revoga as sessões abertas.</summary>
    [HttpPut("{id:int}/pin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> RedefinirPin(int id, RedefinirPinRequest request, CancellationToken ct)
    {
        await usuarios.RedefinirPinAsync(id, request.Pin, ct);
        return NoContent();
    }
}
