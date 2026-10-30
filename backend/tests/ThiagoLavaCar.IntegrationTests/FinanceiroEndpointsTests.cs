using System.Globalization;
using System.Net;
using System.Net.Http.Json;

namespace ThiagoLavaCar.IntegrationTests;

[Collection(ApiCompartilhada.Nome)]
public sealed class FinanceiroEndpointsTests(ApiFactory fabrica)
{
    private static string Url(string caminho) => $"{ApiClientExtensions.Base}/{caminho}";

    /// <summary>Dia em Brasília, deslocado por <paramref name="dias"/>, no formato AAAA-MM-DD.</summary>
    private static string Hoje(ApiFactory f, int dias = 0) =>
        DateOnly.FromDateTime(f.Relogio.GetUtcNow().AddHours(-3).DateTime).AddDays(dias).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    private async Task<HttpClient> DonoAsync() => await fabrica.AutenticadoAsync((await fabrica.CriarDonoAsync()).Email!);

    [Fact]
    public async Task Despesa_ciclo_completo_com_total_edicao_e_exclusao()
    {
        using var http = await DonoAsync();
        var dia = Hoje(fabrica);

        var criada = await http.PostAsJsonAsync(Url("despesas"), new { descricao = "Shampoo", categoria = "Insumos", valor = 45.5m, data = dia });
        var id = (await criada.LerJsonAsync()).GetProperty("id").GetInt32();
        var editada = await http.PutAsJsonAsync(Url($"despesas/{id}"), new { descricao = "Shampoo 5L", categoria = "Insumos", valor = 50m, data = dia });
        var lista = await (await http.GetAsync(Url($"despesas?de={dia}&ate={dia}"))).LerJsonAsync();
        var excluida = await http.DeleteAsync(Url($"despesas/{id}"));
        var outraExclusao = await http.DeleteAsync(Url($"despesas/{id}"));

        Assert.Equal(HttpStatusCode.Created, criada.StatusCode);
        Assert.Equal(HttpStatusCode.OK, editada.StatusCode);
        Assert.Contains(lista.GetProperty("itens").EnumerateArray(), i => i.GetProperty("id").GetInt32() == id && i.GetProperty("descricao").GetString() == "Shampoo 5L");
        Assert.True(lista.GetProperty("total").GetDecimal() >= 50m);
        Assert.Equal(HttpStatusCode.NoContent, excluida.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, outraExclusao.StatusCode);
        var acoes = await fabrica.NoBancoAsync((db, _) => Task.FromResult(db.LogsAuditoria.Where(l => l.Acao == "ExclusaoDespesa" && l.EntidadeId == id).Count()));
        Assert.Equal(1, acoes);
    }

    [Theory]
    [InlineData("", "Insumos", 10)]
    [InlineData("Shampoo", "Combustivel", 10)]
    [InlineData("Shampoo", "Insumos", 0)]
    public async Task Despesa_invalida_da_422(string descricao, string categoria, int valor)
    {
        using var http = await DonoAsync();

        var resposta = await http.PostAsJsonAsync(Url("despesas"), new { descricao, categoria, valor, data = Hoje(fabrica) });

        Assert.Equal(HttpStatusCode.UnprocessableEntity, resposta.StatusCode);
    }

    [Fact]
    public async Task Despesa_com_data_futura_da_422()
    {
        using var http = await DonoAsync();

        var resposta = await http.PostAsJsonAsync(Url("despesas"), new { descricao = "Aluguel", categoria = "Aluguel", valor = 900, data = "2999-01-01" });

        Assert.Equal(HttpStatusCode.UnprocessableEntity, resposta.StatusCode);
    }

    [Fact]
    public async Task Retirada_registra_lista_e_exclui()
    {
        using var http = await DonoAsync();
        var dia = Hoje(fabrica);

        var criada = await http.PostAsJsonAsync(Url("retiradas"), new { valor = 500m, data = dia, observacao = "Pró-labore" });
        var id = (await criada.LerJsonAsync()).GetProperty("id").GetInt32();
        var lista = await (await http.GetAsync(Url($"retiradas?de={dia}&ate={dia}"))).LerJsonAsync();
        var excluida = await http.DeleteAsync(Url($"retiradas/{id}"));
        var invalida = await http.PostAsJsonAsync(Url("retiradas"), new { valor = -1m, data = dia });

        Assert.Equal(HttpStatusCode.Created, criada.StatusCode);
        Assert.Contains(lista.GetProperty("itens").EnumerateArray(), i => i.GetProperty("id").GetInt32() == id);
        Assert.Equal(HttpStatusCode.NoContent, excluida.StatusCode);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, invalida.StatusCode);
    }

    [Fact]
    public async Task Diaria_lanca_nao_duplica_edita_marca_paga_e_exclui()
    {
        using var http = await DonoAsync();
        var lavador = await fabrica.CriarLavadorAsync("Rafa");
        var dia = Hoje(fabrica);

        var criada = await http.PostAsJsonAsync(Url("diarias"), new { lavadorId = lavador.Id, data = dia, valor = 100m });
        var duplicada = await http.PostAsJsonAsync(Url("diarias"), new { lavadorId = lavador.Id, data = dia, valor = 100m });
        var id = (await criada.LerJsonAsync()).GetProperty("id").GetInt32();
        var paga = await http.PutAsJsonAsync(Url($"diarias/{id}"), new { valor = 110m, paga = true });
        var reajustePosPago = await http.PutAsJsonAsync(Url($"diarias/{id}"), new { valor = 90m });
        var exclusaoPosPago = await http.DeleteAsync(Url($"diarias/{id}"));
        var lista = await (await http.GetAsync(Url($"diarias?mes={dia[..7]}"))).LerJsonAsync();

        Assert.Equal(HttpStatusCode.Created, criada.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, duplicada.StatusCode);
        Assert.True((await paga.LerJsonAsync()).GetProperty("paga").GetBoolean());
        Assert.Equal(HttpStatusCode.Conflict, reajustePosPago.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, exclusaoPosPago.StatusCode);
        Assert.Contains(lista.GetProperty("itens").EnumerateArray(), i => i.GetProperty("id").GetInt32() == id);
    }

    [Fact]
    public async Task Diaria_nao_paga_pode_ser_excluida_e_o_lavador_precisa_existir()
    {
        using var http = await DonoAsync();
        var lavador = await fabrica.CriarLavadorAsync("Beto");
        var ontem = Hoje(fabrica, -1);

        var id = (await (await http.PostAsJsonAsync(Url("diarias"), new { lavadorId = lavador.Id, data = ontem, valor = 100m })).LerJsonAsync()).GetProperty("id").GetInt32();
        var excluida = await http.DeleteAsync(Url($"diarias/{id}"));
        var semLavador = await http.PostAsJsonAsync(Url("diarias"), new { lavadorId = 999999, data = ontem, valor = 100m });

        Assert.Equal(HttpStatusCode.NoContent, excluida.StatusCode);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, semLavador.StatusCode);
    }

    [Fact]
    public async Task Lancar_dia_cria_para_todos_e_nao_duplica_quem_ja_tem()
    {
        using var http = await DonoAsync();
        var a = await fabrica.CriarLavadorAsync("Ana");
        var b = await fabrica.CriarLavadorAsync("Bia");
        var dia = Hoje(fabrica, -2);
        await http.PostAsJsonAsync(Url("diarias"), new { lavadorId = a.Id, data = dia, valor = 100m });

        var resposta = await (await http.PostAsJsonAsync(Url($"diarias/lancar-dia?data={dia}"), new { lavadorIds = new[] { a.Id, b.Id }, valor = 100m })).LerJsonAsync();

        Assert.Equal([b.Id], resposta.GetProperty("criadas").EnumerateArray().Select(d => d.GetProperty("lavadorId").GetInt32()));
        Assert.Equal([a.Id], resposta.GetProperty("jaLancadas").EnumerateArray().Select(d => d.GetInt32()));
    }

    [Theory]
    [InlineData("despesas")]
    [InlineData("retiradas")]
    [InlineData("diarias")]
    public async Task So_o_dono_acessa_o_financeiro(string recurso)
    {
        var lavador = await fabrica.CriarLavadorAsync("Zeca");
        using var comoLavador = await fabrica.AutenticadoPorPinAsync(lavador.Id);
        using var anonimo = fabrica.CreateClient();

        Assert.Equal(HttpStatusCode.Forbidden, (await comoLavador.GetAsync(Url(recurso))).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonimo.GetAsync(Url(recurso))).StatusCode);
    }
}
