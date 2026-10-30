using ThiagoLavaCar.Application.Abstractions;
using ThiagoLavaCar.Application.DTOs;
using ThiagoLavaCar.Application.Exceptions;
using ThiagoLavaCar.Domain.Entities;
using ThiagoLavaCar.Domain.Enums;
using ThiagoLavaCar.Domain.Exceptions;

namespace ThiagoLavaCar.Application.Services;

public interface IServicoService
{
    Task<IReadOnlyList<ServicoResponse>> ListarAsync(CancellationToken ct);
    Task<ServicoResponse> CriarAsync(SalvarServicoRequest request, CancellationToken ct);
    Task<ServicoResponse> AtualizarAsync(int id, SalvarServicoRequest request, CancellationToken ct);
    Task<ServicoResponse> AlterarAtivoAsync(int id, bool ativo, CancellationToken ct);
    Task<PrecoCalculadoResponse> ObterPrecoAsync(int id, TipoVeiculo tipo, bool comCera, CancellationToken ct);
}

public sealed class ServicoService(IServicoRepository servicos, IUnitOfWork unitOfWork, IUsuarioLogado usuarioLogado) : IServicoService
{
    private bool EhDono => usuarioLogado.Perfil == Perfil.Dono;

    /// <summary>O Dono vê todo o catálogo; o Cliente só os serviços ativos, sem custos internos.</summary>
    public async Task<IReadOnlyList<ServicoResponse>> ListarAsync(CancellationToken ct) =>
        (await servicos.ListarAsync(somenteAtivos: !EhDono, ct)).Select(Mapear).ToList();

    public async Task<ServicoResponse> CriarAsync(SalvarServicoRequest request, CancellationToken ct)
    {
        await GarantirNomeLivreAsync(request.Nome.Trim(), null, ct);
        var servico = Servico.Criar(request.Nome.Trim(), request.Categoria.Trim(), request.TempoEstimadoMin, request.CustoInsumos);
        servico.SubstituirPrecos(Converter(request.Precos));
        await servicos.AdicionarAsync(servico, ct);
        await unitOfWork.SalvarAsync(ct);
        return Mapear(servico);
    }

    public async Task<ServicoResponse> AtualizarAsync(int id, SalvarServicoRequest request, CancellationToken ct)
    {
        var servico = await BuscarAsync(id, ct);
        await GarantirNomeLivreAsync(request.Nome.Trim(), id, ct);
        servico.Atualizar(request.Nome.Trim(), request.Categoria.Trim(), request.TempoEstimadoMin, request.CustoInsumos);
        servico.SubstituirPrecos(Converter(request.Precos));
        await unitOfWork.SalvarAsync(ct);
        return Mapear(servico);
    }

    public async Task<ServicoResponse> AlterarAtivoAsync(int id, bool ativo, CancellationToken ct)
    {
        var servico = await BuscarAsync(id, ct);
        servico.DefinirAtivo(ativo);
        await unitOfWork.SalvarAsync(ct);
        return Mapear(servico);
    }

    public async Task<PrecoCalculadoResponse> ObterPrecoAsync(int id, TipoVeiculo tipo, bool comCera, CancellationToken ct)
    {
        var servico = await BuscarAsync(id, ct);
        if (!servico.Ativo && !EhDono)
        {
            throw new NaoEncontradoException("Serviço não encontrado.");
        }

        return new PrecoCalculadoResponse(servico.Id, tipo.ToString(), comCera, servico.ObterPreco(tipo, comCera), servico.TempoEstimadoMin);
    }

    private async Task<Servico> BuscarAsync(int id, CancellationToken ct) =>
        await servicos.ObterPorIdAsync(id, ct) ?? throw new NaoEncontradoException("Serviço não encontrado.");

    private async Task GarantirNomeLivreAsync(string nome, int? idAtual, CancellationToken ct)
    {
        var existente = await servicos.ObterPorNomeAsync(nome, ct);
        if (existente is not null && existente.Id != idAtual)
        {
            throw new ConflitoException("Já existe um serviço com este nome.");
        }
    }

    private static List<(TipoVeiculo, decimal, decimal?)> Converter(IReadOnlyList<PrecoServicoDto> precos) =>
        precos.Select(p => (Enum.Parse<TipoVeiculo>(p.TipoVeiculo), p.Preco, p.PrecoComCera)).ToList();

    private ServicoResponse Mapear(Servico s) => new(
        s.Id, s.Nome, s.Categoria, s.TempoEstimadoMin, s.Ativo,
        EhDono ? s.CustoInsumos : null,
        s.Precos.OrderBy(p => p.TipoVeiculo).Select(p => new PrecoServicoDto(p.TipoVeiculo.ToString(), p.Preco, p.PrecoComCera)).ToList());
}
