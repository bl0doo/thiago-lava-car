using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;

namespace ThiagoLavaCar.IntegrationTests;

[Collection(ApiCompartilhada.Nome)]
public sealed class MensagensEndpointsTests(ApiFactory fabrica)
{
    private static string Url(string caminho) => $"{ApiClientExtensions.Base}/ordens-servico/{caminho}";

    private async Task<(HttpClient Http, int OsId)> CenarioAsync()
    {
        await fabrica.GarantirTaxasAsync();
        var dono = await fabrica.CriarDonoAsync();
        var osId = await fabrica.CriarOrdemAguardandoAsync(dono.Id); // R$ 100,00, cliente Renata Souza, Onix
        return (await fabrica.AutenticadoAsync(dono.Email!), osId);
    }

    [Fact]
    public async Task Mensagem_iniciada_so_com_OS_em_andamento_e_traz_texto_canal_e_link()
    {
        var (http, osId) = await CenarioAsync();
        using var _ = http;

        var antes = await http.GetAsync(Url($"{osId}/mensagens/lavagem-iniciada"));
        await http.PostAsync(Url($"{osId}/iniciar"), null);
        var depois = await http.GetAsync(Url($"{osId}/mensagens/lavagem-iniciada"));
        var corpo = await depois.LerJsonAsync();

        Assert.Equal(HttpStatusCode.Conflict, antes.StatusCode);
        Assert.Equal(HttpStatusCode.OK, depois.StatusCode);
        Assert.StartsWith("Olá, Renata!", corpo.GetProperty("texto").GetString());
        Assert.Equal("WhatsApp", corpo.GetProperty("canal").GetString());
        Assert.StartsWith("https://wa.me/55449", corpo.GetProperty("linkWhatsApp").GetString());
        Assert.Contains("text=Ol%C3%A1", corpo.GetProperty("linkWhatsApp").GetString());
    }

    [Fact] // T31 + T32
    public async Task Mensagem_concluida_mostra_o_valor_a_pagar_so_enquanto_houver_saldo()
    {
        var (http, osId) = await CenarioAsync();
        using var _ = http;
        using var lavador = await fabrica.AutenticadoPorPinAsync((await fabrica.CriarLavadorAsync()).Id);
        await lavador.PostAsync(Url($"{osId}/iniciar"), null);
        fabrica.Relogio.Advance(TimeSpan.FromMinutes(10));
        var encerrada = await lavador.PostAsync(Url($"{osId}/encerrar"), null);
        Assert.Equal(HttpStatusCode.OK, encerrada.StatusCode);

        var pendente = (await (await http.GetAsync(Url($"{osId}/mensagens/lavagem-concluida"))).LerJsonAsync()).GetProperty("texto").GetString();
        await http.PostAsJsonAsync(Url($"{osId}/pagamentos"), new { formaPagamento = "Pix", valorBruto = 100m });
        var paga = (await (await http.GetAsync(Url($"{osId}/mensagens/lavagem-concluida"))).LerJsonAsync()).GetProperty("texto").GetString();

        Assert.Contains("Valor a pagar: R$ 100,00", pendente);
        Assert.DoesNotContain("Valor a pagar", paga);
    }

    [Fact]
    public async Task Registrar_envio_guarda_o_texto_final_e_tipo_invalido_ou_OS_inexistente_falham()
    {
        var (http, osId) = await CenarioAsync();
        using var _ = http;
        await http.PostAsync(Url($"{osId}/iniciar"), null);

        var enviada = await http.PostAsync(Url($"{osId}/mensagens/lavagem-iniciada/enviada"), null);
        var tipoRuim = await http.GetAsync(Url($"{osId}/mensagens/outra"));
        var inexistente = await http.GetAsync(Url("999999/mensagens/lavagem-iniciada"));

        Assert.Equal(HttpStatusCode.Created, enviada.StatusCode);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, tipoRuim.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, inexistente.StatusCode);
        var guardada = await fabrica.NoBancoAsync(async (db, _) => await db.MensagensEnviadas.SingleAsync(m => m.OrdemServicoId == osId));
        Assert.StartsWith("Olá, Renata!", guardada.Texto);
    }
}
