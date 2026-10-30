using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using ThiagoLavaCar.Api.Extensions;
using ThiagoLavaCar.Application.DTOs;
using ThiagoLavaCar.Application.Services;

namespace ThiagoLavaCar.Api.Controllers;

/// <summary>Login, renovação e encerramento de sessão.</summary>
[ApiController]
[Route("api/v1/auth")]
[Produces("application/json")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
public sealed class AuthController(IAuthService auth, IWebHostEnvironment ambiente) : ControllerBase
{
    private const string CookieRefresh = "refresh_token";
    private const string CaminhoDoCookie = "/api/v1/auth";

    /// <summary>Login de Dono ou Cliente com e-mail e senha.</summary>
    /// <remarks>Após 5 falhas seguidas o usuário fica bloqueado por 15 minutos. O refresh token vai em cookie HttpOnly.</remarks>
    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting(ServiceCollectionExtensions.PoliticaRateLimitAuth)]
    [ProducesResponseType<AuthResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status423Locked)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest request, CancellationToken ct) =>
        Responder(await auth.LoginAsync(request, ct));

    /// <summary>Lavadores ativos (apelidos) para a tela de PIN.</summary>
    [HttpGet("lavadores")]
    [AllowAnonymous]
    [EnableRateLimiting(ServiceCollectionExtensions.PoliticaRateLimitAuth)]
    [ProducesResponseType<IReadOnlyList<LavadorResumoResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<LavadorResumoResponse>>> Lavadores(CancellationToken ct) =>
        Ok(await auth.ListarLavadoresAsync(ct));

    /// <summary>Login do lavador com PIN de 4 dígitos. A sessão dura o turno (12 h): recarregar a página não pede o PIN de novo.</summary>
    [HttpPost("login-pin")]
    [AllowAnonymous]
    [EnableRateLimiting(ServiceCollectionExtensions.PoliticaRateLimitAuth)]
    [ProducesResponseType<AuthResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status423Locked)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult<AuthResponse>> LoginPin(LoginPinRequest request, CancellationToken ct) =>
        Responder(await auth.LoginPinAsync(request, ct));

    /// <summary>Cliente cria a própria conta (com consentimento LGPD) e já entra.</summary>
    /// <remarks>Cria o usuário com perfil Cliente e a ficha de cliente ligada a ele. Telefone e e-mail são únicos.</remarks>
    [HttpPost("cadastro-cliente")]
    [AllowAnonymous]
    [EnableRateLimiting(ServiceCollectionExtensions.PoliticaRateLimitAuth)]
    [ProducesResponseType<AuthResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult<AuthResponse>> CadastroCliente(CadastroClienteRequest request, CancellationToken ct)
    {
        var sessao = await auth.CadastrarClienteAsync(request, ct);
        GravarCookie(sessao);
        return StatusCode(StatusCodes.Status201Created, sessao.Resposta);
    }

    /// <summary>Troca o refresh token do cookie por um novo par (rotação). Token já usado revoga a sessão inteira.</summary>
    [HttpPost("refresh")]
    [AllowAnonymous]
    [EnableRateLimiting(ServiceCollectionExtensions.PoliticaRateLimitAuth)]
    [ProducesResponseType<AuthResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult<AuthResponse>> Refresh(CancellationToken ct) =>
        Responder(await auth.RenovarAsync(Request.Cookies[CookieRefresh], ct));

    /// <summary>Revoga o refresh token e apaga o cookie.</summary>
    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Logout(CancellationToken ct)
    {
        await auth.SairAsync(Request.Cookies[CookieRefresh], ct);
        ApagarCookie();
        return NoContent();
    }

    /// <summary>Dados do usuário logado.</summary>
    [HttpGet("me")]
    [ProducesResponseType<UsuarioResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<UsuarioResponse>> Me(CancellationToken ct) =>
        Ok(await auth.ObterUsuarioLogadoAsync(ct));

    private ActionResult<AuthResponse> Responder(SessaoIniciada sessao)
    {
        GravarCookie(sessao);
        return Ok(sessao.Resposta);
    }

    private void GravarCookie(SessaoIniciada sessao) =>
        Response.Cookies.Append(CookieRefresh, sessao.RefreshToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = !ambiente.IsDevelopment(),
            SameSite = SameSiteMode.Strict,
            Path = CaminhoDoCookie,
            Expires = sessao.RefreshExpiraEm,
        });

    private void ApagarCookie() => Response.Cookies.Delete(CookieRefresh, new CookieOptions { Path = CaminhoDoCookie });
}
