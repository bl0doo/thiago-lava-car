using Microsoft.EntityFrameworkCore;
using ThiagoLavaCar.Application.Abstractions;
using ThiagoLavaCar.Application.Common;
using ThiagoLavaCar.Domain.Entities;
using ThiagoLavaCar.Domain.Enums;
using ThiagoLavaCar.Domain.Exceptions;
using ThiagoLavaCar.Infrastructure.Data;

namespace ThiagoLavaCar.Infrastructure.Repositories;

internal sealed class UsuarioRepository(AppDbContext db) : IUsuarioRepository
{
    public Task<Usuario?> ObterPorIdAsync(int id, CancellationToken ct) =>
        db.Usuarios.FirstOrDefaultAsync(u => u.Id == id, ct);

    public Task<Usuario?> ObterPorEmailAsync(string email, CancellationToken ct) =>
        db.Usuarios.FirstOrDefaultAsync(u => u.Email == email, ct);

    public async Task<IReadOnlyList<Usuario>> ListarLavadoresAtivosAsync(CancellationToken ct) =>
        await db.Usuarios.AsNoTracking()
            .Where(u => u.Perfil == Perfil.Lavador && u.Ativo)
            .OrderBy(u => u.Apelido)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<Usuario>> ListarAsync(Perfil? perfil, CancellationToken ct) =>
        await db.Usuarios.AsNoTracking().Where(u => perfil == null || u.Perfil == perfil).OrderBy(u => u.Nome).ToListAsync(ct);

    public async Task<bool> ApelidoDeLavadorExisteAsync(string apelido, CancellationToken ct) =>
        (await db.Usuarios.Where(u => u.Perfil == Perfil.Lavador && u.Apelido != null).Select(u => u.Apelido!).ToListAsync(ct))
            .Any(a => string.Equals(a, apelido, StringComparison.OrdinalIgnoreCase));

    public async Task AdicionarAsync(Usuario usuario, CancellationToken ct) =>
        await db.Usuarios.AddAsync(usuario, ct);
}

internal sealed class ModeloVeiculoRepository(AppDbContext db) : IModeloVeiculoRepository
{
    public Task<ModeloVeiculo?> ObterPorIdAsync(int id, CancellationToken ct) =>
        db.ModelosVeiculo.AsNoTracking().FirstOrDefaultAsync(m => m.Id == id, ct);

    public async Task<IReadOnlyList<ModeloVeiculo>> BuscarAsync(string? busca, int limite, CancellationToken ct)
    {
        var consulta = db.ModelosVeiculo.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(busca))
        {
            // % e _ do texto digitado valem como letras, não como curingas do LIKE.
            var padrao = "%" + busca.Trim().Replace("\\", "\\\\", StringComparison.Ordinal).Replace("%", "\\%", StringComparison.Ordinal).Replace("_", "\\_", StringComparison.Ordinal) + "%";
            consulta = consulta.Where(m => EF.Functions.ILike(m.Marca + " " + m.Nome, padrao));
        }

        return await consulta.OrderBy(m => m.Marca).ThenBy(m => m.Nome).Take(limite).ToListAsync(ct);
    }
}

internal sealed class ClienteRepository(AppDbContext db) : IClienteRepository
{
    public async Task<int?> ObterIdPorUsuarioAsync(int usuarioId, CancellationToken ct) =>
        await db.Clientes.Where(c => c.UsuarioId == usuarioId).Select(c => (int?)c.Id).FirstOrDefaultAsync(ct);

    public Task<Cliente?> ObterPorIdAsync(int id, CancellationToken ct) =>
        db.Clientes.FirstOrDefaultAsync(c => c.Id == id, ct);

    public Task<Cliente?> ObterPorUsuarioAsync(int usuarioId, CancellationToken ct) =>
        db.Clientes.FirstOrDefaultAsync(c => c.UsuarioId == usuarioId, ct);

    public Task<Cliente?> ObterPorTelefoneAsync(string telefone, CancellationToken ct) =>
        db.Clientes.AsNoTracking().FirstOrDefaultAsync(c => c.Telefone == telefone, ct);

    public async Task<(IReadOnlyList<Cliente> Itens, int Total)> BuscarAsync(string? busca, int pagina, int tamanho, CancellationToken ct)
    {
        var consulta = db.Clientes.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(busca))
        {
            var termo = busca.Trim();
            var placa = Domain.Common.PlacaBrasileira.Normalizar(termo);
            // Só busca por telefone quando o termo é numérico (aceita máscara); "Ana12" não deve casar com telefones.
            var digitos = termo.Any(char.IsLetter) ? string.Empty : new string(termo.Where(char.IsDigit).ToArray());
            consulta = consulta.Where(c =>
                EF.Functions.ILike(c.Nome, $"%{termo}%")
                || (digitos.Length > 0 && c.Telefone.Contains(digitos))
                || db.Veiculos.Any(v => v.ClienteId == c.Id && v.Placa.Contains(placa)));
        }

        var total = await consulta.CountAsync(ct);
        var itens = await consulta.OrderBy(c => c.Nome).ThenBy(c => c.Id)
            .Skip((pagina - 1) * tamanho).Take(tamanho).ToListAsync(ct);
        return (itens, total);
    }

    public async Task AdicionarAsync(Cliente cliente, CancellationToken ct) => await db.Clientes.AddAsync(cliente, ct);

    public async Task<IReadOnlyList<Veiculo>> ListarVeiculosAsync(int clienteId, CancellationToken ct) =>
        await db.Veiculos.AsNoTracking().Where(v => v.ClienteId == clienteId && v.Ativo).OrderBy(v => v.Placa).ToListAsync(ct);
}

internal sealed class VeiculoRepository(AppDbContext db) : IVeiculoRepository
{
    public Task<Veiculo?> ObterPorIdAsync(int id, CancellationToken ct) =>
        db.Veiculos.Include(v => v.Cliente).FirstOrDefaultAsync(v => v.Id == id, ct);

    public Task<Veiculo?> ObterPorPlacaAsync(string placaNormalizada, CancellationToken ct) =>
        db.Veiculos.FirstOrDefaultAsync(v => v.Placa == placaNormalizada, ct);

    public async Task AdicionarAsync(Veiculo veiculo, CancellationToken ct) => await db.Veiculos.AddAsync(veiculo, ct);

    public async Task<IReadOnlyList<OrdemServico>> ListarOrdensAsync(int veiculoId, CancellationToken ct) =>
        await db.OrdensServico.AsNoTracking().Include(o => o.Itens)
            .Where(o => o.VeiculoId == veiculoId).OrderByDescending(o => o.CriadoEm).ToListAsync(ct);
}

internal sealed class ServicoRepository(AppDbContext db) : IServicoRepository
{
    public Task<Servico?> ObterPorIdAsync(int id, CancellationToken ct) =>
        db.Servicos.Include(s => s.Precos).FirstOrDefaultAsync(s => s.Id == id, ct);

    public Task<Servico?> ObterPorNomeAsync(string nome, CancellationToken ct) =>
        db.Servicos.AsNoTracking().FirstOrDefaultAsync(s => s.Nome == nome, ct);

    public async Task<IReadOnlyList<Servico>> ListarAsync(bool somenteAtivos, CancellationToken ct) =>
        await db.Servicos.AsNoTracking().Include(s => s.Precos)
            .Where(s => !somenteAtivos || s.Ativo).OrderBy(s => s.Categoria).ThenBy(s => s.Nome).ToListAsync(ct);

    public async Task AdicionarAsync(Servico servico, CancellationToken ct) => await db.Servicos.AddAsync(servico, ct);
}

internal sealed class PagamentoRepository(AppDbContext db) : IPagamentoRepository
{
    public Task<Pagamento?> ObterPorIdAsync(int id, CancellationToken ct) =>
        db.Pagamentos.FirstOrDefaultAsync(p => p.Id == id, ct);

    public async Task<decimal> SomarPagoAsync(int ordemServicoId, CancellationToken ct) =>
        await db.Pagamentos.Where(p => p.OrdemServicoId == ordemServicoId && p.EstornadoEm == null)
            .SumAsync(p => (decimal?)p.ValorBruto, ct) ?? 0m;

    public async Task<IReadOnlyList<Pagamento>> ListarPorDataCaixaAsync(DateOnly data, CancellationToken ct) =>
        await db.Pagamentos.AsNoTracking().Where(p => p.DataCaixa == data).OrderBy(p => p.CriadoEm).ToListAsync(ct);

    public async Task<IReadOnlyList<Pagamento>> ListarPorOrdemAsync(int ordemServicoId, CancellationToken ct) =>
        await db.Pagamentos.AsNoTracking().Where(p => p.OrdemServicoId == ordemServicoId).OrderBy(p => p.CriadoEm).ToListAsync(ct);

    public async Task AdicionarAsync(Pagamento pagamento, CancellationToken ct) => await db.Pagamentos.AddAsync(pagamento, ct);
}

internal sealed class TaxaRepository(AppDbContext db) : ITaxaRepository
{
    public Task<TaxaPagamento?> ObterPorFormaAsync(FormaPagamento forma, CancellationToken ct) =>
        db.TaxasPagamento.FirstOrDefaultAsync(t => t.FormaPagamento == forma, ct);

    public async Task<IReadOnlyList<TaxaPagamento>> ListarAsync(CancellationToken ct) =>
        await db.TaxasPagamento.AsNoTracking().OrderBy(t => t.FormaPagamento).ToListAsync(ct);
}

internal sealed class CaixaRepository(AppDbContext db) : ICaixaRepository
{
    public Task<bool> ExisteFechamentoAsync(DateOnly data, CancellationToken ct) =>
        db.FechamentosCaixa.AnyAsync(f => f.Data == data, ct);

    public Task<FechamentoCaixa?> ObterFechamentoAsync(DateOnly data, CancellationToken ct) =>
        db.FechamentosCaixa.Include(f => f.Formas).FirstOrDefaultAsync(f => f.Data == data, ct);

    public async Task<IReadOnlyList<FechamentoCaixa>> ListarFechamentosAsync(DateOnly primeiroDia, DateOnly ultimoDia, CancellationToken ct) =>
        await db.FechamentosCaixa.AsNoTracking().Include(f => f.Formas)
            .Where(f => f.Data >= primeiroDia && f.Data <= ultimoDia).OrderBy(f => f.Data).ToListAsync(ct);

    public async Task AdicionarFechamentoAsync(FechamentoCaixa fechamento, CancellationToken ct) =>
        await db.FechamentosCaixa.AddAsync(fechamento, ct);

    public void RemoverFechamento(FechamentoCaixa fechamento) => db.FechamentosCaixa.Remove(fechamento);

    public async Task<decimal> SomarDespesasAsync(DateOnly data, CancellationToken ct) =>
        await db.Despesas.Where(d => d.Data == data).SumAsync(d => (decimal?)d.Valor, ct) ?? 0m;

    public async Task<decimal> SomarDiariasAsync(DateOnly data, CancellationToken ct) =>
        await db.Diarias.Where(d => d.Data == data).SumAsync(d => (decimal?)d.Valor, ct) ?? 0m;

    public Task<int> ContarOrdensSemPagamentoCompletoAsync(DateTimeOffset inicioDoDia, DateTimeOffset fimDoDia, CancellationToken ct) =>
        db.OrdensServico.CountAsync(o =>
            (o.Status == StatusOrdemServico.Concluido || o.Status == StatusOrdemServico.Entregue)
            && o.FimEm >= inicioDoDia && o.FimEm < fimDoDia
            && (db.Pagamentos.Where(p => p.OrdemServicoId == o.Id && p.EstornadoEm == null).Sum(p => (decimal?)p.ValorBruto) ?? 0m) < o.ValorTotal, ct);
}

internal sealed class DespesaRepository(AppDbContext db) : IDespesaRepository
{
    public Task<Despesa?> ObterPorIdAsync(int id, CancellationToken ct) => db.Despesas.FirstOrDefaultAsync(d => d.Id == id, ct);

    public async Task<IReadOnlyList<Despesa>> ListarAsync(DateOnly de, DateOnly ate, CancellationToken ct) =>
        await db.Despesas.AsNoTracking().Where(d => d.Data >= de && d.Data <= ate)
            .OrderByDescending(d => d.Data).ThenByDescending(d => d.Id).ToListAsync(ct);

    public async Task AdicionarAsync(Despesa despesa, CancellationToken ct) => await db.Despesas.AddAsync(despesa, ct);

    public void Remover(Despesa despesa) => db.Despesas.Remove(despesa);
}

internal sealed class RetiradaRepository(AppDbContext db) : IRetiradaRepository
{
    public Task<RetiradaDono?> ObterPorIdAsync(int id, CancellationToken ct) => db.RetiradasDono.FirstOrDefaultAsync(r => r.Id == id, ct);

    public async Task<IReadOnlyList<RetiradaDono>> ListarAsync(DateOnly de, DateOnly ate, CancellationToken ct) =>
        await db.RetiradasDono.AsNoTracking().Where(r => r.Data >= de && r.Data <= ate)
            .OrderByDescending(r => r.Data).ThenByDescending(r => r.Id).ToListAsync(ct);

    public async Task AdicionarAsync(RetiradaDono retirada, CancellationToken ct) => await db.RetiradasDono.AddAsync(retirada, ct);

    public void Remover(RetiradaDono retirada) => db.RetiradasDono.Remove(retirada);
}

internal sealed class DiariaRepository(AppDbContext db) : IDiariaRepository
{
    public Task<Diaria?> ObterPorIdAsync(int id, CancellationToken ct) => db.Diarias.FirstOrDefaultAsync(d => d.Id == id, ct);

    public async Task<IReadOnlyList<Diaria>> ListarAsync(DateOnly de, DateOnly ate, CancellationToken ct) =>
        await db.Diarias.AsNoTracking().Where(d => d.Data >= de && d.Data <= ate)
            .OrderBy(d => d.Data).ThenBy(d => d.LavadorId).ToListAsync(ct);

    public async Task<IReadOnlyList<int>> LavadoresComDiariaAsync(DateOnly data, CancellationToken ct) =>
        await db.Diarias.Where(d => d.Data == data).Select(d => d.LavadorId).ToListAsync(ct);

    public async Task AdicionarAsync(Diaria diaria, CancellationToken ct) => await db.Diarias.AddAsync(diaria, ct);

    public void Remover(Diaria diaria) => db.Diarias.Remove(diaria);
}

internal sealed class ConfiguracaoRepository(AppDbContext db) : IConfiguracaoRepository
{
    public Task<Configuracao?> ObterAsync(CancellationToken ct) =>
        db.Configuracoes.AsNoTracking().FirstOrDefaultAsync(c => c.Id == Configuracao.IdUnico, ct);

    public Task<Configuracao?> ObterParaEdicaoAsync(CancellationToken ct) =>
        db.Configuracoes.FirstOrDefaultAsync(c => c.Id == Configuracao.IdUnico, ct);
}

internal sealed class MensagemEnviadaRepository(AppDbContext db) : IMensagemEnviadaRepository
{
    public async Task AdicionarAsync(MensagemEnviada mensagem, CancellationToken ct) => await db.MensagensEnviadas.AddAsync(mensagem, ct);
}

internal sealed class ModeloMensagemRepository(AppDbContext db) : IModeloMensagemRepository
{
    public async Task<IReadOnlyList<ModeloMensagem>> ListarAsync(CancellationToken ct) =>
        (await db.ModelosMensagem.AsNoTracking().ToListAsync(ct)).OrderBy(m => m.Tipo).ToList();

    public Task<ModeloMensagem?> ObterPorTipoAsync(TipoMensagem tipo, CancellationToken ct) =>
        db.ModelosMensagem.FirstOrDefaultAsync(m => m.Tipo == tipo, ct);
}

internal sealed class RelatorioRepository(AppDbContext db) : IRelatorioRepository
{
    public async Task<IReadOnlyList<OrdemPaga>> ListarOrdensPagasAsync(DateOnly de, DateOnly ate, CancellationToken ct)
    {
        // Candidatas: OS com algum pagamento válido no período. A data final e o "totalmente paga" são conferidos em memória.
        var ids = await db.Pagamentos.Where(p => p.EstornadoEm == null && p.DataCaixa >= de && p.DataCaixa <= ate)
            .Select(p => p.OrdemServicoId).Distinct().ToListAsync(ct);
        if (ids.Count == 0)
        {
            return [];
        }

        var ordens = await db.OrdensServico.AsNoTracking().Include(o => o.Itens).Include(o => o.Veiculo).Include(o => o.Lavador)
            .Where(o => ids.Contains(o.Id) && o.Status != StatusOrdemServico.Cancelada).ToListAsync(ct);
        var pagamentos = (await db.Pagamentos.AsNoTracking().Where(p => p.EstornadoEm == null && ids.Contains(p.OrdemServicoId)).ToListAsync(ct))
            .ToLookup(p => p.OrdemServicoId);

        var resultado = new List<OrdemPaga>();
        foreach (var ordem in ordens)
        {
            var doOrdem = pagamentos[ordem.Id].ToList();
            var bruto = doOrdem.Sum(p => p.ValorBruto);
            var dataPaga = doOrdem.Max(p => p.DataCaixa);
            if (bruto >= ordem.ValorTotal && dataPaga >= de && dataPaga <= ate)
            {
                resultado.Add(new OrdemPaga(ordem, dataPaga, bruto, doOrdem.Sum(p => p.ValorLiquido)));
            }
        }

        return resultado;
    }

    public async Task<decimal> SomarDespesasAsync(DateOnly de, DateOnly ate, CancellationToken ct) =>
        await db.Despesas.Where(d => d.Data >= de && d.Data <= ate).SumAsync(d => (decimal?)d.Valor, ct) ?? 0m;

    public async Task<decimal> SomarDiariasAsync(DateOnly de, DateOnly ate, CancellationToken ct) =>
        await db.Diarias.Where(d => d.Data >= de && d.Data <= ate).SumAsync(d => (decimal?)d.Valor, ct) ?? 0m;

    public async Task<IReadOnlyList<Pagamento>> ListarPagamentosAsync(DateOnly de, DateOnly ate, CancellationToken ct) =>
        await db.Pagamentos.AsNoTracking().Where(p => p.EstornadoEm == null && p.DataCaixa >= de && p.DataCaixa <= ate)
            .OrderBy(p => p.DataCaixa).ToListAsync(ct);

    public async Task<IReadOnlyList<OrdemServico>> ListarOrdensCronometradasAsync(DateOnly de, DateOnly ate, CancellationToken ct)
    {
        var inicio = FusoBrasilia.IntervaloDoDia(de).Inicio;
        var fim = FusoBrasilia.IntervaloDoDia(ate).Fim;
        return await db.OrdensServico.AsNoTracking().Include(o => o.Itens).Include(o => o.Lavador)
            .Where(o => (o.Status == StatusOrdemServico.Concluido || o.Status == StatusOrdemServico.Entregue)
                && o.DuracaoRealMin != null && o.FimEm >= inicio && o.FimEm < fim)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<DateOnly>> DiasComPagamentoAsync(DateOnly de, DateOnly ate, CancellationToken ct) =>
        await db.Pagamentos.Where(p => p.EstornadoEm == null && p.DataCaixa >= de && p.DataCaixa <= ate)
            .Select(p => p.DataCaixa).Distinct().OrderBy(d => d).ToListAsync(ct);
}

internal sealed class AuditoriaRepository(AppDbContext db) : IAuditoriaRepository
{
    public async Task AdicionarAsync(LogAuditoria log, CancellationToken ct) => await db.LogsAuditoria.AddAsync(log, ct);
}

internal sealed class RefreshTokenRepository(AppDbContext db) : IRefreshTokenRepository
{
    public Task<RefreshToken?> ObterPorHashAsync(string tokenHash, CancellationToken ct) =>
        db.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == tokenHash, ct);

    public async Task AdicionarAsync(RefreshToken token, CancellationToken ct) =>
        await db.RefreshTokens.AddAsync(token, ct);

    public async Task RevogarTodosDoUsuarioAsync(int usuarioId, DateTimeOffset agora, CancellationToken ct)
    {
        var ativos = await db.RefreshTokens.Where(t => t.UsuarioId == usuarioId && t.RevogadoEm == null).ToListAsync(ct);
        foreach (var token in ativos)
        {
            token.Revogar(agora);
        }
    }
}

internal sealed class OrdemServicoRepository(AppDbContext db) : IOrdemServicoRepository
{
    public Task<OrdemServico?> ObterPorIdAsync(int id, CancellationToken ct) =>
        Completa().FirstOrDefaultAsync(o => o.Id == id, ct);

    public Task<OrdemServico?> ObterComAvariasAsync(int id, CancellationToken ct) =>
        Completa().Include(o => o.Avarias).AsNoTracking().FirstOrDefaultAsync(o => o.Id == id, ct);

    public async Task AdicionarAsync(OrdemServico ordem, CancellationToken ct) => await db.OrdensServico.AddAsync(ordem, ct);

    public Task<bool> ExisteAbertaDoVeiculoAsync(int veiculoId, CancellationToken ct) =>
        db.OrdensServico.AnyAsync(o => o.VeiculoId == veiculoId
            && (o.Status == StatusOrdemServico.Aguardando || o.Status == StatusOrdemServico.EmAndamento), ct);

    public async Task<IReadOnlyList<OrdemServico>> ObterQuadroAsync(DateTimeOffset inicioDoDia, DateTimeOffset fimDoDia, CancellationToken ct) =>
        await Completa().AsNoTracking()
            .Where(o => o.Status == StatusOrdemServico.Aguardando
                || o.Status == StatusOrdemServico.EmAndamento
                || o.Status == StatusOrdemServico.Concluido
                || (o.CriadoEm >= inicioDoDia && o.CriadoEm < fimDoDia))
            .OrderBy(o => o.CriadoEm)
            .ToListAsync(ct);

    private IQueryable<OrdemServico> Completa() =>
        db.OrdensServico.Include(o => o.Veiculo).Include(o => o.Cliente).Include(o => o.Lavador).Include(o => o.Itens);
}

internal sealed class AgendamentoRepository(AppDbContext db) : IAgendamentoRepository
{
    private const long ChaveDaAgenda = 7_001;

    private IQueryable<AgendamentoDetalhado> Detalhar(IQueryable<Agendamento> origem) =>
        from a in origem.AsNoTracking()
        join v in db.Veiculos on a.VeiculoId equals v.Id
        join s in db.Servicos on a.ServicoId equals s.Id
        join c in db.Clientes on a.ClienteId equals c.Id
        select new AgendamentoDetalhado(a, v.Placa, s.Nome, c.Nome);

    public Task<AgendamentoDetalhado?> ObterDetalheAsync(int id, CancellationToken ct) =>
        Detalhar(db.Agendamentos.Where(a => a.Id == id)).FirstOrDefaultAsync(ct);

    public async Task<IReadOnlyList<AgendamentoDetalhado>> ListarDoPeriodoAsync(DateTimeOffset de, DateTimeOffset ate, CancellationToken ct) =>
        await Detalhar(db.Agendamentos.Where(a => a.Inicio >= de && a.Inicio < ate))
            .OrderBy(d => d.Agendamento.Inicio).ToListAsync(ct);

    public async Task<IReadOnlyList<AgendamentoDetalhado>> ListarDoClienteAsync(int clienteId, CancellationToken ct) =>
        await Detalhar(db.Agendamentos.Where(a => a.ClienteId == clienteId))
            .OrderByDescending(d => d.Agendamento.Inicio).ToListAsync(ct);

    public Task<Agendamento?> ObterPorIdAsync(int id, CancellationToken ct) =>
        db.Agendamentos.FirstOrDefaultAsync(a => a.Id == id, ct);

    public async Task AdicionarAsync(Agendamento agendamento, CancellationToken ct) => await db.Agendamentos.AddAsync(agendamento, ct);

    public async Task<IReadOnlyList<AgendaOcupada>> ListarOcupacaoAsync(DateTimeOffset de, DateTimeOffset ate, CancellationToken ct) =>
        await db.Agendamentos.AsNoTracking()
            .Where(a => a.Status == StatusAgendamento.Agendado && a.Inicio >= de && a.Inicio < ate)
            .Select(a => new AgendaOcupada(a.Inicio, a.DuracaoEstimadaMin))
            .ToListAsync(ct);

    public Task<int> ContarFuturosDoClienteAsync(int clienteId, DateTimeOffset agora, CancellationToken ct) =>
        db.Agendamentos.CountAsync(a => a.ClienteId == clienteId && a.Status == StatusAgendamento.Agendado && a.Inicio > agora, ct);

    public Task<T> ExecutarSerializadoAsync<T>(Func<Task<T>> acao, CancellationToken ct) =>
        db.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
        {
            await using var transacao = await db.Database.BeginTransactionAsync(ct);
            await db.Database.ExecuteSqlAsync($"SELECT pg_advisory_xact_lock({ChaveDaAgenda})", ct);
            var resultado = await acao();
            await transacao.CommitAsync(ct);
            return resultado;
        });
}

internal sealed class UnitOfWork(AppDbContext db) : IUnitOfWork
{
    public async Task SalvarAsync(CancellationToken ct)
    {
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConflitoException("Este cartão acabou de ser alterado por outra pessoa. Atualize a tela e tente de novo.");
        }
        catch (DbUpdateException e) when (e.InnerException is Npgsql.PostgresException { SqlState: Npgsql.PostgresErrorCodes.UniqueViolation })
        {
            // Duas requisições criaram o mesmo registro único ao mesmo tempo (telefone, placa, fechamento do dia...).
            throw new ConflitoException("Este registro acabou de ser criado por outra pessoa. Atualize a tela e confira.");
        }
    }
}
