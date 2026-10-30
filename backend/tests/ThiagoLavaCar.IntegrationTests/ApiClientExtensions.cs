using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.IdentityModel.JsonWebTokens;

namespace ThiagoLavaCar.IntegrationTests;

internal static class ApiClientExtensions
{
    public const string Base = "/api/v1";

    public static async Task<HttpResponseMessage> LoginAsync(this HttpClient http, string email, string senha) =>
        await http.PostAsJsonAsync($"{Base}/auth/login", new { email, senha });

    public static async Task<HttpResponseMessage> LoginPinAsync(this HttpClient http, int usuarioId, string pin) =>
        await http.PostAsJsonAsync($"{Base}/auth/login-pin", new { usuarioId, pin });

    /// <summary>Faz login e devolve um cliente HTTP já com o Bearer configurado.</summary>
    public static async Task<HttpClient> AutenticadoAsync(this ApiFactory fabrica, string email, string senha = ApiFactory.Senha)
    {
        var http = fabrica.CreateClient();
        var resposta = await http.LoginAsync(email, senha);
        resposta.EnsureSuccessStatusCode();
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await resposta.LerTokenAsync());
        return http;
    }

    public static async Task<HttpClient> AutenticadoPorPinAsync(this ApiFactory fabrica, int usuarioId, string pin = ApiFactory.Pin)
    {
        var http = fabrica.CreateClient();
        var resposta = await http.LoginPinAsync(usuarioId, pin);
        resposta.EnsureSuccessStatusCode();
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await resposta.LerTokenAsync());
        return http;
    }

    public static async Task<string> LerTokenAsync(this HttpResponseMessage resposta)
    {
        using var json = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync());
        return json.RootElement.GetProperty("accessToken").GetString()!;
    }

    public static async Task<JsonElement> LerJsonAsync(this HttpResponseMessage resposta)
    {
        using var json = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync());
        return json.RootElement.Clone();
    }

    public static JsonWebToken LerJwt(string token) => new JsonWebTokenHandler().ReadJsonWebToken(token);

    /// <summary>Valor do cookie de refresh devolvido no Set-Cookie (sem atributos).</summary>
    public static string? CookieRefresh(this HttpResponseMessage resposta) =>
        resposta.Headers.TryGetValues("Set-Cookie", out var valores)
            ? valores.FirstOrDefault(v => v.StartsWith("refresh_token=", StringComparison.Ordinal))
            : null;

    public static HttpRequestMessage ComCookie(this HttpRequestMessage req, string setCookie)
    {
        req.Headers.Add("Cookie", setCookie.Split(';')[0]);
        return req;
    }
}
