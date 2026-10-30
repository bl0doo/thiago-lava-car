using System.Net;
using System.Net.Http.Json;

namespace ThiagoLavaCar.IntegrationTests;

[Collection(ApiCompartilhada.Nome)]
public sealed class OrdemDetalheEndpointsTests(ApiFactory fabrica)
{
    private static string Url(string caminho) => $"{ApiClientExtensions.Base}/{caminho}";

    [Fact]
    public async Task Detalhe_traz_itens_pagamentos_valor_pago_e_saldo()
    {
        await fabrica.GarantirTaxasAsync();
        var dono = await fabrica.CriarDonoAsync();
        var osId = await fabrica.CriarOrdemAguardandoAsync(dono.Id); // R$ 100,00
        using var http = await fabrica.AutenticadoAsync(dono.Email!);
        await http.PostAsJsonAsync(Url($"ordens-servico/{osId}/pagamentos"), new { formaPagamento = "Pix", valorBruto = 40m });

        var resposta = await http.GetAsync(Url($"ordens-servico/{osId}/detalhe"));
        var detalhe = await resposta.LerJsonAsync();

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        Assert.Equal(40m, detalhe.GetProperty("valorPago").GetDecimal());
        Assert.Equal(60m, detalhe.GetProperty("saldoPendente").GetDecimal());
        Assert.Equal(100m, detalhe.GetProperty("cartao").GetProperty("valorTotal").GetDecimal());
        Assert.Equal(1, detalhe.GetProperty("pagamentos").GetArrayLength());
        Assert.Equal(100m, detalhe.GetProperty("itens").EnumerateArray().Sum(i => i.GetProperty("precoFinal").GetDecimal()));
    }

    [Fact]
    public async Task Detalhe_e_so_do_dono_e_da_404_para_ordem_inexistente()
    {
        var dono = await fabrica.CriarDonoAsync();
        var lavador = await fabrica.CriarLavadorAsync("Edu");
        var osId = await fabrica.CriarOrdemAguardandoAsync(dono.Id);
        using var comoLavador = await fabrica.AutenticadoPorPinAsync(lavador.Id);
        using var comoDono = await fabrica.AutenticadoAsync(dono.Email!);

        Assert.Equal(HttpStatusCode.Forbidden, (await comoLavador.GetAsync(Url($"ordens-servico/{osId}/detalhe"))).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await comoDono.GetAsync(Url("ordens-servico/999999/detalhe"))).StatusCode);
    }
}
