using ThiagoLavaCar.Application.DTOs;

namespace ThiagoLavaCar.Application.Services;

public interface IAuthService
{
    Task<SessaoIniciada> LoginAsync(LoginRequest request, CancellationToken ct);
    Task<SessaoIniciada> LoginPinAsync(LoginPinRequest request, CancellationToken ct);
    Task<SessaoIniciada> CadastrarClienteAsync(CadastroClienteRequest request, CancellationToken ct);
    Task<SessaoIniciada> RenovarAsync(string? refreshToken, CancellationToken ct);
    Task SairAsync(string? refreshToken, CancellationToken ct);
    Task<UsuarioResponse> ObterUsuarioLogadoAsync(CancellationToken ct);
    Task<IReadOnlyList<LavadorResumoResponse>> ListarLavadoresAsync(CancellationToken ct);
}
