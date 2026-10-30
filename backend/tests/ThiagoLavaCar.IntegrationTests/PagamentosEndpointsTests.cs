using System.Net;
using System.Net.Http.Json;

namespace ThiagoLavaCar.IntegrationTests;

[Collection(ApiCompartilhada.Nome)]
public sealed class PagamentosEndpointsTests(ApiFactory fabrica)
{
    private static string Url(string caminho) => $"{ApiClientExtensions.Base}/{caminho}";

    private async Task<(HttpClient Http, int OsId)> CenarioAsync()
    {
        await fabrica.GarantirTaxasAsync();
        var dono = await fabrica.CriarDonoAsync();
        var osId = await fabrica.CriarOrdemAguardandoAsync(dono.Id); // valor total R$ 100,00
        return (await fabrica.AutenticadoAsync(dono.Email!), osId);
    }

    [Fact] // T33 + T34
    public async Task Pagamento_dividido_copia_a_taxa_e_zera_o_saldo()
    {
        var (http, osId) = await CenarioAsync();
        using var _ = http;

        var credito = await http.PostAsJsonAsync(Url($"ordens-servico/{osId}/pagamentos"), new { formaPagamento = "Credito", valorBruto = 60m });
        var pix = await http.PostAsJsonAsync(Url($"ordens-servico/{osId}/pagamentos"), new { formaPagamento = "Pix", valorBruto = 40m });

        Assert.Equal(HttpStatusCode.Created, credito.StatusCode);
        var primeiro = await credito.LerJsonAsync();
        Assert.Equal(3.49m, primeiro.GetProperty("percentualTaxa").GetDecimal());
        Assert.Equal(2.09m, primeiro.GetProperty("valorTaxa").GetDecimal());
        Assert.Equal(57.91m, primeiro.GetProperty("valorLiquido").GetDecimal());
        Assert.Equal(40m, primeiro.GetProperty("saldoPendente").GetDecimal());
        Assert.Equal(0m, (await pix.LerJsonAsync()).GetProperty("saldoPendente").GetDecimal());
    }

    [Fact] // T35
    public async Task Valor_acima_do_saldo_devolve_422_com_o_saldo()
    {
        var (http, osId) = await CenarioAsync();
        using var _ = http;

        var resposta = await http.PostAsJsonAsync(Url($"ordens-servico/{osId}/pagamentos"), new { formaPagamento = "Pix", valorBruto = 100.01m });

        Assert.Equal(HttpStatusCode.UnprocessableEntity, resposta.StatusCode);
        Assert.Contains("100,00", (await resposta.LerJsonAsync()).GetProperty("detail").GetString());
    }

    [Fact]
    public async Task Estorno_devolve_o_saldo_e_aparece_na_lista_do_dia()
    {
        var (http, osId) = await CenarioAsync();
        using var _ = http;
        var pagamento = await (await http.PostAsJsonAsync(Url($"ordens-servico/{osId}/pagamentos"), new { formaPagamento = "Pix", valorBruto = 100m })).LerJsonAsync();
        var id = pagamento.GetProperty("id").GetInt32();
        var data = pagamento.GetProperty("dataCaixa").GetString();

        var estorno = await http.PostAsJsonAsync(Url($"pagamentos/{id}/estorno"), new { motivo = "Cliente desistiu" });
        var repetido = await http.PostAsync(Url($"pagamentos/{id}/estorno"), null);
        var novo = await http.PostAsJsonAsync(Url($"ordens-servico/{osId}/pagamentos"), new { formaPagamento = "Dinheiro", valorBruto = 100m });
        var lista = await (await http.GetAsync(Url($"pagamentos?data={data}"))).LerJsonAsync();

        Assert.Equal(HttpStatusCode.OK, estorno.StatusCode);
        Assert.True((await estorno.LerJsonAsync()).GetProperty("estornado").GetBoolean());
        Assert.Equal(HttpStatusCode.Conflict, repetido.StatusCode);
        Assert.Equal(HttpStatusCode.Created, novo.StatusCode);
        Assert.Contains(lista.EnumerateArray(), p => p.GetProperty("id").GetInt32() == id && p.GetProperty("estornado").GetBoolean());
    }

    [Fact]
    public async Task Estorno_grava_auditoria_no_banco()
    {
        var (http, osId) = await CenarioAsync();
        using var _ = http;
        var id = (await (await http.PostAsJsonAsync(Url($"ordens-servico/{osId}/pagamentos"), new { formaPagamento = "Pix", valorBruto = 10m })).LerJsonAsync()).GetProperty("id").GetInt32();

        await http.PostAsync(Url($"pagamentos/{id}/estorno"), null);

        var registros = await fabrica.NoBancoAsync((db, _) => Task.FromResult(db.LogsAuditoria.Count(l => l.Acao == "EstornoPagamento" && l.EntidadeId == id)));
        Assert.Equal(1, registros);
    }

    [Fact]
    public async Task Os_cancelada_nao_recebe_pagamento_e_os_inexistente_devolve_404()
    {
        var (http, osId) = await CenarioAsync();
        using var _ = http;
        await http.PostAsync(Url($"ordens-servico/{osId}/cancelar"), null);

        var cancelada = await http.PostAsJsonAsync(Url($"ordens-servico/{osId}/pagamentos"), new { formaPagamento = "Pix", valorBruto = 10m });
        var inexistente = await http.PostAsJsonAsync(Url("ordens-servico/999999/pagamentos"), new { formaPagamento = "Pix", valorBruto = 10m });

        Assert.Equal(HttpStatusCode.Conflict, cancelada.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, inexistente.StatusCode);
    }

    [Fact]
    public async Task Taxas_listam_e_alteram_so_para_o_dono_e_so_valem_para_pagamentos_futuros()
    {
        var (http, osId) = await CenarioAsync();
        using var _ = http;
        var antes = await (await http.PostAsJsonAsync(Url($"ordens-servico/{osId}/pagamentos"), new { formaPagamento = "Debito", valorBruto = 50m })).LerJsonAsync();
        var lavador = await fabrica.CriarLavadorAsync();
        using var comoLavador = await fabrica.AutenticadoPorPinAsync(lavador.Id);

        var alterada = await http.PutAsJsonAsync(Url("taxas/Debito"), new { percentual = 2.5m, valorFixo = 0m });
        var depois = await (await http.PostAsJsonAsync(Url($"ordens-servico/{osId}/pagamentos"), new { formaPagamento = "Debito", valorBruto = 50m })).LerJsonAsync();
        var proibido = await comoLavador.GetAsync(Url("taxas"));
        var invalida = await http.PutAsJsonAsync(Url("taxas/Debito"), new { percentual = 150m, valorFixo = 0m });

        Assert.Equal(HttpStatusCode.OK, alterada.StatusCode);
        Assert.Equal(1.5m, antes.GetProperty("percentualTaxa").GetDecimal());
        Assert.Equal(2.5m, depois.GetProperty("percentualTaxa").GetDecimal());
        Assert.Equal(HttpStatusCode.Forbidden, proibido.StatusCode);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, invalida.StatusCode);
        await http.PutAsJsonAsync(Url("taxas/Debito"), new { percentual = 1.5m, valorFixo = 0m });
    }
}
