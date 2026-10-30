using ThiagoLavaCar.Application.Abstractions;
using ThiagoLavaCar.Application.DTOs;
using ThiagoLavaCar.Application.Exceptions;
using ThiagoLavaCar.Domain.Entities;
using ThiagoLavaCar.Domain.Enums;

namespace ThiagoLavaCar.Application.Services;

public interface IClienteService
{
    Task<PaginaResponse<ClienteResponse>> BuscarAsync(string? busca, int pagina, int tamanho, CancellationToken ct);
    Task<ClienteDetalheResponse> ObterAsync(int id, CancellationToken ct);
    Task<ClienteResponse> CriarAsync(CriarClienteRequest request, CancellationToken ct);
    Task<ClienteResponse> AtualizarAsync(int id, AtualizarClienteRequest request, CancellationToken ct);
    Task<ClienteDetalheResponse> ObterMeuAsync(CancellationToken ct);
    Task<ClienteResponse> AtualizarMeuAsync(AtualizarClienteRequest request, CancellationToken ct);
    Task<IReadOnlyList<VeiculoResumoResponse>> ListarMeusVeiculosAsync(CancellationToken ct);
}

public sealed class ClienteService(
    IClienteRepository clientes,
    IUnitOfWork unitOfWork,
    IUsuarioLogado usuarioLogado,
    TimeProvider relogio) : IClienteService
{
    public async Task<PaginaResponse<ClienteResponse>> BuscarAsync(string? busca, int pagina, int tamanho, CancellationToken ct)
    {
        (pagina, tamanho) = Paginacao.Normalizar(pagina, tamanho);
        var (itens, total) = await clientes.BuscarAsync(busca, pagina, tamanho, ct);
        return new PaginaResponse<ClienteResponse>(itens.Select(Mapear).ToList(), total, pagina, tamanho);
    }

    public async Task<ClienteDetalheResponse> ObterAsync(int id, CancellationToken ct) =>
        await DetalharAsync(await clientes.ObterPorIdAsync(id, ct) ?? throw ClienteNaoEncontrado(), ct);

    public async Task<ClienteResponse> CriarAsync(CriarClienteRequest request, CancellationToken ct)
    {
        var existente = await clientes.ObterPorTelefoneAsync(request.Telefone, ct);
        if (existente is not null)
        {
            throw new ClienteDuplicadoException(existente.Id);
        }

        var cliente = Cliente.Criar(request.Nome.Trim(), request.Telefone, LimparEmail(request.Email),
            Enum.Parse<CanalContato>(request.CanalPreferido), request.ConsentimentoLgpd, relogio.GetUtcNow());
        await clientes.AdicionarAsync(cliente, ct);
        await unitOfWork.SalvarAsync(ct);
        return Mapear(cliente);
    }

    public async Task<ClienteResponse> AtualizarAsync(int id, AtualizarClienteRequest request, CancellationToken ct) =>
        await AtualizarAsync(await clientes.ObterPorIdAsync(id, ct) ?? throw ClienteNaoEncontrado(), request, ct);

    public async Task<ClienteDetalheResponse> ObterMeuAsync(CancellationToken ct) =>
        await DetalharAsync(await MeuAsync(ct), ct);

    public async Task<ClienteResponse> AtualizarMeuAsync(AtualizarClienteRequest request, CancellationToken ct) =>
        await AtualizarAsync(await MeuAsync(ct), request, ct);

    public async Task<IReadOnlyList<VeiculoResumoResponse>> ListarMeusVeiculosAsync(CancellationToken ct) =>
        (await clientes.ListarVeiculosAsync((await MeuAsync(ct)).Id, ct)).Select(Mapear).ToList();

    private async Task<ClienteResponse> AtualizarAsync(Cliente cliente, AtualizarClienteRequest request, CancellationToken ct)
    {
        if (request.Telefone != cliente.Telefone)
        {
            var outro = await clientes.ObterPorTelefoneAsync(request.Telefone, ct);
            if (outro is not null && outro.Id != cliente.Id)
            {
                throw new ClienteDuplicadoException(outro.Id);
            }
        }

        cliente.Atualizar(request.Nome.Trim(), request.Telefone, LimparEmail(request.Email), Enum.Parse<CanalContato>(request.CanalPreferido));
        await unitOfWork.SalvarAsync(ct);
        return Mapear(cliente);
    }

    private async Task<Cliente> MeuAsync(CancellationToken ct) =>
        await clientes.ObterPorUsuarioAsync(usuarioLogado.Id, ct) ?? throw ClienteNaoEncontrado();

    private async Task<ClienteDetalheResponse> DetalharAsync(Cliente c, CancellationToken ct)
    {
        var veiculos = (await clientes.ListarVeiculosAsync(c.Id, ct)).Select(Mapear).ToList();
        return new ClienteDetalheResponse(c.Id, c.Nome, c.Telefone, c.Email, c.CanalPreferido.ToString(), c.ConsentimentoLgpd, veiculos);
    }

    private static string? LimparEmail(string? email) => string.IsNullOrWhiteSpace(email) ? null : email.Trim().ToLowerInvariant();

    private static NaoEncontradoException ClienteNaoEncontrado() => new("Cliente não encontrado.");

    private static ClienteResponse Mapear(Cliente c) =>
        new(c.Id, c.Nome, c.Telefone, c.Email, c.CanalPreferido.ToString(), c.ConsentimentoLgpd);

    private static VeiculoResumoResponse Mapear(Veiculo v) => new(v.Id, v.Placa, v.ModeloDescricao, v.Tipo.ToString(), v.Cor);
}

/// <summary>Convenção de listas: pagina padrão 1, tamanho padrão 20 e máximo 100 (Entrega 2, seção 3).</summary>
public static class Paginacao
{
    public static (int Pagina, int Tamanho) Normalizar(int pagina, int tamanho) =>
        (Math.Max(pagina, 1), tamanho <= 0 ? 20 : Math.Min(tamanho, 100));
}
