using System.Globalization;
using ThiagoLavaCar.Application.Abstractions;
using ThiagoLavaCar.Application.Common;
using ThiagoLavaCar.Application.DTOs;
using ThiagoLavaCar.Application.Exceptions;
using ThiagoLavaCar.Domain.Common;
using ThiagoLavaCar.Domain.Entities;
using ThiagoLavaCar.Domain.Enums;

namespace ThiagoLavaCar.Application.Services;

public interface IDespesaService
{
    Task<DespesasResponse> ListarAsync(DateOnly? de, DateOnly? ate, CancellationToken ct);
    Task<DespesaResponse> CriarAsync(SalvarDespesaRequest request, CancellationToken ct);
    Task<DespesaResponse> EditarAsync(int id, SalvarDespesaRequest request, CancellationToken ct);
    Task ExcluirAsync(int id, CancellationToken ct);
}

public sealed class DespesaService(
    IDespesaRepository despesas,
    Auditoria auditoria,
    IUnitOfWork unitOfWork,
    IUsuarioLogado usuarioLogado,
    TimeProvider relogio) : IDespesaService
{
    public async Task<DespesasResponse> ListarAsync(DateOnly? de, DateOnly? ate, CancellationToken ct)
    {
        var (inicio, fim) = Periodo.Resolver(de, ate, relogio);
        var itens = (await despesas.ListarAsync(inicio, fim, ct)).Select(Mapear).ToList();
        return new DespesasResponse(itens, Dinheiro.Arredondar(itens.Sum(d => d.Valor)));
    }

    public async Task<DespesaResponse> CriarAsync(SalvarDespesaRequest request, CancellationToken ct)
    {
        var agora = relogio.GetUtcNow();
        var despesa = Despesa.Criar(usuarioLogado.Id, request.Descricao, Enum.Parse<CategoriaDespesa>(request.Categoria), request.Valor, request.Data, FusoBrasilia.DataDe(agora), agora);
        await despesas.AdicionarAsync(despesa, ct);
        await unitOfWork.SalvarAsync(ct);
        return Mapear(despesa);
    }

    public async Task<DespesaResponse> EditarAsync(int id, SalvarDespesaRequest request, CancellationToken ct)
    {
        var despesa = await ObterAsync(id, ct);
        despesa.Atualizar(request.Descricao, Enum.Parse<CategoriaDespesa>(request.Categoria), request.Valor, request.Data, FusoBrasilia.DataDe(relogio.GetUtcNow()));
        await unitOfWork.SalvarAsync(ct);
        return Mapear(despesa);
    }

    public async Task ExcluirAsync(int id, CancellationToken ct)
    {
        var despesa = await ObterAsync(id, ct);
        despesas.Remover(despesa);
        await auditoria.RegistrarAsync("ExclusaoDespesa", nameof(Despesa), despesa.Id, new
        {
            despesa.Descricao,
            categoria = despesa.Categoria.ToString(),
            despesa.Valor,
            data = despesa.Data.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
        }, ct);
        await unitOfWork.SalvarAsync(ct);
    }

    private async Task<Despesa> ObterAsync(int id, CancellationToken ct) =>
        await despesas.ObterPorIdAsync(id, ct) ?? throw new NaoEncontradoException("Despesa não encontrada.");

    private static DespesaResponse Mapear(Despesa d) => new(d.Id, d.Descricao, d.Categoria.ToString(), d.Valor, d.Data);
}

