using ThiagoLavaCar.Domain.Entities;
using ThiagoLavaCar.Domain.Enums;
using ThiagoLavaCar.Domain.Exceptions;

namespace ThiagoLavaCar.UnitTests.Domain;

public sealed class AgendamentoTests
{
    private static Agendamento Novo(bool comCera = true) =>
        Agendamento.Criar(1, 2, Fabrica.Lavagem(), TipoVeiculo.Carro, comCera, Fabrica.Agora.AddDays(1), Fabrica.Agora);

    [Fact]
    public void Criar_copia_preco_e_duracao_do_servico()
    {
        var a = Novo();

        Assert.Equal(100m, a.PrecoEstimado);
        Assert.Equal(90, a.DuracaoEstimadaMin);
        Assert.Equal(StatusAgendamento.Agendado, a.Status);
    }

    [Fact]
    public void Horario_no_passado_e_recusado() =>
        Assert.Throws<RegraInvalidaException>(() =>
            Agendamento.Criar(1, 2, Fabrica.Lavagem(), TipoVeiculo.Carro, false, Fabrica.Agora.AddMinutes(-1), Fabrica.Agora));

    [Fact]
    public void Cancelado_nao_pode_ser_atendido()
    {
        var a = Novo();
        a.Cancelar();

        Assert.Throws<ConflitoException>(a.MarcarAtendido);
    }
}
