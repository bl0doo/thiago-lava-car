using System.Globalization;
using ThiagoLavaCar.Application.Abstractions;
using ThiagoLavaCar.Application.Common;
using ThiagoLavaCar.Application.DTOs;
using ThiagoLavaCar.Application.Exceptions;
using ThiagoLavaCar.Domain.Common;
using ThiagoLavaCar.Domain.Entities;
using ThiagoLavaCar.Domain.Enums;
using ThiagoLavaCar.Domain.Exceptions;

namespace ThiagoLavaCar.Application.Services;

public interface IPagamentoService
{
    Task<PagamentoRegistradoResponse> RegistrarAsync(int ordemServicoId, RegistrarPagamentoRequest request, CancellationToken ct);
    Task<IReadOnlyList<PagamentoResponse>> ListarAsync(DateOnly? data, CancellationToken ct);
    Task<PagamentoResponse> EstornarAsync(int id, string? motivo, CancellationToken ct);
    Task<IReadOnlyList<TaxaResponse>> ListarTaxasAsync(CancellationToken ct);
    Task<TaxaResponse> AlterarTaxaAsync(FormaPagamento forma, AlterarTaxaRequest request, CancellationToken ct);
}

public sealed class PagamentoService(
    IPagamentoRepository pagamentos,
    IOrdemServicoRepository ordens,
    ITaxaRepository taxas,
    ICaixaRepository caixa,
    Auditoria auditoria,
    IUnitOfWork unitOfWork,
    IUsuarioLogado usuarioLogado,
    TimeProvider relogio) : IPagamentoService
{
    private static readonly CultureInfo Pt = new("pt-BR");

    public async Task<PagamentoRegistradoResponse> RegistrarAsync(int ordemServicoId, RegistrarPagamentoRequest request, CancellationToken ct)
    {
        var os = await ordens.ObterPorIdAsync(ordemServicoId, ct) ?? throw new NaoEncontradoException("Ordem de serviço não encontrada.");
        if (os.Status == StatusOrdemServico.Cancelada)
        {
            throw new ConflitoException("Esta ordem de serviço foi cancelada e não recebe pagamento.");
        }

        var agora = relogio.GetUtcNow();
        var dataCaixa = FusoBrasilia.DataDe(agora);
        if (await caixa.ExisteFechamentoAsync(dataCaixa, ct))
        {
            throw new ConflitoException("O caixa de hoje já foi fechado.");
        }

        var valor = Dinheiro.Arredondar(request.ValorBruto);
        var saldo = os.ValorTotal - await pagamentos.SomarPagoAsync(os.Id, ct);
        if (valor > saldo)
        {
            throw new RegraInvalidaException($"Valor maior que o saldo pendente ({saldo.ToString("C2", Pt)}).");
        }

        var forma = Enum.Parse<FormaPagamento>(request.FormaPagamento);
        var taxa = await taxas.ObterPorFormaAsync(forma, ct) ?? throw new RegraInvalidaException("Taxa da forma de pagamento não cadastrada.");
        var pagamento = Pagamento.Criar(os.Id, usuarioLogado.Id, valor, taxa, dataCaixa, agora);

        await pagamentos.AdicionarAsync(pagamento, ct);
        await unitOfWork.SalvarAsync(ct);
        return new PagamentoRegistradoResponse(
            pagamento.Id, pagamento.ValorBruto, pagamento.PercentualTaxa, pagamento.ValorTaxa, pagamento.ValorLiquido, saldo - valor, dataCaixa);
    }

    public async Task<IReadOnlyList<PagamentoResponse>> ListarAsync(DateOnly? data, CancellationToken ct)
    {
        var dia = data ?? FusoBrasilia.DataDe(relogio.GetUtcNow());
        return (await pagamentos.ListarPorDataCaixaAsync(dia, ct)).Select(Mapear).ToList();
    }

    public async Task<PagamentoResponse> EstornarAsync(int id, string? motivo, CancellationToken ct)
    {
        var pagamento = await pagamentos.ObterPorIdAsync(id, ct) ?? throw new NaoEncontradoException("Pagamento não encontrado.");
        if (await caixa.ExisteFechamentoAsync(pagamento.DataCaixa, ct))
        {
            throw new ConflitoException("O caixa deste dia já foi fechado.");
        }

        pagamento.Estornar(relogio.GetUtcNow());
        await auditoria.RegistrarAsync("EstornoPagamento", nameof(Pagamento), pagamento.Id,
            new { pagamento.OrdemServicoId, pagamento.ValorBruto, pagamento.FormaPagamento, motivo = string.IsNullOrWhiteSpace(motivo) ? null : motivo.Trim() }, ct);
        await unitOfWork.SalvarAsync(ct);
        return Mapear(pagamento);
    }

    public async Task<IReadOnlyList<TaxaResponse>> ListarTaxasAsync(CancellationToken ct) =>
        (await taxas.ListarAsync(ct)).Select(Mapear).ToList();

    public async Task<TaxaResponse> AlterarTaxaAsync(FormaPagamento forma, AlterarTaxaRequest request, CancellationToken ct)
    {
        var taxa = await taxas.ObterPorFormaAsync(forma, ct) ?? throw new NaoEncontradoException("Taxa não encontrada.");
        var antes = new { taxa.Percentual, taxa.ValorFixo };
        taxa.Alterar(request.Percentual, request.ValorFixo);
        await auditoria.RegistrarAsync("AlteracaoTaxa", nameof(TaxaPagamento), taxa.Id,
            new { forma = forma.ToString(), antes, depois = new { taxa.Percentual, taxa.ValorFixo } }, ct);
        await unitOfWork.SalvarAsync(ct);
        return Mapear(taxa);
    }

    private static PagamentoResponse Mapear(Pagamento p) => new(
        p.Id, p.OrdemServicoId, p.FormaPagamento.ToString(), p.ValorBruto, p.PercentualTaxa, p.ValorTaxa,
        p.ValorLiquido, p.DataCaixa, p.CriadoEm, p.Estornado);

    private static TaxaResponse Mapear(TaxaPagamento t) => new(t.FormaPagamento.ToString(), t.Percentual, t.ValorFixo);
}
