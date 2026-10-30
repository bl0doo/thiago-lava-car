using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using ThiagoLavaCar.Domain.Entities;
using ThiagoLavaCar.Domain.Enums;

namespace ThiagoLavaCar.IntegrationTests;

[Collection(ApiCompartilhada.Nome)]
public sealed class OrdensServicoEndpointsTests(ApiFactory fabrica)
{
    private static string Url(string caminho) => $"{ApiClientExtensions.Base}/ordens-servico/{caminho}";

    [Fact] // T12
    public async Task Rota_protegida_sem_token_devolve_401_em_ProblemDetails()
    {
        using var http = fabrica.CreateClient();

        var resposta = await http.GetAsync(Url("quadro"));

        Assert.Equal(HttpStatusCode.Unauthorized, resposta.StatusCode);
        Assert.Equal("Não autenticado", (await resposta.LerJsonAsync()).GetProperty("title").GetString());
    }

    [Fact] // T09 (equivalente: perfil sem permissão na rota)
    public async Task Cliente_no_quadro_devolve_403()
    {
        var cliente = await fabrica.CriarClienteComLoginAsync();
        using var http = await fabrica.AutenticadoAsync(cliente.Email!);

        var resposta = await http.GetAsync(Url("quadro"));

        Assert.Equal(HttpStatusCode.Forbidden, resposta.StatusCode);
        Assert.Equal("Sem permissão", (await resposta.LerJsonAsync()).GetProperty("title").GetString());
    }

    [Fact] // T11
    public async Task Lavador_abre_o_quadro_sem_valorTotal_e_sem_telefone_e_o_dono_com_os_dois()
    {
        var dono = await fabrica.CriarDonoAsync();
        var lavador = await fabrica.CriarLavadorAsync();
        var osId = await fabrica.CriarOrdemAguardandoAsync(dono.Id);
        using var comoLavador = await fabrica.AutenticadoPorPinAsync(lavador.Id);
        using var comoDono = await fabrica.AutenticadoAsync(dono.Email!);

        var cartaoLavador = (await (await comoLavador.GetAsync(Url("quadro"))).LerJsonAsync())
            .GetProperty("aguardando").EnumerateArray().Single(c => c.GetProperty("id").GetInt32() == osId);
        var cartaoDono = (await (await comoDono.GetAsync(Url("quadro"))).LerJsonAsync())
            .GetProperty("aguardando").EnumerateArray().Single(c => c.GetProperty("id").GetInt32() == osId);

        Assert.False(cartaoLavador.TryGetProperty("valorTotal", out _));
        Assert.False(cartaoLavador.TryGetProperty("telefone", out _));
        Assert.False(cartaoLavador.TryGetProperty("cliente", out _));
        Assert.Equal(100m, cartaoDono.GetProperty("valorTotal").GetDecimal());
        Assert.True(cartaoDono.TryGetProperty("telefone", out _));
    }

    [Fact] // T26 + T28 + T29
    public async Task Fluxo_iniciar_desfazer_iniciar_encerrar_calcula_102_minutos()
    {
        var dono = await fabrica.CriarDonoAsync();
        var lavador = await fabrica.CriarLavadorAsync();
        var osId = await fabrica.CriarOrdemAguardandoAsync(dono.Id);
        using var http = await fabrica.AutenticadoPorPinAsync(lavador.Id);

        var iniciada = await http.PostAsync(Url($"{osId}/iniciar"), null);
        Assert.Equal(HttpStatusCode.OK, iniciada.StatusCode);
        var cartao = await iniciada.LerJsonAsync();
        Assert.Equal("EmAndamento", cartao.GetProperty("status").GetString());
        Assert.Equal(lavador.Apelido, cartao.GetProperty("lavador").GetString());

        fabrica.Relogio.Advance(TimeSpan.FromMinutes(1));
        var desfeita = await http.PostAsync(Url($"{osId}/desfazer"), null);
        Assert.Equal("Aguardando", (await desfeita.LerJsonAsync()).GetProperty("status").GetString());

        await http.PostAsync(Url($"{osId}/iniciar"), null);
        fabrica.Relogio.Advance(TimeSpan.FromMinutes(102));
        // o access token dura 30 min; depois de 102 min o app já o teria renovado pelo cookie
        using var renovado = await fabrica.AutenticadoPorPinAsync(lavador.Id);
        var encerrada = await renovado.PostAsync(Url($"{osId}/encerrar"), null);

        var final = await encerrada.LerJsonAsync();
        Assert.Equal("Concluido", final.GetProperty("status").GetString());
        Assert.Equal(102, final.GetProperty("duracaoRealMin").GetInt32());
    }

    [Fact] // T27
    public async Task Encerrar_cartao_em_Aguardando_devolve_409_com_a_mensagem_em_portugues()
    {
        var dono = await fabrica.CriarDonoAsync();
        var osId = await fabrica.CriarOrdemAguardandoAsync(dono.Id);
        using var http = await fabrica.AutenticadoAsync(dono.Email!);

        var resposta = await http.PostAsync(Url($"{osId}/encerrar"), null);

        Assert.Equal(HttpStatusCode.Conflict, resposta.StatusCode);
        Assert.Equal("A lavagem ainda não foi iniciada.", (await resposta.LerJsonAsync()).GetProperty("detail").GetString());
    }

    [Fact] // T30
    public async Task Desfazer_3_minutos_depois_devolve_409()
    {
        var dono = await fabrica.CriarDonoAsync();
        var osId = await fabrica.CriarOrdemAguardandoAsync(dono.Id);
        using var http = await fabrica.AutenticadoAsync(dono.Email!);
        await http.PostAsync(Url($"{osId}/iniciar"), null);
        fabrica.Relogio.Advance(TimeSpan.FromMinutes(3));

        var resposta = await http.PostAsync(Url($"{osId}/desfazer"), null);

        Assert.Equal(HttpStatusCode.Conflict, resposta.StatusCode);
    }

    [Fact]
    public async Task Cartao_inexistente_devolve_404_e_id_nao_numerico_devolve_400()
    {
        var dono = await fabrica.CriarDonoAsync();
        using var http = await fabrica.AutenticadoAsync(dono.Email!);

        Assert.Equal(HttpStatusCode.NotFound, (await http.GetAsync(Url("999999"))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await http.GetAsync(Url("abc"))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await http.GetAsync(Url("quadro?data=ontem"))).StatusCode);
    }

    [Fact]
    public async Task Dono_inicia_em_nome_de_um_lavador_e_lavador_nao_pode_indicar_outro()
    {
        var dono = await fabrica.CriarDonoAsync();
        var lavador = await fabrica.CriarLavadorAsync("Marcos");
        var outro = await fabrica.CriarLavadorAsync("Paulo");
        var os1 = await fabrica.CriarOrdemAguardandoAsync(dono.Id);
        var os2 = await fabrica.CriarOrdemAguardandoAsync(dono.Id);
        using var comoDono = await fabrica.AutenticadoAsync(dono.Email!);
        using var comoLavador = await fabrica.AutenticadoPorPinAsync(lavador.Id);

        var pelaDono = await comoDono.PostAsync(Url($"{os1}/iniciar?lavadorId={lavador.Id}"), null);
        var pelaLavador = await comoLavador.PostAsync(Url($"{os2}/iniciar?lavadorId={outro.Id}"), null);

        Assert.Equal("Marcos", (await pelaDono.LerJsonAsync()).GetProperty("lavador").GetString());
        Assert.Equal(HttpStatusCode.UnprocessableEntity, pelaLavador.StatusCode);
    }

    [Fact]
    public async Task Dois_lavadores_iniciando_o_mesmo_cartao_ao_mesmo_tempo_terminam_com_um_200_e_um_409()
    {
        var dono = await fabrica.CriarDonoAsync();
        var diego = await fabrica.CriarLavadorAsync("Diego");
        var marcos = await fabrica.CriarLavadorAsync("Marcos");
        var osId = await fabrica.CriarOrdemAguardandoAsync(dono.Id);
        using var a = await fabrica.AutenticadoPorPinAsync(diego.Id);
        using var b = await fabrica.AutenticadoPorPinAsync(marcos.Id);

        var respostas = await Task.WhenAll(a.PostAsync(Url($"{osId}/iniciar"), null), b.PostAsync(Url($"{osId}/iniciar"), null));

        Assert.Single(respostas, r => r.StatusCode == HttpStatusCode.OK);
        Assert.Single(respostas, r => r.StatusCode == HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Token_de_lavador_deixa_de_valer_quando_ele_e_desativado()
    {
        var dono = await fabrica.CriarDonoAsync();
        var lavador = await fabrica.CriarLavadorAsync();
        using var http = await fabrica.AutenticadoPorPinAsync(lavador.Id);
        Assert.Equal(HttpStatusCode.OK, (await http.GetAsync(Url("quadro"))).StatusCode);

        await fabrica.NoBancoAsync(async (db, _) =>
        {
            (await db.Usuarios.SingleAsync(u => u.Id == lavador.Id)).Desativar();
            return await db.SaveChangesAsync();
        });

        Assert.Equal(HttpStatusCode.Unauthorized, (await http.GetAsync(Url("quadro"))).StatusCode);
        Assert.NotNull(dono);
    }

    private static object Entrada(int veiculoId, int servicoId, bool cera = true) => new
    {
        veiculoId,
        itens = new object[] { new { servicoId, comCera = cera, precoManual = (decimal?)null } },
        semAvarias = false,
        avarias = new object[] { new { local = "Para-choque traseiro", tipo = "Risco", descricao = "Risco de 5 cm" } },
        pertencesEntrada = "Cadeirinha infantil",
        aceiteCliente = true,
    };

    private async Task<(int VeiculoId, int ServicoId)> PrepararAsync()
    {
        var (veiculoId, servicoId) = await fabrica.NoBancoAsync(async (db, _) =>
        {
            var cliente = Cliente.Criar("Renata Souza", ApiFactory.NovoTelefone(), null, CanalContato.WhatsApp, true, fabrica.Relogio.GetUtcNow());
            db.Clientes.Add(cliente);
            var servico = Servico.Criar($"Lavagem {Guid.NewGuid():N}", "Lavagem", 90, 8m);
            servico.DefinirPreco(TipoVeiculo.Carro, 80m, 100m);
            db.Servicos.Add(servico);
            await db.SaveChangesAsync();
            var veiculo = Veiculo.Criar(cliente.Id, ApiFactory.NovaPlaca(), "Chevrolet Onix", TipoVeiculo.Carro, "Prata");
            db.Veiculos.Add(veiculo);
            await db.SaveChangesAsync();
            return (veiculo.Id, servico.Id);
        });
        return (veiculoId, servicoId);
    }

    [Fact] // T13
    public async Task Dono_registra_entrada_e_o_cartao_aparece_em_Aguardando_no_quadro()
    {
        var dono = await fabrica.CriarDonoAsync();
        var (veiculoId, servicoId) = await PrepararAsync();
        using var http = await fabrica.AutenticadoAsync(dono.Email!);

        var resposta = await http.PostAsJsonAsync(Url("").TrimEnd('/'), Entrada(veiculoId, servicoId));

        Assert.Equal(HttpStatusCode.Created, resposta.StatusCode);
        var criada = await resposta.LerJsonAsync();
        Assert.Equal("Aguardando", criada.GetProperty("status").GetString());
        Assert.Equal(100m, criada.GetProperty("valorTotal").GetDecimal());
        Assert.Equal(90, criada.GetProperty("duracaoEstimadaMin").GetInt32());
        var id = criada.GetProperty("id").GetInt32();
        var quadro = await (await http.GetAsync(Url("quadro"))).LerJsonAsync();
        Assert.Contains(quadro.GetProperty("aguardando").EnumerateArray(), c => c.GetProperty("id").GetInt32() == id);
    }

    [Fact] // T20
    public async Task Entrada_pedindo_cera_em_servico_sem_preco_com_cera_devolve_422()
    {
        var (veiculoId, servicoId) = await fabrica.NoBancoAsync(async (db, _) =>
        {
            var cliente = Cliente.Criar("Renata Souza", ApiFactory.NovoTelefone(), null, CanalContato.WhatsApp, true, fabrica.Relogio.GetUtcNow());
            db.Clientes.Add(cliente);
            var servico = Servico.Criar($"Aspiração {Guid.NewGuid():N}", "Lavagem", 30, 2m);
            servico.DefinirPreco(TipoVeiculo.Carro, 40m, null);
            db.Servicos.Add(servico);
            await db.SaveChangesAsync();
            var veiculo = Veiculo.Criar(cliente.Id, ApiFactory.NovaPlaca(), "Chevrolet Onix", TipoVeiculo.Carro, "Prata");
            db.Veiculos.Add(veiculo);
            await db.SaveChangesAsync();
            return (veiculo.Id, servico.Id);
        });
        using var http = await fabrica.AutenticadoAsync((await fabrica.CriarDonoAsync()).Email!);

        var comCera = await http.PostAsJsonAsync(Url("").TrimEnd('/'), Entrada(veiculoId, servicoId, cera: true));
        var semCera = await http.PostAsJsonAsync(Url("").TrimEnd('/'), Entrada(veiculoId, servicoId, cera: false));

        Assert.Equal(HttpStatusCode.UnprocessableEntity, comCera.StatusCode);
        Assert.Equal(HttpStatusCode.Created, semCera.StatusCode);
        Assert.Equal(40m, (await semCera.LerJsonAsync()).GetProperty("valorTotal").GetDecimal());
    }

    [Fact] // T21
    public async Task Tabela_de_precos_muda_depois_da_venda_e_a_OS_ja_registrada_mantem_o_valor()
    {
        var (veiculoId, servicoId) = await PrepararAsync();
        using var http = await fabrica.AutenticadoAsync((await fabrica.CriarDonoAsync()).Email!);
        var osId = (await (await http.PostAsJsonAsync(Url("").TrimEnd('/'), Entrada(veiculoId, servicoId))).LerJsonAsync()).GetProperty("id").GetInt32();

        await fabrica.NoBancoAsync(async (db, _) =>
        {
            var servico = await db.Servicos.Include(s => s.Precos).SingleAsync(s => s.Id == servicoId);
            servico.DefinirPreco(TipoVeiculo.Carro, 90m, 120m);
            await db.SaveChangesAsync();
            return 0;
        });
        var cartao = await (await http.GetAsync(Url(osId.ToString(System.Globalization.CultureInfo.InvariantCulture)))).LerJsonAsync();

        Assert.Equal(100m, cartao.GetProperty("valorTotal").GetDecimal());
    }

    [Fact] // T24
    public async Task Sem_avarias_falso_com_lista_vazia_devolve_422_no_endpoint()
    {
        var (veiculoId, servicoId) = await PrepararAsync();
        using var http = await fabrica.AutenticadoAsync((await fabrica.CriarDonoAsync()).Email!);

        var resposta = await http.PostAsJsonAsync(Url("").TrimEnd('/'), new
        {
            veiculoId,
            itens = new object[] { new { servicoId, comCera = false } },
            semAvarias = false,
            avarias = Array.Empty<object>(),
            aceiteCliente = true,
        });

        Assert.Equal(HttpStatusCode.UnprocessableEntity, resposta.StatusCode);
    }

    [Fact] // T14
    public async Task Segunda_entrada_do_mesmo_veiculo_devolve_409_e_sem_aceite_422()
    {
        var dono = await fabrica.CriarDonoAsync();
        var (veiculoId, servicoId) = await PrepararAsync();
        using var http = await fabrica.AutenticadoAsync(dono.Email!);
        var url = Url("").TrimEnd('/');
        await http.PostAsJsonAsync(url, Entrada(veiculoId, servicoId));

        var repetida = await http.PostAsJsonAsync(url, Entrada(veiculoId, servicoId));
        var semAceite = await http.PostAsJsonAsync(url, new { veiculoId, itens = new object[] { new { servicoId } }, semAvarias = true, aceiteCliente = false });

        Assert.Equal(HttpStatusCode.Conflict, repetida.StatusCode);
        Assert.Equal("Este veículo já está no pátio.", (await repetida.LerJsonAsync()).GetProperty("detail").GetString());
        Assert.Equal(HttpStatusCode.UnprocessableEntity, semAceite.StatusCode);
        Assert.True((await semAceite.LerJsonAsync()).GetProperty("errors").TryGetProperty("aceiteCliente", out _));
    }

    [Fact]
    public async Task Lavador_nao_registra_entrada_mas_registra_pertences_encontrados()
    {
        var dono = await fabrica.CriarDonoAsync();
        var lavador = await fabrica.CriarLavadorAsync();
        var osId = await fabrica.CriarOrdemAguardandoAsync(dono.Id);
        using var http = await fabrica.AutenticadoPorPinAsync(lavador.Id);

        var entrada = await http.PostAsJsonAsync(Url("").TrimEnd('/'), new { veiculoId = 1 });
        var pertences = await http.PatchAsJsonAsync(Url($"{osId}/pertences-encontrados"), new { texto = "Óculos no porta-luvas" });

        Assert.Equal(HttpStatusCode.Forbidden, entrada.StatusCode);
        Assert.Equal(HttpStatusCode.OK, pertences.StatusCode);
    }

    [Fact]
    public async Task Ciclo_completo_iniciar_encerrar_avaliar_e_entregar_sem_corpo()
    {
        var dono = await fabrica.CriarDonoAsync();
        var osId = await fabrica.CriarOrdemAguardandoAsync(dono.Id);
        using var http = await fabrica.AutenticadoAsync(dono.Email!);

        var cedo = await http.PostAsync(Url($"{osId}/entregar"), null);
        await http.PostAsync(Url($"{osId}/iniciar"), null);
        fabrica.Relogio.Advance(TimeSpan.FromMinutes(30));
        using var renovado = await fabrica.AutenticadoAsync(dono.Email!);
        await renovado.PostAsync(Url($"{osId}/encerrar"), null);
        var invalida = await renovado.PatchAsJsonAsync(Url($"{osId}/avaliacao"), new { nota = 9 });
        var avaliada = await renovado.PatchAsJsonAsync(Url($"{osId}/avaliacao"), new { nota = 5 });
        var entregue = await renovado.PostAsync(Url($"{osId}/entregar"), null);
        var repetida = await renovado.PostAsync(Url($"{osId}/entregar"), null);

        Assert.Equal(HttpStatusCode.Conflict, cedo.StatusCode);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, invalida.StatusCode);
        Assert.Equal(HttpStatusCode.OK, avaliada.StatusCode);
        Assert.Equal("Entregue", (await entregue.LerJsonAsync()).GetProperty("status").GetString());
        Assert.Equal(HttpStatusCode.Conflict, repetida.StatusCode);
    }

    [Fact]
    public async Task Cancelar_em_Aguardando_funciona_e_depois_nao_aceita_iniciar()
    {
        var dono = await fabrica.CriarDonoAsync();
        var osId = await fabrica.CriarOrdemAguardandoAsync(dono.Id);
        using var http = await fabrica.AutenticadoAsync(dono.Email!);

        var cancelada = await http.PostAsync(Url($"{osId}/cancelar"), null);
        var iniciar = await http.PostAsync(Url($"{osId}/iniciar"), null);

        Assert.Equal("Cancelada", (await cancelada.LerJsonAsync()).GetProperty("status").GetString());
        Assert.Equal(HttpStatusCode.Conflict, iniciar.StatusCode);
    }

    [Fact]
    public async Task Editar_troca_os_servicos_e_recalcula_o_valor()
    {
        var dono = await fabrica.CriarDonoAsync();
        var (veiculoId, servicoId) = await PrepararAsync();
        using var http = await fabrica.AutenticadoAsync(dono.Email!);
        var osId = (await (await http.PostAsJsonAsync(Url("").TrimEnd('/'), Entrada(veiculoId, servicoId, cera: true))).LerJsonAsync()).GetProperty("id").GetInt32();

        var resposta = await http.PutAsJsonAsync(Url(osId.ToString(System.Globalization.CultureInfo.InvariantCulture)),
            new { itens = new object[] { new { servicoId, comCera = false } }, pertencesEntrada = "Nada" });

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        Assert.Equal(80m, (await resposta.LerJsonAsync()).GetProperty("valorTotal").GetDecimal());
        var cartao = await (await http.GetAsync(Url(osId.ToString(System.Globalization.CultureInfo.InvariantCulture)))).LerJsonAsync();
        Assert.Equal(80m, cartao.GetProperty("valorTotal").GetDecimal());
    }

    [Fact]
    public async Task Lavador_nao_cancela_nem_entrega_nem_edita()
    {
        var dono = await fabrica.CriarDonoAsync();
        var lavador = await fabrica.CriarLavadorAsync();
        var osId = await fabrica.CriarOrdemAguardandoAsync(dono.Id);
        using var http = await fabrica.AutenticadoPorPinAsync(lavador.Id);

        Assert.Equal(HttpStatusCode.Forbidden, (await http.PostAsync(Url($"{osId}/cancelar"), null)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await http.PostAsync(Url($"{osId}/entregar"), null)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await http.PutAsJsonAsync(Url($"{osId}"), new { itens = Array.Empty<object>() })).StatusCode);
    }

    private static object EntradaComPreco(int veiculoId, int servicoId, decimal? precoManual) => new
    {
        veiculoId,
        itens = new object[] { new { servicoId, comCera = true, precoManual } },
        semAvarias = true,
        avarias = Array.Empty<object>(),
        aceiteCliente = true,
    };

    [Fact]
    public async Task Preco_manual_vira_o_preco_do_item_marca_ajustado_e_vai_para_a_auditoria()
    {
        var (veiculoId, servicoId) = await PrepararAsync();
        using var http = await fabrica.AutenticadoAsync((await fabrica.CriarDonoAsync()).Email!);

        var criada = await http.PostAsJsonAsync(Url("").TrimEnd('/'), EntradaComPreco(veiculoId, servicoId, 150m));
        var osId = (await criada.LerJsonAsync()).GetProperty("id").GetInt32();
        var editada = await http.PutAsJsonAsync(Url(osId.ToString(System.Globalization.CultureInfo.InvariantCulture)), new
        {
            itens = new object[] { new { servicoId, comCera = true, precoManual = (decimal?)130m } },
        });

        Assert.Equal(HttpStatusCode.Created, criada.StatusCode);
        Assert.Equal(150m, (await criada.LerJsonAsync()).GetProperty("valorTotal").GetDecimal());
        Assert.Equal(HttpStatusCode.OK, editada.StatusCode);
        var (ajustado, preco, logs) = await fabrica.NoBancoAsync(async (db, _) =>
        {
            var item = await db.Set<ItemOrdemServico>().SingleAsync(i => i.OrdemServicoId == osId);
            var total = await db.LogsAuditoria.CountAsync(l => l.Acao == "PrecoManual" && l.EntidadeId == osId);
            return (item.PrecoAjustado, item.PrecoFinal, total);
        });
        Assert.True(ajustado);
        Assert.Equal(130m, preco);
        Assert.Equal(2, logs);
    }

    [Fact]
    public async Task Preco_manual_zero_ou_negativo_devolve_422()
    {
        var (veiculoId, servicoId) = await PrepararAsync();
        using var http = await fabrica.AutenticadoAsync((await fabrica.CriarDonoAsync()).Email!);

        var zero = await http.PostAsJsonAsync(Url("").TrimEnd('/'), EntradaComPreco(veiculoId, servicoId, 0m));
        var negativo = await http.PostAsJsonAsync(Url("").TrimEnd('/'), EntradaComPreco(veiculoId, servicoId, -5m));

        Assert.Equal(HttpStatusCode.UnprocessableEntity, zero.StatusCode);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, negativo.StatusCode);
    }
}
