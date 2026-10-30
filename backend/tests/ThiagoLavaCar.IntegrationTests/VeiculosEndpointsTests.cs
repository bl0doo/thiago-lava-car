using System.Net;
using System.Net.Http.Json;

namespace ThiagoLavaCar.IntegrationTests;

[Collection(ApiCompartilhada.Nome)]
public sealed class VeiculosEndpointsTests(ApiFactory fabrica)
{
    private static string Url(string caminho = "") => $"{ApiClientExtensions.Base}/veiculos{caminho}";

    private static object Corpo(int? clienteId, string? placa = null) =>
        new { clienteId, placa = placa ?? ApiFactory.NovaPlaca(), modeloDescricao = "Chevrolet Onix", tipoVeiculo = "Carro", cor = "Prata" };

    private static async Task<int> CriarClienteAsync(HttpClient dono) =>
        (await (await dono.PostAsJsonAsync($"{ApiClientExtensions.Base}/clientes",
            new { nome = "Renata Souza", telefone = ApiFactory.NovoTelefone(), canalPreferido = "WhatsApp", consentimentoLgpd = true })).LerJsonAsync())
        .GetProperty("id").GetInt32();

    [Fact]
    public async Task Dono_cadastra_veiculo_com_placa_normalizada_e_busca_por_placa_com_hifen()
    {
        using var http = await fabrica.AutenticadoAsync((await fabrica.CriarDonoAsync()).Email!);
        var clienteId = await CriarClienteAsync(http);

        var criado = await http.PostAsJsonAsync(Url(), Corpo(clienteId, "qwe-1r23"));
        var busca = await http.GetAsync(Url("?placa=qwe-1r23"));

        Assert.Equal(HttpStatusCode.Created, criado.StatusCode);
        Assert.Equal("QWE1R23", (await criado.LerJsonAsync()).GetProperty("placa").GetString());
        Assert.Equal(HttpStatusCode.OK, busca.StatusCode);
    }

    [Fact]
    public async Task Placa_repetida_devolve_409_e_placa_invalida_422()
    {
        using var http = await fabrica.AutenticadoAsync((await fabrica.CriarDonoAsync()).Email!);
        var clienteId = await CriarClienteAsync(http);
        var placa = ApiFactory.NovaPlaca();
        await http.PostAsJsonAsync(Url(), Corpo(clienteId, placa));

        var repetida = await http.PostAsJsonAsync(Url(), Corpo(clienteId, placa));
        var invalida = await http.PostAsJsonAsync(Url(), Corpo(clienteId, "123ABCD"));

        Assert.Equal(HttpStatusCode.Conflict, repetida.StatusCode);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, invalida.StatusCode);
        Assert.True((await invalida.LerJsonAsync()).GetProperty("errors").TryGetProperty("placa", out _));
    }

    [Fact]
    public async Task Cliente_logado_cadastra_veiculo_para_si_e_nao_ve_o_de_outro()
    {
        var usuario = await fabrica.CriarClienteComLoginAsync();
        using var comoCliente = await fabrica.AutenticadoAsync(usuario.Email!);
        using var comoDono = await fabrica.AutenticadoAsync((await fabrica.CriarDonoAsync()).Email!);
        var outroCliente = await CriarClienteAsync(comoDono);
        var alheio = (await (await comoDono.PostAsJsonAsync(Url(), Corpo(outroCliente))).LerJsonAsync()).GetProperty("id").GetInt32();

        var proprio = await comoCliente.PostAsJsonAsync(Url(), Corpo(outroCliente)); // clienteId do corpo é ignorado
        var meusVeiculos = await (await comoCliente.GetAsync($"{ApiClientExtensions.Base}/clientes/me/veiculos")).LerJsonAsync();
        var fichaAlheia = await comoCliente.GetAsync(Url($"/{alheio}/ficha"));

        Assert.Equal(HttpStatusCode.Created, proprio.StatusCode);
        Assert.Equal(1, meusVeiculos.GetArrayLength());
        Assert.Equal(HttpStatusCode.NotFound, fichaAlheia.StatusCode);
    }

    [Fact]
    public async Task Ficha_lista_o_historico_de_servicos_do_veiculo()
    {
        var dono = await fabrica.CriarDonoAsync();
        var osId = await fabrica.CriarOrdemAguardandoAsync(dono.Id);
        using var http = await fabrica.AutenticadoAsync(dono.Email!);
        var veiculoId = (await (await http.GetAsync($"{ApiClientExtensions.Base}/ordens-servico/{osId}")).LerJsonAsync()).GetProperty("placa").GetString()!;
        var id = (await (await http.GetAsync(Url($"?placa={veiculoId}"))).LerJsonAsync()).GetProperty("id").GetInt32();

        var ficha = await (await http.GetAsync(Url($"/{id}/ficha"))).LerJsonAsync();

        Assert.Equal(veiculoId, ficha.GetProperty("veiculo").GetProperty("placa").GetString());
        var historico = ficha.GetProperty("historico");
        Assert.Equal(1, historico.GetArrayLength());
        Assert.Equal(osId, historico[0].GetProperty("id").GetInt32());
        Assert.Equal(100m, historico[0].GetProperty("valorTotal").GetDecimal());
    }

    [Fact]
    public async Task Lavador_nao_acessa_veiculos()
    {
        using var http = await fabrica.AutenticadoPorPinAsync((await fabrica.CriarLavadorAsync()).Id);

        Assert.Equal(HttpStatusCode.Forbidden, (await http.GetAsync(Url("?placa=ABC1234"))).StatusCode);
    }

    [Fact]
    public async Task Busca_de_modelos_filtra_por_marca_e_nome_e_cadastro_herda_do_modelo()
    {
        var marca = $"Zeta{Guid.NewGuid():N}"[..12];
        var modeloId = await fabrica.NoBancoAsync(async (db, _) =>
        {
            var modelo = ThiagoLavaCar.Domain.Entities.ModeloVeiculo.Criar(marca, "Rapido", ThiagoLavaCar.Domain.Enums.TipoVeiculo.Moto);
            db.ModelosVeiculo.Add(modelo);
            await db.SaveChangesAsync();
            return modelo.Id;
        });
        var usuario = await fabrica.CriarClienteComLoginAsync();
        using var http = await fabrica.AutenticadoAsync(usuario.Email!);

        var busca = await (await http.GetAsync($"{ApiClientExtensions.Base}/modelos-veiculo?busca={marca.ToUpperInvariant()}%20rap")).LerJsonAsync();
        var criado = await http.PostAsJsonAsync(Url(), new { placa = ApiFactory.NovaPlaca(), modeloVeiculoId = modeloId });

        Assert.Equal(1, busca.GetArrayLength());
        Assert.Equal(modeloId, busca[0].GetProperty("id").GetInt32());
        Assert.Equal(HttpStatusCode.Created, criado.StatusCode);
        var corpo = await criado.LerJsonAsync();
        Assert.Equal($"{marca} Rapido", corpo.GetProperty("modelo").GetString());
        Assert.Equal("Moto", corpo.GetProperty("tipo").GetString());
    }
}
