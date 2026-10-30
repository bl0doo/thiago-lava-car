using ThiagoLavaCar.Domain.Common;
using ThiagoLavaCar.Domain.Enums;
using ThiagoLavaCar.Domain.Exceptions;

namespace ThiagoLavaCar.Domain.Entities;

public sealed class Servico : Entidade
{
    private readonly List<ServicoPreco> _precos = [];

    private Servico() { }

    public string Nome { get; private set; } = null!;
    public string Categoria { get; private set; } = null!;
    public int TempoEstimadoMin { get; private set; }
    public decimal CustoInsumos { get; private set; }
    public bool Ativo { get; private set; } = true;

    public IReadOnlyCollection<ServicoPreco> Precos => _precos;

    public static Servico Criar(string nome, string categoria, int tempoEstimadoMin, decimal custoInsumos)
    {
        if (tempoEstimadoMin <= 0)
        {
            throw new RegraInvalidaException("O tempo estimado deve ser maior que zero.");
        }

        if (custoInsumos < 0)
        {
            throw new RegraInvalidaException("O custo de insumos não pode ser negativo.");
        }

        return new Servico
        {
            Nome = nome,
            Categoria = categoria,
            TempoEstimadoMin = tempoEstimadoMin,
            CustoInsumos = Dinheiro.Arredondar(custoInsumos),
        };
    }

    public void DefinirPreco(TipoVeiculo tipo, decimal preco, decimal? precoComCera)
    {
        if (preco <= 0 || precoComCera is <= 0)
        {
            throw new RegraInvalidaException("O preço deve ser maior que zero.");
        }

        // Atualiza a linha existente em vez de apagar e recriar: a restrição única (servico, tipo) não admite as duas ao mesmo tempo.
        var atual = _precos.FirstOrDefault(p => p.TipoVeiculo == tipo);
        if (atual is null)
        {
            _precos.Add(ServicoPreco.Criar(tipo, preco, precoComCera));
        }
        else
        {
            atual.Atualizar(preco, precoComCera);
        }
    }

    /// <summary>Deixa a tabela de preços exatamente com os tipos informados.</summary>
    public void SubstituirPrecos(IReadOnlyCollection<(TipoVeiculo Tipo, decimal Preco, decimal? PrecoComCera)> novos)
    {
        foreach (var (tipo, preco, precoComCera) in novos)
        {
            DefinirPreco(tipo, preco, precoComCera);
        }

        _precos.RemoveAll(p => novos.All(n => n.Tipo != p.TipoVeiculo));
    }

    public void Atualizar(string nome, string categoria, int tempoEstimadoMin, decimal custoInsumos)
    {
        if (tempoEstimadoMin <= 0)
        {
            throw new RegraInvalidaException("O tempo estimado deve ser maior que zero.");
        }

        if (custoInsumos < 0)
        {
            throw new RegraInvalidaException("O custo de insumos não pode ser negativo.");
        }

        Nome = nome;
        Categoria = categoria;
        TempoEstimadoMin = tempoEstimadoMin;
        CustoInsumos = Dinheiro.Arredondar(custoInsumos);
    }

    public void DefinirAtivo(bool ativo) => Ativo = ativo;

    /// <summary>Preço vigente para o tipo de veículo; falha se o serviço não tem a opção pedida.</summary>
    public decimal ObterPreco(TipoVeiculo tipo, bool comCera)
    {
        var tabela = _precos.FirstOrDefault(p => p.TipoVeiculo == tipo)
            ?? throw new RegraInvalidaException($"O serviço \"{Nome}\" não tem preço para este tipo de veículo.");

        if (!comCera)
        {
            return tabela.Preco;
        }

        return tabela.PrecoComCera
            ?? throw new RegraInvalidaException($"O serviço \"{Nome}\" não tem opção com cera para este tipo de veículo.");
    }
}
