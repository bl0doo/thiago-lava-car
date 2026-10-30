using ThiagoLavaCar.Domain.Entities;
using ThiagoLavaCar.Domain.Enums;

namespace ThiagoLavaCar.Application.Abstractions;

public interface IUsuarioRepository
{
    Task<Usuario?> ObterPorIdAsync(int id, CancellationToken ct);
    Task<Usuario?> ObterPorEmailAsync(string email, CancellationToken ct);
    Task<IReadOnlyList<Usuario>> ListarLavadoresAtivosAsync(CancellationToken ct);

    /// <summary>Todos os usuários, ativos ou não, opcionalmente de um só perfil, por nome.</summary>
    Task<IReadOnlyList<Usuario>> ListarAsync(Perfil? perfil, CancellationToken ct);

    /// <summary>Se algum lavador (ativo ou não) já usa o apelido, sem diferenciar maiúsculas.</summary>
    Task<bool> ApelidoDeLavadorExisteAsync(string apelido, CancellationToken ct);
    Task AdicionarAsync(Usuario usuario, CancellationToken ct);
}

public interface IClienteRepository
{
    Task<int?> ObterIdPorUsuarioAsync(int usuarioId, CancellationToken ct);
    Task<Cliente?> ObterPorIdAsync(int id, CancellationToken ct);
    Task<Cliente?> ObterPorUsuarioAsync(int usuarioId, CancellationToken ct);
    Task<Cliente?> ObterPorTelefoneAsync(string telefone, CancellationToken ct);
    Task<(IReadOnlyList<Cliente> Itens, int Total)> BuscarAsync(string? busca, int pagina, int tamanho, CancellationToken ct);
    Task AdicionarAsync(Cliente cliente, CancellationToken ct);
    Task<IReadOnlyList<Veiculo>> ListarVeiculosAsync(int clienteId, CancellationToken ct);
}

public interface IVeiculoRepository
{
    Task<Veiculo?> ObterPorIdAsync(int id, CancellationToken ct);
    Task<Veiculo?> ObterPorPlacaAsync(string placaNormalizada, CancellationToken ct);
    Task AdicionarAsync(Veiculo veiculo, CancellationToken ct);

    /// <summary>OS do veículo (com itens), da mais recente para a mais antiga.</summary>
    Task<IReadOnlyList<OrdemServico>> ListarOrdensAsync(int veiculoId, CancellationToken ct);
}

public interface IModeloVeiculoRepository
{
    Task<ModeloVeiculo?> ObterPorIdAsync(int id, CancellationToken ct);

    /// <summary>Modelos cuja marca ou nome contém o texto, sem diferenciar maiúsculas, por marca e nome.</summary>
    Task<IReadOnlyList<ModeloVeiculo>> BuscarAsync(string? busca, int limite, CancellationToken ct);
}

public interface IServicoRepository
{
    Task<Servico?> ObterPorIdAsync(int id, CancellationToken ct);
    Task<Servico?> ObterPorNomeAsync(string nome, CancellationToken ct);
    Task<IReadOnlyList<Servico>> ListarAsync(bool somenteAtivos, CancellationToken ct);
    Task AdicionarAsync(Servico servico, CancellationToken ct);
}

public interface IPagamentoRepository
{
    Task<Pagamento?> ObterPorIdAsync(int id, CancellationToken ct);

    /// <summary>Soma dos valores brutos não estornados da OS.</summary>
    Task<decimal> SomarPagoAsync(int ordemServicoId, CancellationToken ct);
    Task<IReadOnlyList<Pagamento>> ListarPorDataCaixaAsync(DateOnly data, CancellationToken ct);
    Task<IReadOnlyList<Pagamento>> ListarPorOrdemAsync(int ordemServicoId, CancellationToken ct);
    Task AdicionarAsync(Pagamento pagamento, CancellationToken ct);
}

public interface ITaxaRepository
{
    Task<TaxaPagamento?> ObterPorFormaAsync(FormaPagamento forma, CancellationToken ct);
    Task<IReadOnlyList<TaxaPagamento>> ListarAsync(CancellationToken ct);
}

public interface ICaixaRepository
{
    Task<bool> ExisteFechamentoAsync(DateOnly data, CancellationToken ct);
    Task<FechamentoCaixa?> ObterFechamentoAsync(DateOnly data, CancellationToken ct);
    Task<IReadOnlyList<FechamentoCaixa>> ListarFechamentosAsync(DateOnly primeiroDia, DateOnly ultimoDia, CancellationToken ct);
    Task AdicionarFechamentoAsync(FechamentoCaixa fechamento, CancellationToken ct);
    void RemoverFechamento(FechamentoCaixa fechamento);
    Task<decimal> SomarDespesasAsync(DateOnly data, CancellationToken ct);
    Task<decimal> SomarDiariasAsync(DateOnly data, CancellationToken ct);

    /// <summary>OS concluídas ou entregues no dia (hora de término em Brasília) cujo valor não foi totalmente recebido.</summary>
    Task<int> ContarOrdensSemPagamentoCompletoAsync(DateTimeOffset inicioDoDia, DateTimeOffset fimDoDia, CancellationToken ct);
}

public interface IDespesaRepository
{
    Task<Despesa?> ObterPorIdAsync(int id, CancellationToken ct);
    Task<IReadOnlyList<Despesa>> ListarAsync(DateOnly de, DateOnly ate, CancellationToken ct);
    Task AdicionarAsync(Despesa despesa, CancellationToken ct);
    void Remover(Despesa despesa);
}

public interface IRetiradaRepository
{
    Task<RetiradaDono?> ObterPorIdAsync(int id, CancellationToken ct);
    Task<IReadOnlyList<RetiradaDono>> ListarAsync(DateOnly de, DateOnly ate, CancellationToken ct);
    Task AdicionarAsync(RetiradaDono retirada, CancellationToken ct);
    void Remover(RetiradaDono retirada);
}

public interface IDiariaRepository
{
    Task<Diaria?> ObterPorIdAsync(int id, CancellationToken ct);
    Task<IReadOnlyList<Diaria>> ListarAsync(DateOnly de, DateOnly ate, CancellationToken ct);

    /// <summary>Lavadores que já têm diária lançada no dia.</summary>
    Task<IReadOnlyList<int>> LavadoresComDiariaAsync(DateOnly data, CancellationToken ct);
    Task AdicionarAsync(Diaria diaria, CancellationToken ct);
    void Remover(Diaria diaria);
}

public interface IConfiguracaoRepository
{
    Task<Configuracao?> ObterAsync(CancellationToken ct);

    /// <summary>Como <see cref="ObterAsync"/>, mas rastreada pelo contexto, para ser editada.</summary>
    Task<Configuracao?> ObterParaEdicaoAsync(CancellationToken ct);
}

public interface IMensagemEnviadaRepository
{
    Task AdicionarAsync(MensagemEnviada mensagem, CancellationToken ct);
}

public interface IModeloMensagemRepository
{
    Task<IReadOnlyList<ModeloMensagem>> ListarAsync(CancellationToken ct);

    /// <summary>Rastreado pelo contexto, para ser editado.</summary>
    Task<ModeloMensagem?> ObterPorTipoAsync(TipoMensagem tipo, CancellationToken ct);
}

/// <summary>OS totalmente paga (soma dos pagamentos não estornados cobre o valor total); <c>DataPaga</c> é a data de caixa do último pagamento.</summary>
public sealed record OrdemPaga(OrdemServico Ordem, DateOnly DataPaga, decimal Bruto, decimal Liquido)
{
    public decimal Insumos => Ordem.Itens.Sum(i => i.CustoInsumos);
}

/// <summary>Consultas de leitura que alimentam o painel e os relatórios.</summary>
public interface IRelatorioRepository
{
    /// <summary>OS totalmente pagas cuja data de pagamento final cai em [de, ate], com os itens carregados.</summary>
    Task<IReadOnlyList<OrdemPaga>> ListarOrdensPagasAsync(DateOnly de, DateOnly ate, CancellationToken ct);
    Task<decimal> SomarDespesasAsync(DateOnly de, DateOnly ate, CancellationToken ct);
    Task<decimal> SomarDiariasAsync(DateOnly de, DateOnly ate, CancellationToken ct);

    /// <summary>Pagamentos não estornados com data de caixa em [de, ate].</summary>
    Task<IReadOnlyList<Pagamento>> ListarPagamentosAsync(DateOnly de, DateOnly ate, CancellationToken ct);

    /// <summary>OS concluídas ou entregues cujo término (em Brasília) cai em [de, ate] e que têm duração real, com itens e lavador.</summary>
    Task<IReadOnlyList<OrdemServico>> ListarOrdensCronometradasAsync(DateOnly de, DateOnly ate, CancellationToken ct);

    /// <summary>Dias com ao menos um pagamento não estornado.</summary>
    Task<IReadOnlyList<DateOnly>> DiasComPagamentoAsync(DateOnly de, DateOnly ate, CancellationToken ct);
}

public interface IAuditoriaRepository
{
    Task AdicionarAsync(LogAuditoria log, CancellationToken ct);
}

public interface IRefreshTokenRepository
{
    Task<RefreshToken?> ObterPorHashAsync(string tokenHash, CancellationToken ct);
    Task AdicionarAsync(RefreshToken token, CancellationToken ct);
    Task RevogarTodosDoUsuarioAsync(int usuarioId, DateTimeOffset agora, CancellationToken ct);
}

public interface IOrdemServicoRepository
{
    Task<OrdemServico?> ObterPorIdAsync(int id, CancellationToken ct);

    /// <summary>Como <see cref="ObterPorIdAsync"/>, mas com as avarias da vistoria carregadas.</summary>
    Task<OrdemServico?> ObterComAvariasAsync(int id, CancellationToken ct);
    Task AdicionarAsync(OrdemServico ordem, CancellationToken ct);

    /// <summary>Um veículo não pode ter duas OS abertas (Aguardando ou Em andamento).</summary>
    Task<bool> ExisteAbertaDoVeiculoAsync(int veiculoId, CancellationToken ct);

    /// <summary>Cartões abertos (Aguardando, Em andamento, Concluído) mais os de qualquer status criados no intervalo.</summary>
    Task<IReadOnlyList<OrdemServico>> ObterQuadroAsync(DateTimeOffset inicioDoDia, DateTimeOffset fimDoDia, CancellationToken ct);
}

/// <summary>Agendamento ainda ativo que ocupa um lavador durante <see cref="DuracaoMin"/>.</summary>
public sealed record AgendaOcupada(DateTimeOffset Inicio, int DuracaoMin);

/// <summary>Agendamento com os nomes que as listas mostram (veículo, serviço e cliente).</summary>
public sealed record AgendamentoDetalhado(Agendamento Agendamento, string Placa, string Servico, string Cliente);

public interface IAgendamentoRepository
{
    Task<AgendamentoDetalhado?> ObterDetalheAsync(int id, CancellationToken ct);

    /// <summary>Agendamentos de qualquer status que começam em [de, ate), por horário.</summary>
    Task<IReadOnlyList<AgendamentoDetalhado>> ListarDoPeriodoAsync(DateTimeOffset de, DateTimeOffset ate, CancellationToken ct);

    /// <summary>Todos os agendamentos do cliente, do mais recente para o mais antigo.</summary>
    Task<IReadOnlyList<AgendamentoDetalhado>> ListarDoClienteAsync(int clienteId, CancellationToken ct);

    Task<Agendamento?> ObterPorIdAsync(int id, CancellationToken ct);
    Task AdicionarAsync(Agendamento agendamento, CancellationToken ct);

    /// <summary>Agendamentos ainda marcados (status Agendado) que começam em [de, ate).</summary>
    Task<IReadOnlyList<AgendaOcupada>> ListarOcupacaoAsync(DateTimeOffset de, DateTimeOffset ate, CancellationToken ct);

    /// <summary>Agendamentos ainda marcados do cliente que começam depois de <paramref name="agora"/>.</summary>
    Task<int> ContarFuturosDoClienteAsync(int clienteId, DateTimeOffset agora, CancellationToken ct);

    /// <summary>Roda a ação numa transação que serializa as reservas (trava consultiva), para que duas pessoas não levem a última vaga.</summary>
    Task<T> ExecutarSerializadoAsync<T>(Func<Task<T>> acao, CancellationToken ct);
}

public interface IUnitOfWork
{
    /// <summary>Grava as mudanças. Conflito de concorrência vira <see cref="Domain.Exceptions.ConflitoException"/>.</summary>
    Task SalvarAsync(CancellationToken ct);
}
