using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ThiagoLavaCar.Infrastructure.Data;

/// <summary>Usada só pelas ferramentas do EF (dotnet ef migrations ...); não conecta ao banco para gerar migrations.</summary>
internal sealed class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var conexao = Environment.GetEnvironmentVariable("ConnectionStrings__Default")
            ?? "Host=localhost;Port=5432;Database=thiago_lava_car;Username=postgres;Password=postgres";
        var opcoes = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(conexao)
            .UseSnakeCaseNamingConvention()
            .Options;
        return new AppDbContext(opcoes);
    }
}
