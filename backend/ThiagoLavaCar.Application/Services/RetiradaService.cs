using System.Globalization;
using ThiagoLavaCar.Application.Abstractions;
using ThiagoLavaCar.Application.Common;
using ThiagoLavaCar.Application.DTOs;
using ThiagoLavaCar.Application.Exceptions;
using ThiagoLavaCar.Domain.Common;
using ThiagoLavaCar.Domain.Entities;

namespace ThiagoLavaCar.Application.Services;

public interface IRetiradaService
{
    Task<RetiradasResponse> ListarAsync(DateOnly? de, DateOnly? ate, CancellationToken ct);
    Task<RetiradaResponse> RegistrarAsync(RegistrarRetiradaRequest request, CancellationToken ct);
    Task ExcluirAsync(int id, CancellationToken ct);
}

public sealed class RetiradaService(
    IRetiradaRepository retiradas,
    Auditoria auditoria,
    IUnitOfWork unitOfWork,
    IUsuarioLogado usuarioLogado,
    TimeProvider relogio) : IRetiradaService
{
    public async Task<RetiradasResponse> ListarAsync(DateOnly? de, DateOnly? ate, CancellationToken ct)
    {
        var (inicio, fim) = Periodo.Resolver(de, ate, relogio);
        var itens = (await retiradas.ListarAsync(inicio, fim, ct)).Select(Mapear).ToList();
        return new RetiradasResponse(itens, Dinheiro.Arredondar(itens.Sum(r => r.Valor)));
    }

    public async Task<RetiradaResponse> RegistrarAsync(RegistrarRetiradaRequest request, CancellationToken ct)
    {
        var retirada = RetiradaDono.Criar(usuarioLogado.Id, request.Valor, request.Data, FusoBrasilia.DataDe(relogio.GetUtcNow()), request.Observacao);
        await retiradas.AdicionarAsync(retirada, ct);
        await unitOfWork.SalvarAsync(ct);
        return Mapear(retirada);
    }

    public async Task ExcluirAsync(int id, CancellationToken ct)
    {
        var retirada = await retiradas.ObterPorIdAsync(id, ct) ?? throw new NaoEncontradoException("Retirada não encontrada.");
        retiradas.Remover(retirada);
        await auditoria.RegistrarAsync("ExclusaoRetirada", nameof(RetiradaDono), retirada.Id, new
        {
            retirada.Valor,
            data = retirada.Data.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            retirada.Observacao,
        }, ct);
        await unitOfWork.SalvarAsync(ct);
    }

    private static RetiradaResponse Mapear(RetiradaDono r) => new(r.Id, r.Valor, r.Data, r.Observacao);
}
