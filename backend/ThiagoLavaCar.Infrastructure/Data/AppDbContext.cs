using Microsoft.EntityFrameworkCore;
using ThiagoLavaCar.Domain.Entities;

namespace ThiagoLavaCar.Infrastructure.Data;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<Cliente> Clientes => Set<Cliente>();
    public DbSet<ModeloVeiculo> ModelosVeiculo => Set<ModeloVeiculo>();
    public DbSet<Veiculo> Veiculos => Set<Veiculo>();
    public DbSet<Agendamento> Agendamentos => Set<Agendamento>();
    public DbSet<Servico> Servicos => Set<Servico>();
    public DbSet<ServicoPreco> ServicosPrecos => Set<ServicoPreco>();
    public DbSet<OrdemServico> OrdensServico => Set<OrdemServico>();
    public DbSet<ItemOrdemServico> ItensOrdemServico => Set<ItemOrdemServico>();
    public DbSet<Avaria> Avarias => Set<Avaria>();
    public DbSet<TaxaPagamento> TaxasPagamento => Set<TaxaPagamento>();
    public DbSet<Pagamento> Pagamentos => Set<Pagamento>();
    public DbSet<FechamentoCaixa> FechamentosCaixa => Set<FechamentoCaixa>();
    public DbSet<FechamentoForma> FechamentosForma => Set<FechamentoForma>();
    public DbSet<ModeloMensagem> ModelosMensagem => Set<ModeloMensagem>();
    public DbSet<MensagemEnviada> MensagensEnviadas => Set<MensagemEnviada>();
    public DbSet<Configuracao> Configuracoes => Set<Configuracao>();
    public DbSet<LogAuditoria> LogsAuditoria => Set<LogAuditoria>();
    public DbSet<Despesa> Despesas => Set<Despesa>();
    public DbSet<RetiradaDono> RetiradasDono => Set<RetiradaDono>();
    public DbSet<Diaria> Diarias => Set<Diaria>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
}
