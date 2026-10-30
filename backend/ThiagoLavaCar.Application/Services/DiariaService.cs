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

public interface IDiariaService
{
    Task<DiariasResponse> ListarAsync(string? mes, CancellationToken ct);
    Task<DiariaResponse> LancarAsync(LancarDiariaRequest request, CancellationToken ct);
    Task<DiariasDoDiaResponse> LancarDoDiaAsync(DateOnly data, LancarDiariasDoDiaRequest request, CancellationToken ct);
    Task<DiariaResponse> EditarAsync(int id, EditarDiariaRequest request, CancellationToken ct);
    Task ExcluirAsync(int id, CancellationToken ct);
}

public sealed class DiariaService(
    IDiariaRepository diarias,
    IUsuarioRepository usuarios,
    IConfiguracaoRepository configuracao,
    Auditoria auditoria,
    IUnitOfWork unitOfWork,
    TimeProvider relogio) : IDiariaService
{
    public async Task<DiariasResponse> ListarAsync(string? mes, CancellationToken ct)
    {
        var primeiro = ResolverMes(mes);
        var itens = (await diarias.ListarAsync(primeiro, primeiro.AddMonths(1).AddDays(-1), ct)).Select(Mapear).ToList();
        return new DiariasResponse(itens, Dinheiro.Arredondar(itens.Sum(d => d.Valor)));
    }

    public async Task<DiariaResponse> LancarAsync(LancarDiariaRequest request, CancellationToken ct)
    {
        GarantirNaoFutura(request.Data);
        await GarantirLavadorAsync(request.LavadorId, ct);
        if ((await diarias.LavadoresComDiariaAsync(request.Data, ct)).Contains(request.LavadorId))
        {
            throw new ConflitoException("Este lavador já tem diária lançada neste dia.");
        }

        var diaria = Diaria.Criar(request.LavadorId, request.Data, await ValorAsync(request.Valor, request.Data, ct));
        await diarias.AdicionarAsync(diaria, ct);
        await unitOfWork.SalvarAsync(ct);
        return Mapear(diaria);
    }

    public async Task<DiariasDoDiaResponse> LancarDoDiaAsync(DateOnly data, LancarDiariasDoDiaRequest request, CancellationToken ct)
    {
        GarantirNaoFutura(data);
        var escolhidos = request.LavadorIds.Distinct().ToList();
        foreach (var lavadorId in escolhidos)
        {
            await GarantirLavadorAsync(lavadorId, ct);
        }

        var existentes = await diarias.LavadoresComDiariaAsync(data, ct);
        var valor = await ValorAsync(request.Valor, data, ct);
        var criadas = new List<Diaria>();
        foreach (var lavadorId in escolhidos.Where(id => !existentes.Contains(id)))
        {
            var diaria = Diaria.Criar(lavadorId, data, valor);
            await diarias.AdicionarAsync(diaria, ct);
            criadas.Add(diaria);
        }

        await unitOfWork.SalvarAsync(ct);
        return new DiariasDoDiaResponse(criadas.Select(Mapear).ToList(), escolhidos.Where(existentes.Contains).ToList());
    }

    public async Task<DiariaResponse> EditarAsync(int id, EditarDiariaRequest request, CancellationToken ct)
    {
        var diaria = await ObterAsync(id, ct);
        if (diaria.Paga && request.Valor != diaria.Valor)
        {
            throw new ConflitoException("Esta diária já foi paga; o valor não pode mais ser alterado.");
        }

        var valorAnterior = diaria.Valor;
        diaria.Reajustar(request.Valor);
        if (request.Paga == true)
        {
            diaria.MarcarPaga();
        }

        if (diaria.Valor != valorAnterior)
        {
            await auditoria.RegistrarAsync("AjusteDiaria", nameof(Diaria), diaria.Id, new { valorAnterior, valorNovo = diaria.Valor }, ct);
        }

        await unitOfWork.SalvarAsync(ct);
        return Mapear(diaria);
    }

    public async Task ExcluirAsync(int id, CancellationToken ct)
    {
        var diaria = await ObterAsync(id, ct);
        if (diaria.Paga)
        {
            throw new ConflitoException("Esta diária já foi paga e não pode ser excluída.");
        }

        diarias.Remover(diaria);
        await auditoria.RegistrarAsync("ExclusaoDiaria", nameof(Diaria), diaria.Id, new
        {
            diaria.LavadorId,
            data = diaria.Data.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            diaria.Valor,
        }, ct);
        await unitOfWork.SalvarAsync(ct);
    }

    private async Task<Diaria> ObterAsync(int id, CancellationToken ct) =>
        await diarias.ObterPorIdAsync(id, ct) ?? throw new NaoEncontradoException("Diária não encontrada.");

    private void GarantirNaoFutura(DateOnly data)
    {
        if (data > FusoBrasilia.DataDe(relogio.GetUtcNow()))
        {
            throw new RegraInvalidaException("Não é possível lançar diária de uma data futura.");
        }
    }

    private async Task GarantirLavadorAsync(int lavadorId, CancellationToken ct)
    {
        var usuario = await usuarios.ObterPorIdAsync(lavadorId, ct);
        if (usuario is not { Perfil: Perfil.Lavador, Ativo: true })
        {
            throw new RegraInvalidaException($"O lavador {lavadorId} não existe ou está inativo.");
        }
    }

    /// <summary>O valor informado vale; sem ele, usa a diária padrão da configuração (R$ 100 na semana, R$ 120 aos sábados).</summary>
    private async Task<decimal> ValorAsync(decimal? informado, DateOnly data, CancellationToken ct)
    {
        if (informado is not null)
        {
            return informado.Value;
        }

        var config = await configuracao.ObterAsync(ct)
            ?? throw new RegraInvalidaException("Informe o valor da diária: a configuração do lava-jato ainda não tem diária padrão.");
        return config.DiariaSugerida(data);
    }

    private DateOnly ResolverMes(string? mes)
    {
        if (string.IsNullOrWhiteSpace(mes))
        {
            var hoje = FusoBrasilia.DataDe(relogio.GetUtcNow());
            return new DateOnly(hoje.Year, hoje.Month, 1);
        }

        if (!DateOnly.TryParseExact(mes + "-01", "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var primeiro))
        {
            throw new RegraInvalidaException("Mês inválido. Use o formato AAAA-MM, por exemplo 2026-10.");
        }

        return primeiro;
    }

    private static DiariaResponse Mapear(Diaria d) => new(d.Id, d.LavadorId, d.Data, d.Valor, d.Paga);
}
