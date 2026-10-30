using ThiagoLavaCar.Application.DTOs;
using ThiagoLavaCar.Domain.Entities;
using ThiagoLavaCar.Domain.Enums;

namespace ThiagoLavaCar.Application.Mapping;

public static class CartaoMapper
{
    /// <summary>O Lavador nunca recebe valores nem dados de contato do cliente.</summary>
    public static CartaoResponse Mapear(OrdemServico os, Perfil perfil)
    {
        var verFinanceiro = perfil == Perfil.Dono;
        return new CartaoResponse(
            os.Id,
            os.Status.ToString(),
            os.Veiculo!.Placa,
            os.Veiculo.ModeloDescricao,
            os.Itens.Select(i => i.NomeServico).ToList(),
            os.DuracaoEstimadaMin,
            os.InicioEm,
            os.FimEm,
            os.DuracaoRealMin,
            os.RevisaoNecessaria,
            os.Lavador?.Apelido ?? os.Lavador?.Nome,
            verFinanceiro ? os.ValorTotal : null,
            verFinanceiro ? os.Cliente!.Nome : null,
            verFinanceiro ? os.Cliente!.Telefone : null);
    }
}
