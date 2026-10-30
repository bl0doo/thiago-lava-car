using ThiagoLavaCar.Domain.Common;
using ThiagoLavaCar.Domain.Enums;

namespace ThiagoLavaCar.Domain.Entities;

public sealed class ServicoPreco : Entidade
{
    private ServicoPreco() { }

    public int ServicoId { get; private set; }
    public TipoVeiculo TipoVeiculo { get; private set; }
    public decimal Preco { get; private set; }
    public decimal? PrecoComCera { get; private set; }

    internal static ServicoPreco Criar(TipoVeiculo tipo, decimal preco, decimal? precoComCera) =>
        new()
        {
            TipoVeiculo = tipo,
            Preco = Dinheiro.Arredondar(preco),
            PrecoComCera = precoComCera is null ? null : Dinheiro.Arredondar(precoComCera.Value),
        };

    internal void Atualizar(decimal preco, decimal? precoComCera)
    {
        Preco = Dinheiro.Arredondar(preco);
        PrecoComCera = precoComCera is null ? null : Dinheiro.Arredondar(precoComCera.Value);
    }
}
