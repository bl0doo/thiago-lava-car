using ThiagoLavaCar.Application.Abstractions;
using ThiagoLavaCar.Application.DTOs;
using ThiagoLavaCar.Application.Exceptions;
using ThiagoLavaCar.Application.Mapping;
using ThiagoLavaCar.Domain.Common;

namespace ThiagoLavaCar.Application.Services;

public interface IOrdemDetalheService
{
    Task<DetalheOrdemResponse> ObterAsync(int id, CancellationToken ct);
}

public sealed class OrdemDetalheService(IOrdemServicoRepository ordens, IPagamentoRepository pagamentos, IUsuarioLogado usuarioLogado) : IOrdemDetalheService
{
    public async Task<DetalheOrdemResponse> ObterAsync(int id, CancellationToken ct)
    {
        var os = await ordens.ObterComAvariasAsync(id, ct) ?? throw new NaoEncontradoException("Ordem de serviço não encontrada.");
        var lista = await pagamentos.ListarPorOrdemAsync(id, ct);
        var pago = Dinheiro.Arredondar(lista.Where(p => !p.Estornado).Sum(p => p.ValorBruto));

        return new DetalheOrdemResponse(
            CartaoMapper.Mapear(os, usuarioLogado.Perfil),
            os.Itens.Select(i => new ItemDetalheResponse(i.ServicoId, i.NomeServico, i.ComCera, i.PrecoFinal)).ToList(),
            os.PertencesEntrada,
            os.PertencesEncontrados,
            os.NomeQuemRetira,
            os.SemAvarias,
            os.Avarias.Select(a => new AvariaDetalheResponse(a.Local, a.Tipo, a.Descricao)).ToList(),
            os.Avaliacao,
            pago,
            Math.Max(0m, os.ValorTotal - pago),
            lista.Select(p => new PagamentoResponse(
                p.Id, p.OrdemServicoId, p.FormaPagamento.ToString(), p.ValorBruto, p.PercentualTaxa, p.ValorTaxa,
                p.ValorLiquido, p.DataCaixa, p.CriadoEm, p.Estornado)).ToList());
    }
}
