using System.Net;
using System.Net.Http.Json;

namespace ThiagoLavaCar.IntegrationTests;

[Collection(ApiCompartilhada.Nome)]
public sealed class PainelEndpointsTests(ApiFactory fabrica)
{
    private static string Url(string caminho) => $"{ApiClientExtensions.Base}/{caminho}";

    [Fact] // T40
    public async Task Painel_mostra_o_que_entrou_hoje_e_o_que_sobrou()
    {
        await fabrica.GarantirTaxasAsync();
        var dono = await fabrica.CriarDonoAsync();
        var osId = await fabrica.CriarOrdemAguardandoAsync(dono.Id); // R$ 100,00, insumos R$ 8,00
        using var http = await fabrica.AutenticadoAsync(dono.Email!);
        await http.PostAsJsonAsync(Url($"ordens-servico/{osId}/pagamentos"), new { formaPagamento = "Pix", valorBruto = 100m });

        var resposta = await http.GetAsync(Url("painel/hoje"));
        var painel = await resposta.LerJsonAsync();

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        Assert.True(painel.GetProperty("entrouHoje").GetDecimal() >= 100m);
        Assert.True(painel.GetProperty("liquidoHoje").GetDecimal() >= 100m);
        Assert.True(painel.GetProperty("carrosNoMes").GetInt32() >= 1);
        Assert.Equal(System.Text.Json.JsonValueKind.Array, painel.GetProperty("pendencias").ValueKind);
        Assert.True(painel.TryGetProperty("sobrouDeVerdade", out _));
    }

    [Fact]
    public async Task Painel_e_so_do_dono()
    {
        var lavador = await fabrica.CriarLavadorAsync("Nando");
        using var comoLavador = await fabrica.AutenticadoPorPinAsync(lavador.Id);
        using var anonimo = fabrica.CreateClient();

        Assert.Equal(HttpStatusCode.Forbidden, (await comoLavador.GetAsync(Url("painel/hoje"))).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonimo.GetAsync(Url("painel/hoje"))).StatusCode);
    }
}
