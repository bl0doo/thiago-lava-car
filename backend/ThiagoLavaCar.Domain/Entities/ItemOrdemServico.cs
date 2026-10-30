using ThiagoLavaCar.Domain.Common;

namespace ThiagoLavaCar.Domain.Entities;

/// <summary>Serviço dentro da OS, com preço, custo e tempo copiados no momento da venda (histórico imutável).</summary>
public sealed class ItemOrdemServico : Entidade
{
    private ItemOrdemServico() { }

    public int OrdemServicoId { get; private set; }
    public int ServicoId { get; private set; }
    public string NomeServico { get; private set; } = null!;
    public bool ComCera { get; private set; }
    public decimal PrecoFinal { get; private set; }
    public bool PrecoAjustado { get; private set; }
    public decimal CustoInsumos { get; private set; }
    public int TempoEstimadoMin { get; private set; }

    public static ItemOrdemServico Criar(Servico servico, bool comCera, decimal precoFinal, bool precoAjustado) =>
        new()
        {
            ServicoId = servico.Id,
            NomeServico = servico.Nome,
            ComCera = comCera,
            PrecoFinal = Dinheiro.Arredondar(precoFinal),
            PrecoAjustado = precoAjustado,
            CustoInsumos = servico.CustoInsumos,
            TempoEstimadoMin = servico.TempoEstimadoMin,
        };
}
