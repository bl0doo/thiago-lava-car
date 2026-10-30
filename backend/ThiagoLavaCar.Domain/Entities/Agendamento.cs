using ThiagoLavaCar.Domain.Common;
using ThiagoLavaCar.Domain.Enums;
using ThiagoLavaCar.Domain.Exceptions;

namespace ThiagoLavaCar.Domain.Entities;

/// <summary>Horário reservado pelo cliente; no check-in gera no máximo uma ordem de serviço.</summary>
public sealed class Agendamento : Entidade
{
    private Agendamento() { }

    public int ClienteId { get; private set; }
    public int VeiculoId { get; private set; }
    public int ServicoId { get; private set; }
    public bool ComCera { get; private set; }
    public DateTimeOffset Inicio { get; private set; }
    public int DuracaoEstimadaMin { get; private set; }
    public decimal PrecoEstimado { get; private set; }
    public StatusAgendamento Status { get; private set; }
    public DateTimeOffset CriadoEm { get; private set; }

    public static Agendamento Criar(
        int clienteId, int veiculoId, Servico servico, TipoVeiculo tipoVeiculo, bool comCera, DateTimeOffset inicio, DateTimeOffset agora)
    {
        if (inicio <= agora)
        {
            throw new RegraInvalidaException("O horário do agendamento deve ser no futuro.");
        }

        return new Agendamento
        {
            ClienteId = clienteId,
            VeiculoId = veiculoId,
            ServicoId = servico.Id,
            ComCera = comCera,
            Inicio = inicio,
            DuracaoEstimadaMin = servico.TempoEstimadoMin,
            PrecoEstimado = servico.ObterPreco(tipoVeiculo, comCera),
            Status = StatusAgendamento.Agendado,
            CriadoEm = agora,
        };
    }

    public void Cancelar() => MudarDeAgendado(StatusAgendamento.Cancelado, "cancelar");

    public void MarcarAtendido() => MudarDeAgendado(StatusAgendamento.Atendido, "atender");

    public void MarcarNaoCompareceu() => MudarDeAgendado(StatusAgendamento.NaoCompareceu, "marcar como não compareceu");

    private void MudarDeAgendado(StatusAgendamento novo, string acao)
    {
        if (Status != StatusAgendamento.Agendado)
        {
            throw new ConflitoException($"Só é possível {acao} um agendamento que ainda está agendado.");
        }

        Status = novo;
    }
}
