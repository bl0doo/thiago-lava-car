using System.Reflection;
using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using ThiagoLavaCar.Api.Infrastructure;
using ThiagoLavaCar.Application.Abstractions;
using ThiagoLavaCar.Domain.Enums;
using ThiagoLavaCar.Infrastructure.Security;

namespace ThiagoLavaCar.Api.Extensions;

internal static class ServiceCollectionExtensions
{
    private static readonly string[] MensagemCampoInvalido = ["Valor inválido ou ausente."];

    public const string PoliticaCors = "Frontend";
    public const string PoliticaRateLimitAuth = "auth";

    public static IServiceCollection AddApiControllers(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();
        services.AddScoped<IUsuarioLogado, UsuarioLogado>();

        services.AddControllers(o => o.Filters.Add<ValidacaoFilter>())
            .ConfigureApiBehaviorOptions(o =>
            {
                // JSON malformado ou parâmetro de rota/consulta no formato errado: 400 (a validação de campos é 422).
                o.InvalidModelStateResponseFactory = contexto =>
                {
                    var problema = new ValidationProblemDetails(CamposInvalidos(contexto))
                    {
                        Status = StatusCodes.Status400BadRequest,
                        Detail = "A requisição está mal formada. Confira o corpo e os parâmetros enviados.",
                    };
                    ProblemasPadrao.Completar(problema, contexto.HttpContext);
                    return new ObjectResult(problema) { StatusCode = StatusCodes.Status400BadRequest };
                };
            });

        services.AddProblemDetails(o => o.CustomizeProblemDetails = ProblemasPadrao.Completar);
        services.AddExceptionHandler<TratamentoDeErros>();
        return services;
    }

    /// <summary>Só os nomes dos campos, com mensagem genérica em português: a mensagem do parser de JSON não sai da API.</summary>
    private static Dictionary<string, string[]> CamposInvalidos(ActionContext contexto)
    {
        var parametros = contexto.ActionDescriptor.Parameters.Select(p => p.Name).ToHashSet(StringComparer.Ordinal);
        var campos = contexto.ModelState
            .Where(e => e.Value?.Errors.Count > 0)
            .Select(e => e.Key.StartsWith('$') ? (e.Key.Length > 2 ? e.Key[2..] : "corpo") : e.Key)
            .Distinct(StringComparer.Ordinal)
            .ToList();
        if (campos.Count > 1)
        {
            campos.RemoveAll(parametros.Contains);
        }

        return campos.ToDictionary(c => System.Text.Json.JsonNamingPolicy.CamelCase.ConvertName(c), _ => MensagemCampoInvalido);
    }

    public static IServiceCollection AddJwtAuth(this IServiceCollection services)
    {
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();

        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<JwtOptions>, TimeProvider>((bearer, jwt, relogio) =>
            {
                var o = jwt.Value;
                bearer.MapInboundClaims = false;
                bearer.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = o.Issuer,
                    ValidateAudience = true,
                    ValidAudience = o.Audience,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(o.Key)),
                    ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
                    ClockSkew = TimeSpan.FromMinutes(1),
                    // validade conferida pelo mesmo relógio que emite os tokens (e que os testes controlam)
                    LifetimeValidator = (inicio, fim, _, parametros) =>
                    {
                        var agora = relogio.GetUtcNow().UtcDateTime;
                        return fim is not null
                            && (inicio is null || inicio.Value - parametros.ClockSkew <= agora)
                            && fim.Value + parametros.ClockSkew > agora;
                    },
                    NameClaimType = "name",
                    RoleClaimType = ClaimsNames.Role,
                };
                bearer.Events = new JwtBearerEvents { OnTokenValidated = ConferirLavadorAtivoAsync };
            });

        // Seguro por padrão: toda rota exige login, exceto as marcadas com [AllowAnonymous].
        services.AddAuthorizationBuilder()
            .SetFallbackPolicy(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());
        return services;
    }

    /// <summary>A sessão do lavador dura o turno; a API confere no banco a cada requisição se ele continua ativo (desativar derruba na hora).</summary>
    private static async Task ConferirLavadorAtivoAsync(TokenValidatedContext contexto)
    {
        var principal = contexto.Principal;
        if (principal?.FindFirst(ClaimsNames.Role)?.Value != nameof(Perfil.Lavador))
        {
            return;
        }

        var sub = principal.FindFirst("sub")?.Value;
        var usuarios = contexto.HttpContext.RequestServices.GetRequiredService<IUsuarioRepository>();
        var usuario = int.TryParse(sub, out var id)
            ? await usuarios.ObterPorIdAsync(id, contexto.HttpContext.RequestAborted)
            : null;

        if (usuario is not { Ativo: true })
        {
            contexto.Fail("Usuário inativo.");
        }
    }

    public static IServiceCollection AddCorsDoFrontend(this IServiceCollection services, IConfiguration configuration)
    {
        var origens = configuration.GetSection("Cors:Origens").Get<string[]>() ?? [];
        services.AddCors(o => o.AddPolicy(PoliticaCors, p => p
            .WithOrigins(origens)
            .WithHeaders("Authorization", "Content-Type")
            .WithMethods("GET", "POST", "PUT", "PATCH", "DELETE")
            .AllowCredentials()));
        return services;
    }

    /// <summary>Limite por IP nas rotas /auth/*.</summary>
    public static IServiceCollection AddRateLimitDeLogin(this IServiceCollection services, IConfiguration configuration)
    {
        var porMinuto = configuration.GetValue("RateLimit:AuthPorMinuto", 10);
        services.AddRateLimiter(o =>
        {
            o.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            o.AddPolicy(PoliticaRateLimitAuth, http => RateLimitPartition.GetFixedWindowLimiter(
                http.Connection.RemoteIpAddress?.ToString() ?? "desconhecido",
                _ => new FixedWindowRateLimiterOptions { PermitLimit = porMinuto, Window = TimeSpan.FromMinutes(1) }));
            o.OnRejected = async (contexto, ct) =>
            {
                var problemas = contexto.HttpContext.RequestServices.GetRequiredService<IProblemDetailsService>();
                await problemas.TryWriteAsync(new ProblemDetailsContext
                {
                    HttpContext = contexto.HttpContext,
                    ProblemDetails = { Status = StatusCodes.Status429TooManyRequests, Detail = "Muitas tentativas. Aguarde um minuto e tente de novo." },
                });
            };
        });
        return services;
    }

    public static IServiceCollection AddSwaggerComJwt(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(o =>
        {
            o.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "Thiago Lava Car API",
                Version = "v1",
                Description = "API do sistema de gestão do lava-jato: autenticação, quadro Pit Stop e temporizador. " +
                              "Erros seguem ProblemDetails (RFC 9457) em português.",
            });
            o.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                Description = "Cole o accessToken devolvido por /auth/login ou /auth/login-pin.",
            });
            o.AddSecurityRequirement(documento => new OpenApiSecurityRequirement
            {
                [new OpenApiSecuritySchemeReference("Bearer", documento)] = [],
            });
            o.SupportNonNullableReferenceTypes();
            o.SchemaFilter<PropriedadesNaoNulasSaoObrigatorias>();
            o.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, $"{Assembly.GetExecutingAssembly().GetName().Name}.xml"));
        });
        return services;
    }
}
