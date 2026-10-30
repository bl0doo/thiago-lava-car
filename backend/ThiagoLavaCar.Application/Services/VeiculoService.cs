using ThiagoLavaCar.Application.Abstractions;
using ThiagoLavaCar.Application.DTOs;
using ThiagoLavaCar.Application.Exceptions;
using ThiagoLavaCar.Domain.Common;
using ThiagoLavaCar.Domain.Entities;
using ThiagoLavaCar.Domain.Enums;
using ThiagoLavaCar.Domain.Exceptions;

namespace ThiagoLavaCar.Application.Services;

public interface IVeiculoService
{
    Task<VeiculoResponse> BuscarPorPlacaAsync(string placa, CancellationToken ct);
    Task<VeiculoResponse> CriarAsync(CriarVeiculoRequest request, CancellationToken ct);
    Task<VeiculoResponse> AtualizarAsync(int id, AtualizarVeiculoRequest request, CancellationToken ct);
    Task<FichaVeiculoResponse> ObterFichaAsync(int id, CancellationToken ct);
    Task<IReadOnlyList<ModeloVeiculoResponse>> BuscarModelosAsync(string? busca, CancellationToken ct);
}

public sealed class VeiculoService(
    IVeiculoRepository veiculos,
    IClienteRepository clientes,
    IModeloVeiculoRepository modelos,
    IUnitOfWork unitOfWork,
    IUsuarioLogado usuarioLogado) : IVeiculoService
{
    private const int LimiteDeModelos = 20;

    public async Task<VeiculoResponse> BuscarPorPlacaAsync(string placa, CancellationToken ct)
    {
        var veiculo = await veiculos.ObterPorPlacaAsync(PlacaBrasileira.Normalizar(placa), ct)
            ?? throw new NaoEncontradoException("Veículo não encontrado.");
        return Mapear(veiculo);
    }

    public async Task<VeiculoResponse> CriarAsync(CriarVeiculoRequest request, CancellationToken ct)
    {
        // O cliente só cadastra veículo para si: o clienteId do corpo é ignorado e vem do token.
        var clienteId = usuarioLogado.Perfil == Perfil.Cliente
            ? await clientes.ObterIdPorUsuarioAsync(usuarioLogado.Id, ct) ?? throw new NaoEncontradoException("Cliente não encontrado.")
            : await ResolverClienteDoDonoAsync(request.ClienteId, ct);

        var placa = PlacaBrasileira.Normalizar(request.Placa);
        await GarantirPlacaLivreAsync(placa, null, ct);

        var modelo = request.ModeloVeiculoId is { } modeloId
            ? await modelos.ObterPorIdAsync(modeloId, ct) ?? throw new NaoEncontradoException("Modelo de veículo não encontrado.")
            : null;
        var descricao = string.IsNullOrWhiteSpace(request.ModeloDescricao) ? modelo!.Descricao : request.ModeloDescricao.Trim();
        var tipo = request.TipoVeiculo is null ? modelo!.TipoVeiculo : Enum.Parse<TipoVeiculo>(request.TipoVeiculo);

        var veiculo = Veiculo.Criar(clienteId, placa, descricao, tipo, LimparCor(request.Cor), modelo?.Id);
        await veiculos.AdicionarAsync(veiculo, ct);
        await unitOfWork.SalvarAsync(ct);
        return Mapear(veiculo);
    }

    public async Task<IReadOnlyList<ModeloVeiculoResponse>> BuscarModelosAsync(string? busca, CancellationToken ct) =>
        (await modelos.BuscarAsync(busca, LimiteDeModelos, ct))
            .Select(m => new ModeloVeiculoResponse(m.Id, m.Marca, m.Nome, m.Descricao, m.TipoVeiculo.ToString()))
            .ToList();

    public async Task<VeiculoResponse> AtualizarAsync(int id, AtualizarVeiculoRequest request, CancellationToken ct)
    {
        var veiculo = await BuscarAutorizadoAsync(id, ct);
        var placa = PlacaBrasileira.Normalizar(request.Placa);
        if (placa != veiculo.Placa)
        {
            await GarantirPlacaLivreAsync(placa, veiculo.Id, ct);
        }

        veiculo.Atualizar(placa, request.ModeloDescricao.Trim(), Enum.Parse<TipoVeiculo>(request.TipoVeiculo), LimparCor(request.Cor));
        await unitOfWork.SalvarAsync(ct);
        return Mapear(veiculo);
    }

    public async Task<FichaVeiculoResponse> ObterFichaAsync(int id, CancellationToken ct)
    {
        var veiculo = await BuscarAutorizadoAsync(id, ct);
        var ordens = await veiculos.ListarOrdensAsync(id, ct);
        var historico = ordens
            .Select(o => new FichaOrdemResponse(o.Id, o.CriadoEm, o.Status.ToString(),
                o.Itens.Select(i => i.NomeServico).ToList(), o.ValorTotal, o.DuracaoRealMin))
            .ToList();
        return new FichaVeiculoResponse(Mapear(veiculo), ordens.Count(o => o.Status is StatusOrdemServico.Concluido or StatusOrdemServico.Entregue), historico);
    }

    /// <summary>Veículo de outro cliente responde 404, igual a inexistente, para não revelar que existe.</summary>
    private async Task<Veiculo> BuscarAutorizadoAsync(int id, CancellationToken ct)
    {
        var veiculo = await veiculos.ObterPorIdAsync(id, ct) ?? throw new NaoEncontradoException("Veículo não encontrado.");
        if (usuarioLogado.Perfil == Perfil.Cliente
            && await clientes.ObterIdPorUsuarioAsync(usuarioLogado.Id, ct) != veiculo.ClienteId)
        {
            throw new NaoEncontradoException("Veículo não encontrado.");
        }

        return veiculo;
    }

    private async Task<int> ResolverClienteDoDonoAsync(int? clienteId, CancellationToken ct)
    {
        if (clienteId is null)
        {
            throw new RegraInvalidaException("Informe o cliente do veículo.");
        }

        return (await clientes.ObterPorIdAsync(clienteId.Value, ct) ?? throw new NaoEncontradoException("Cliente não encontrado.")).Id;
    }

    private async Task GarantirPlacaLivreAsync(string placa, int? idAtual, CancellationToken ct)
    {
        var existente = await veiculos.ObterPorPlacaAsync(placa, ct);
        if (existente is not null && existente.Id != idAtual)
        {
            throw new ConflitoException("Placa já cadastrada.");
        }
    }

    private static string? LimparCor(string? cor) => string.IsNullOrWhiteSpace(cor) ? null : cor.Trim();

    private static VeiculoResponse Mapear(Veiculo v) => new(v.Id, v.ClienteId, v.Placa, v.ModeloDescricao, v.Tipo.ToString(), v.Cor);
}
