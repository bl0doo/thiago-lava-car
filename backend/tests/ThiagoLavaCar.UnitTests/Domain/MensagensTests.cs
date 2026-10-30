using ThiagoLavaCar.Domain.Entities;
using ThiagoLavaCar.Domain.Enums;
using ThiagoLavaCar.Domain.Exceptions;

namespace ThiagoLavaCar.UnitTests.Domain;

public sealed class MensagensTests
{
    [Fact]
    public void Modelo_guarda_o_texto_aparado_e_a_hora_da_edicao()
    {
        var m = ModeloMensagem.Criar(TipoMensagem.LavagemIniciada, "  Olá, {nome}!  ", Fabrica.Agora);

        Assert.Equal("Olá, {nome}!", m.Texto);
        Assert.Equal(Fabrica.Agora, m.AtualizadoEm);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Modelo_vazio_e_recusado(string texto) =>
        Assert.Throws<RegraInvalidaException>(() => ModeloMensagem.Criar(TipoMensagem.LavagemConcluida, texto, Fabrica.Agora));

    [Fact]
    public void Modelo_acima_de_500_caracteres_e_recusado() =>
        Assert.Throws<RegraInvalidaException>(() => ModeloMensagem.Criar(TipoMensagem.LavagemConcluida, new string('a', 501), Fabrica.Agora));

    [Fact]
    public void Mensagem_enviada_sem_texto_e_recusada() =>
        Assert.Throws<RegraInvalidaException>(() => MensagemEnviada.Registrar(1, 1, TipoMensagem.LavagemIniciada, " ", Fabrica.Agora));
}
