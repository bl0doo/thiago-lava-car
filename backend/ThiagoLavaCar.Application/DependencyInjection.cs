using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using ThiagoLavaCar.Application.Services;

namespace ThiagoLavaCar.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssemblyContaining<AuthService>(ServiceLifetime.Singleton);
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IUsuarioService, UsuarioService>();
        services.AddScoped<IClienteService, ClienteService>();
        services.AddScoped<IVeiculoService, VeiculoService>();
        services.AddScoped<IServicoService, ServicoService>();
        services.AddScoped<Auditoria>();
        services.AddScoped<IPagamentoService, PagamentoService>();
        services.AddScoped<ICaixaService, CaixaService>();
        services.AddScoped<IDespesaService, DespesaService>();
        services.AddScoped<IRetiradaService, RetiradaService>();
        services.AddScoped<IDiariaService, DiariaService>();
        services.AddScoped<PontoEquilibrioCalculador>();
        services.AddScoped<IPainelService, PainelService>();
        services.AddScoped<IRelatorioService, RelatorioService>();
        services.AddScoped<IOrdemServicoService, OrdemServicoService>();
        services.AddScoped<IOrdemDetalheService, OrdemDetalheService>();
        services.AddScoped<IConfiguracaoService, ConfiguracaoService>();
        services.AddScoped<IMensagemService, MensagemService>();
        services.AddScoped<IAgendamentoService, AgendamentoService>();
        return services;
    }
}
