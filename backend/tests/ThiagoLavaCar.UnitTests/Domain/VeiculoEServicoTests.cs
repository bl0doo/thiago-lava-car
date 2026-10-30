using ThiagoLavaCar.Domain.Common;
using ThiagoLavaCar.Domain.Entities;
using ThiagoLavaCar.Domain.Enums;
using ThiagoLavaCar.Domain.Exceptions;

namespace ThiagoLavaCar.UnitTests.Domain;

public class VeiculoEServicoTests
{
    [Theory] // T16
    [InlineData("abc-1d23", "ABC1D23")]
    [InlineData("ABC1234", "ABC1234")]
    [InlineData(" abc 1234 ", "ABC1234")]
    public void Placa_valida_e_normalizada(string entrada, string esperado)
    {
        var veiculo = Veiculo.Criar(1, entrada, "Onix", TipoVeiculo.Carro, null);

        Assert.Equal(esperado, veiculo.Placa);
    }

    [Theory] // T17
    [InlineData("AB12345")]
    [InlineData("ABCD123")]
    [InlineData("")]
    [InlineData(null)]
    public void Placa_invalida_falha(string? entrada)
    {
        Assert.False(PlacaBrasileira.EhValida(entrada));
        Assert.Throws<RegraInvalidaException>(() => Veiculo.Criar(1, entrada!, "Onix", TipoVeiculo.Carro, null));
    }

    [Fact] // T19
    public void Preco_de_carro_com_cera_vem_da_tabela_do_tipo()
    {
        Assert.Equal(100m, Fabrica.Lavagem().ObterPreco(TipoVeiculo.Carro, comCera: true));
        Assert.Equal(80m, Fabrica.Lavagem().ObterPreco(TipoVeiculo.Carro, comCera: false));
    }

    [Fact] // T20
    public void Servico_sem_preco_com_cera_falha_quando_a_cera_e_pedida()
    {
        var erro = Assert.Throws<RegraInvalidaException>(() => Fabrica.Lavagem().ObterPreco(TipoVeiculo.Moto, comCera: true));
        Assert.Contains("cera", erro.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Servico_sem_preco_para_o_tipo_de_veiculo_falha()
    {
        Assert.Throws<RegraInvalidaException>(() => Fabrica.Lavagem().ObterPreco(TipoVeiculo.Caminhonete, comCera: false));
    }

    [Fact] // T21
    public void Item_da_OS_guarda_o_preco_da_venda_mesmo_se_a_tabela_mudar_depois()
    {
        var servico = Servico.Criar("Lavagem", "Lavagem", 60, 5m).ComId(1);
        servico.DefinirPreco(TipoVeiculo.Carro, 80m, null);
        var item = ItemOrdemServico.Criar(servico, false, servico.ObterPreco(TipoVeiculo.Carro, false), false);

        servico.DefinirPreco(TipoVeiculo.Carro, 90m, null);

        Assert.Equal(80m, item.PrecoFinal);
        Assert.Equal(90m, servico.ObterPreco(TipoVeiculo.Carro, false));
    }

    [Fact]
    public void Cliente_sem_consentimento_lgpd_nao_e_criado()
    {
        Assert.Throws<RegraInvalidaException>(() =>
            Cliente.Criar("Renata", "44999998888", null, CanalContato.WhatsApp, consentimentoLgpd: false, Fabrica.Agora));
    }
}
