using System.Net;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace ThiagoLavaCar.IntegrationTests;

/// <summary>Prova a regra "seguro por padrão": cada rota da API é pública de propósito ou só abre para o perfil previsto.</summary>
[Collection(ApiCompartilhada.Nome)]
public sealed class MatrizDeAcessoTests(ApiFactory fabrica)
{
    private static readonly string[] RotasPublicas =
    [
        "POST /api/v1/auth/login",
        "GET /api/v1/auth/lavadores",
        "POST /api/v1/auth/login-pin",
        "POST /api/v1/auth/refresh",
        "POST /api/v1/auth/cadastro-cliente",
        "GET /health",
    ];

    private static string? Chave(Endpoint e)
    {
        if (e is not RouteEndpoint rota)
        {
            return null;
        }

        var metodo = e.Metadata.GetMetadata<HttpMethodMetadata>()?.HttpMethods[0];
        return metodo is null ? null : $"{metodo} /{rota.RoutePattern.RawText!.TrimStart('/')}";
    }

    private IEnumerable<(string Chave, RouteEndpoint Endpoint)> Rotas() =>
        fabrica.Services.GetRequiredService<EndpointDataSource>().Endpoints
            .OfType<RouteEndpoint>()
            .Where(e => e.RoutePattern.RawText?.StartsWith("api/v1", StringComparison.Ordinal) == true || e.RoutePattern.RawText == "/health")
            .Select(e => (Chave(e)!, e))
            .Where(x => x.Item1 is not null);

    [Fact]
    public void Toda_rota_e_publica_de_proposito_ou_exige_autorizacao()
    {
        var abertasPorEngano = Rotas()
            .Where(r => r.Endpoint.Metadata.GetMetadata<IAllowAnonymous>() is not null)
            .Select(r => r.Chave)
            .Where(c => !RotasPublicas.Contains(c))
            .ToList();

        Assert.Empty(abertasPorEngano);
    }

    [Fact]
    public async Task Sem_token_toda_rota_protegida_devolve_401()
    {
        using var http = fabrica.CreateClient();

        foreach (var (chave, _) in Rotas())
        {
            var (metodo, caminho) = (chave.Split(' ')[0], chave.Split(' ')[1]);
            var url = System.Text.RegularExpressions.Regex.Replace(caminho, @"\{[^}]+\}", "1");
            var resposta = await http.SendAsync(new HttpRequestMessage(new HttpMethod(metodo), url));

            if (!RotasPublicas.Contains(chave))
            {
                Assert.True(resposta.StatusCode == HttpStatusCode.Unauthorized, $"{chave} devolveu {(int)resposta.StatusCode} sem token");
            }
        }
    }

    [Fact]
    public async Task Rotas_so_do_Dono_recusam_Lavador_e_Cliente_com_403()
    {
        var lavador = await fabrica.CriarLavadorAsync("Hugo");
        var cliente = await fabrica.CriarClienteComLoginAsync();
        using var comoLavador = await fabrica.AutenticadoPorPinAsync(lavador.Id);
        using var comoCliente = await fabrica.AutenticadoAsync(cliente.Email!);

        var soDono = Rotas().Where(r => r.Endpoint.Metadata.GetOrderedMetadata<IAuthorizeData>().Any(a => a.Roles == "Dono")).ToList();
        Assert.NotEmpty(soDono);

        foreach (var (chave, _) in soDono)
        {
            var url = System.Text.RegularExpressions.Regex.Replace(chave.Split(' ')[1], @"\{[^}]+\}", "1");
            foreach (var http in new[] { comoLavador, comoCliente })
            {
                var resposta = await http.SendAsync(new HttpRequestMessage(new HttpMethod(chave.Split(' ')[0]), url));
                Assert.True(resposta.StatusCode == HttpStatusCode.Forbidden, $"{chave} devolveu {(int)resposta.StatusCode} para perfil sem permissão");
            }
        }
    }

    [Fact]
    public async Task Cliente_nao_acessa_o_quadro_do_Pit_Stop()
    {
        var cliente = await fabrica.CriarClienteComLoginAsync();
        using var http = await fabrica.AutenticadoAsync(cliente.Email!);

        var resposta = await http.GetAsync($"{ApiClientExtensions.Base}/ordens-servico/quadro");

        Assert.Equal(HttpStatusCode.Forbidden, resposta.StatusCode);
    }

    [Fact]
    public async Task Token_adulterado_ou_de_outro_emissor_e_recusado()
    {
        var dono = await fabrica.CriarDonoAsync();
        using var http = await fabrica.AutenticadoAsync(dono.Email!);
        var token = http.DefaultRequestHeaders.Authorization!.Parameter!;
        var partes = token.Split('.');
        var adulterado = $"{partes[0]}.{partes[1]}.{new string(partes[2].Reverse().ToArray())}";

        using var invasor = fabrica.CreateClient();
        invasor.DefaultRequestHeaders.Authorization = new("Bearer", adulterado);

        Assert.Equal(HttpStatusCode.Unauthorized, (await invasor.GetAsync($"{ApiClientExtensions.Base}/auth/me")).StatusCode);
    }

    [Fact]
    public async Task Token_expirado_devolve_401()
    {
        var dono = await fabrica.CriarDonoAsync();
        using var http = await fabrica.AutenticadoAsync(dono.Email!);

        // o relógio é compartilhado, mas só anda para frente e cada teste faz login novo
        fabrica.Relogio.Advance(TimeSpan.FromHours(2));

        Assert.Equal(HttpStatusCode.Unauthorized, (await http.GetAsync($"{ApiClientExtensions.Base}/auth/me")).StatusCode);
    }
}
