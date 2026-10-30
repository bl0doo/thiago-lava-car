using System.Net;
using System.Net.Http.Json;

namespace ThiagoLavaCar.IntegrationTests;

[Collection(ApiCompartilhada.Nome)]
public sealed class RelatoriosEndpointsTests(ApiFactory fabrica)
{
    private static string Url(string caminho) => $"{ApiClientExtensions.Base}/{caminho}";

    [Fact] // T41
    public async Task Lucro_por_servico_lista_o_servico_da_OS_paga()
    {
        await fabrica.GarantirTaxasAsync();
        var dono = await fabrica.CriarDonoAsync();
        var osId = await fabrica.CriarOrdemAguardandoAsync(dono.Id); // R$ 100,00
        using var http = await fabrica.AutenticadoAsync(dono.Email!);
        await http.PostAsJsonAsync(Url($"ordens-servico/{osId}/pagamentos"), new { formaPagamento = "Pix", valorBruto = 100m });

        var resposta = await http.GetAsync(Url("relatorios/lucro-por-servico"));
        var linhas = await resposta.LerJsonAsync();

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        Assert.NotEmpty(linhas.EnumerateArray());
        var primeira = linhas.EnumerateArray().First();
        foreach (var campo in new[] { "servico", "quantidade", "receitaLiquida", "insumos", "maoDeObra", "lucro", "lucroMedio", "tempoMedioMin" })
        {
            Assert.True(primeira.TryGetProperty(campo, out _), campo);
        }
    }

    [Fact]
    public async Task Faturamento_mostra_o_dia_do_pagamento_e_produtividade_responde_com_a_estrutura()
    {
        await fabrica.GarantirTaxasAsync();
        var dono = await fabrica.CriarDonoAsync();
        var osId = await fabrica.CriarOrdemAguardandoAsync(dono.Id);
        using var http = await fabrica.AutenticadoAsync(dono.Email!);
        await http.PostAsJsonAsync(Url($"ordens-servico/{osId}/pagamentos"), new { formaPagamento = "Pix", valorBruto = 100m });

        var faturamento = await (await http.GetAsync(Url("relatorios/faturamento"))).LerJsonAsync();
        var produtividade = await http.GetAsync(Url("relatorios/produtividade"));

        Assert.True(faturamento.GetProperty("totalBruto").GetDecimal() >= 100m);
        Assert.NotEmpty(faturamento.GetProperty("dias").EnumerateArray());
        Assert.True(faturamento.GetProperty("ticketMedio").GetDecimal() > 0m);
        Assert.Equal(HttpStatusCode.OK, produtividade.StatusCode);
        var corpo = await produtividade.LerJsonAsync();
        Assert.True(corpo.TryGetProperty("porServico", out _) && corpo.TryGetProperty("porLavador", out _));
    }

    [Theory]
    [InlineData("inicio=2026-10-20&fim=2026-10-01")]
    [InlineData("inicio=2024-01-01&fim=2026-01-01")]
    public async Task Periodo_invalido_da_422(string consulta)
    {
        var dono = await fabrica.CriarDonoAsync();
        using var http = await fabrica.AutenticadoAsync(dono.Email!);

        foreach (var relatorio in new[] { "lucro-por-servico", "faturamento", "produtividade" })
        {
            Assert.Equal(HttpStatusCode.UnprocessableEntity, (await http.GetAsync(Url($"relatorios/{relatorio}?{consulta}"))).StatusCode);
        }
    }

    [Fact]
    public async Task Ponto_de_equilibrio_do_mes_atual_e_mes_invalido()
    {
        var dono = await fabrica.CriarDonoAsync();
        using var http = await fabrica.AutenticadoAsync(dono.Email!);

        var atual = await http.GetAsync(Url("relatorios/ponto-equilibrio"));
        var invalido = await http.GetAsync(Url("relatorios/ponto-equilibrio?mes=outubro"));

        Assert.Equal(HttpStatusCode.OK, atual.StatusCode);
        var corpo = await atual.LerJsonAsync();
        Assert.True(corpo.TryGetProperty("carrosFeitos", out _));
        Assert.True(corpo.TryGetProperty("contas", out _));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, invalido.StatusCode);
    }

    [Theory]
    [InlineData("relatorios/lucro-por-servico")]
    [InlineData("relatorios/ponto-equilibrio")]
    [InlineData("relatorios/faturamento")]
    [InlineData("relatorios/produtividade")]
    public async Task Relatorios_sao_so_do_dono(string caminho)
    {
        var lavador = await fabrica.CriarLavadorAsync("Dudu");
        using var comoLavador = await fabrica.AutenticadoPorPinAsync(lavador.Id);
        using var anonimo = fabrica.CreateClient();

        Assert.Equal(HttpStatusCode.Forbidden, (await comoLavador.GetAsync(Url(caminho))).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonimo.GetAsync(Url(caminho))).StatusCode);
    }
}
