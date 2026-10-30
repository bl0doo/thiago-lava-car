using System.Net;
using System.Net.Http.Json;

namespace ThiagoLavaCar.IntegrationTests;

[Collection(ApiCompartilhada.Nome)]
public sealed class ServicosEndpointsTests(ApiFactory fabrica)
{
    private static string Url(string caminho = "") => $"{ApiClientExtensions.Base}/servicos{caminho}";

    private static object Corpo(string nome, decimal carro = 80m) => new
    {
        nome, categoria = "Lavagem", tempoEstimadoMin = 90, custoInsumos = 8.5m,
        precos = new object[] { new { tipoVeiculo = "Carro", preco = carro, precoComCera = (decimal?)100m }, new { tipoVeiculo = "Moto", preco = 40m } },
    };

    [Fact]
    public async Task Dono_cria_edita_preco_e_desativa_o_servico()
    {
        using var http = await fabrica.AutenticadoAsync((await fabrica.CriarDonoAsync()).Email!);
        var nome = $"Polimento {Guid.NewGuid():N}";

        var criado = await http.PostAsJsonAsync(Url(), Corpo(nome));
        var id = (await criado.LerJsonAsync()).GetProperty("id").GetInt32();
        var editado = await http.PutAsJsonAsync(Url($"/{id}"), Corpo(nome, carro: 95m));
        var preco = await (await http.GetAsync(Url($"/{id}/preco?tipoVeiculo=Carro&comCera=true"))).LerJsonAsync();
        var inativo = await http.PatchAsJsonAsync(Url($"/{id}/ativo"), new { ativo = false });

        Assert.Equal(HttpStatusCode.Created, criado.StatusCode);
        Assert.Equal(HttpStatusCode.OK, editado.StatusCode);
        Assert.Equal(100m, preco.GetProperty("preco").GetDecimal());
        Assert.Equal(90, preco.GetProperty("duracaoEstimadaMin").GetInt32());
        Assert.False((await inativo.LerJsonAsync()).GetProperty("ativo").GetBoolean());
    }

    [Fact]
    public async Task Nome_repetido_409_e_preco_invalido_422()
    {
        using var http = await fabrica.AutenticadoAsync((await fabrica.CriarDonoAsync()).Email!);
        var nome = $"Cristalização {Guid.NewGuid():N}";
        await http.PostAsJsonAsync(Url(), Corpo(nome));

        var repetido = await http.PostAsJsonAsync(Url(), Corpo(nome));
        var invalido = await http.PostAsJsonAsync(Url(), Corpo($"{nome}x", carro: 0m));

        Assert.Equal(HttpStatusCode.Conflict, repetido.StatusCode);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, invalido.StatusCode);
    }

    [Fact]
    public async Task Cliente_lista_so_ativos_sem_custo_e_nao_cria_servico()
    {
        using var comoDono = await fabrica.AutenticadoAsync((await fabrica.CriarDonoAsync()).Email!);
        var nome = $"Higienização {Guid.NewGuid():N}";
        var id = (await (await comoDono.PostAsJsonAsync(Url(), Corpo(nome))).LerJsonAsync()).GetProperty("id").GetInt32();
        await comoDono.PatchAsJsonAsync(Url($"/{id}/ativo"), new { ativo = false });
        using var comoCliente = await fabrica.AutenticadoAsync((await fabrica.CriarClienteComLoginAsync()).Email!);

        var lista = await (await comoCliente.GetAsync(Url())).LerJsonAsync();
        var criar = await comoCliente.PostAsJsonAsync(Url(), Corpo("Qualquer"));

        Assert.DoesNotContain(lista.EnumerateArray(), s => s.GetProperty("id").GetInt32() == id);
        Assert.All(lista.EnumerateArray(), s => Assert.False(s.TryGetProperty("custoInsumos", out _)));
        Assert.Equal(HttpStatusCode.Forbidden, criar.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await comoCliente.GetAsync(Url($"/{id}/preco?tipoVeiculo=Carro"))).StatusCode);
    }

    [Fact]
    public async Task Preco_com_cera_inexistente_para_o_tipo_devolve_422()
    {
        using var http = await fabrica.AutenticadoAsync((await fabrica.CriarDonoAsync()).Email!);
        var id = (await (await http.PostAsJsonAsync(Url(), Corpo($"Enceramento {Guid.NewGuid():N}"))).LerJsonAsync()).GetProperty("id").GetInt32();

        var resposta = await http.GetAsync(Url($"/{id}/preco?tipoVeiculo=Moto&comCera=true"));

        Assert.Equal(HttpStatusCode.UnprocessableEntity, resposta.StatusCode);
    }
}
