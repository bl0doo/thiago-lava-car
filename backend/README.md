# Backend (API .NET)

ASP.NET Core 10 em camadas. Cada camada só enxerga a de baixo, por interfaces.

| Projeto | Responsabilidade |
|---|---|
| `ThiagoLavaCar.Domain` | Entidades que protegem o próprio estado, enums e exceções de regra. Sem dependências. |
| `ThiagoLavaCar.Application` | Services (regras de negócio), DTOs, validators (FluentValidation) e interfaces de repositório e segurança. |
| `ThiagoLavaCar.Infrastructure` | EF Core + Npgsql (migrations), repositórios, JWT, hash de senha/PIN, dados de exemplo. |
| `ThiagoLavaCar.Api` | Controllers finos, JWT Bearer, CORS, rate limit, ProblemDetails, Swagger. |
| `tests/UnitTests` | xUnit + NSubstitute: domínio, services, validators. |
| `tests/IntegrationTests` | API inteira contra PostgreSQL real (Testcontainers). |

## Rodar
Veja o [README da raiz](../README.md). Resumo, com o banco no ar (`docker compose up -d db`):

```bash
cd backend/ThiagoLavaCar.Api
dotnet user-secrets set "Jwt:Key" "<32+ caracteres>"
ASPNETCORE_ENVIRONMENT=Development Database__MigrarAoIniciar=true Seed__Habilitar=true dotnet run
```
Swagger em `http://localhost:5205/swagger`.

## Testes
```bash
dotnet test      # precisa de Docker (Testcontainers sobe o PostgreSQL)
```

## Migrations
```bash
dotnet tool restore
dotnet tool run dotnet-ef migrations add NomeDaMigration --project ThiagoLavaCar.Infrastructure --output-dir Data/Migrations
dotnet tool run dotnet-ef database update --project ThiagoLavaCar.Infrastructure
```

## Contrato OpenAPI
`docs/api/openapi-v1.json` é o snapshot do Swagger; o frontend gera os tipos dele (`npm run api:types`). Depois de mudar um endpoint ou DTO: subir a API, salvar `http://localhost:5205/swagger/v1/swagger.json` em `docs/api/openapi-v1.json` e regenerar os tipos.
