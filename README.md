# Thiago Lava Car

![CI Status](https://github.com/OWNER/REPOSITORY/actions/workflows/ci.yml/badge.svg)
![React](https://img.shields.io/badge/React-19-61dafb?logo=react&logoColor=black)
![TypeScript](https://img.shields.io/badge/TypeScript-5.9-3178c6?logo=typescript&logoColor=white)
![TailwindCSS](https://img.shields.io/badge/Tailwind_CSS-4.0-38bdf8?logo=tailwindcss&logoColor=white)
![.NET](https://img.shields.io/badge/.NET-10.0-512bd4?logo=dotnet&logoColor=white)
![PostgreSQL](https://img.shields.io/badge/PostgreSQL-16-336791?logo=postgresql&logoColor=white)
![License](https://img.shields.io/badge/License-MIT-yellow.svg)

Sistema web para gestão de um lava-jato de bairro: caixa e lucro por serviço, fila de serviços (quadro Pit Stop), vistoria de entrada e histórico do veículo (Ficha do carro). Três perfis: **Dono**, **Lavador** (login por PIN) e **Cliente**.

## Tecnologias
| Camada | Tecnologia |
|---|---|
| Frontend | React 19, TypeScript, Vite, Tailwind CSS 4, React Router, TanStack Query, Axios, React Hook Form + Zod |
| API | ASP.NET Core 10, EF Core 10, Npgsql, FluentValidation, Swagger |
| Banco | PostgreSQL 16 em Docker |
| Autenticação | JWT com refresh token em cookie HttpOnly |
| Testes | xUnit + Testcontainers (API), Vitest e Playwright (frontend) |

## Estrutura
```
backend/              API .NET em camadas (Api, Application, Domain, Infrastructure, tests)
frontend/             SPA React
docs/api/             contrato OpenAPI (os tipos do frontend são gerados dele)
docker-compose.yml    PostgreSQL local
```

## Pré-requisitos
.NET SDK 10 (fixado em `backend/global.json`), Node 22 e Docker.

## Como executar
```bash
# 1. Banco (PostgreSQL 16 em localhost:5432, banco thiago_lava_car)
docker compose up -d db

# 2. API (http://localhost:5205/swagger)
cd backend/ThiagoLavaCar.Api
dotnet user-secrets set "Jwt:Key" "<texto aleatório com 32+ caracteres>"
dotnet user-secrets set "Seed:DonoSenha" "troque-esta-senha1"
dotnet user-secrets set "Seed:LavadorPin" "4821"
ASPNETCORE_ENVIRONMENT=Development Database__MigrarAoIniciar=true Seed__Habilitar=true dotnet run

# 3. Frontend (http://localhost:5173; o Vite repassa /api para a API)
cd frontend
npm ci
npm run dev
```
No PowerShell, defina as variáveis antes do `dotnet run`:
```powershell
$env:ASPNETCORE_ENVIRONMENT="Development"; $env:Database__MigrarAoIniciar="true"; $env:Seed__Habilitar="true"; dotnet run
```

- `Database__MigrarAoIniciar=true` aplica as migrations ao subir.
- `Seed__Habilitar=true` (só em Development) cria o dono, o lavador de exemplo e o catálogo de serviços.

### Primeiro acesso
| Perfil | Como entra |
|---|---|
| Dono | `thiago@lavacar.local` e o valor de `Seed:DonoSenha` |
| Lavador | escolhe o apelido e digita o PIN (`Seed:LavadorPin`) |
| Cliente | cadastro próprio na tela de login |

### Variáveis de configuração
| Chave | Padrão | Observação |
|---|---|---|
| `ConnectionStrings__Default` | PostgreSQL do `docker-compose.yml` | em produção, só por variável de ambiente |
| `Jwt__Key` | sem padrão | mínimo de 32 caracteres |
| `Cors__Origens__0` | `http://localhost:5173` | origem do frontend |
| `RateLimit__AuthPorMinuto` | 10 | limite por IP nas rotas de login |
| `Swagger__Habilitar` | true | |
| `Database__MigrarAoIniciar` | false | |
| `Seed__Habilitar` | false | só vale em Development |
| `VITE_API_URL` (frontend) | vazio | em produção, URL da API com `/api/v1` |

Atrás de proxy reverso, ligue `ASPNETCORE_FORWARDEDHEADERS_ENABLED=true`.

## Testes
```bash
cd backend && dotnet test          # unitários + integração (precisa de Docker)
cd frontend && npm test            # Vitest
cd frontend && npm run e2e         # Playwright (simula a API)
```

## Problemas comuns
| Sintoma | Causa provável |
|---|---|
| API não sobe: chave JWT | `Jwt:Key` ausente ou com menos de 32 caracteres |
| Erro de conexão | contêiner do banco parado (`docker compose up -d db`) |
| Login do lavador sem apelidos | seed desligado ou banco vazio |
| Frontend com erro de CORS | origem fora de `Cors:Origens` |

Detalhes por projeto em [backend/README.md](backend/README.md) e [frontend/README.md](frontend/README.md).
