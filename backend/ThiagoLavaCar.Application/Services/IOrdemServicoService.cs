using ThiagoLavaCar.Application.DTOs;

namespace ThiagoLavaCar.Application.Services;

public interface IOrdemServicoService
{
    Task<QuadroResponse> ObterQuadroAsync(DateOnly? data, CancellationToken ct);
    Task<CartaoResponse> ObterAsync(int id, CancellationToken ct);
    Task<CartaoResponse> IniciarAsync(int id, int? lavadorId, CancellationToken ct);
    Task<CartaoResponse> EncerrarAsync(int id, CancellationToken ct);
    Task<OrdemCriadaResponse> RegistrarEntradaAsync(RegistrarEntradaRequest request, CancellationToken ct);
    Task<CartaoResponse> RegistrarPertencesEncontradosAsync(int id, string? texto, CancellationToken ct);
    Task<CartaoResponse> EditarAsync(int id, EditarOrdemRequest request, CancellationToken ct);
    Task<CartaoResponse> EntregarAsync(int id, string? nomeQuemRetira, CancellationToken ct);
    Task<CartaoResponse> CancelarAsync(int id, CancellationToken ct);
    Task<CartaoResponse> AvaliarAsync(int id, int nota, CancellationToken ct);
    Task<CartaoResponse> DesfazerAsync(int id, CancellationToken ct);
}
