# Diretrizes para Agentes de IA e Assistentes de Código

Este documento define regras mandatórias para qualquer agente autônomo de IA ou assistente de código que execute tarefas neste repositório.

## 1. Fluxo de Branches e Git (Regra Estrita)
- **NUNCA faça push diretamente na branch `main`.**
- Sempre crie uma branch com nome semântico a partir da `main`:
  - `feat/nome-da-funcionalidade` para novos recursos.
  - `fix/nome-do-bug` para correções.
  - `refactor/nome-da-melhoria` para melhorias sem alterar comportamento.
  - `chore/nome-da-tarefa` para atualizações de dependências e tooling.
- Abra um Pull Request detalhado utilizando o template [`.github/pull_request_template.md`](pull_request_template.md).
- Os commits devem seguir a convenção **Conventional Commits**:
  - Exemplo: `feat: adiciona filtro por status no pit stop`, `fix: corrige validacao de pin do lavador`.

## 2. Validação Obrigatória Pré-Commit / Pré-PR
Antes de submeter qualquer alteração, o agente deve obrigatoriamente rodar e garantir status verde:
1. **Frontend**:
   ```bash
   cd frontend
   npm run lint
   npm test
   npm run build
   ```
2. **Backend**:
   ```bash
   cd backend
   dotnet build ThiagoLavaCar.slnx
   dotnet test tests/ThiagoLavaCar.UnitTests
   ```
3. Nenhuma alteração com erros de tipo TypeScript ou avisos de compilação C# deve ser submetida.

## 3. Segurança e Credenciais
- **JAMAIS** inclua chaves secretas, tokens, senhas ou arquivos `.env` no repositório.
- Não altere arquivos de credenciais ou tente ignorar regras do `.gitignore`.

## 4. Arquitetura e Padrões
- **Backend**: Respeite a Clean Architecture (Domain -> Application -> Infrastructure -> Api).
  - Entidades de domínio protegem seu próprio estado e regras de negócio.
  - Não coloque lógica de negócio em Controllers.
  - Se alterar endpoints ou DTOs, atualize o OpenAPI snapshot em `docs/api/openapi-v1.json` e regenere os tipos do frontend (`npm run api:types`).
- **Frontend**:
  - Respeite o Design System (tokens em `src/styles/index.css`).
  - Mantenha componentes desacoplados e tipagem estrita com TypeScript e Zod.
