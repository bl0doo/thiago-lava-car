using System.Net;
using Microsoft.EntityFrameworkCore;
using ThiagoLavaCar.Domain.Entities;
using ThiagoLavaCar.Domain.Enums;

namespace ThiagoLavaCar.IntegrationTests;

/// <summary>Restrições do banco (Entrega 2, seção 2) e endpoints de infraestrutura.</summary>
[Collection(ApiCompartilhada.Nome)]
public sealed class BancoEInfraTests(ApiFactory fabrica)
{
    [Fact]
    public async Task Health_responde_200_com_o_banco_acessivel()
    {
        using var http = fabrica.CreateClient();

        Assert.Equal(HttpStatusCode.OK, (await http.GetAsync("/health")).StatusCode);
    }

    [Fact]
    public async Task Swagger_publica_o_contrato_com_os_endpoints_e_o_esquema_Bearer()
    {
        using var http = fabrica.CreateClient();

        var resposta = await http.GetAsync("/swagger/v1/swagger.json");

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        var doc = await resposta.LerJsonAsync();
        var caminhos = doc.GetProperty("paths");
        Assert.True(caminhos.TryGetProperty("/api/v1/auth/login", out _));
        Assert.True(caminhos.TryGetProperty("/api/v1/ordens-servico/quadro", out _));
        Assert.True(caminhos.TryGetProperty("/api/v1/ordens-servico/{id}/iniciar", out _));
        Assert.True(doc.GetProperty("components").GetProperty("securitySchemes").TryGetProperty("Bearer", out _));
    }

    [Fact] // T18
    public async Task Placa_repetida_e_barrada_pelo_indice_unico()
    {
        var placa = ApiFactory.NovaPlaca();

        var erro = await Assert.ThrowsAsync<DbUpdateException>(() => fabrica.NoBancoAsync(async (db, _) =>
        {
            var cliente = Cliente.Criar("Renata", ApiFactory.NovoTelefone(), null, CanalContato.WhatsApp, true, fabrica.Relogio.GetUtcNow());
            db.Clientes.Add(cliente);
            await db.SaveChangesAsync();
            db.Veiculos.AddRange(
                Veiculo.Criar(cliente.Id, placa, "Onix", TipoVeiculo.Carro, null),
                Veiculo.Criar(cliente.Id, placa, "Onix", TipoVeiculo.Carro, null));
            return await db.SaveChangesAsync();
        }));

        Assert.Contains("ix_veiculos_placa", erro.InnerException?.Message, StringComparison.Ordinal);
    }

    [Fact] // T15
    public async Task Telefone_repetido_e_barrado_pelo_indice_unico()
    {
        var telefone = ApiFactory.NovoTelefone();

        await Assert.ThrowsAsync<DbUpdateException>(() => fabrica.NoBancoAsync(async (db, _) =>
        {
            db.Clientes.AddRange(
                Cliente.Criar("A", telefone, null, CanalContato.WhatsApp, true, fabrica.Relogio.GetUtcNow()),
                Cliente.Criar("B", telefone, null, CanalContato.WhatsApp, true, fabrica.Relogio.GetUtcNow()));
            return await db.SaveChangesAsync();
        }));
    }

    [Fact]
    public async Task Check_do_banco_recusa_placa_fora_do_padrao_mesmo_sem_passar_pelo_dominio()
    {
        var erro = await Assert.ThrowsAsync<Npgsql.PostgresException>(() => fabrica.NoBancoAsync(async (db, _) =>
        {
            var cliente = Cliente.Criar("Renata", ApiFactory.NovoTelefone(), null, CanalContato.WhatsApp, true, fabrica.Relogio.GetUtcNow());
            db.Clientes.Add(cliente);
            await db.SaveChangesAsync();
            return await db.Database.ExecuteSqlRawAsync(
                "INSERT INTO veiculos (cliente_id, placa, modelo_descricao, tipo, ativo) VALUES ({0}, 'AB12345', 'X', 'Carro', true)", cliente.Id);
        }));

        Assert.Equal("ck_veiculos_placa", erro.ConstraintName);
    }

    [Fact]
    public async Task Row_level_security_esta_ligado_em_todas_as_tabelas_da_aplicacao()
    {
        var semRls = await fabrica.NoBancoAsync(async (db, _) =>
            await db.Database.SqlQueryRaw<string>(
                "SELECT c.relname AS \"Value\" FROM pg_class c JOIN pg_namespace n ON n.oid = c.relnamespace " +
                "WHERE n.nspname = 'public' AND c.relkind = 'r' AND NOT c.relrowsecurity AND c.relname <> '__EFMigrationsHistory'")
                .ToListAsync());

        Assert.Empty(semRls);
    }

    [Fact]
    public async Task Dinheiro_e_gravado_com_duas_casas_e_taxa_copiada_no_pagamento()
    {
        var dono = await fabrica.CriarDonoAsync();
        var osId = await fabrica.CriarOrdemAguardandoAsync(dono.Id);

        var gravado = await fabrica.NoBancoAsync(async (db, _) =>
        {
            var taxa = TaxaPagamento.Criar(FormaPagamento.Credito, 3.49m, 0m);
            db.TaxasPagamento.RemoveRange(db.TaxasPagamento.Where(t => t.FormaPagamento == FormaPagamento.Credito));
            db.TaxasPagamento.Add(taxa);
            await db.SaveChangesAsync();
            var pagamento = Pagamento.Criar(osId, dono.Id, 100m, taxa, new DateOnly(2026, 10, 20), fabrica.Relogio.GetUtcNow());
            db.Pagamentos.Add(pagamento);
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();
            return await db.Pagamentos.SingleAsync(p => p.Id == pagamento.Id);
        });

        Assert.Equal(3.49m, gravado.ValorTaxa);
        Assert.Equal(96.51m, gravado.ValorLiquido);
        Assert.Equal(new DateOnly(2026, 10, 20), gravado.DataCaixa);
    }
}
