using ThiagoLavaCar.Domain.Common;
using ThiagoLavaCar.Domain.Enums;
using ThiagoLavaCar.Domain.Exceptions;

namespace ThiagoLavaCar.Domain.Entities;

/// <summary>Cartão do quadro Pit Stop. Protege as transições de status e o temporizador.</summary>
public sealed class OrdemServico : Entidade
{
    public static readonly TimeSpan JanelaDesfazer = TimeSpan.FromMinutes(2);
    public const int LimiteRevisaoMin = 8 * 60;

    private readonly List<ItemOrdemServico> _itens = [];
    private readonly List<Avaria> _avarias = [];

    private OrdemServico() { }

    public int VeiculoId { get; private set; }
    public int ClienteId { get; private set; }
    public int CriadoPorId { get; private set; }
    public int? LavadorId { get; private set; }
    public int? AgendamentoId { get; private set; }
    public StatusOrdemServico Status { get; private set; }
    public decimal ValorTotal { get; private set; }
    public int DuracaoEstimadaMin { get; private set; }
    public bool SemAvarias { get; private set; }
    public bool AceiteCliente { get; private set; }
    public DateTimeOffset AceiteEm { get; private set; }
    public string? PertencesEntrada { get; private set; }
    public string? PertencesEncontrados { get; private set; }
    public string? NomeQuemRetira { get; private set; }
    public DateTimeOffset CriadoEm { get; private set; }
    public DateTimeOffset? InicioEm { get; private set; }
    public DateTimeOffset? FimEm { get; private set; }
    public int? DuracaoRealMin { get; private set; }
    public bool RevisaoNecessaria { get; private set; }
    public short? Avaliacao { get; private set; }
    public DateTimeOffset? EntregueEm { get; private set; }

    /// <summary>Hora da última mudança de coluna; base da janela de 2 minutos do Desfazer.</summary>
    public DateTimeOffset? UltimaTransicaoEm { get; private set; }

    public Veiculo? Veiculo { get; private set; }
    public Cliente? Cliente { get; private set; }
    public Usuario? Lavador { get; private set; }

    public IReadOnlyCollection<ItemOrdemServico> Itens => _itens;
    public IReadOnlyCollection<Avaria> Avarias => _avarias;

    /// <summary>Registra a entrada do carro com a vistoria; o cartão nasce em Aguardando.</summary>
    public static OrdemServico Registrar(
        Veiculo veiculo,
        int criadoPorId,
        IReadOnlyCollection<ItemOrdemServico> itens,
        bool semAvarias,
        IReadOnlyCollection<Avaria> avarias,
        bool aceiteCliente,
        string? pertencesEntrada,
        string? nomeQuemRetira,
        DateTimeOffset agora,
        int? agendamentoId = null)
    {
        if (itens.Count == 0)
        {
            throw new RegraInvalidaException("Informe ao menos um serviço.");
        }

        if (!aceiteCliente)
        {
            throw new RegraInvalidaException("O cliente precisa aceitar a vistoria de entrada.");
        }

        if (semAvarias && avarias.Count > 0)
        {
            throw new RegraInvalidaException("Vistoria inconsistente: marcada sem avarias, mas há avarias listadas.");
        }

        if (!semAvarias && avarias.Count == 0)
        {
            throw new RegraInvalidaException("Marque \"Sem avarias\" ou registre ao menos uma avaria.");
        }

        var os = new OrdemServico
        {
            VeiculoId = veiculo.Id,
            ClienteId = veiculo.ClienteId,
            CriadoPorId = criadoPorId,
            Status = StatusOrdemServico.Aguardando,
            SemAvarias = semAvarias,
            AceiteCliente = true,
            AceiteEm = agora,
            AgendamentoId = agendamentoId,
            PertencesEntrada = pertencesEntrada,
            NomeQuemRetira = nomeQuemRetira,
            CriadoEm = agora,
        };
        os._itens.AddRange(itens);
        os._avarias.AddRange(avarias);
        os.ValorTotal = Dinheiro.Arredondar(itens.Sum(i => i.PrecoFinal));
        os.DuracaoEstimadaMin = itens.Sum(i => i.TempoEstimadoMin);
        return os;
    }

    public void Iniciar(int lavadorId, DateTimeOffset agora)
    {
        if (Status != StatusOrdemServico.Aguardando)
        {
            throw new ConflitoException(Status == StatusOrdemServico.Cancelada
                ? "Esta ordem de serviço foi cancelada."
                : "Esta lavagem já foi iniciada.");
        }

        Status = StatusOrdemServico.EmAndamento;
        LavadorId = lavadorId;
        InicioEm = agora;
        UltimaTransicaoEm = agora;
    }

    public void Encerrar(DateTimeOffset agora)
    {
        if (Status != StatusOrdemServico.EmAndamento)
        {
            throw new ConflitoException(Status == StatusOrdemServico.Aguardando
                ? "A lavagem ainda não foi iniciada."
                : "Esta lavagem já foi encerrada.");
        }

        Status = StatusOrdemServico.Concluido;
        FimEm = agora;
        // minutos inteiros, arredondados para o mais próximo, para o rateio da mão de obra
        DuracaoRealMin = (int)Math.Round((agora - InicioEm!.Value).TotalMinutes, MidpointRounding.AwayFromZero);
        RevisaoNecessaria = DuracaoRealMin > LimiteRevisaoMin;
        UltimaTransicaoEm = agora;
    }

    /// <summary>Volta o último passo (iniciar ou encerrar) se ele ocorreu há no máximo 2 minutos.</summary>
    public void Desfazer(DateTimeOffset agora)
    {
        var dentroDaJanela = UltimaTransicaoEm is { } ultima && agora - ultima <= JanelaDesfazer;
        if (!dentroDaJanela || Status is not (StatusOrdemServico.EmAndamento or StatusOrdemServico.Concluido))
        {
            throw new ConflitoException("Não é mais possível desfazer: o prazo de 2 minutos acabou.");
        }

        if (Status == StatusOrdemServico.EmAndamento)
        {
            Status = StatusOrdemServico.Aguardando;
            InicioEm = null;
            LavadorId = null;
        }
        else
        {
            Status = StatusOrdemServico.EmAndamento;
            FimEm = null;
            DuracaoRealMin = null;
            RevisaoNecessaria = false;
        }

        UltimaTransicaoEm = null;
    }

    /// <summary>Troca os serviços e os textos de uma OS que ainda não foi iniciada; a vistoria não muda.</summary>
    public void Editar(IReadOnlyCollection<ItemOrdemServico> itens, string? pertencesEntrada, string? nomeQuemRetira)
    {
        if (Status != StatusOrdemServico.Aguardando)
        {
            throw new ConflitoException("Só é possível editar uma ordem de serviço que está em Aguardando.");
        }

        if (itens.Count == 0)
        {
            throw new RegraInvalidaException("Informe ao menos um serviço.");
        }

        _itens.Clear();
        _itens.AddRange(itens);
        ValorTotal = Dinheiro.Arredondar(itens.Sum(i => i.PrecoFinal));
        DuracaoEstimadaMin = itens.Sum(i => i.TempoEstimadoMin);
        PertencesEntrada = pertencesEntrada;
        NomeQuemRetira = nomeQuemRetira;
    }

    public void Cancelar()
    {
        if (Status != StatusOrdemServico.Aguardando)
        {
            throw new ConflitoException(Status == StatusOrdemServico.Cancelada
                ? "Esta ordem de serviço já foi cancelada."
                : "Só é possível cancelar uma ordem de serviço em Aguardando.");
        }

        Status = StatusOrdemServico.Cancelada;
        UltimaTransicaoEm = null;
    }

    /// <summary>Entrega o carro ao cliente (só depois de concluído). Saldo pendente não impede, mas aparece como pendência no caixa.</summary>
    public void Entregar(DateTimeOffset agora, string? nomeQuemRetira)
    {
        if (Status != StatusOrdemServico.Concluido)
        {
            throw new ConflitoException(Status == StatusOrdemServico.Entregue
                ? "Este carro já foi entregue."
                : "O carro só pode ser entregue depois que a lavagem for concluída.");
        }

        Status = StatusOrdemServico.Entregue;
        EntregueEm = agora;
        UltimaTransicaoEm = null;
        if (!string.IsNullOrWhiteSpace(nomeQuemRetira))
        {
            NomeQuemRetira = nomeQuemRetira;
        }
    }

    public void Avaliar(int nota)
    {
        if (nota is < 1 or > 5)
        {
            throw new RegraInvalidaException("A nota deve ser de 1 a 5.");
        }

        if (Status is not (StatusOrdemServico.Concluido or StatusOrdemServico.Entregue))
        {
            throw new ConflitoException("A avaliação só pode ser registrada depois da lavagem concluída.");
        }

        Avaliacao = (short)nota;
    }

    public void RegistrarPertencesEncontrados(string? texto)
    {
        if (Status is StatusOrdemServico.Entregue or StatusOrdemServico.Cancelada)
        {
            throw new ConflitoException("Esta ordem de serviço já foi encerrada; não aceita novos registros.");
        }

        PertencesEncontrados = texto;
    }
}
