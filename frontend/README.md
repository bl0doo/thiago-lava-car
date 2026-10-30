# Frontend (React + TypeScript + Vite)

SPA mobile-first: React 19, React Router, TanStack Query, Axios, React Hook Form + Zod, Tailwind CSS 4.

## Já implementado
- Sessão: access token só em memória; refresh por cookie HttpOnly com renovação única entre requisições simultâneas; `RotaProtegida` por perfil.
- Login de Dono/Cliente e login do Lavador (botões com apelido + teclado de PIN de 4 dígitos com teclas de 72 px; tela de acesso travado após 5 erros).
- **Pit Stop (dono)** e **Minha fila (lavador)**: consulta a cada 15 s, atualização otimista de Iniciar/Encerrar/Desfazer, cronômetro, Desfazer por 2 min.
- **Todas as telas previstas estão implementadas** (nenhuma rota em "Em construção"). Rotas por perfil:
  - Público: `/login`, `/login/lavador`, `/cadastro`.
  - Dono (`/dono`): painel (`/`), `pit-stop`, `entrada` (nova entrada: veículo, serviços, vistoria), `os/:id` (detalhe, pagamentos e estorno, avaliação, mensagem de WhatsApp), `agenda` (check-in, falta, cancelamento), `clientes`, `veiculos/:id` (ficha do carro), `caixa`, `despesas` (despesas e retiradas), `equipe` (lavadores e diárias), `servicos`, `relatorios`, `configuracoes` (lava-jato, taxas, modelos de mensagem), `mais`.
  - Lavador (`/lavador`): minha fila e `os/:id` (pertences encontrados).
  - Cliente (`/cliente`): `agendar`, `agendamentos`, `veiculos`, `veiculos/:id`, `perfil`.
- **Design System "Box de pista"** (Figma, página Design System): tokens em `src/styles/index.css` com os mesmos nomes das variáveis do Figma (`--bg-app`, `--acao-primaria-bg`, `--espaco-md`, `--raio-md`, `--alvo-lavador-principal`…), expostos como classes Tailwind (`bg-app`, `bg-acao-primaria`, `p-md`, `rounded-md`, `min-h-alvo-lavador-principal`) e os 16 estilos de texto como utilitários (`texto-h1`, `texto-eyebrow`, `texto-mono-placa`…). Fontes Barlow, Barlow Condensed e IBM Plex Mono servidas pelo próprio app (`@fontsource`), ícones Lucide (`lucide-react`), os mesmos do Figma.
- Componentes do Design System em `src/components/ui` (Botão nos 4 tipos e 2 tamanhos, Campo de texto, Aviso, Selo de status, Placa Mercosul, Estado vazio, Esqueleto de carregamento, Teclado de PIN) e `src/components/layout` (Barra de título, Marca, Barra inferior e Menu lateral).
- Telas seguindo o protótipo: 01 Login (e E1 desktop), 02 PIN, A3/A4 PIN incorreto e bloqueado, 04 Pit Stop com abas no celular e 11 Pit Stop em 3 colunas no tablet, 06/07 Minha fila e No box, C1 fila vazia, 14 Mais, D10/D11 erros 403/404. Celular e tablet usam a barra inferior; a partir de 1280 px, o menu lateral.
- Atenção ao Tailwind: como os espaços se chamam `sm/md/lg/xl`, `max-w-md` vira 16 px. Use larguras explícitas (`max-w-[28rem]`).

## Rodar
```bash
cd frontend
npm ci
npm run dev        # http://localhost:5173, repassa /api para a API em localhost:5205
```

## Scripts
`npm run lint` · `npm run build` · `npm test` (Vitest) · `npm run e2e` (Playwright, celular; simula a API) · `npm run e2e:integracao` (Playwright contra a API e o banco reais) · `npm run api:types` (regenera `src/api/schema.d.ts` de `docs/api/openapi-v1.json`).

Sem o Chromium do Playwright instalado: `PW_CHROMIUM_PATH=/caminho/do/chrome npm run e2e`.
