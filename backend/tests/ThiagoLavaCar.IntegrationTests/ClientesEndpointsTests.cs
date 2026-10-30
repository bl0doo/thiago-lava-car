using System.Net;
using System.Net.Http.Json;

namespace ThiagoLavaCar.IntegrationTests;

[Collection(ApiCompartilhada.Nome)]
public sealed class ClientesEndpointsTests(ApiFactory fabrica)
{
    private static string Url(string caminho = "") => $"{ApiClientExtensions.Base}/clientes{caminho}";

    private static object Corpo(string? telefone = null, bool lgpd = true) => new
    {
        nome = "Renata Souza", telefone = telefone ?? ApiFactory.NovoTelefone(), email = (string?)null, canalPreferido = "WhatsApp", consentimentoLgpd = lgpd,
    };

    [Fact]
    public async Task Dono_cadastra_cliente_e_recebe_201_com_Location()
    {
        using var http = await fabrica.AutenticadoAsync((await fabrica.CriarDonoAsync()).Email!);

        var resposta = await http.PostAsJsonAsync(Url(), Corpo());

        Assert.Equal(HttpStatusCode.Created, resposta.StatusCode);
        Assert.NotNull(resposta.Headers.Location);
        Assert.True((await resposta.LerJsonAsync()).GetProperty("id").GetInt32() > 0);
    }

    [Fact]
    public async Task Telefone_repetido_devolve_409_com_o_id_existente()
    {
        using var http = await fabrica.AutenticadoAsync((await fabrica.CriarDonoAsync()).Email!);
        var telefone = ApiFactory.NovoTelefone();
        var primeiro = await (await http.PostAsJsonAsync(Url(), Corpo(telefone))).LerJsonAsync();

        var segundo = await http.PostAsJsonAsync(Url(), Corpo(telefone));

        Assert.Equal(HttpStatusCode.Conflict, segundo.StatusCode);
        Assert.Equal(primeiro.GetProperty("id").GetInt32(), (await segundo.LerJsonAsync()).GetProperty("clienteId").GetInt32());
    }

    [Fact]
    public async Task Sem_consentimento_e_com_telefone_curto_devolve_422_com_lista_de_campos()
    {
        using var http = await fabrica.AutenticadoAsync((await fabrica.CriarDonoAsync()).Email!);

        var resposta = await http.PostAsJsonAsync(Url(), new { nome = "R", telefone = "123", canalPreferido = "WhatsApp", consentimentoLgpd = false });

        Assert.Equal(HttpStatusCode.UnprocessableEntity, resposta.StatusCode);
        var erros = (await resposta.LerJsonAsync()).GetProperty("errors");
        Assert.True(erros.TryGetProperty("nome", out _));
        Assert.True(erros.TryGetProperty("telefone", out _));
        Assert.True(erros.TryGetProperty("consentimentoLgpd", out _));
    }

    [Fact]
    public async Task Busca_encontra_por_nome_e_pagina_o_resultado()
    {
        using var http = await fabrica.AutenticadoAsync((await fabrica.CriarDonoAsync()).Email!);
        var nome = $"Zeferino{Guid.NewGuid():N}"[..16];
        await http.PostAsJsonAsync(Url(), new { nome, telefone = ApiFactory.NovoTelefone(), canalPreferido = "Telefone", consentimentoLgpd = true });

        var json = await (await http.GetAsync(Url($"?busca={nome[..12]}&tamanho=5"))).LerJsonAsync();

        Assert.Equal(1, json.GetProperty("total").GetInt32());
        Assert.Equal(1, json.GetProperty("pagina").GetInt32());
        Assert.Equal(5, json.GetProperty("tamanho").GetInt32());
        Assert.Equal(nome, json.GetProperty("itens")[0].GetProperty("nome").GetString());
    }

    [Fact]
    public async Task Cliente_logado_ve_e_edita_so_os_proprios_dados_e_nao_acessa_a_lista()
    {
        var usuario = await fabrica.CriarClienteComLoginAsync();
        using var http = await fabrica.AutenticadoAsync(usuario.Email!);

        var meu = await http.GetAsync(Url("/me"));
        var editado = await http.PutAsJsonAsync(Url("/me"), new { nome = "Renata S.", telefone = ApiFactory.NovoTelefone(), canalPreferido = "Email" });
        var lista = await http.GetAsync(Url());

        Assert.Equal(HttpStatusCode.OK, meu.StatusCode);
        Assert.Equal("Renata S.", (await editado.LerJsonAsync()).GetProperty("nome").GetString());
        Assert.Equal(HttpStatusCode.Forbidden, lista.StatusCode);
    }

    [Fact]
    public async Task Lavador_nao_acessa_clientes()
    {
        using var http = await fabrica.AutenticadoPorPinAsync((await fabrica.CriarLavadorAsync()).Id);

        Assert.Equal(HttpStatusCode.Forbidden, (await http.GetAsync(Url())).StatusCode);
    }

    [Fact]
    public async Task Cliente_inexistente_devolve_404()
    {
        using var http = await fabrica.AutenticadoAsync((await fabrica.CriarDonoAsync()).Email!);

        Assert.Equal(HttpStatusCode.NotFound, (await http.GetAsync(Url("/999999"))).StatusCode);
    }
}
