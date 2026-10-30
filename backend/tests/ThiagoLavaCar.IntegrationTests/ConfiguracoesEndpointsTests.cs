using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;

namespace ThiagoLavaCar.IntegrationTests;

[Collection(ApiCompartilhada.Nome)]
public sealed class ConfiguracoesEndpointsTests(ApiFactory fabrica)
{
    private static string Url(string caminho) => $"{ApiClientExtensions.Base}{caminho}";

    private static object Config(string abertura = "08:00", string fechamento = "18:00", int[]? dias = null, int intervalo = 30) => new
    {
        nomeLavaJato = "Thiago Lava Car",
        telefone = "44999990000",
        abertura,
        fechamento,
        diasFuncionamento = dias ?? [1, 2, 3, 4, 5, 6],
        diariaSemana = 100m,
        diariaSabado = 120m,
        intervaloAgendaMin = intervalo,
    };

    [Fact]
    public async Task Dono_le_atualiza_e_a_mudanca_vai_para_a_auditoria_e_volta_ao_padrao()
    {
        using var dono = await fabrica.AutenticadoAsync((await fabrica.CriarDonoAsync()).Email!);

        var padrao = await (await dono.GetAsync(Url("/configuracoes"))).LerJsonAsync();
        var resposta = await dono.PutAsJsonAsync(Url("/configuracoes"), Config(abertura: "07:30", fechamento: "17:30", dias: [6, 1, 2, 3, 4, 5], intervalo: 15));
        var atualizada = await resposta.LerJsonAsync();
        await dono.PutAsJsonAsync(Url("/configuracoes"), Config());

        Assert.Equal("08:00", padrao.GetProperty("abertura").GetString());
        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        Assert.Equal("07:30", atualizada.GetProperty("abertura").GetString());
        Assert.Equal(15, atualizada.GetProperty("intervaloAgendaMin").GetInt32());
        Assert.Equal([1, 2, 3, 4, 5, 6], atualizada.GetProperty("diasFuncionamento").EnumerateArray().Select(d => d.GetInt32()).ToArray());
        Assert.True(await fabrica.NoBancoAsync(async (db, _) => await db.LogsAuditoria.AnyAsync(l => l.Acao == "AjusteConfiguracao")));
    }

    [Fact]
    public async Task Configuracao_invalida_devolve_422_e_so_o_Dono_acessa()
    {
        using var dono = await fabrica.AutenticadoAsync((await fabrica.CriarDonoAsync()).Email!);
        using var cliente = await fabrica.AutenticadoAsync((await fabrica.CriarClienteComLoginAsync()).Email!);

        var horaRuim = await dono.PutAsJsonAsync(Url("/configuracoes"), Config(abertura: "8h"));
        var fechaAntes = await dono.PutAsJsonAsync(Url("/configuracoes"), Config(abertura: "18:00", fechamento: "08:00"));
        var diaRuim = await dono.PutAsJsonAsync(Url("/configuracoes"), Config(dias: [1, 9]));
        var proibido = await cliente.GetAsync(Url("/configuracoes"));

        Assert.Equal(HttpStatusCode.UnprocessableEntity, horaRuim.StatusCode);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, fechaAntes.StatusCode);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, diaRuim.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, proibido.StatusCode);
    }

    [Fact]
    public async Task Modelos_de_mensagem_listam_os_dois_tipos_e_o_Dono_edita_o_texto()
    {
        using var dono = await fabrica.AutenticadoAsync((await fabrica.CriarDonoAsync()).Email!);

        var lista = await (await dono.GetAsync(Url("/modelos-mensagem"))).LerJsonAsync();
        var editado = await dono.PutAsJsonAsync(Url("/modelos-mensagem/lavagem-iniciada"), new { texto = "Olá, {nome}! Começamos a lavar o seu {modelo}." });
        var vazio = await dono.PutAsJsonAsync(Url("/modelos-mensagem/lavagem-iniciada"), new { texto = " " });
        var longo = await dono.PutAsJsonAsync(Url("/modelos-mensagem/lavagem-iniciada"), new { texto = new string('a', 501) });
        var tipoRuim = await dono.PutAsJsonAsync(Url("/modelos-mensagem/outra"), new { texto = "x" });

        Assert.Equal(["lavagem-iniciada", "lavagem-concluida"], lista.EnumerateArray().Select(m => m.GetProperty("tipo").GetString()!).ToArray());
        Assert.Equal(HttpStatusCode.OK, editado.StatusCode);
        Assert.Equal("Olá, {nome}! Começamos a lavar o seu {modelo}.", (await editado.LerJsonAsync()).GetProperty("texto").GetString());
        Assert.Equal(HttpStatusCode.UnprocessableEntity, vazio.StatusCode);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, longo.StatusCode);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, tipoRuim.StatusCode);
    }
}
