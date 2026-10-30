using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Time.Testing;
using Testcontainers.PostgreSql;
using ThiagoLavaCar.Application.Abstractions;
using ThiagoLavaCar.Domain.Entities;
using ThiagoLavaCar.Domain.Enums;
using ThiagoLavaCar.Infrastructure.Data;

namespace ThiagoLavaCar.IntegrationTests;

/// <summary>
/// Sobe a API inteira (pipeline, DI, JWT, EF) contra um PostgreSQL real. Sobe um contêiner
/// PostgreSQL (Testcontainers).
/// </summary>
public sealed class ApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    public const string ChaveJwt = "chave-de-teste-com-mais-de-32-caracteres-aleatorios";
    public const string Senha = "Senha123";
    public const string Pin = "4821";

    private PostgreSqlContainer? _container;
    private string _conexao = string.Empty;

    /// <summary>Relógio controlável: o teste de "desfazer em 2 minutos" avança o tempo sem esperar.</summary>
    public FakeTimeProvider Relogio { get; } = new(DateTimeOffset.UtcNow);

    public async Task InitializeAsync()
    {
        _container = new PostgreSqlBuilder("postgres:16-alpine").Build();
        await _container.StartAsync();
        _conexao = _container.GetConnectionString();

        // Variáveis de ambiente chegam à configuração antes de o Program ler qualquer valor.
        Environment.SetEnvironmentVariable("ConnectionStrings__Default", _conexao);
        Environment.SetEnvironmentVariable("Jwt__Key", ChaveJwt);
        Environment.SetEnvironmentVariable("Database__MigrarAoIniciar", "true");
        Environment.SetEnvironmentVariable("RateLimit__AuthPorMinuto", "10000");
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<TimeProvider>();
            services.AddSingleton<TimeProvider>(Relogio);
        });
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await DisposeAsync();
        if (_container is not null)
        {
            await _container.DisposeAsync();
        }
    }

    /// <summary>Executa um bloco com acesso direto ao banco (para montar cenários e conferir restrições).</summary>
    public async Task<T> NoBancoAsync<T>(Func<AppDbContext, IServiceProvider, Task<T>> acao)
    {
        await using var escopo = Services.CreateAsyncScope();
        return await acao(escopo.ServiceProvider.GetRequiredService<AppDbContext>(), escopo.ServiceProvider);
    }

    public Task<Usuario> CriarDonoAsync(string? email = null, bool ativo = true) => NoBancoAsync(async (db, sp) =>
    {
        var usuario = Usuario.CriarComSenha("Thiago", email ?? $"dono-{Guid.NewGuid():N}@lavacar.com",
            sp.GetRequiredService<ISenhaHasher>().Gerar(Senha), Perfil.Dono, Relogio.GetUtcNow());
        if (!ativo)
        {
            usuario.Desativar();
        }

        db.Usuarios.Add(usuario);
        await db.SaveChangesAsync();
        return usuario;
    });

    public Task<Usuario> CriarLavadorAsync(string apelido = "Diego") => NoBancoAsync(async (db, sp) =>
    {
        var usuario = Usuario.CriarLavador($"{apelido} da Silva", apelido, sp.GetRequiredService<ISenhaHasher>().Gerar(Pin), Relogio.GetUtcNow());
        db.Usuarios.Add(usuario);
        await db.SaveChangesAsync();
        return usuario;
    });

    public Task<Usuario> CriarClienteComLoginAsync() => NoBancoAsync(async (db, sp) =>
    {
        var usuario = Usuario.CriarComSenha("Renata Souza", $"cliente-{Guid.NewGuid():N}@email.com",
            sp.GetRequiredService<ISenhaHasher>().Gerar(Senha), Perfil.Cliente, Relogio.GetUtcNow());
        db.Usuarios.Add(usuario);
        await db.SaveChangesAsync();
        db.Clientes.Add(Cliente.Criar("Renata Souza", NovoTelefone(), null, CanalContato.WhatsApp, true, Relogio.GetUtcNow(), usuario.Id));
        await db.SaveChangesAsync();
        return usuario;
    });

    /// <summary>Cria cliente, veículo (placa única) e uma OS em Aguardando; devolve o id da OS.</summary>
    public Task<int> CriarOrdemAguardandoAsync(int criadoPorId) => NoBancoAsync(async (db, _) =>
    {
        var agora = Relogio.GetUtcNow();
        var cliente = Cliente.Criar("Renata Souza", NovoTelefone(), null, CanalContato.WhatsApp, true, agora);
        db.Clientes.Add(cliente);
        var servico = Servico.Criar($"Lavagem {Guid.NewGuid():N}", "Lavagem", 90, 8m);
        servico.DefinirPreco(TipoVeiculo.Carro, 80m, 100m);
        db.Servicos.Add(servico);
        await db.SaveChangesAsync();

        var veiculo = Veiculo.Criar(cliente.Id, NovaPlaca(), "Chevrolet Onix", TipoVeiculo.Carro, "Prata");
        db.Veiculos.Add(veiculo);
        await db.SaveChangesAsync();

        var os = OrdemServico.Registrar(
            veiculo, criadoPorId, [ItemOrdemServico.Criar(servico, true, servico.ObterPreco(TipoVeiculo.Carro, true), false)],
            true, [], true, null, null, agora);
        db.OrdensServico.Add(os);
        await db.SaveChangesAsync();
        return os.Id;
    });

    /// <summary>A migração de dados de referência cria as quatro taxas zeradas; os testes precisam das taxas de referência (Débito 1,5%, Crédito 3,49%).</summary>
    public Task GarantirTaxasAsync() => NoBancoAsync(async (db, _) =>
    {
        var taxas = await db.TaxasPagamento.ToListAsync();
        foreach (var (forma, percentual) in new[] { (FormaPagamento.Pix, 0m), (FormaPagamento.Debito, 1.5m), (FormaPagamento.Credito, 3.49m), (FormaPagamento.Dinheiro, 0m) })
        {
            var existente = taxas.FirstOrDefault(t => t.FormaPagamento == forma);
            if (existente is null)
            {
                db.TaxasPagamento.Add(TaxaPagamento.Criar(forma, percentual, 0m));
            }
            else
            {
                existente.Alterar(percentual, 0m);
            }
        }

        await db.SaveChangesAsync();
        return 0;
    });

    private static readonly Random Aleatorio = new();

    public static string NovoTelefone() => "449" + Aleatorio.Next(10_000_000, 99_999_999).ToString(System.Globalization.CultureInfo.InvariantCulture);

    public static string NovaPlaca()
    {
        const string letras = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        lock (Aleatorio)
        {
            return $"{letras[Aleatorio.Next(26)]}{letras[Aleatorio.Next(26)]}{letras[Aleatorio.Next(26)]}{Aleatorio.Next(10)}{letras[Aleatorio.Next(26)]}{Aleatorio.Next(10)}{Aleatorio.Next(10)}";
        }
    }
}
