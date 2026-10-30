using ThiagoLavaCar.Domain.Entities;
using ThiagoLavaCar.Domain.Enums;
using ThiagoLavaCar.Domain.Exceptions;

namespace ThiagoLavaCar.UnitTests.Domain;

public class OrdemServicoTests
{
    private static readonly DateTimeOffset Inicio = new(2026, 10, 20, 13, 5, 0, TimeSpan.Zero);

    [Fact] // T22
    public void Registrar_com_aceite_e_uma_avaria_cria_cartao_em_Aguardando()
    {
        var servico = Fabrica.Lavagem();
        var os = OrdemServico.Registrar(
            Fabrica.Onix(), 1,
            [ItemOrdemServico.Criar(servico, true, 100m, false)],
            semAvarias: false, avarias: [Avaria.Criar("Para-choque traseiro", "Risco", "Risco de 5 cm")],
            aceiteCliente: true, "Cadeirinha infantil", null, Inicio);

        Assert.Equal(StatusOrdemServico.Aguardando, os.Status);
        Assert.Single(os.Avarias);
        Assert.Equal(100m, os.ValorTotal);
        Assert.Equal(90, os.DuracaoEstimadaMin);
        Assert.Equal(30, os.VeiculoId);
        Assert.Equal(10, os.ClienteId);
    }

    [Fact] // T23
    public void Registrar_sem_aceite_do_cliente_falha()
    {
        var servico = Fabrica.Lavagem();
        Assert.Throws<RegraInvalidaException>(() => OrdemServico.Registrar(
            Fabrica.Onix(), 1, [ItemOrdemServico.Criar(servico, false, 80m, false)],
            true, [], aceiteCliente: false, null, null, Inicio));
    }

    [Theory] // T24
    [InlineData(false)] // semAvarias = false e lista vazia
    [InlineData(true)] // semAvarias = true mas com avaria listada
    public void Registrar_com_vistoria_inconsistente_falha(bool semAvarias)
    {
        var servico = Fabrica.Lavagem();
        Avaria[] avarias = semAvarias ? [Avaria.Criar("Porta", "Amassado", null)] : [];
        Assert.Throws<RegraInvalidaException>(() => OrdemServico.Registrar(
            Fabrica.Onix(), 1, [ItemOrdemServico.Criar(servico, false, 80m, false)],
            semAvarias, avarias, true, null, null, Inicio));
    }

    [Fact]
    public void Registrar_sem_servicos_falha()
    {
        Assert.Throws<RegraInvalidaException>(() => OrdemServico.Registrar(
            Fabrica.Onix(), 1, [], true, [], true, null, null, Inicio));
    }

    [Fact] // T26
    public void Iniciar_em_Aguardando_grava_hora_do_servidor_e_lavador()
    {
        var os = Fabrica.OrdemAguardando();

        os.Iniciar(lavadorId: 2, Inicio);

        Assert.Equal(StatusOrdemServico.EmAndamento, os.Status);
        Assert.Equal(Inicio, os.InicioEm);
        Assert.Equal(2, os.LavadorId);
    }

    [Fact]
    public void Iniciar_duas_vezes_falha_com_conflito()
    {
        var os = Fabrica.OrdemAguardando();
        os.Iniciar(2, Inicio);

        var erro = Assert.Throws<ConflitoException>(() => os.Iniciar(2, Inicio));
        Assert.Equal("Esta lavagem já foi iniciada.", erro.Message);
    }

    [Fact] // T27
    public void Encerrar_em_Aguardando_falha()
    {
        var os = Fabrica.OrdemAguardando();

        var erro = Assert.Throws<ConflitoException>(() => os.Encerrar(Inicio));
        Assert.Equal("A lavagem ainda não foi iniciada.", erro.Message);
    }

    [Fact] // T28
    public void Iniciar_as_1305_e_encerrar_as_1447_dura_102_minutos()
    {
        var os = Fabrica.OrdemAguardando();
        os.Iniciar(2, Inicio);

        os.Encerrar(Inicio.AddMinutes(102));

        Assert.Equal(StatusOrdemServico.Concluido, os.Status);
        Assert.Equal(102, os.DuracaoRealMin);
        Assert.False(os.RevisaoNecessaria);
    }

    [Fact]
    public void Duracao_acima_de_8_horas_fica_marcada_para_revisao()
    {
        var os = Fabrica.OrdemAguardando();
        os.Iniciar(2, Inicio);

        os.Encerrar(Inicio.AddHours(8).AddMinutes(1));

        Assert.True(os.RevisaoNecessaria);
    }

    [Fact]
    public void Encerrar_duas_vezes_falha()
    {
        var os = Fabrica.OrdemAguardando();
        os.Iniciar(2, Inicio);
        os.Encerrar(Inicio.AddMinutes(30));

        Assert.Throws<ConflitoException>(() => os.Encerrar(Inicio.AddMinutes(31)));
    }

    [Fact] // T29
    public void Desfazer_1_minuto_apos_iniciar_volta_para_Aguardando_e_apaga_o_horario()
    {
        var os = Fabrica.OrdemAguardando();
        os.Iniciar(2, Inicio);

        os.Desfazer(Inicio.AddMinutes(1));

        Assert.Equal(StatusOrdemServico.Aguardando, os.Status);
        Assert.Null(os.InicioEm);
        Assert.Null(os.LavadorId);
    }

    [Fact] // T30
    public void Desfazer_3_minutos_apos_iniciar_falha()
    {
        var os = Fabrica.OrdemAguardando();
        os.Iniciar(2, Inicio);

        Assert.Throws<ConflitoException>(() => os.Desfazer(Inicio.AddMinutes(3)));
        Assert.Equal(StatusOrdemServico.EmAndamento, os.Status);
    }

    [Fact]
    public void Desfazer_no_limite_exato_de_2_minutos_ainda_vale()
    {
        var os = Fabrica.OrdemAguardando();
        os.Iniciar(2, Inicio);

        os.Desfazer(Inicio.AddMinutes(2));

        Assert.Equal(StatusOrdemServico.Aguardando, os.Status);
    }

    [Fact]
    public void Desfazer_apos_encerrar_volta_para_EmAndamento_e_apaga_o_fim()
    {
        var os = Fabrica.OrdemAguardando();
        os.Iniciar(2, Inicio);
        os.Encerrar(Inicio.AddMinutes(60));

        os.Desfazer(Inicio.AddMinutes(61));

        Assert.Equal(StatusOrdemServico.EmAndamento, os.Status);
        Assert.Null(os.FimEm);
        Assert.Null(os.DuracaoRealMin);
        Assert.Equal(2, os.LavadorId);
    }

    [Fact]
    public void Desfazer_sem_nenhuma_mudanca_falha()
    {
        var os = Fabrica.OrdemAguardando();

        Assert.Throws<ConflitoException>(() => os.Desfazer(Inicio));
    }
}
