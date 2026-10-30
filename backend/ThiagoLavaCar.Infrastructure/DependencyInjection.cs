using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ThiagoLavaCar.Application.Abstractions;
using ThiagoLavaCar.Infrastructure.Data;
using ThiagoLavaCar.Infrastructure.Repositories;
using ThiagoLavaCar.Infrastructure.Security;

namespace ThiagoLavaCar.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var conexao = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException("Defina ConnectionStrings:Default (variável ConnectionStrings__Default).");

        services.AddDbContext<AppDbContext>(opcoes => opcoes
            .UseNpgsql(conexao, npgsql => npgsql.EnableRetryOnFailure(3))
            .UseSnakeCaseNamingConvention());

        services.AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.Secao))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<ISenhaHasher, SenhaHasher>();
        services.AddSingleton<ITokenService, TokenService>();
        services.AddScoped<IUsuarioRepository, UsuarioRepository>();
        services.AddScoped<IClienteRepository, ClienteRepository>();
        services.AddScoped<IVeiculoRepository, VeiculoRepository>();
        services.AddScoped<IModeloVeiculoRepository, ModeloVeiculoRepository>();
        services.AddScoped<IServicoRepository, ServicoRepository>();
        services.AddScoped<IPagamentoRepository, PagamentoRepository>();
        services.AddScoped<ITaxaRepository, TaxaRepository>();
        services.AddScoped<ICaixaRepository, CaixaRepository>();
        services.AddScoped<IDespesaRepository, DespesaRepository>();
        services.AddScoped<IRetiradaRepository, RetiradaRepository>();
        services.AddScoped<IDiariaRepository, DiariaRepository>();
        services.AddScoped<IConfiguracaoRepository, ConfiguracaoRepository>();
        services.AddScoped<IModeloMensagemRepository, ModeloMensagemRepository>();
        services.AddScoped<IMensagemEnviadaRepository, MensagemEnviadaRepository>();
        services.AddScoped<IRelatorioRepository, RelatorioRepository>();
        services.AddScoped<IAuditoriaRepository, AuditoriaRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<IOrdemServicoRepository, OrdemServicoRepository>();
        services.AddScoped<IAgendamentoRepository, AgendamentoRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        return services;
    }
}
