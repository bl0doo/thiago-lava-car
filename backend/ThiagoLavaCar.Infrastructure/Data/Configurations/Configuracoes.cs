using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ThiagoLavaCar.Domain.Common;
using ThiagoLavaCar.Domain.Entities;
using ThiagoLavaCar.Domain.Enums;

namespace ThiagoLavaCar.Infrastructure.Data.Configurations;

// Nomes de colunas em snake_case vêm da convenção (UseSnakeCaseNamingConvention); aqui entram só tabelas,
// chaves, índices e as restrições CHECK da Entrega 2 (seção 2, regras de integridade).

internal static class Sql
{
    /// <summary>Lista de valores de um enum para o CHECK ... IN (...).</summary>
    public static string Valores<TEnum>() where TEnum : struct, Enum =>
        string.Join(", ", Enum.GetNames<TEnum>().Select(n => $"'{n}'"));
}

internal sealed class UsuarioConfiguration : IEntityTypeConfiguration<Usuario>
{
    public void Configure(EntityTypeBuilder<Usuario> b)
    {
        b.ToTable("usuarios", t =>
        {
            t.HasCheckConstraint("ck_usuarios_perfil", $"perfil IN ({Sql.Valores<Perfil>()})");
            t.HasCheckConstraint(
                "ck_usuarios_credenciais",
                "(perfil = 'Lavador' AND pin_hash IS NOT NULL) " +
                "OR (perfil IN ('Dono', 'Cliente') AND email IS NOT NULL AND senha_hash IS NOT NULL)");
        });
        b.Property(x => x.Nome).HasMaxLength(100).IsRequired();
        b.Property(x => x.Apelido).HasMaxLength(30);
        b.Property(x => x.Email).HasMaxLength(254);
        b.Property(x => x.Perfil).HasConversion<string>().HasMaxLength(10);
        b.Property(x => x.SenhaHash).HasMaxLength(200);
        b.Property(x => x.PinHash).HasMaxLength(200);
        b.HasIndex(x => x.Email).IsUnique();
    }
}

internal sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> b)
    {
        b.ToTable("refresh_tokens");
        b.Property(x => x.TokenHash).HasMaxLength(64).IsRequired();
        b.Ignore(x => x.FoiRevogado);
        b.HasIndex(x => x.TokenHash).IsUnique();
        b.HasOne<Usuario>().WithMany().HasForeignKey(x => x.UsuarioId).OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class ClienteConfiguration : IEntityTypeConfiguration<Cliente>
{
    public void Configure(EntityTypeBuilder<Cliente> b)
    {
        b.ToTable("clientes", t => t.HasCheckConstraint("ck_clientes_canal", $"canal_preferido IN ({Sql.Valores<CanalContato>()})"));
        b.Property(x => x.Nome).HasMaxLength(100).IsRequired();
        b.Property(x => x.Telefone).HasMaxLength(11).IsRequired();
        b.Property(x => x.Email).HasMaxLength(254);
        b.Property(x => x.CanalPreferido).HasConversion<string>().HasMaxLength(10);
        b.HasIndex(x => x.Telefone).IsUnique();
        b.HasIndex(x => x.UsuarioId).IsUnique();
        b.HasOne<Usuario>().WithMany().HasForeignKey(x => x.UsuarioId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class ModeloVeiculoConfiguration : IEntityTypeConfiguration<ModeloVeiculo>
{
    public void Configure(EntityTypeBuilder<ModeloVeiculo> b)
    {
        b.ToTable("modelos_veiculo", t => t.HasCheckConstraint("ck_modelos_veiculo_tipo", $"tipo_veiculo IN ({Sql.Valores<TipoVeiculo>()})"));
        b.Property(x => x.Marca).HasMaxLength(50).IsRequired();
        b.Property(x => x.Nome).HasMaxLength(80).IsRequired();
        b.Property(x => x.TipoVeiculo).HasConversion<string>().HasMaxLength(12);
        b.Ignore(x => x.Descricao);
        b.HasIndex(x => new { x.Marca, x.Nome }).IsUnique();
    }
}

internal sealed class VeiculoConfiguration : IEntityTypeConfiguration<Veiculo>
{
    public void Configure(EntityTypeBuilder<Veiculo> b)
    {
        b.ToTable("veiculos", t =>
        {
            t.HasCheckConstraint("ck_veiculos_placa", $"placa ~ '{PlacaBrasileira.Padrao}'");
            t.HasCheckConstraint("ck_veiculos_tipo", $"tipo IN ({Sql.Valores<TipoVeiculo>()})");
        });
        b.Property(x => x.Placa).HasMaxLength(7).IsRequired();
        b.Property(x => x.ModeloDescricao).HasMaxLength(100).IsRequired();
        b.Property(x => x.Tipo).HasConversion<string>().HasMaxLength(12);
        b.Property(x => x.Cor).HasMaxLength(30);
        b.HasIndex(x => x.Placa).IsUnique();
        b.HasIndex(x => x.ClienteId);
        b.Property(x => x.CriadoEm).HasDefaultValueSql("now()");
        b.HasOne(x => x.Cliente).WithMany().HasForeignKey(x => x.ClienteId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<ModeloVeiculo>().WithMany().HasForeignKey(x => x.ModeloVeiculoId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class ServicoConfiguration : IEntityTypeConfiguration<Servico>
{
    public void Configure(EntityTypeBuilder<Servico> b)
    {
        b.ToTable("servicos", t =>
        {
            t.HasCheckConstraint("ck_servicos_tempo", "tempo_estimado_min > 0");
            t.HasCheckConstraint("ck_servicos_custo", "custo_insumos >= 0");
        });
        b.Property(x => x.Nome).HasMaxLength(100).IsRequired();
        b.Property(x => x.Categoria).HasMaxLength(50).IsRequired();
        b.Property(x => x.CustoInsumos).HasPrecision(10, 2);
        b.HasIndex(x => x.Nome).IsUnique();
        b.HasMany(x => x.Precos).WithOne().HasForeignKey(p => p.ServicoId).OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class ServicoPrecoConfiguration : IEntityTypeConfiguration<ServicoPreco>
{
    public void Configure(EntityTypeBuilder<ServicoPreco> b)
    {
        b.ToTable("servicos_precos", t =>
        {
            t.HasCheckConstraint("ck_servicos_precos_preco", "preco >= 0 AND (preco_com_cera IS NULL OR preco_com_cera >= 0)");
            t.HasCheckConstraint("ck_servicos_precos_tipo", $"tipo_veiculo IN ({Sql.Valores<TipoVeiculo>()})");
        });
        b.Property(x => x.TipoVeiculo).HasConversion<string>().HasMaxLength(12);
        b.Property(x => x.Preco).HasPrecision(10, 2);
        b.Property(x => x.PrecoComCera).HasPrecision(10, 2);
        b.HasIndex(x => new { x.ServicoId, x.TipoVeiculo }).IsUnique();
    }
}

internal sealed class AgendamentoConfiguration : IEntityTypeConfiguration<Agendamento>
{
    public void Configure(EntityTypeBuilder<Agendamento> b)
    {
        b.ToTable("agendamentos", t =>
        {
            t.HasCheckConstraint("ck_agendamentos_status", $"status IN ({Sql.Valores<StatusAgendamento>()})");
            t.HasCheckConstraint("ck_agendamentos_duracao", "duracao_estimada_min > 0");
            t.HasCheckConstraint("ck_agendamentos_preco", "preco_estimado >= 0");
        });
        b.Property(x => x.Status).HasConversion<string>().HasMaxLength(15);
        b.Property(x => x.PrecoEstimado).HasPrecision(10, 2);
        b.HasIndex(x => x.Inicio);
        b.HasOne<Cliente>().WithMany().HasForeignKey(x => x.ClienteId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Veiculo>().WithMany().HasForeignKey(x => x.VeiculoId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Servico>().WithMany().HasForeignKey(x => x.ServicoId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class OrdemServicoConfiguration : IEntityTypeConfiguration<OrdemServico>
{
    public void Configure(EntityTypeBuilder<OrdemServico> b)
    {
        b.ToTable("ordens_servico", t =>
        {
            t.HasCheckConstraint("ck_ordens_servico_status", $"status IN ({Sql.Valores<StatusOrdemServico>()})");
            t.HasCheckConstraint("ck_ordens_servico_valor", "valor_total >= 0");
            t.HasCheckConstraint("ck_ordens_servico_temporizador", "fim_em IS NULL OR (inicio_em IS NOT NULL AND fim_em > inicio_em)");
            t.HasCheckConstraint("ck_ordens_servico_aceite", "aceite_cliente = TRUE");
            t.HasCheckConstraint("ck_ordens_servico_avaliacao", "avaliacao IS NULL OR avaliacao BETWEEN 1 AND 5");
        });
        b.Property(x => x.Status).HasConversion<string>().HasMaxLength(12);
        b.Property(x => x.ValorTotal).HasPrecision(10, 2);
        b.Property(x => x.PertencesEntrada).HasMaxLength(500);
        b.Property(x => x.PertencesEncontrados).HasMaxLength(500);
        b.Property(x => x.NomeQuemRetira).HasMaxLength(100);

        // xmin do PostgreSQL como token de concorrência: dois lavadores tocando em Iniciar ao mesmo tempo
        // não sobrescrevem um ao outro (o segundo recebe 409).
        b.Property<uint>("xmin").IsRowVersion();

        b.Property(x => x.AceiteEm).HasDefaultValueSql("now()");
        b.HasIndex(x => new { x.Status, x.CriadoEm });
        // Um veículo não pode ter duas OS abertas ao mesmo tempo (a regra também é checada no Service, antes de gravar).
        b.HasIndex(x => x.VeiculoId).IsUnique().HasFilter("status IN ('Aguardando', 'EmAndamento')").HasDatabaseName("ux_ordens_servico_veiculo_aberta");
        b.HasIndex(x => x.AgendamentoId).IsUnique();
        b.HasOne<Agendamento>().WithMany().HasForeignKey(x => x.AgendamentoId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Veiculo).WithMany().HasForeignKey(x => x.VeiculoId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Cliente).WithMany().HasForeignKey(x => x.ClienteId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Lavador).WithMany().HasForeignKey(x => x.LavadorId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Usuario>().WithMany().HasForeignKey(x => x.CriadoPorId).OnDelete(DeleteBehavior.Restrict);
        b.HasMany(x => x.Itens).WithOne().HasForeignKey(i => i.OrdemServicoId).OnDelete(DeleteBehavior.Cascade);
        b.HasMany(x => x.Avarias).WithOne().HasForeignKey(a => a.OrdemServicoId).OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class ItemOrdemServicoConfiguration : IEntityTypeConfiguration<ItemOrdemServico>
{
    public void Configure(EntityTypeBuilder<ItemOrdemServico> b)
    {
        b.ToTable("itens_ordem_servico", t =>
        {
            t.HasCheckConstraint("ck_itens_preco", "preco_final >= 0 AND custo_insumos >= 0");
            t.HasCheckConstraint("ck_itens_tempo", "tempo_estimado_min > 0");
        });
        b.Property(x => x.NomeServico).HasMaxLength(100).IsRequired();
        b.Property(x => x.PrecoFinal).HasPrecision(10, 2);
        b.Property(x => x.CustoInsumos).HasPrecision(10, 2);
        b.HasOne<Servico>().WithMany().HasForeignKey(x => x.ServicoId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class AvariaConfiguration : IEntityTypeConfiguration<Avaria>
{
    public void Configure(EntityTypeBuilder<Avaria> b)
    {
        b.ToTable("avarias");
        b.Property(x => x.Local).HasMaxLength(100).IsRequired();
        b.Property(x => x.Tipo).HasMaxLength(50).IsRequired();
        b.Property(x => x.Descricao).HasMaxLength(500);
    }
}

internal sealed class TaxaPagamentoConfiguration : IEntityTypeConfiguration<TaxaPagamento>
{
    public void Configure(EntityTypeBuilder<TaxaPagamento> b)
    {
        b.ToTable("taxas_pagamento", t =>
        {
            t.HasCheckConstraint("ck_taxas_percentual", "percentual >= 0 AND percentual <= 100");
            t.HasCheckConstraint("ck_taxas_fixo", "valor_fixo >= 0");
            t.HasCheckConstraint("ck_taxas_forma", $"forma_pagamento IN ({Sql.Valores<FormaPagamento>()})");
        });
        b.Property(x => x.FormaPagamento).HasConversion<string>().HasMaxLength(10);
        b.Property(x => x.Percentual).HasPrecision(5, 2);
        b.Property(x => x.ValorFixo).HasPrecision(10, 2);
        b.HasIndex(x => x.FormaPagamento).IsUnique();
    }
}

internal sealed class PagamentoConfiguration : IEntityTypeConfiguration<Pagamento>
{
    public void Configure(EntityTypeBuilder<Pagamento> b)
    {
        b.ToTable("pagamentos", t =>
        {
            t.HasCheckConstraint("ck_pagamentos_valor", "valor_bruto > 0 AND valor_taxa >= 0 AND valor_liquido >= 0");
            t.HasCheckConstraint("ck_pagamentos_forma", $"forma_pagamento IN ({Sql.Valores<FormaPagamento>()})");
        });
        b.Property(x => x.FormaPagamento).HasConversion<string>().HasMaxLength(10);
        b.Property(x => x.ValorBruto).HasPrecision(10, 2);
        b.Property(x => x.PercentualTaxa).HasPrecision(5, 2);
        b.Property(x => x.ValorTaxa).HasPrecision(10, 2);
        b.Property(x => x.ValorLiquido).HasPrecision(10, 2);
        b.Ignore(x => x.Estornado);
        b.HasIndex(x => x.DataCaixa);
        b.HasOne<OrdemServico>().WithMany().HasForeignKey(x => x.OrdemServicoId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Usuario>().WithMany().HasForeignKey(x => x.RegistradoPorId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class DespesaConfiguration : IEntityTypeConfiguration<Despesa>
{
    public void Configure(EntityTypeBuilder<Despesa> b)
    {
        b.ToTable("despesas", t =>
        {
            t.HasCheckConstraint("ck_despesas_valor", "valor > 0");
            t.HasCheckConstraint("ck_despesas_categoria", $"categoria IN ({Sql.Valores<CategoriaDespesa>()})");
        });
        b.Property(x => x.Descricao).HasMaxLength(200).IsRequired();
        b.Property(x => x.Categoria).HasConversion<string>().HasMaxLength(12);
        b.Property(x => x.Valor).HasPrecision(10, 2);
        b.HasIndex(x => x.Data);
        b.HasOne<Usuario>().WithMany().HasForeignKey(x => x.RegistradoPorId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class RetiradaDonoConfiguration : IEntityTypeConfiguration<RetiradaDono>
{
    public void Configure(EntityTypeBuilder<RetiradaDono> b)
    {
        b.ToTable("retiradas_dono", t => t.HasCheckConstraint("ck_retiradas_dono_valor", "valor > 0"));
        b.Property(x => x.Valor).HasPrecision(10, 2);
        b.Property(x => x.Observacao).HasMaxLength(200);
        b.HasIndex(x => x.Data);
        b.HasOne<Usuario>().WithMany().HasForeignKey(x => x.RegistradoPorId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class DiariaConfiguration : IEntityTypeConfiguration<Diaria>
{
    public void Configure(EntityTypeBuilder<Diaria> b)
    {
        b.ToTable("diarias", t => t.HasCheckConstraint("ck_diarias_valor", "valor > 0"));
        b.Property(x => x.Valor).HasPrecision(10, 2);
        b.HasIndex(x => new { x.LavadorId, x.Data }).IsUnique();
        b.HasOne<Usuario>().WithMany().HasForeignKey(x => x.LavadorId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class FechamentoCaixaConfiguration : IEntityTypeConfiguration<FechamentoCaixa>
{
    public void Configure(EntityTypeBuilder<FechamentoCaixa> b)
    {
        b.ToTable("fechamentos_caixa", t =>
        {
            t.HasCheckConstraint("ck_fechamentos_caixa_totais", "total_bruto >= 0 AND total_liquido >= 0 AND total_conferido >= 0");
            t.HasCheckConstraint("ck_fechamentos_caixa_diferenca", "diferenca = total_conferido - total_bruto");
            t.HasCheckConstraint(
                "ck_fechamentos_caixa_justificativa",
                "diferenca = 0 OR (justificativa IS NOT NULL AND char_length(justificativa) BETWEEN 10 AND 500)");
        });
        b.Property(x => x.TotalBruto).HasPrecision(10, 2);
        b.Property(x => x.TotalLiquido).HasPrecision(10, 2);
        b.Property(x => x.TotalConferido).HasPrecision(10, 2);
        b.Property(x => x.Diferenca).HasPrecision(10, 2);
        b.Property(x => x.Justificativa).HasMaxLength(500);
        b.HasIndex(x => x.Data).IsUnique();
        b.HasOne<Usuario>().WithMany().HasForeignKey(x => x.FechadoPorId).OnDelete(DeleteBehavior.Restrict);
        b.HasMany(x => x.Formas).WithOne().HasForeignKey(f => f.FechamentoId).OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class FechamentoFormaConfiguration : IEntityTypeConfiguration<FechamentoForma>
{
    public void Configure(EntityTypeBuilder<FechamentoForma> b)
    {
        b.ToTable("fechamentos_forma", t =>
        {
            t.HasCheckConstraint("ck_fechamentos_forma_valores", "valor_calculado >= 0 AND valor_conferido >= 0");
            t.HasCheckConstraint("ck_fechamentos_forma_forma", $"forma_pagamento IN ({Sql.Valores<FormaPagamento>()})");
        });
        b.Property(x => x.FormaPagamento).HasConversion<string>().HasMaxLength(10);
        b.Property(x => x.ValorCalculado).HasPrecision(10, 2);
        b.Property(x => x.ValorConferido).HasPrecision(10, 2);
        b.Ignore(x => x.TemDiferenca);
        b.HasIndex(x => new { x.FechamentoId, x.FormaPagamento }).IsUnique();
    }
}

internal sealed class ModeloMensagemConfiguration : IEntityTypeConfiguration<ModeloMensagem>
{
    public void Configure(EntityTypeBuilder<ModeloMensagem> b)
    {
        b.ToTable("modelos_mensagem", t => t.HasCheckConstraint("ck_modelos_mensagem_tipo", $"tipo IN ({Sql.Valores<TipoMensagem>()})"));
        b.Property(x => x.Tipo).HasConversion<string>().HasMaxLength(20);
        b.Property(x => x.Texto).HasMaxLength(ModeloMensagem.TextoMax).IsRequired();
        b.HasIndex(x => x.Tipo).IsUnique();
    }
}

internal sealed class MensagemEnviadaConfiguration : IEntityTypeConfiguration<MensagemEnviada>
{
    public void Configure(EntityTypeBuilder<MensagemEnviada> b)
    {
        b.ToTable("mensagens_enviadas", t => t.HasCheckConstraint("ck_mensagens_enviadas_tipo", $"tipo IN ({Sql.Valores<TipoMensagem>()})"));
        b.Property(x => x.Tipo).HasConversion<string>().HasMaxLength(20);
        b.Property(x => x.Texto).HasMaxLength(1000).IsRequired();
        b.HasIndex(x => x.OrdemServicoId);
        b.HasOne<OrdemServico>().WithMany().HasForeignKey(x => x.OrdemServicoId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne<Usuario>().WithMany().HasForeignKey(x => x.EnviadaPorId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class ConfiguracaoConfiguration : IEntityTypeConfiguration<Configuracao>
{
    public void Configure(EntityTypeBuilder<Configuracao> b)
    {
        b.ToTable("configuracoes", t =>
        {
            t.HasCheckConstraint("ck_configuracoes_linha_unica", "id = 1");
            t.HasCheckConstraint("ck_configuracoes_horario", "fechamento > abertura");
            t.HasCheckConstraint("ck_configuracoes_dias", "dias_funcionamento ~ '^[0-6](,[0-6])*$'");
            t.HasCheckConstraint("ck_configuracoes_diarias", "diaria_semana > 0 AND diaria_sabado > 0");
            t.HasCheckConstraint("ck_configuracoes_intervalo", "intervalo_agenda_min BETWEEN 5 AND 240");
        });
        b.Property(x => x.Id).ValueGeneratedNever();
        b.Property(x => x.NomeLavaJato).HasMaxLength(100).IsRequired();
        b.Property(x => x.Telefone).HasMaxLength(11);
        b.Property(x => x.DiasFuncionamento).HasMaxLength(13).IsRequired();
        b.Property(x => x.DiariaSemana).HasPrecision(10, 2);
        b.Property(x => x.DiariaSabado).HasPrecision(10, 2);
    }
}

internal sealed class LogAuditoriaConfiguration : IEntityTypeConfiguration<LogAuditoria>
{
    public void Configure(EntityTypeBuilder<LogAuditoria> b)
    {
        b.ToTable("logs_auditoria");
        b.Property(x => x.Acao).HasMaxLength(50).IsRequired();
        b.Property(x => x.Entidade).HasMaxLength(50).IsRequired();
        b.Property(x => x.Detalhes).HasColumnType("jsonb");
        b.HasIndex(x => new { x.Entidade, x.EntidadeId });
        b.HasIndex(x => x.CriadoEm);
        b.HasOne<Usuario>().WithMany().HasForeignKey(x => x.UsuarioId).OnDelete(DeleteBehavior.Restrict);
    }
}
