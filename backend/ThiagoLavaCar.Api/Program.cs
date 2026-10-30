using Microsoft.EntityFrameworkCore;
using ThiagoLavaCar.Api.Extensions;
using ThiagoLavaCar.Application;
using ThiagoLavaCar.Infrastructure;
using ThiagoLavaCar.Infrastructure.Data;
using ThiagoLavaCar.Infrastructure.Seed;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddApplication()
    .AddInfrastructure(builder.Configuration)
    .AddApiControllers()
    .AddJwtAuth()
    .AddCorsDoFrontend(builder.Configuration)
    .AddRateLimitDeLogin(builder.Configuration)
    .AddSwaggerComJwt();

builder.Services.AddScoped<DadosDeDesenvolvimento>();
builder.Services.AddHealthChecks().AddDbContextCheck<AppDbContext>("banco");

var app = builder.Build();

if (app.Configuration.GetValue<bool>("Database:MigrarAoIniciar"))
{
    await using var escopo = app.Services.CreateAsyncScope();
    await escopo.ServiceProvider.GetRequiredService<AppDbContext>().Database.MigrateAsync();
    if (app.Environment.IsDevelopment() && app.Configuration.GetValue<bool>("Seed:Habilitar"))
    {
        await escopo.ServiceProvider.GetRequiredService<DadosDeDesenvolvimento>().ExecutarAsync(CancellationToken.None);
    }
}

app.UseExceptionHandler();
app.UseStatusCodePages();

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
    app.UseHttpsRedirection();
}

if (app.Configuration.GetValue<bool>("Swagger:Habilitar"))
{
    app.UseSwagger();
    app.UseSwaggerUI(o => o.DocumentTitle = "Thiago Lava Car API");
}

app.UseCors(ServiceCollectionExtensions.PoliticaCors);
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health").AllowAnonymous();

await app.RunAsync();

/// <summary>Exposta para os testes de integração (WebApplicationFactory).</summary>
public partial class Program;
