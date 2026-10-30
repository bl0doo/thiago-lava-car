using Microsoft.EntityFrameworkCore;
using Npgsql;
using ThiagoLavaCar.Domain.Entities;
using ThiagoLavaCar.Domain.Enums;

namespace ThiagoLavaCar.IntegrationTests;

/// <summary>Esquema físico da Entrega 2 (seção 2): tabelas, RLS, chaves únicas e CHECKs das tabelas novas.</summary>
[Collection(ApiCompartilhada.Nome)]
public sealed class EsquemaDoBancoTests(ApiFactory fabrica)
{
    [Fact]
    public async Task Toda_tabela_do_modelo_existe_com_row_level_security_ligado()
    {
        var tabelas = await fabrica.NoBancoAsync(async (db, _) =>
            await db.Database.SqlQueryRaw<string>(
                "SELECT relname AS \"Value\" FROM pg_class WHERE relkind = 'r' AND relrowsecurity " +
                "AND relnamespace = 'public'::regnamespace").ToListAsync());

        foreach (var esperada in new[] { "modelos_veiculo", "agendamentos", "ordens_servico", "veiculos", "despesas", "retiradas_dono", "diarias", "fechamentos_caixa", "fechamentos_forma", "modelos_mensagem", "mensagens_enviadas", "configuracoes", "logs_auditoria" })
        {
            Assert.Contains(esperada, tabelas);
        }
    }

    [Fact]
    public async Task Duas_ordens_nao_podem_vir_do_mesmo_agendamento()
    {
        var dono = await fabrica.CriarDonoAsync();
        var osId = await fabrica.CriarOrdemAguardandoAsync(dono.Id);

        var erro = await Assert.ThrowsAsync<PostgresException>(() => fabrica.NoBancoAsync(async (db, _) =>
        {
            var os = await db.OrdensServico.Include(o => o.Veiculo).SingleAsync(o => o.Id == osId);
            var servico = await db.Servicos.Include(s => s.Precos).FirstAsync(s => s.Precos.Any(p => p.TipoVeiculo == os.Veiculo!.Tipo));
            var agendamento = Agendamento.Criar(
                os.ClienteId, os.VeiculoId, servico, os.Veiculo!.Tipo, false, fabrica.Relogio.GetUtcNow().AddDays(1), fabrica.Relogio.GetUtcNow());
            db.Agendamentos.Add(agendamento);
            await db.SaveChangesAsync();
            await db.Database.ExecuteSqlRawAsync("UPDATE ordens_servico SET agendamento_id = {0} WHERE id = {1}", agendamento.Id, osId);
            var outraId = await fabrica.CriarOrdemAguardandoAsync(dono.Id);
            return await db.Database.ExecuteSqlRawAsync("UPDATE ordens_servico SET agendamento_id = {0} WHERE id = {1}", agendamento.Id, outraId);
        }));

        Assert.Equal("ix_ordens_servico_agendamento_id", erro.ConstraintName);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    public async Task Avaliacao_fora_de_1_a_5_e_barrada_pelo_CHECK(int nota)
    {
        var dono = await fabrica.CriarDonoAsync();
        var osId = await fabrica.CriarOrdemAguardandoAsync(dono.Id);

        var erro = await Assert.ThrowsAsync<PostgresException>(() => fabrica.NoBancoAsync(async (db, _) =>
            await db.Database.ExecuteSqlRawAsync("UPDATE ordens_servico SET avaliacao = {0} WHERE id = {1}", (short)nota, osId)));

        Assert.Equal("ck_ordens_servico_avaliacao", erro.ConstraintName);
    }

    [Fact]
    public async Task Modelo_de_veiculo_repetido_e_barrado_pelo_indice_unico()
    {
        var marca = $"Marca{Guid.NewGuid():N}"[..20];

        await Assert.ThrowsAsync<DbUpdateException>(() => fabrica.NoBancoAsync(async (db, _) =>
        {
            db.ModelosVeiculo.AddRange(
                ModeloVeiculo.Criar(marca, "X", TipoVeiculo.Carro),
                ModeloVeiculo.Criar(marca, "X", TipoVeiculo.Carro));
            return await db.SaveChangesAsync();
        }));
    }

    [Fact] // regra 1: uma diária por lavador por dia
    public async Task Diaria_repetida_para_o_mesmo_lavador_e_dia_e_barrada()
    {
        var lavador = await fabrica.CriarLavadorAsync($"Lav{Guid.NewGuid():N}"[..12]);
        var dia = new DateOnly(2026, 10, 20);

        var erro = await Assert.ThrowsAsync<DbUpdateException>(() => fabrica.NoBancoAsync(async (db, _) =>
        {
            db.Diarias.AddRange(Diaria.Criar(lavador.Id, dia, 100m), Diaria.Criar(lavador.Id, dia, 120m));
            return await db.SaveChangesAsync();
        }));

        Assert.Contains("ix_diarias_lavador_id_data", erro.InnerException?.Message, StringComparison.Ordinal);
    }

    [Fact] // regra 10: usuário com histórico não pode ser apagado
    public async Task Usuario_com_despesa_nao_pode_ser_excluido()
    {
        var dono = await fabrica.CriarDonoAsync();
        await fabrica.NoBancoAsync(async (db, _) =>
        {
            db.Despesas.Add(Despesa.Criar(dono.Id, "Sabão", CategoriaDespesa.Insumos, 30m, new DateOnly(2026, 10, 20), new DateOnly(2026, 10, 20), fabrica.Relogio.GetUtcNow()));
            return await db.SaveChangesAsync();
        });

        var erro = await Assert.ThrowsAsync<PostgresException>(() => fabrica.NoBancoAsync(async (db, _) =>
            await db.Database.ExecuteSqlRawAsync("DELETE FROM usuarios WHERE id = {0}", dono.Id)));

        Assert.Equal(PostgresErrorCodes.ForeignKeyViolation, erro.SqlState);
    }

    [Fact]
    public async Task Despesa_com_categoria_fora_da_lista_e_barrada_pelo_CHECK()
    {
        var dono = await fabrica.CriarDonoAsync();

        var erro = await Assert.ThrowsAsync<PostgresException>(() => fabrica.NoBancoAsync(async (db, _) =>
            await db.Database.ExecuteSqlRawAsync(
                "INSERT INTO despesas (registrado_por_id, descricao, categoria, valor, data, criado_em) VALUES ({0}, 'x', 'Viagem', 10, '2026-10-20', now())", dono.Id)));

        Assert.Equal("ck_despesas_categoria", erro.ConstraintName);
    }

    [Fact] // T41: só um fechamento por data
    public async Task Segundo_fechamento_da_mesma_data_e_barrado_e_as_formas_vao_junto_do_fechamento()
    {
        var dono = await fabrica.CriarDonoAsync();
        var data = new DateOnly(2030, 1, 1).AddDays(Random.Shared.Next(0, 3000));
        FechamentoCaixa Novo() => FechamentoCaixa.Fechar(
            data, data, dono.Id, [new(FormaPagamento.Pix, 100m, 100m), new(FormaPagamento.Dinheiro, 50m, 50m)], 150m, null, fabrica.Relogio.GetUtcNow());

        var id = await fabrica.NoBancoAsync(async (db, _) =>
        {
            var f = Novo();
            db.FechamentosCaixa.Add(f);
            await db.SaveChangesAsync();
            return f.Id;
        });
        var formas = await fabrica.NoBancoAsync(async (db, _) => await db.FechamentosForma.CountAsync(x => x.FechamentoId == id));
        Assert.Equal(2, formas);

        var erro = await Assert.ThrowsAsync<DbUpdateException>(() => fabrica.NoBancoAsync(async (db, _) =>
        {
            db.FechamentosCaixa.Add(Novo());
            return await db.SaveChangesAsync();
        }));
        Assert.Contains("ix_fechamentos_caixa_data", erro.InnerException?.Message, StringComparison.Ordinal);

        // regra 10: as linhas de fechamento somem junto com o fechamento (CASCADE)
        await fabrica.NoBancoAsync(async (db, _) => await db.Database.ExecuteSqlRawAsync("DELETE FROM fechamentos_caixa WHERE id = {0}", id));
        Assert.Equal(0, await fabrica.NoBancoAsync(async (db, _) => await db.FechamentosForma.CountAsync(x => x.FechamentoId == id)));
    }

    [Fact]
    public async Task Diferenca_sem_justificativa_e_barrada_pelo_CHECK_do_banco()
    {
        var dono = await fabrica.CriarDonoAsync();

        var erro = await Assert.ThrowsAsync<PostgresException>(() => fabrica.NoBancoAsync(async (db, _) =>
            await db.Database.ExecuteSqlRawAsync(
                "INSERT INTO fechamentos_caixa (data, total_bruto, total_liquido, total_conferido, diferenca, fechado_por_id, fechado_em) " +
                "VALUES ('2099-01-01', 100, 100, 90, -10, {0}, now())", dono.Id)));

        Assert.Equal("ck_fechamentos_caixa_justificativa", erro.ConstraintName);
    }

    [Fact] // regra 10: mensagens somem com a OS (CASCADE), mas o usuário que enviou não pode ser apagado
    public async Task Mensagens_de_uma_OS_acompanham_a_exclusao_da_OS()
    {
        var dono = await fabrica.CriarDonoAsync();
        var osId = await fabrica.CriarOrdemAguardandoAsync(dono.Id);
        await fabrica.NoBancoAsync(async (db, _) =>
        {
            db.MensagensEnviadas.Add(MensagemEnviada.Registrar(osId, dono.Id, TipoMensagem.LavagemIniciada, "Olá!", fabrica.Relogio.GetUtcNow()));
            return await db.SaveChangesAsync();
        });

        await fabrica.NoBancoAsync(async (db, _) => await db.Database.ExecuteSqlRawAsync("DELETE FROM ordens_servico WHERE id = {0}", osId));

        Assert.Equal(0, await fabrica.NoBancoAsync(async (db, _) => await db.MensagensEnviadas.CountAsync(m => m.OrdemServicoId == osId)));
    }

    [Fact]
    public async Task Tipo_de_mensagem_fora_da_lista_e_barrado_pelo_CHECK()
    {
        var erro = await Assert.ThrowsAsync<PostgresException>(() => fabrica.NoBancoAsync(async (db, _) =>
            await db.Database.ExecuteSqlRawAsync("INSERT INTO modelos_mensagem (tipo, texto, atualizado_em) VALUES ('Promocao', 'x', now())")));

        Assert.Equal("ck_modelos_mensagem_tipo", erro.ConstraintName);
    }

    [Fact] // bug encontrado no seed: "LavagemConcluida" (16 letras) não cabia em varchar(15)
    public async Task Os_dois_tipos_de_mensagem_cabem_na_coluna()
    {
        var dono = await fabrica.CriarDonoAsync();
        var osId = await fabrica.CriarOrdemAguardandoAsync(dono.Id);

        var gravadas = await fabrica.NoBancoAsync(async (db, _) =>
        {
            foreach (var tipo in Enum.GetValues<TipoMensagem>())
            {
                db.MensagensEnviadas.Add(MensagemEnviada.Registrar(osId, dono.Id, tipo, "Olá!", fabrica.Relogio.GetUtcNow()));
            }

            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();
            return await db.MensagensEnviadas.Where(m => m.OrdemServicoId == osId).Select(m => m.Tipo).ToListAsync();
        });

        Assert.Equal(Enum.GetValues<TipoMensagem>().Length, gravadas.Count);
    }

    [Fact] // configuracoes é uma linha única
    public async Task Configuracoes_aceita_so_a_linha_de_id_1()
    {
        var erro = await Assert.ThrowsAsync<PostgresException>(() => fabrica.NoBancoAsync(async (db, _) =>
            await db.Database.ExecuteSqlRawAsync(
                "INSERT INTO configuracoes (id, nome_lava_jato, abertura, fechamento, dias_funcionamento, diaria_semana, diaria_sabado, intervalo_agenda_min) " +
                "VALUES (2, 'Outro', '08:00', '18:00', '1,2', 100, 120, 30)")));

        Assert.Equal("ck_configuracoes_linha_unica", erro.ConstraintName);
    }

    [Fact]
    public async Task Auditoria_guarda_detalhes_em_jsonb_e_pode_ser_consultada_por_entidade()
    {
        var dono = await fabrica.CriarDonoAsync();
        var entidadeId = Random.Shared.Next(1000, 900000);
        await fabrica.NoBancoAsync(async (db, _) =>
        {
            db.LogsAuditoria.Add(LogAuditoria.Registrar(
                dono.Id, "ajuste-caixa", "fechamentos_caixa", entidadeId, "{\"antes\":80.00,\"depois\":70.00}", fabrica.Relogio.GetUtcNow()));
            return await db.SaveChangesAsync();
        });

        var antes = await fabrica.NoBancoAsync(async (db, _) =>
            await db.Database.SqlQueryRaw<decimal>(
                "SELECT (detalhes->>'antes')::numeric AS \"Value\" FROM logs_auditoria WHERE entidade = 'fechamentos_caixa' AND entidade_id = {0}", entidadeId).SingleAsync());

        Assert.Equal(80m, antes);
    }

    [Fact] // um veículo só pode ter uma OS aberta; depois de concluída, outra pode entrar
    public async Task Veiculo_nao_pode_ter_duas_ordens_abertas_mas_pode_ter_outra_depois_de_concluir()
    {
        var dono = await fabrica.CriarDonoAsync();
        var osId = await fabrica.CriarOrdemAguardandoAsync(dono.Id);

        Task<int> NovaOrdemDoMesmoVeiculo() => fabrica.NoBancoAsync(async (db, _) =>
        {
            var primeira = await db.OrdensServico.Include(o => o.Veiculo).Include(o => o.Itens).SingleAsync(o => o.Id == osId);
            var segunda = OrdemServico.Registrar(
                primeira.Veiculo!, dono.Id, primeira.Itens.Select(i => i).ToList(), true, [], true, null, null, fabrica.Relogio.GetUtcNow());
            db.OrdensServico.Add(segunda);
            return await db.SaveChangesAsync();
        });

        var erro = await Assert.ThrowsAsync<DbUpdateException>(NovaOrdemDoMesmoVeiculo);
        Assert.Contains("ux_ordens_servico_veiculo_aberta", erro.InnerException?.Message, StringComparison.Ordinal);

        await fabrica.NoBancoAsync(async (db, _) =>
            await db.Database.ExecuteSqlRawAsync(
                "UPDATE ordens_servico SET status = 'Concluido', inicio_em = now() - interval '2 hours', fim_em = now() WHERE id = {0}", osId));

        Assert.True(await NovaOrdemDoMesmoVeiculo() > 0);
    }

    [Fact] // a migration DadosDeReferencia deixa o ambiente funcional mesmo sem seed
    public async Task Migrations_criam_as_4_taxas_a_configuracao_e_os_2_modelos_de_mensagem()
    {
        var (formas, config, tipos) = await fabrica.NoBancoAsync(async (db, _) => (
            await db.TaxasPagamento.Select(t => t.FormaPagamento).ToListAsync(),
            await db.Configuracoes.SingleAsync(),
            await db.ModelosMensagem.Select(m => m.Tipo).ToListAsync()));

        Assert.Equal(Enum.GetValues<FormaPagamento>().OrderBy(f => f), formas.OrderBy(f => f));
        Assert.Equal(Configuracao.IdUnico, config.Id);
        Assert.Equal(new TimeOnly(8, 0), config.Abertura);
        Assert.Equal(Enum.GetValues<TipoMensagem>().OrderBy(t => t), tipos.OrderBy(t => t));
    }
}
