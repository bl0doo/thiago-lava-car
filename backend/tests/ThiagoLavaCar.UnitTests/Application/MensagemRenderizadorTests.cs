using ThiagoLavaCar.Application.Services;

namespace ThiagoLavaCar.UnitTests.Application;

public class MensagemRenderizadorTests
{
    private const string Concluida = "Olá, {nome}! A lavagem do seu {modelo} (placa {placa}) ficou pronta e já pode ser retirada. Valor: {valor}. Obrigado pela preferência! {nome do lava-jato}";
    private const string Iniciada = "Olá, {nome}! Serviço: {serviço}. Previsão: {duração estimada}. {nome do lava-jato}";

    private static DadosDaMensagem Dados(decimal saldo) =>
        new("Renata Souza", "Chevrolet Onix", "ABC1D23", "Lavagem completa com cera", 90, saldo, "Thiago Lava Car");

    [Fact] // T31
    public void Com_saldo_pendente_mostra_a_linha_Valor_a_pagar()
    {
        var texto = MensagemRenderizador.Renderizar(Concluida, Dados(120m));

        Assert.Equal(
            "Olá, Renata! A lavagem do seu Chevrolet Onix (placa ABC1D23) ficou pronta e já pode ser retirada. Valor a pagar: R$ 120,00. Obrigado pela preferência! Thiago Lava Car",
            texto);
    }

    [Fact] // T32
    public void Sem_saldo_pendente_a_frase_do_valor_some()
    {
        var texto = MensagemRenderizador.Renderizar(Concluida, Dados(0m));

        Assert.DoesNotContain("Valor", texto);
        Assert.Equal(
            "Olá, Renata! A lavagem do seu Chevrolet Onix (placa ABC1D23) ficou pronta e já pode ser retirada. Obrigado pela preferência! Thiago Lava Car",
            texto);
    }

    [Fact]
    public void Troca_servico_e_duracao_em_horas_e_minutos()
    {
        var texto = MensagemRenderizador.Renderizar(Iniciada, Dados(0m));

        Assert.Equal("Olá, Renata! Serviço: Lavagem completa com cera. Previsão: 1h30. Thiago Lava Car", texto);
    }

    [Theory]
    [InlineData(45, "45 min")]
    [InlineData(60, "1h")]
    [InlineData(125, "2h05")]
    public void Formata_a_duracao(int minutos, string esperado) => Assert.Equal(esperado, MensagemRenderizador.Duracao(minutos));
}
