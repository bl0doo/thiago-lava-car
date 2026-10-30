using System.Net;
using System.Net.Http.Json;

namespace ThiagoLavaCar.IntegrationTests;

[Collection(ApiCompartilhada.Nome)]
public sealed class AuthEndpointsTests(ApiFactory fabrica)
{
    private readonly HttpClient _http = fabrica.CreateClient();

    [Fact] // T01
    public async Task Login_do_dono_devolve_200_token_com_role_Dono_e_cookie_HttpOnly_de_refresh()
    {
        var dono = await fabrica.CriarDonoAsync();

        var resposta = await _http.LoginAsync(dono.Email!, ApiFactory.Senha);

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        var jwt = ApiClientExtensions.LerJwt(await resposta.LerTokenAsync());
        Assert.Equal("Dono", jwt.GetClaim("role").Value);
        Assert.Equal("thiagolavacar-api", jwt.Issuer);
        Assert.True(jwt.ValidTo - jwt.ValidFrom <= TimeSpan.FromMinutes(31));
        var cookie = resposta.CookieRefresh();
        Assert.NotNull(cookie);
        Assert.Contains("httponly", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("samesite=strict", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("path=/api/v1/auth", cookie, StringComparison.OrdinalIgnoreCase);
    }

    [Fact] // T02
    public async Task Senha_errada_devolve_401_em_ProblemDetails_sem_dizer_se_o_email_existe()
    {
        var dono = await fabrica.CriarDonoAsync();

        var errada = await _http.LoginAsync(dono.Email!, "errada123");
        var inexistente = await _http.LoginAsync("ninguem@lavacar.com", "errada123");

        Assert.Equal(HttpStatusCode.Unauthorized, errada.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, inexistente.StatusCode);
        Assert.Equal("application/problem+json", errada.Content.Headers.ContentType?.MediaType);
        var corpo = await errada.LerJsonAsync();
        Assert.Equal("E-mail ou senha incorretos.", corpo.GetProperty("detail").GetString());
        Assert.Equal(401, corpo.GetProperty("status").GetInt32());
        Assert.False(string.IsNullOrEmpty(corpo.GetProperty("traceId").GetString()));
        Assert.Equal((await inexistente.LerJsonAsync()).GetProperty("detail").GetString(), corpo.GetProperty("detail").GetString());
    }

    [Fact] // T03
    public async Task Quinta_senha_errada_devolve_423_e_a_senha_certa_tambem_ate_passarem_15_minutos()
    {
        var dono = await fabrica.CriarDonoAsync();

        for (var i = 0; i < 4; i++)
        {
            Assert.Equal(HttpStatusCode.Unauthorized, (await _http.LoginAsync(dono.Email!, "errada123")).StatusCode);
        }

        Assert.Equal((HttpStatusCode)423, (await _http.LoginAsync(dono.Email!, "errada123")).StatusCode);
        Assert.Equal((HttpStatusCode)423, (await _http.LoginAsync(dono.Email!, ApiFactory.Senha)).StatusCode);

        fabrica.Relogio.Advance(TimeSpan.FromMinutes(15));
        Assert.Equal(HttpStatusCode.OK, (await _http.LoginAsync(dono.Email!, ApiFactory.Senha)).StatusCode);
    }

    [Fact] // T04
    public async Task Usuario_desativado_com_senha_certa_recebe_401()
    {
        var dono = await fabrica.CriarDonoAsync(ativo: false);

        Assert.Equal(HttpStatusCode.Unauthorized, (await _http.LoginAsync(dono.Email!, ApiFactory.Senha)).StatusCode);
    }

    [Fact] // T05
    public async Task Login_do_lavador_com_PIN_abre_sessao_de_um_turno_com_cookie()
    {
        var lavador = await fabrica.CriarLavadorAsync();

        var resposta = await _http.LoginPinAsync(lavador.Id, ApiFactory.Pin);

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        var jwt = ApiClientExtensions.LerJwt(await resposta.LerTokenAsync());
        Assert.Equal("Lavador", jwt.GetClaim("role").Value);
        Assert.True(jwt.ValidTo - jwt.ValidFrom <= TimeSpan.FromMinutes(31));
        var cookie = resposta.CookieRefresh();
        Assert.NotNull(cookie);
        Assert.Contains("httponly", cookie, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Lavador_recupera_a_sessao_pelo_cookie_sem_digitar_o_PIN_de_novo()
    {
        var lavador = await fabrica.CriarLavadorAsync();
        var cookie = (await _http.LoginPinAsync(lavador.Id, ApiFactory.Pin)).CookieRefresh()!;

        var resposta = await _http.SendAsync(new HttpRequestMessage(HttpMethod.Post, $"{ApiClientExtensions.Base}/auth/refresh").ComCookie(cookie));

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        Assert.Equal("Lavador", (await resposta.LerJsonAsync()).GetProperty("usuario").GetProperty("perfil").GetString());
    }

    [Theory] // T06
    [InlineData("123")]
    [InlineData("12345")]
    [InlineData("12ab")]
    public async Task PIN_que_nao_tem_4_digitos_devolve_422_no_campo_pin(string pin)
    {
        var lavador = await fabrica.CriarLavadorAsync();

        var resposta = await _http.LoginPinAsync(lavador.Id, pin);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, resposta.StatusCode);
        var erros = (await resposta.LerJsonAsync()).GetProperty("errors");
        Assert.Equal("O PIN deve ter 4 dígitos.", erros.GetProperty("pin")[0].GetString());
    }

    [Fact]
    public async Task PIN_errado_devolve_401_e_cinco_falhas_bloqueiam_o_lavador()
    {
        var lavador = await fabrica.CriarLavadorAsync();

        for (var i = 0; i < 4; i++)
        {
            Assert.Equal(HttpStatusCode.Unauthorized, (await _http.LoginPinAsync(lavador.Id, "0000")).StatusCode);
        }

        Assert.Equal((HttpStatusCode)423, (await _http.LoginPinAsync(lavador.Id, "0000")).StatusCode);
        Assert.Equal((HttpStatusCode)423, (await _http.LoginPinAsync(lavador.Id, ApiFactory.Pin)).StatusCode);
    }

    [Fact]
    public async Task Corpo_malformado_devolve_400_e_nao_vaza_detalhes_do_parser()
    {
        var resposta = await _http.PostAsync($"{ApiClientExtensions.Base}/auth/login", new StringContent("{bad", System.Text.Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
        Assert.DoesNotContain("LineNumber", await resposta.Content.ReadAsStringAsync(), StringComparison.Ordinal);
    }

    [Fact] // T07
    public async Task Refresh_com_cookie_valido_devolve_novo_token_e_troca_o_cookie()
    {
        var dono = await fabrica.CriarDonoAsync();
        var login = await _http.LoginAsync(dono.Email!, ApiFactory.Senha);
        var cookie = login.CookieRefresh()!;

        var resposta = await _http.SendAsync(new HttpRequestMessage(HttpMethod.Post, $"{ApiClientExtensions.Base}/auth/refresh").ComCookie(cookie));

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        Assert.NotEqual(cookie.Split(';')[0], resposta.CookieRefresh()!.Split(';')[0]);
        Assert.False(string.IsNullOrEmpty(await resposta.LerTokenAsync()));
    }

    [Fact] // T08
    public async Task Refresh_ja_usado_devolve_401_e_derruba_tambem_o_token_novo()
    {
        var dono = await fabrica.CriarDonoAsync();
        var cookieAntigo = (await _http.LoginAsync(dono.Email!, ApiFactory.Senha)).CookieRefresh()!;
        var rotacao = await _http.SendAsync(new HttpRequestMessage(HttpMethod.Post, $"{ApiClientExtensions.Base}/auth/refresh").ComCookie(cookieAntigo));
        var cookieNovo = rotacao.CookieRefresh()!;

        var reuso = await _http.SendAsync(new HttpRequestMessage(HttpMethod.Post, $"{ApiClientExtensions.Base}/auth/refresh").ComCookie(cookieAntigo));
        var novoDepoisDoReuso = await _http.SendAsync(new HttpRequestMessage(HttpMethod.Post, $"{ApiClientExtensions.Base}/auth/refresh").ComCookie(cookieNovo));

        Assert.Equal(HttpStatusCode.Unauthorized, reuso.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, novoDepoisDoReuso.StatusCode);
    }

    [Fact]
    public async Task Refresh_sem_cookie_devolve_401()
    {
        var resposta = await _http.PostAsync($"{ApiClientExtensions.Base}/auth/refresh", null);

        Assert.Equal(HttpStatusCode.Unauthorized, resposta.StatusCode);
    }

    [Fact]
    public async Task Logout_revoga_o_refresh_token()
    {
        var dono = await fabrica.CriarDonoAsync();
        var login = await _http.LoginAsync(dono.Email!, ApiFactory.Senha);
        var cookie = login.CookieRefresh()!;
        using var autenticado = await fabrica.AutenticadoAsync(dono.Email!);

        var logout = await autenticado.SendAsync(new HttpRequestMessage(HttpMethod.Post, $"{ApiClientExtensions.Base}/auth/logout").ComCookie(cookie));
        var refresh = await _http.SendAsync(new HttpRequestMessage(HttpMethod.Post, $"{ApiClientExtensions.Base}/auth/refresh").ComCookie(cookie));

        Assert.Equal(HttpStatusCode.NoContent, logout.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, refresh.StatusCode);
    }

    [Fact]
    public async Task Lista_de_lavadores_e_publica_e_mostra_so_ativos_com_apelido()
    {
        var lavador = await fabrica.CriarLavadorAsync($"Zeca{Guid.NewGuid():N}"[..12]);

        var resposta = await _http.GetAsync($"{ApiClientExtensions.Base}/auth/lavadores");

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        var lista = await resposta.Content.ReadFromJsonAsync<List<Dictionary<string, object>>>();
        Assert.Contains(lista!, l => l["apelido"].ToString() == lavador.Apelido);
        Assert.All(lista!, l => Assert.Equal(2, l.Count));
    }

    [Fact]
    public async Task Me_devolve_o_usuario_do_token_e_exige_login()
    {
        var dono = await fabrica.CriarDonoAsync();
        using var autenticado = await fabrica.AutenticadoAsync(dono.Email!);

        var eu = await autenticado.GetAsync($"{ApiClientExtensions.Base}/auth/me");
        var anonimo = await _http.GetAsync($"{ApiClientExtensions.Base}/auth/me");

        Assert.Equal(HttpStatusCode.OK, eu.StatusCode);
        Assert.Equal("Dono", (await eu.LerJsonAsync()).GetProperty("perfil").GetString());
        Assert.Equal(HttpStatusCode.Unauthorized, anonimo.StatusCode);
    }

    private static object Cadastro(string email, string telefone, string senha = "Senha123", bool lgpd = true) =>
        new { nome = "Renata Souza", telefone, email, senha, canalPreferido = "WhatsApp", consentimentoLgpd = lgpd };

    [Fact]
    public async Task Cadastro_de_cliente_devolve_201_token_com_role_Cliente_e_o_cliente_ja_acessa_a_propria_sessao()
    {
        var email = $"novo-{Guid.NewGuid():N}@email.com";

        var resposta = await _http.PostAsJsonAsync($"{ApiClientExtensions.Base}/auth/cadastro-cliente", Cadastro(email, ApiFactory.NovoTelefone()));

        Assert.Equal(HttpStatusCode.Created, resposta.StatusCode);
        Assert.NotNull(resposta.CookieRefresh());
        var jwt = ApiClientExtensions.LerJwt(await resposta.LerTokenAsync());
        Assert.Equal("Cliente", jwt.Claims.First(c => c.Type == "role").Value);
        Assert.Contains(jwt.Claims, c => c.Type == "clienteId");
        Assert.Equal(HttpStatusCode.OK, (await _http.LoginAsync(email, "Senha123")).StatusCode);
    }

    [Fact]
    public async Task Cadastro_sem_consentimento_ou_com_senha_fraca_devolve_422_com_os_campos()
    {
        var resposta = await _http.PostAsJsonAsync($"{ApiClientExtensions.Base}/auth/cadastro-cliente",
            Cadastro($"x-{Guid.NewGuid():N}@email.com", ApiFactory.NovoTelefone(), senha: "abcdefgh", lgpd: false));

        Assert.Equal(HttpStatusCode.UnprocessableEntity, resposta.StatusCode);
        var erros = (await resposta.LerJsonAsync()).GetProperty("errors");
        Assert.True(erros.TryGetProperty("senha", out _));
        Assert.True(erros.TryGetProperty("consentimentoLgpd", out _));
    }

    [Fact]
    public async Task Cadastro_com_email_ou_telefone_repetido_devolve_409()
    {
        var existente = await fabrica.CriarClienteComLoginAsync();
        var telefone = ApiFactory.NovoTelefone();

        var porEmail = await _http.PostAsJsonAsync($"{ApiClientExtensions.Base}/auth/cadastro-cliente", Cadastro(existente.Email!, telefone));
        var primeiro = await _http.PostAsJsonAsync($"{ApiClientExtensions.Base}/auth/cadastro-cliente", Cadastro($"a-{Guid.NewGuid():N}@email.com", telefone));
        var porTelefone = await _http.PostAsJsonAsync($"{ApiClientExtensions.Base}/auth/cadastro-cliente", Cadastro($"b-{Guid.NewGuid():N}@email.com", telefone));

        Assert.Equal(HttpStatusCode.Conflict, porEmail.StatusCode);
        Assert.Equal(HttpStatusCode.Created, primeiro.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, porTelefone.StatusCode);
    }
}
