using System.Net;
using System.Net.Http.Json;

namespace ThiagoLavaCar.IntegrationTests;

// O fechamento é por data e único; os testes de caixa não rodam em paralelo com os de pagamento, que usam o dia de hoje.
[Collection(ApiCompartilhada.Nome)]
public sealed class CaixaEndpointsTests(ApiFactory fabrica)
{
    private static string Url(string caminho) => $"{ApiClientExtensions.Base}/{caminho}";

    /// <summary>Um dia isolado no passado distante não é possível (pagamento usa a data atual), então cada teste avança o relógio um dia.</summary>
    private async Task<(HttpClient Http, string Dia, int OsId)> CenarioAsync()
    {
        fabrica.Relogio.Advance(TimeSpan.FromDays(1));
        await fabrica.GarantirTaxasAsync();
        var dono = await fabrica.CriarDonoAsync();
        var osId = await fabrica.CriarOrdemAguardandoAsync(dono.Id); // R$ 100,00
        var http = await fabrica.AutenticadoAsync(dono.Email!);
        var dia = (await (await http.PostAsJsonAsync(Url($"ordens-servico/{osId}/pagamentos"), new { formaPagamento = "Credito", valorBruto = 100m })).LerJsonAsync())
            .GetProperty("dataCaixa").GetString()!;
        return (http, dia, osId);
    }

    [Fact] // T36
    public async Task Resumo_mostra_o_pagamento_por_forma_com_taxa_e_liquido()
    {
        var (http, dia, _) = await CenarioAsync();
        using var _ = http;

        var resumo = await (await http.GetAsync(Url($"caixa/{dia}/resumo"))).LerJsonAsync();

        Assert.False(resumo.GetProperty("fechado").GetBoolean());
        var credito = resumo.GetProperty("porForma").EnumerateArray().Single(f => f.GetProperty("forma").GetString() == "Credito");
        Assert.Equal(100m, credito.GetProperty("bruto").GetDecimal());
        Assert.Equal(3.49m, credito.GetProperty("taxas").GetDecimal());
        Assert.Equal(96.51m, resumo.GetProperty("totalLiquido").GetDecimal());
    }

    [Fact] // T37 + T38
    public async Task Fecha_com_diferenca_bloqueia_pagamentos_do_dia_e_reabre_com_justificativa()
    {
        var (http, dia, osId) = await CenarioAsync();
        using var _ = http;
        var corpoComDiferenca = new { conferido = new[] { new { forma = "Credito", valor = 90m } }, justificativa = (string?)null };

        var semJustificativa = await http.PostAsJsonAsync(Url($"caixa/{dia}/fechamento"), corpoComDiferenca);
        var fechado = await http.PostAsJsonAsync(Url($"caixa/{dia}/fechamento"),
            new { conferido = new[] { new { forma = "Credito", valor = 90m } }, justificativa = "Cliente pagou R$ 10 a menos" });
        var repetido = await http.PostAsJsonAsync(Url($"caixa/{dia}/fechamento"), corpoComDiferenca);
        var novoPagamento = await http.PostAsJsonAsync(Url($"ordens-servico/{osId}/pagamentos"), new { formaPagamento = "Pix", valorBruto = 1m });
        var resumoFechado = await (await http.GetAsync(Url($"caixa/{dia}/resumo"))).LerJsonAsync();
        var reaberto = await http.PostAsJsonAsync(Url($"caixa/{dia}/reabrir"), new { justificativa = "Esqueci de lançar um pagamento" });
        var reabertoDeNovo = await http.PostAsJsonAsync(Url($"caixa/{dia}/reabrir"), new { justificativa = "Esqueci de lançar um pagamento" });

        Assert.Equal(HttpStatusCode.UnprocessableEntity, semJustificativa.StatusCode);
        Assert.Equal(HttpStatusCode.Created, fechado.StatusCode);
        Assert.Equal(-10m, (await fechado.LerJsonAsync()).GetProperty("diferenca").GetDecimal());
        Assert.Equal(HttpStatusCode.Conflict, repetido.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, novoPagamento.StatusCode);
        Assert.True(resumoFechado.GetProperty("fechado").GetBoolean());
        Assert.Equal(HttpStatusCode.NoContent, reaberto.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, reabertoDeNovo.StatusCode);
        var acoes = await fabrica.NoBancoAsync((db, _) => Task.FromResult(db.LogsAuditoria.Where(l => l.Acao == "AjusteCaixa" || l.Acao == "ReaberturaCaixa").Select(l => l.Acao).ToList()));
        Assert.Contains("AjusteCaixa", acoes);
        Assert.Contains("ReaberturaCaixa", acoes);
    }

    [Fact] // T38
    public async Task Fecha_sem_diferenca_devolve_201_com_diferenca_zero_e_sem_justificativa()
    {
        var (http, dia, _) = await CenarioAsync();
        using var _ = http;

        var fechado = await http.PostAsJsonAsync(Url($"caixa/{dia}/fechamento"), new { conferido = new[] { new { forma = "Credito", valor = 100m } } });

        Assert.Equal(HttpStatusCode.Created, fechado.StatusCode);
        Assert.Equal(0m, (await fechado.LerJsonAsync()).GetProperty("diferenca").GetDecimal());
        await http.PostAsJsonAsync(Url($"caixa/{dia}/reabrir"), new { justificativa = "Limpeza do teste de integração" });
    }

    [Fact]
    public async Task Historico_do_mes_lista_o_fechamento()
    {
        var (http, dia, _) = await CenarioAsync();
        using var _ = http;
        await http.PostAsJsonAsync(Url($"caixa/{dia}/fechamento"), new { conferido = new[] { new { forma = "Credito", valor = 100m } } });

        var lista = await (await http.GetAsync(Url($"caixa/fechamentos?mes={dia[..7]}"))).LerJsonAsync();
        var invalido = await http.GetAsync(Url("caixa/fechamentos?mes=2026-13"));

        Assert.Contains(lista.EnumerateArray(), f => f.GetProperty("data").GetString() == dia);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, invalido.StatusCode);
        await http.PostAsJsonAsync(Url($"caixa/{dia}/reabrir"), new { justificativa = "Limpeza do teste de integração" });
    }

    [Fact]
    public async Task Data_futura_422_data_malformada_400_e_lavador_403()
    {
        var (http, _, _) = await CenarioAsync();
        using var _ = http;
        var lavador = await fabrica.CriarLavadorAsync();
        using var comoLavador = await fabrica.AutenticadoPorPinAsync(lavador.Id);

        var futura = await http.GetAsync(Url("caixa/2999-01-01/resumo"));
        var malformada = await http.GetAsync(Url("caixa/ontem/resumo"));
        var proibido = await comoLavador.GetAsync(Url("caixa/2026-10-20/resumo"));

        Assert.Equal(HttpStatusCode.UnprocessableEntity, futura.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, malformada.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, proibido.StatusCode);
    }
}
