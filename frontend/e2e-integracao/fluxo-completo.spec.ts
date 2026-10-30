import { expect, test, type APIRequestContext, type Page } from '@playwright/test';

// Fluxo completo contra a API .NET e o PostgreSQL de verdade (nada é simulado): UI -> API -> banco.
// Pré-requisito: API de pé com o seed de desenvolvimento (ver README, "Testes de integração ponta a ponta").
const DONO = { email: 'thiago@lavacar.local', senha: process.env.E2E_DONO_SENHA ?? 'SenhaE2E123' };
const PIN_LAVADOR = process.env.E2E_LAVADOR_PIN ?? '4821';

/** Placa e celular novos a cada execução, para o teste poder rodar várias vezes no mesmo banco. */
function dadosUnicos() {
  const letras = 'ABCDEFGHJKLMNPRSTUVXZ';
  const sorteio = (n: number) => Math.floor(Math.random() * n);
  const placa = `${[0, 0, 0].map(() => letras[sorteio(letras.length)]).join('')}${sorteio(10)}${letras[sorteio(letras.length)]}${sorteio(10)}${sorteio(10)}`;
  const telefone = `449${String(Math.floor(10_000_000 + Math.random() * 89_999_999))}`;
  return { placa, telefone };
}

let tokenEmCache: string | undefined;

/** Um login só por execução: as rotas de autenticação têm limite de requisições por minuto. */
async function tokenDoDono(request: APIRequestContext) {
  if (tokenEmCache) return tokenEmCache;
  const r = await request.post('/api/v1/auth/login', { data: DONO });
  expect(r.ok(), 'login do dono (o seed está ligado?)').toBeTruthy();
  tokenEmCache = ((await r.json()) as { accessToken: string }).accessToken;
  return tokenEmCache;
}

/** O app do lavador só libera "Iniciar" no primeiro da fila; o seed já traz carros, então atende os da frente pela API. */
async function limparFilaDaFrente(request: APIRequestContext, auth: { Authorization: string }, placa: string) {
  const quadro = await (await request.get('/api/v1/ordens-servico/quadro', { headers: auth })).json();
  const lavadores = (await (await request.get('/api/v1/auth/lavadores')).json()) as { id: number }[];
  const outros = [...quadro.emAndamento, ...quadro.aguardando].filter((c: { placa: string }) => c.placa !== placa);
  for (const c of outros as { id: number; status: string }[]) {
    if (c.status === 'Aguardando') {
      const r = await request.post(`/api/v1/ordens-servico/${c.id}/iniciar?lavadorId=${lavadores[0]!.id}`, { headers: auth });
      expect(r.ok()).toBeTruthy();
    }
    expect((await request.post(`/api/v1/ordens-servico/${c.id}/encerrar`, { headers: auth })).ok()).toBeTruthy();
  }
}

async function prepararVeiculo(request: APIRequestContext) {
  const { placa, telefone } = dadosUnicos();
  const auth = { Authorization: `Bearer ${await tokenDoDono(request)}` };
  const cliente = await request.post('/api/v1/clientes', {
    headers: auth,
    data: { nome: 'Cliente E2E', telefone, canalPreferido: 'WhatsApp', consentimentoLgpd: true },
  });
  expect(cliente.status()).toBe(201);
  const clienteId = ((await cliente.json()) as { id: number }).id;
  const veiculo = await request.post('/api/v1/veiculos', {
    headers: auth,
    data: { clienteId, placa, modeloDescricao: 'Chevrolet Onix', tipoVeiculo: 'Carro' },
  });
  expect(veiculo.status()).toBe(201);
  return { placa, auth };
}

async function entrarComoDono(page: Page) {
  await page.goto('/login');
  await page.getByLabel('E-mail').fill(DONO.email);
  await page.getByLabel('Senha').fill(DONO.senha);
  await page.getByRole('button', { name: 'Entrar' }).click();
  await expect(page).toHaveURL(/\/dono(\/|$)/);
}

async function registrarEntrada(page: Page, placa: string) {
  await page.goto('/dono/entrada');
  await page.getByLabel('Placa do carro').fill(placa);
  await page.getByRole('button', { name: 'Procurar placa' }).click();
  await page.getByRole('button', { name: 'Continuar com este veículo' }).click();
  await page.getByRole('checkbox', { name: /Lavagem/ }).first().click();
  await page.getByRole('button', { name: 'Continuar' }).click();
  await page.getByLabel(/Sem avarias/).check();
  await page.getByLabel(/O cliente viu a vistoria/).check();
  await page.getByRole('button', { name: 'Registrar entrada' }).click();
}

test('dono registra a entrada e o lavador inicia e encerra a lavagem (UI, API e banco reais)', async ({ page, browser, request }) => {
  const { placa, auth } = await prepararVeiculo(request);

  // Dono: entrada do carro.
  await entrarComoDono(page);
  await registrarEntrada(page, placa);
  await expect(page.getByText('Entrada registrada')).toBeVisible();

  // Persistiu no banco: o quadro da API mostra o cartão em Aguardando.
  const quadro1 = await (await request.get('/api/v1/ordens-servico/quadro', { headers: auth })).json();
  const criada = (quadro1.aguardando as { id: number; placa: string }[]).find((c) => c.placa === placa);
  expect(criada, 'cartão criado no banco').toBeTruthy();

  // Lavador, em outra sessão: PIN, fila, iniciar e encerrar.
  await limparFilaDaFrente(request, auth, placa);
  const contextoLavador = await browser.newContext({ ...test.info().project.use });
  const lavador = await contextoLavador.newPage();
  await lavador.goto('/login/lavador');
  await lavador.getByRole('button', { name: 'Diego' }).click();
  for (const digito of PIN_LAVADOR) await lavador.getByRole('button', { name: digito, exact: true }).click();
  await expect(lavador).toHaveURL(/\/lavador$/);

  // O botão grande fica logo abaixo do cartão do próximo carro, fora do <article>.
  await expect(lavador.getByRole('article', { name: new RegExp(placa) })).toBeVisible();
  await lavador.getByRole('button', { name: 'Iniciar' }).click();
  await lavador.getByRole('button', { name: 'Encerrar' }).click();
  await expect(lavador.getByText(new RegExp(`${placa} encerrada às`))).toBeVisible();
  await contextoLavador.close();

  // Resultado no banco: concluído, com lavador e duração gravados pelo servidor.
  const quadro2 = await (await request.get('/api/v1/ordens-servico/quadro', { headers: auth })).json();
  const concluido = (quadro2.concluido as { id: number; placa: string; lavador: string | null; duracaoRealMin: number | null }[]).find((c) => c.placa === placa);
  expect(concluido?.lavador).toBe('Diego');
  expect(concluido?.duracaoRealMin).not.toBeNull();
});

test('segunda entrada do mesmo carro mostra o aviso de que ele já está no pátio (T25)', async ({ page, request }) => {
  const { placa } = await prepararVeiculo(request);
  await entrarComoDono(page);
  await registrarEntrada(page, placa);
  await expect(page.getByText('Entrada registrada')).toBeVisible();

  await registrarEntrada(page, placa);
  await expect(page.getByText(/já está no pátio/)).toBeVisible();
});
