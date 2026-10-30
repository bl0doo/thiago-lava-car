using System.Net;
using System.Net.Http.Json;

namespace ThiagoLavaCar.IntegrationTests;

[Collection(ApiCompartilhada.Nome)]
public sealed class UsuariosEndpointsTests(ApiFactory fabrica)
{
    private static string Rota(int id, string sufixo = "") => $"{ApiClientExtensions.Base}/usuarios/{id}{sufixo}";

    [Fact]
    public async Task Dono_redefine_o_PIN_do_lavador_que_entra_com_o_novo_e_perde_o_antigo_e_as_sessoes()
    {
        var dono = await fabrica.CriarDonoAsync();
        var lavador = await fabrica.CriarLavadorAsync("Beto");
        using var admin = await fabrica.AutenticadoAsync(dono.Email!);
        var cookieAntigo = (await fabrica.CreateClient().LoginPinAsync(lavador.Id, ApiFactory.Pin)).CookieRefresh()!;

        var resposta = await admin.PutAsJsonAsync(Rota(lavador.Id, "/pin"), new { pin = "7391" });

        Assert.Equal(HttpStatusCode.NoContent, resposta.StatusCode);
        using var http = fabrica.CreateClient();
        Assert.Equal(HttpStatusCode.OK, (await http.LoginPinAsync(lavador.Id, "7391")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await http.LoginPinAsync(lavador.Id, ApiFactory.Pin)).StatusCode);
        var renovar = await http.SendAsync(new HttpRequestMessage(HttpMethod.Post, $"{ApiClientExtensions.Base}/auth/refresh").ComCookie(cookieAntigo));
        Assert.Equal(HttpStatusCode.Unauthorized, renovar.StatusCode);
    }

    [Theory]
    [InlineData("1234")]
    [InlineData("0000")]
    [InlineData("12")]
    [InlineData("abcd")]
    public async Task PIN_obvio_ou_malformado_devolve_422(string pin)
    {
        var dono = await fabrica.CriarDonoAsync();
        var lavador = await fabrica.CriarLavadorAsync("Caio");
        using var admin = await fabrica.AutenticadoAsync(dono.Email!);

        var resposta = await admin.PutAsJsonAsync(Rota(lavador.Id, "/pin"), new { pin });

        Assert.Equal(HttpStatusCode.UnprocessableEntity, resposta.StatusCode);
        Assert.True((await resposta.LerJsonAsync()).GetProperty("errors").TryGetProperty("pin", out _));
    }

    [Fact]
    public async Task Redefinir_PIN_de_quem_nao_e_lavador_devolve_422_e_de_usuario_inexistente_404()
    {
        var dono = await fabrica.CriarDonoAsync();
        using var admin = await fabrica.AutenticadoAsync(dono.Email!);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await admin.PutAsJsonAsync(Rota(dono.Id, "/pin"), new { pin = "7391" })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await admin.PutAsJsonAsync(Rota(999_999, "/pin"), new { pin = "7391" })).StatusCode);
    }

    [Fact]
    public async Task Desativar_lavador_derruba_o_token_que_ele_ja_tinha_e_ativar_libera_de_novo()
    {
        var dono = await fabrica.CriarDonoAsync();
        var lavador = await fabrica.CriarLavadorAsync("Edu");
        using var admin = await fabrica.AutenticadoAsync(dono.Email!);
        using var sessaoLavador = await fabrica.AutenticadoPorPinAsync(lavador.Id);
        Assert.Equal(HttpStatusCode.OK, (await sessaoLavador.GetAsync($"{ApiClientExtensions.Base}/ordens-servico/quadro")).StatusCode);

        var desativar = await admin.PatchAsJsonAsync(Rota(lavador.Id, "/ativo"), new { ativo = false });

        Assert.Equal(HttpStatusCode.NoContent, desativar.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await sessaoLavador.GetAsync($"{ApiClientExtensions.Base}/ordens-servico/quadro")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await fabrica.CreateClient().LoginPinAsync(lavador.Id, ApiFactory.Pin)).StatusCode);

        await admin.PatchAsJsonAsync(Rota(lavador.Id, "/ativo"), new { ativo = true });
        Assert.Equal(HttpStatusCode.OK, (await fabrica.CreateClient().LoginPinAsync(lavador.Id, ApiFactory.Pin)).StatusCode);
    }

    [Fact]
    public async Task Dono_nao_desativa_o_proprio_acesso()
    {
        var dono = await fabrica.CriarDonoAsync();
        using var admin = await fabrica.AutenticadoAsync(dono.Email!);

        var resposta = await admin.PatchAsJsonAsync(Rota(dono.Id, "/ativo"), new { ativo = false });

        Assert.Equal(HttpStatusCode.UnprocessableEntity, resposta.StatusCode);
    }

    [Fact]
    public async Task Dono_edita_nome_e_apelido_do_lavador()
    {
        var dono = await fabrica.CriarDonoAsync();
        var lavador = await fabrica.CriarLavadorAsync("Fabio");
        using var admin = await fabrica.AutenticadoAsync(dono.Email!);

        var resposta = await admin.PutAsJsonAsync(Rota(lavador.Id), new { nome = "Fábio Lima", apelido = "Fabão" });

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        var json = await resposta.LerJsonAsync();
        Assert.Equal("Fabão", json.GetProperty("apelido").GetString());
        Assert.Equal("Fábio Lima", json.GetProperty("nome").GetString());
    }

    [Fact]
    public async Task Lavador_e_cliente_recebem_403_e_anonimo_401_na_gestao_de_usuarios()
    {
        var lavador = await fabrica.CriarLavadorAsync("Gil");
        var cliente = await fabrica.CriarClienteComLoginAsync();
        using var comoLavador = await fabrica.AutenticadoPorPinAsync(lavador.Id);
        using var comoCliente = await fabrica.AutenticadoAsync(cliente.Email!);
        using var anonimo = fabrica.CreateClient();

        foreach (var http in new[] { comoLavador, comoCliente })
        {
            Assert.Equal(HttpStatusCode.Forbidden, (await http.PutAsJsonAsync(Rota(lavador.Id, "/pin"), new { pin = "7391" })).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await http.PatchAsJsonAsync(Rota(lavador.Id, "/ativo"), new { ativo = false })).StatusCode);
        }

        Assert.Equal(HttpStatusCode.Unauthorized, (await anonimo.PutAsJsonAsync(Rota(lavador.Id, "/pin"), new { pin = "7391" })).StatusCode);
    }

    [Fact]
    public async Task Dono_cadastra_lavador_que_entra_com_o_PIN_e_aparece_na_lista_mesmo_desativado()
    {
        var dono = await fabrica.CriarDonoAsync();
        using var admin = await fabrica.AutenticadoAsync(dono.Email!);
        var apelido = $"Rafa{Guid.NewGuid():N}"[..12];

        var criado = await admin.PostAsJsonAsync($"{ApiClientExtensions.Base}/usuarios", new { nome = "Rafael Lima", apelido, pin = "7391" });
        var id = (await criado.LerJsonAsync()).GetProperty("id").GetInt32();
        var repetido = await admin.PostAsJsonAsync($"{ApiClientExtensions.Base}/usuarios", new { nome = "Outro", apelido = apelido.ToUpperInvariant(), pin = "7391" });
        await admin.PatchAsJsonAsync(Rota(id, "/ativo"), new { ativo = false });
        var lista = await (await admin.GetAsync($"{ApiClientExtensions.Base}/usuarios?perfil=Lavador")).LerJsonAsync();

        Assert.Equal(HttpStatusCode.Created, criado.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, repetido.StatusCode);
        var linha = lista.EnumerateArray().Single(u => u.GetProperty("id").GetInt32() == id);
        Assert.False(linha.GetProperty("ativo").GetBoolean());
        Assert.All(lista.EnumerateArray(), u => Assert.Equal("Lavador", u.GetProperty("perfil").GetString()));
        await admin.PatchAsJsonAsync(Rota(id, "/ativo"), new { ativo = true });
        Assert.Equal(HttpStatusCode.OK, (await fabrica.CreateClient().LoginPinAsync(id, "7391")).StatusCode);
    }

    [Theory]
    [InlineData("Rafael", "Rafa", "1234")]
    [InlineData("", "Rafa", "7391")]
    [InlineData("Rafael", "", "7391")]
    public async Task Cadastro_de_lavador_invalido_da_422(string nome, string apelido, string pin)
    {
        var dono = await fabrica.CriarDonoAsync();
        using var admin = await fabrica.AutenticadoAsync(dono.Email!);

        var resposta = await admin.PostAsJsonAsync($"{ApiClientExtensions.Base}/usuarios", new { nome, apelido, pin });

        Assert.Equal(HttpStatusCode.UnprocessableEntity, resposta.StatusCode);
    }

    [Fact]
    public async Task Lista_e_cadastro_de_usuarios_sao_so_do_dono()
    {
        var lavador = await fabrica.CriarLavadorAsync("Tuca");
        using var comoLavador = await fabrica.AutenticadoPorPinAsync(lavador.Id);

        Assert.Equal(HttpStatusCode.Forbidden, (await comoLavador.GetAsync($"{ApiClientExtensions.Base}/usuarios")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await comoLavador.PostAsJsonAsync($"{ApiClientExtensions.Base}/usuarios", new { nome = "X Y", apelido = "xy", pin = "7391" })).StatusCode);
    }
}
