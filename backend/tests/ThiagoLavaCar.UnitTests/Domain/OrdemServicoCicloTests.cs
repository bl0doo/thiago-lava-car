using ThiagoLavaCar.Domain.Entities;
using ThiagoLavaCar.Domain.Enums;
using ThiagoLavaCar.Domain.Exceptions;

namespace ThiagoLavaCar.UnitTests.Domain;

public class OrdemServicoCicloTests
{
    private static OrdemServico Concluida()
    {
        var os = Fabrica.OrdemAguardando();
        os.Iniciar(2, Fabrica.Agora);
        os.Encerrar(Fabrica.Agora.AddMinutes(60));
        return os;
    }

    [Fact]
    public void Cancelar_so_vale_em_Aguardando()
    {
        var aguardando = Fabrica.OrdemAguardando();
        aguardando.Cancelar();
        Assert.Equal(StatusOrdemServico.Cancelada, aguardando.Status);
        Assert.Throws<ConflitoException>(aguardando.Cancelar);

        var andamento = Fabrica.OrdemAguardando();
        andamento.Iniciar(2, Fabrica.Agora);
        Assert.Throws<ConflitoException>(andamento.Cancelar);
    }

    [Fact]
    public void Entregar_exige_Concluido_grava_a_hora_e_o_nome_de_quem_retira()
    {
        Assert.Throws<ConflitoException>(() => Fabrica.OrdemAguardando().Entregar(Fabrica.Agora, null));

        var os = Concluida();
        os.Entregar(Fabrica.Agora.AddMinutes(90), "Marido da cliente");

        Assert.Equal(StatusOrdemServico.Entregue, os.Status);
        Assert.Equal(Fabrica.Agora.AddMinutes(90), os.EntregueEm);
        Assert.Equal("Marido da cliente", os.NomeQuemRetira);
        Assert.Throws<ConflitoException>(() => os.Entregar(Fabrica.Agora, null));
    }

    [Fact]
    public void Entregue_nao_pode_mais_ser_desfeita()
    {
        var os = Concluida();
        os.Entregar(Fabrica.Agora.AddMinutes(61), null);

        Assert.Throws<ConflitoException>(() => os.Desfazer(Fabrica.Agora.AddMinutes(62)));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    public void Avaliacao_fora_de_1_a_5_e_rejeitada(int nota) =>
        Assert.Throws<RegraInvalidaException>(() => Concluida().Avaliar(nota));

    [Fact]
    public void Avaliacao_so_depois_da_lavagem_concluida()
    {
        Assert.Throws<ConflitoException>(() => Fabrica.OrdemAguardando().Avaliar(5));

        var os = Concluida();
        os.Avaliar(4);
        Assert.Equal((short)4, os.Avaliacao);
    }

    [Fact]
    public void Editar_recalcula_valor_e_duracao_e_so_vale_em_Aguardando()
    {
        var os = Fabrica.OrdemAguardando();
        var servico = Fabrica.Lavagem();

        os.Editar([ItemOrdemServico.Criar(servico, false, 80m, false), ItemOrdemServico.Criar(servico, true, 100m, false)], "Óculos", null);

        Assert.Equal(180m, os.ValorTotal);
        Assert.Equal(180, os.DuracaoEstimadaMin);
        Assert.Equal("Óculos", os.PertencesEntrada);
        Assert.Throws<RegraInvalidaException>(() => os.Editar([], null, null));

        os.Iniciar(2, Fabrica.Agora);
        Assert.Throws<ConflitoException>(() => os.Editar([ItemOrdemServico.Criar(servico, false, 80m, false)], null, null));
    }
}
