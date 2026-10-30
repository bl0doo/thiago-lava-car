using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using ThiagoLavaCar.Application.Common;
using ThiagoLavaCar.Domain.Entities;
using ThiagoLavaCar.Domain.Enums;

namespace ThiagoLavaCar.IntegrationTests;

[Collection(ApiCompartilhada.Nome)]
public sealed class AgendamentosEndpointsTests(ApiFactory fabrica)
{
    private static string Url(string caminho = "") => $"{ApiClientExtensions.Base}/agendamentos{caminho}";

    /// <summary>O n-ésimo dia aberto (segunda a sábado) a partir de 3 dias à frente; cada teste usa o seu para não disputar vagas.</summary>
    private DateOnly DiaAberto(int n)
    {
        var dia = FusoBrasilia.DataDe(fabrica.Relogio.GetUtcNow()).AddDays(3);
        for (var achados = 0; ; dia = dia.AddDays(1))
        {
            if (dia.DayOfWeek != DayOfWeek.Sunday && achados++ == n)
            {
                return dia;
            }
        }
    }

    private static DateTimeOffset As(DateOnly dia, int hora, int minuto = 0) => FusoBrasilia.ParaUtc(dia, new TimeOnly(hora, minuto));

    private async Task<int> ServicoAsync(bool comCera = true) => await fabrica.NoBancoAsync(async (db, _) =>
    {
        var servico = Servico.Criar($"Lavagem {Guid.NewGuid():N}", "Lavagem", 90, 8m);
        servico.DefinirPreco(TipoVeiculo.Carro, 80m, comCera ? 100m : null);
        db.Servicos.Add(servico);
        await db.SaveChangesAsync();
        return servico.Id;
    });

    private async Task<int> VeiculoDoClienteAsync(int usuarioId) => await fabrica.NoBancoAsync(async (db, _) =>
    {
        var cliente = await db.Clientes.SingleAsync(c => c.UsuarioId == usuarioId);
        var veiculo = Veiculo.Criar(cliente.Id, ApiFactory.NovaPlaca(), "Chevrolet Onix", TipoVeiculo.Carro, "Prata");
        db.Veiculos.Add(veiculo);
        await db.SaveChangesAsync();
        return veiculo.Id;
    });

    private static object Pedido(int veiculoId, int servicoId, DateTimeOffset inicio, bool comCera = true) =>
        new { veiculoId, servicoId, comCera, inicio };

    [Fact]
    public async Task Horarios_trazem_preco_duracao_e_grade_de_30_em_30_ate_o_servico_caber()
    {
        await fabrica.CriarLavadorAsync();
        var servicoId = await ServicoAsync();
        using var http = await fabrica.AutenticadoAsync((await fabrica.CriarClienteComLoginAsync()).Email!);
        var dia = DiaAberto(0);

        var resposta = await (await http.GetAsync(Url($"/horarios?data={dia:yyyy-MM-dd}&servicoId={servicoId}&tipoVeiculo=Carro&comCera=true"))).LerJsonAsync();

        Assert.Equal(100m, resposta.GetProperty("preco").GetDecimal());
        Assert.Equal(90, resposta.GetProperty("duracaoEstimadaMin").GetInt32());
        var horarios = resposta.GetProperty("horarios").EnumerateArray().Select(h => h.GetString()).ToList();
        Assert.Equal("08:00", horarios.First());
        Assert.Equal("08:30", horarios[1]);
        Assert.Equal("16:30", horarios.Last()); // 16:30 + 90 min = 18:00, hora de fechar
    }

    [Fact]
    public async Task Horarios_recusam_domingo_data_fora_da_janela_e_cera_inexistente()
    {
        var servicoId = await ServicoAsync();
        var semCera = await ServicoAsync(comCera: false);
        using var http = await fabrica.AutenticadoAsync((await fabrica.CriarClienteComLoginAsync()).Email!);
        var hoje = FusoBrasilia.DataDe(fabrica.Relogio.GetUtcNow());
        var domingo = Enumerable.Range(1, 7).Select(hoje.AddDays).First(d => d.DayOfWeek == DayOfWeek.Sunday);

        async Task<HttpStatusCode> Status(DateOnly data, int servico, bool cera = false) =>
            (await http.GetAsync(Url($"/horarios?data={data:yyyy-MM-dd}&servicoId={servico}&tipoVeiculo=Carro&comCera={cera.ToString().ToLowerInvariant()}"))).StatusCode;

        Assert.Equal(HttpStatusCode.UnprocessableEntity, await Status(domingo, servicoId));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, await Status(hoje.AddDays(-1), servicoId));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, await Status(hoje.AddDays(31), servicoId));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, await Status(DiaAberto(1), semCera, cera: true));
        Assert.Equal(HttpStatusCode.NotFound, await Status(DiaAberto(1), 999_999));
    }

    [Fact]
    public async Task Cliente_agenda_e_nao_passa_de_dois_agendamentos_futuros()
    {
        await fabrica.CriarLavadorAsync();
        var servicoId = await ServicoAsync();
        var usuario = await fabrica.CriarClienteComLoginAsync();
        using var http = await fabrica.AutenticadoAsync(usuario.Email!);
        var veiculoId = await VeiculoDoClienteAsync(usuario.Id);
        var dia = DiaAberto(2);

        var primeiro = await http.PostAsJsonAsync(Url(), Pedido(veiculoId, servicoId, As(dia, 8)));
        var segundo = await http.PostAsJsonAsync(Url(), Pedido(veiculoId, servicoId, As(dia, 10)));
        var terceiro = await http.PostAsJsonAsync(Url(), Pedido(veiculoId, servicoId, As(dia, 14)));

        Assert.Equal(HttpStatusCode.Created, primeiro.StatusCode);
        var corpo = await primeiro.LerJsonAsync();
        Assert.Equal("Agendado", corpo.GetProperty("status").GetString());
        Assert.Equal(100m, corpo.GetProperty("precoEstimado").GetDecimal());
        Assert.Equal(90, corpo.GetProperty("duracaoEstimadaMin").GetInt32());
        Assert.Equal(HttpStatusCode.Created, segundo.StatusCode);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, terceiro.StatusCode);
    }

    [Fact]
    public async Task Horario_fora_da_grade_ou_do_funcionamento_devolve_422_e_veiculo_de_outro_cliente_404()
    {
        await fabrica.CriarLavadorAsync();
        var servicoId = await ServicoAsync();
        var dono = await fabrica.CriarClienteComLoginAsync();
        var outro = await fabrica.CriarClienteComLoginAsync();
        var veiculoDoOutro = await VeiculoDoClienteAsync(outro.Id);
        using var http = await fabrica.AutenticadoAsync(dono.Email!);
        var veiculoProprio = await VeiculoDoClienteAsync(dono.Id);
        var dia = DiaAberto(3);

        var quebrado = await http.PostAsJsonAsync(Url(), Pedido(veiculoProprio, servicoId, As(dia, 10, 15)));
        var cedo = await http.PostAsJsonAsync(Url(), Pedido(veiculoProprio, servicoId, As(dia, 7)));
        var tarde = await http.PostAsJsonAsync(Url(), Pedido(veiculoProprio, servicoId, As(dia, 17))); // 17:00 + 90 min passa das 18:00
        var alheio = await http.PostAsJsonAsync(Url(), Pedido(veiculoDoOutro, servicoId, As(dia, 10)));

        Assert.Equal(HttpStatusCode.UnprocessableEntity, quebrado.StatusCode);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, cedo.StatusCode);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, tarde.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, alheio.StatusCode);
    }

    [Fact] // T44
    public async Task Duas_reservas_para_a_ultima_vaga_resultam_em_201_e_409()
    {
        await fabrica.CriarLavadorAsync();
        var servicoId = await ServicoAsync();
        var dia = DiaAberto(4);
        var inicio = As(dia, 11);

        // Deixa só uma vaga: ocupa o horário com um agendamento a menos que o número de lavadores ativos.
        await fabrica.NoBancoAsync(async (db, _) =>
        {
            var lavadores = await db.Usuarios.CountAsync(u => u.Perfil == Perfil.Lavador && u.Ativo);
            var servico = await db.Servicos.Include(s => s.Precos).SingleAsync(s => s.Id == servicoId);
            for (var i = 0; i < lavadores - 1; i++)
            {
                var cliente = Cliente.Criar("Ocupante", ApiFactory.NovoTelefone(), null, CanalContato.WhatsApp, true, fabrica.Relogio.GetUtcNow());
                db.Clientes.Add(cliente);
                await db.SaveChangesAsync();
                var veiculo = Veiculo.Criar(cliente.Id, ApiFactory.NovaPlaca(), "Fiat Uno", TipoVeiculo.Carro, null);
                db.Veiculos.Add(veiculo);
                await db.SaveChangesAsync();
                db.Agendamentos.Add(Agendamento.Criar(cliente.Id, veiculo.Id, servico, TipoVeiculo.Carro, false, inicio, fabrica.Relogio.GetUtcNow()));
            }

            return await db.SaveChangesAsync();
        });

        var primeiro = await fabrica.CriarClienteComLoginAsync();
        var segundo = await fabrica.CriarClienteComLoginAsync();
        using var httpA = await fabrica.AutenticadoAsync(primeiro.Email!);
        using var httpB = await fabrica.AutenticadoAsync(segundo.Email!);
        var veiculoA = await VeiculoDoClienteAsync(primeiro.Id);
        var veiculoB = await VeiculoDoClienteAsync(segundo.Id);

        var respostas = await Task.WhenAll(
            httpA.PostAsJsonAsync(Url(), Pedido(veiculoA, servicoId, inicio)),
            httpB.PostAsJsonAsync(Url(), Pedido(veiculoB, servicoId, inicio)));

        Assert.Single(respostas, r => r.StatusCode == HttpStatusCode.Created);
        var perdedora = Assert.Single(respostas, r => r.StatusCode == HttpStatusCode.Conflict);
        Assert.Equal("Este horário acabou de ser ocupado.", (await perdedora.LerJsonAsync()).GetProperty("detail").GetString());
    }

    [Fact]
    public async Task Cliente_ve_so_os_proprios_e_cancela_liberando_o_limite_e_o_alheio_da_404()
    {
        await fabrica.CriarLavadorAsync();
        var servicoId = await ServicoAsync();
        var usuario = await fabrica.CriarClienteComLoginAsync();
        var outro = await fabrica.CriarClienteComLoginAsync();
        using var http = await fabrica.AutenticadoAsync(usuario.Email!);
        using var httpOutro = await fabrica.AutenticadoAsync(outro.Email!);
        var veiculo = await VeiculoDoClienteAsync(usuario.Id);
        var veiculoOutro = await VeiculoDoClienteAsync(outro.Id);
        var dia = DiaAberto(5);
        var id = (await (await http.PostAsJsonAsync(Url(), Pedido(veiculo, servicoId, As(dia, 9)))).LerJsonAsync()).GetProperty("id").GetInt32();
        await httpOutro.PostAsJsonAsync(Url(), Pedido(veiculoOutro, servicoId, As(dia, 9)));

        var meus = await (await http.GetAsync(Url("/meus"))).LerJsonAsync();
        var alheio = await httpOutro.PatchAsync(Url($"/{id}/cancelar"), null);
        var cancelado = await http.PatchAsync(Url($"/{id}/cancelar"), null);
        var denovo = await http.PatchAsync(Url($"/{id}/cancelar"), null);

        Assert.Equal(1, meus.GetArrayLength());
        Assert.Equal(id, meus[0].GetProperty("id").GetInt32());
        Assert.Equal(HttpStatusCode.NotFound, alheio.StatusCode);
        Assert.Equal(HttpStatusCode.OK, cancelado.StatusCode);
        Assert.Equal("Cancelado", (await cancelado.LerJsonAsync()).GetProperty("status").GetString());
        Assert.Equal(HttpStatusCode.Conflict, denovo.StatusCode);
    }

    [Fact]
    public async Task Dono_ve_a_agenda_do_dia_com_o_cliente_e_marca_falta_e_cliente_nao_acessa_a_agenda()
    {
        await fabrica.CriarLavadorAsync();
        var servicoId = await ServicoAsync();
        var usuario = await fabrica.CriarClienteComLoginAsync();
        using var comoCliente = await fabrica.AutenticadoAsync(usuario.Email!);
        using var comoDono = await fabrica.AutenticadoAsync((await fabrica.CriarDonoAsync()).Email!);
        var veiculo = await VeiculoDoClienteAsync(usuario.Id);
        var dia = DiaAberto(6);
        var id = (await (await comoCliente.PostAsJsonAsync(Url(), Pedido(veiculo, servicoId, As(dia, 15)))).LerJsonAsync()).GetProperty("id").GetInt32();

        var agenda = await (await comoDono.GetAsync(Url($"?data={dia:yyyy-MM-dd}"))).LerJsonAsync();
        var falta = await comoDono.PatchAsync(Url($"/{id}/nao-compareceu"), null);
        var clienteNaAgenda = await comoCliente.GetAsync(Url($"?data={dia:yyyy-MM-dd}"));
        var clienteMarcaFalta = await comoCliente.PatchAsync(Url($"/{id}/nao-compareceu"), null);

        var item = agenda.GetProperty("agendamentos").EnumerateArray().Single(a => a.GetProperty("id").GetInt32() == id);
        Assert.Equal("Renata Souza", item.GetProperty("cliente").GetString());
        Assert.Equal("Agendado", item.GetProperty("status").GetString());
        Assert.Equal("NaoCompareceu", (await falta.LerJsonAsync()).GetProperty("status").GetString());
        Assert.Equal(HttpStatusCode.Forbidden, clienteNaAgenda.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, clienteMarcaFalta.StatusCode);
    }

    private static object Vistoria(bool aceite = true) =>
        new { semAvarias = true, avarias = Array.Empty<object>(), pertencesEntrada = "Cadeirinha", nomeQuemRetira = "Renata", aceiteCliente = aceite };

    [Fact]
    public async Task Check_in_vira_OS_em_Aguardando_marca_atendido_e_nao_repete()
    {
        await fabrica.CriarLavadorAsync();
        var servicoId = await ServicoAsync();
        var usuario = await fabrica.CriarClienteComLoginAsync();
        using var comoCliente = await fabrica.AutenticadoAsync(usuario.Email!);
        using var comoDono = await fabrica.AutenticadoAsync((await fabrica.CriarDonoAsync()).Email!);
        var veiculo = await VeiculoDoClienteAsync(usuario.Id);
        var id = (await (await comoCliente.PostAsJsonAsync(Url(), Pedido(veiculo, servicoId, As(DiaAberto(7), 9)))).LerJsonAsync()).GetProperty("id").GetInt32();

        var semAceite = await comoDono.PostAsJsonAsync(Url($"/{id}/check-in"), Vistoria(aceite: false));
        var clienteTentando = await comoCliente.PostAsJsonAsync(Url($"/{id}/check-in"), Vistoria());
        var feito = await comoDono.PostAsJsonAsync(Url($"/{id}/check-in"), Vistoria());
        var denovo = await comoDono.PostAsJsonAsync(Url($"/{id}/check-in"), Vistoria());

        Assert.Equal(HttpStatusCode.UnprocessableEntity, semAceite.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, clienteTentando.StatusCode);
        Assert.Equal(HttpStatusCode.Created, feito.StatusCode);
        var os = await feito.LerJsonAsync();
        Assert.Equal("Aguardando", os.GetProperty("status").GetString());
        Assert.Equal(100m, os.GetProperty("valorTotal").GetDecimal());
        Assert.Equal(HttpStatusCode.Conflict, denovo.StatusCode);
        var ligada = await fabrica.NoBancoAsync(async (db, _) =>
        {
            var ordem = await db.OrdensServico.SingleAsync(o => o.AgendamentoId == id);
            var agendamento = await db.Agendamentos.SingleAsync(a => a.Id == id);
            return (ordem.VeiculoId, agendamento.Status);
        });
        Assert.Equal((veiculo, StatusAgendamento.Atendido), ligada);
    }

    [Fact]
    public async Task Check_in_de_agendamento_cancelado_ou_inexistente_e_barrado()
    {
        await fabrica.CriarLavadorAsync();
        var servicoId = await ServicoAsync();
        var usuario = await fabrica.CriarClienteComLoginAsync();
        using var comoCliente = await fabrica.AutenticadoAsync(usuario.Email!);
        using var comoDono = await fabrica.AutenticadoAsync((await fabrica.CriarDonoAsync()).Email!);
        var veiculo = await VeiculoDoClienteAsync(usuario.Id);
        var id = (await (await comoCliente.PostAsJsonAsync(Url(), Pedido(veiculo, servicoId, As(DiaAberto(8), 9)))).LerJsonAsync()).GetProperty("id").GetInt32();
        await comoCliente.PatchAsync(Url($"/{id}/cancelar"), null);

        var cancelado = await comoDono.PostAsJsonAsync(Url($"/{id}/check-in"), Vistoria());
        var inexistente = await comoDono.PostAsJsonAsync(Url("/999999/check-in"), Vistoria());

        Assert.Equal(HttpStatusCode.Conflict, cancelado.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, inexistente.StatusCode);
    }
}
