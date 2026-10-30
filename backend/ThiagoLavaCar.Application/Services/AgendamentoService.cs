using System.Globalization;
using ThiagoLavaCar.Application.Abstractions;
using ThiagoLavaCar.Application.Common;
using ThiagoLavaCar.Application.DTOs;
using ThiagoLavaCar.Application.Exceptions;
using ThiagoLavaCar.Domain.Entities;
using ThiagoLavaCar.Domain.Enums;
using ThiagoLavaCar.Domain.Exceptions;

namespace ThiagoLavaCar.Application.Services;

public interface IAgendamentoService
{
    Task<HorariosResponse> ListarHorariosAsync(DateOnly data, int servicoId, string tipoVeiculo, bool comCera, CancellationToken ct);
    Task<AgendamentoResponse> CriarAsync(CriarAgendamentoRequest request, CancellationToken ct);
    Task<AgendaDoDiaResponse> ListarDoDiaAsync(DateOnly? data, CancellationToken ct);
    Task<IReadOnlyList<AgendamentoResponse>> ListarMeusAsync(CancellationToken ct);
    Task<AgendamentoResponse> CancelarAsync(int id, CancellationToken ct);
    Task<AgendamentoResponse> MarcarNaoCompareceuAsync(int id, CancellationToken ct);
    Task<OrdemCriadaResponse> FazerCheckInAsync(int id, CheckInRequest request, CancellationToken ct);
}

public sealed class AgendamentoService(
    IAgendamentoRepository agendamentos,
    IServicoRepository servicos,
    IVeiculoRepository veiculos,
    IClienteRepository clientes,
    IUsuarioRepository usuarios,
    IConfiguracaoRepository configuracoes,
    IOrdemServicoRepository ordens,
    IUnitOfWork unitOfWork,
    IUsuarioLogado usuarioLogado,
    TimeProvider relogio) : IAgendamentoService
{
    private const int LimiteDeAgendamentosFuturos = 2;

    public async Task<HorariosResponse> ListarHorariosAsync(DateOnly data, int servicoId, string tipoVeiculo, bool comCera, CancellationToken ct)
    {
        var agora = relogio.GetUtcNow();
        ValidarJanela(data, agora);
        var tipo = ParseTipo(tipoVeiculo);
        var servico = await ObterServicoAtivoAsync(servicoId, ct);
        var preco = servico.ObterPreco(tipo, comCera);
        var configuracao = await ObterConfiguracaoAsync(ct);
        GarantirDiaAberto(configuracao, data);

        var (dia, fimDoDia) = FusoBrasilia.IntervaloDoDia(data);
        var ocupacao = await agendamentos.ListarOcupacaoAsync(dia, fimDoDia, ct);
        var lavadores = (await usuarios.ListarLavadoresAtivosAsync(ct)).Count;

        var horarios = AgendaCalculo.HorariosLivres(configuracao, data, servico.TempoEstimadoMin, ocupacao.ToList(), lavadores, agora)
            .Select(h => h.ToString("HH:mm", CultureInfo.InvariantCulture))
            .ToList();
        return new HorariosResponse(preco, servico.TempoEstimadoMin, horarios);
    }

    public async Task<AgendamentoResponse> CriarAsync(CriarAgendamentoRequest request, CancellationToken ct)
    {
        var veiculo = await veiculos.ObterPorIdAsync(request.VeiculoId, ct) ?? throw new NaoEncontradoException("Veículo não encontrado.");
        if (usuarioLogado.Perfil == Perfil.Cliente
            && veiculo.ClienteId != (await clientes.ObterIdPorUsuarioAsync(usuarioLogado.Id, ct) ?? 0))
        {
            // Veículo de outro cliente é tratado como inexistente, para não revelar que existe.
            throw new NaoEncontradoException("Veículo não encontrado.");
        }

        var agora = relogio.GetUtcNow();
        var data = FusoBrasilia.DataDe(request.Inicio);
        ValidarJanela(data, agora);
        var servico = await ObterServicoAtivoAsync(request.ServicoId, ct);
        _ = servico.ObterPreco(veiculo.Tipo, request.ComCera);
        var configuracao = await ObterConfiguracaoAsync(ct);
        GarantirDiaAberto(configuracao, data);

        if (!AgendaCalculo.Grade(configuracao, servico.TempoEstimadoMin).Contains(FusoBrasilia.HoraDe(request.Inicio))
            || request.Inicio.Second != 0 || request.Inicio.Millisecond != 0)
        {
            throw new RegraInvalidaException("Esse horário está fora do funcionamento ou não cabe a duração do serviço antes de fechar.");
        }

        if (request.Inicio < agora + AgendaCalculo.AntecedenciaMinima)
        {
            throw new RegraInvalidaException("Agende com pelo menos 1 hora de antecedência.");
        }

        if (await agendamentos.ContarFuturosDoClienteAsync(veiculo.ClienteId, agora, ct) >= LimiteDeAgendamentosFuturos)
        {
            throw new RegraInvalidaException($"Você já tem {LimiteDeAgendamentosFuturos} agendamentos futuros. Cancele um para marcar outro.");
        }

        var lavadores = (await usuarios.ListarLavadoresAtivosAsync(ct)).Count;
        var (dia, fimDoDia) = FusoBrasilia.IntervaloDoDia(data);

        // Conferência final dentro da trava: duas reservas para a última vaga não passam juntas.
        var criado = await agendamentos.ExecutarSerializadoAsync(async () =>
        {
            var ocupacao = await agendamentos.ListarOcupacaoAsync(dia, fimDoDia, ct);
            if (AgendaCalculo.Sobrepostos(ocupacao, request.Inicio, servico.TempoEstimadoMin) >= lavadores)
            {
                throw new ConflitoException("Este horário acabou de ser ocupado.");
            }

            var agendamento = Agendamento.Criar(veiculo.ClienteId, veiculo.Id, servico, veiculo.Tipo, request.ComCera, request.Inicio, agora);
            await agendamentos.AdicionarAsync(agendamento, ct);
            await unitOfWork.SalvarAsync(ct);
            return agendamento;
        }, ct);

        return Mapear(criado, veiculo.Placa, servico.Nome);
    }

    public async Task<AgendaDoDiaResponse> ListarDoDiaAsync(DateOnly? data, CancellationToken ct)
    {
        var dia = data ?? FusoBrasilia.DataDe(relogio.GetUtcNow());
        var (de, ate) = FusoBrasilia.IntervaloDoDia(dia);
        var lista = await agendamentos.ListarDoPeriodoAsync(de, ate, ct);
        return new AgendaDoDiaResponse(dia, lista.Select(MapearDetalhe).ToList());
    }

    public async Task<IReadOnlyList<AgendamentoResponse>> ListarMeusAsync(CancellationToken ct)
    {
        var clienteId = await clientes.ObterIdPorUsuarioAsync(usuarioLogado.Id, ct) ?? throw new NaoEncontradoException("Cliente não encontrado.");
        return (await agendamentos.ListarDoClienteAsync(clienteId, ct)).Select(MapearDetalhe).ToList();
    }

    public async Task<AgendamentoResponse> CancelarAsync(int id, CancellationToken ct)
    {
        var agendamento = await BuscarAcessivelAsync(id, ct);
        agendamento.Cancelar();
        await unitOfWork.SalvarAsync(ct);
        return await ObterRespostaAsync(id, ct);
    }

    public async Task<AgendamentoResponse> MarcarNaoCompareceuAsync(int id, CancellationToken ct)
    {
        var agendamento = await BuscarAcessivelAsync(id, ct);
        agendamento.MarcarNaoCompareceu();
        await unitOfWork.SalvarAsync(ct);
        return await ObterRespostaAsync(id, ct);
    }

    public async Task<OrdemCriadaResponse> FazerCheckInAsync(int id, CheckInRequest request, CancellationToken ct)
    {
        var agendamento = await agendamentos.ObterPorIdAsync(id, ct) ?? throw new NaoEncontradoException("Agendamento não encontrado.");
        if (agendamento.Status != StatusAgendamento.Agendado)
        {
            throw new ConflitoException("Só é possível fazer o check-in de um agendamento que ainda está agendado.");
        }

        var veiculo = await veiculos.ObterPorIdAsync(agendamento.VeiculoId, ct) ?? throw new NaoEncontradoException("Veículo não encontrado.");
        if (await ordens.ExisteAbertaDoVeiculoAsync(veiculo.Id, ct))
        {
            throw new ConflitoException("Este veículo já está no pátio.");
        }

        var servico = await ObterServicoAtivoAsync(agendamento.ServicoId, ct);
        var item = ItemOrdemServico.Criar(servico, agendamento.ComCera, servico.ObterPreco(veiculo.Tipo, agendamento.ComCera), precoAjustado: false);
        var agora = relogio.GetUtcNow();
        var avarias = (request.Avarias ?? []).Select(a => Avaria.Criar(a.Local.Trim(), a.Tipo.Trim(), a.Descricao?.Trim())).ToList();
        var ordem = OrdemServico.Registrar(
            veiculo, usuarioLogado.Id, [item], request.SemAvarias, avarias, request.AceiteCliente,
            LimparTexto(request.PertencesEntrada), LimparTexto(request.NomeQuemRetira), agora, agendamento.Id);

        agendamento.MarcarAtendido();
        await ordens.AdicionarAsync(ordem, ct);
        await unitOfWork.SalvarAsync(ct);
        return new OrdemCriadaResponse(ordem.Id, ordem.Status.ToString(), ordem.ValorTotal, ordem.DuracaoEstimadaMin, agora.AddMinutes(ordem.DuracaoEstimadaMin));
    }

    private static string? LimparTexto(string? texto) => string.IsNullOrWhiteSpace(texto) ? null : texto.Trim();

    /// <summary>O cliente só enxerga os próprios agendamentos; o de outro cliente é tratado como inexistente.</summary>
    private async Task<Agendamento> BuscarAcessivelAsync(int id, CancellationToken ct)
    {
        var agendamento = await agendamentos.ObterPorIdAsync(id, ct) ?? throw new NaoEncontradoException("Agendamento não encontrado.");
        if (usuarioLogado.Perfil == Perfil.Cliente
            && agendamento.ClienteId != (await clientes.ObterIdPorUsuarioAsync(usuarioLogado.Id, ct) ?? 0))
        {
            throw new NaoEncontradoException("Agendamento não encontrado.");
        }

        return agendamento;
    }

    private async Task<AgendamentoResponse> ObterRespostaAsync(int id, CancellationToken ct) =>
        MapearDetalhe(await agendamentos.ObterDetalheAsync(id, ct) ?? throw new NaoEncontradoException("Agendamento não encontrado."));

    private AgendamentoResponse MapearDetalhe(AgendamentoDetalhado d) =>
        Mapear(d.Agendamento, d.Placa, d.Servico) with { Cliente = usuarioLogado.Perfil == Perfil.Dono ? d.Cliente : null };

    internal static AgendamentoResponse Mapear(Agendamento a, string placa, string servico) => new(
        a.Id, a.ClienteId, a.VeiculoId, placa, a.ServicoId, servico, a.ComCera, a.Inicio, a.DuracaoEstimadaMin, a.PrecoEstimado, a.Status.ToString());

    private static void ValidarJanela(DateOnly data, DateTimeOffset agora)
    {
        var hoje = FusoBrasilia.DataDe(agora);
        if (data < hoje || data > hoje.AddDays(AgendaCalculo.DiasDeAntecedenciaMaxima))
        {
            throw new RegraInvalidaException($"Escolha uma data entre hoje e {AgendaCalculo.DiasDeAntecedenciaMaxima} dias à frente.");
        }
    }

    private static void GarantirDiaAberto(Configuracao configuracao, DateOnly data)
    {
        if (!AgendaCalculo.DiaAberto(configuracao, data))
        {
            throw new RegraInvalidaException("O lava-jato não abre nesse dia.");
        }
    }

    private static TipoVeiculo ParseTipo(string valor) =>
        Enum.TryParse<TipoVeiculo>(valor, ignoreCase: true, out var tipo) && !int.TryParse(valor, out _)
            ? tipo
            : throw new RegraInvalidaException("Tipo de veículo inválido. Use Moto, Carro ou Caminhonete.");

    private async Task<Servico> ObterServicoAtivoAsync(int servicoId, CancellationToken ct)
    {
        var servico = await servicos.ObterPorIdAsync(servicoId, ct) ?? throw new NaoEncontradoException("Serviço não encontrado.");
        return servico.Ativo ? servico : throw new RegraInvalidaException($"O serviço \"{servico.Nome}\" está desativado.");
    }

    private async Task<Configuracao> ObterConfiguracaoAsync(CancellationToken ct) =>
        await configuracoes.ObterAsync(ct) ?? throw new NaoEncontradoException("Configuração do lava-jato não encontrada.");
}
