# Instruções para Agentes e IAs

Para especificações detalhadas de arquitetura, fluxo de branches e regras de validação pré-PR, consulte [.github/copilot-instructions.md](.github/copilot-instructions.md).

### Resumo das Regras Obrigatórias
1. **Push direto na `main` é estritamente proibido**: Crie sempre uma branch separada (`feat/...`, `fix/...`) e abra Pull Request.
2. **Execute os testes antes de abrir PR**:
   - Frontend: `npm run lint`, `npm test`, `npm run build`
   - Backend: `dotnet test tests/ThiagoLavaCar.UnitTests`
3. **Mantenha os commits no padrão Conventional Commits** (`feat:`, `fix:`, `refactor:`, etc.).
4. **Nunca versione segredos ou variáveis locais**.
