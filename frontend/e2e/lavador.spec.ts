import { expect, test, type Page } from '@playwright/test';

const usuarioLavador = { id: 2, nome: 'Diego Alves', perfil: 'Lavador' };
const cartaoAguardando = {
  id: 88,
  status: 'Aguardando',
  placa: 'ABC1D23',
  modelo: 'Chevrolet Onix',
  servicos: ['Lavagem completa'],
  duracaoEstimadaMin: 90,
  revisaoNecessaria: false,
  inicioEm: null,
  fimEm: null,
  duracaoRealMin: null,
  lavador: null,
};

/** Simula a API do lavador: lista de apelidos, login por PIN e a fila com um cartão. */
async function simularApi(page: Page) {
  let cartao: Record<string, unknown> = cartaoAguardando;
  await page.route('**/api/v1/auth/refresh', (r) => r.fulfill({ status: 401, json: { detail: 'Sessão expirada.' } }));
  await page.route('**/api/v1/auth/lavadores', (r) => r.fulfill({ json: [{ id: 2, apelido: 'Diego' }, { id: 3, apelido: 'Marcos' }] }));
  await page.route('**/api/v1/auth/login-pin', (r) =>
    r.fulfill({ json: { accessToken: 'jwt-de-teste', expiraEm: '2099-01-01T00:00:00Z', usuario: usuarioLavador } }),
  );
  await page.route('**/api/v1/ordens-servico/quadro*', (r) =>
    r.fulfill({
      json: {
        data: '2026-10-20',
        aguardando: cartao.status === 'Aguardando' ? [cartao] : [],
        emAndamento: cartao.status === 'EmAndamento' ? [cartao] : [],
        concluido: [],
      },
    }),
  );
  await page.route('**/api/v1/ordens-servico/88/iniciar', (r) => {
    cartao = { ...cartaoAguardando, status: 'EmAndamento', inicioEm: new Date().toISOString(), lavador: 'Diego' };
    return r.fulfill({ json: cartao });
  });
}

test('lavador escolhe o nome, digita o PIN e inicia uma lavagem com um toque', async ({ page }) => {
  await simularApi(page);

  await page.goto('/login/lavador');
  await page.getByRole('button', { name: 'Diego' }).click();
  for (const digito of '4821') await page.getByRole('button', { name: digito, exact: true }).click();

  await expect(page).toHaveURL(/\/lavador$/);
  await expect(page.getByRole('article', { name: /ABC1D23/ })).toBeVisible();

  const iniciar = page.getByRole('button', { name: 'Iniciar' });
  const caixa = await iniciar.boundingBox();
  expect(caixa?.height).toBeGreaterThanOrEqual(56); // área de toque para uso com luva

  await iniciar.click();
  await expect(page.getByRole('button', { name: 'Encerrar' })).toBeVisible();
  await expect(page.getByRole('button', { name: 'Desfazer' })).toBeVisible();
});

test('lavador que digita um endereço do dono é levado para "Sem permissão"', async ({ page }) => {
  await simularApi(page);
  await page.goto('/login/lavador');
  await page.getByRole('button', { name: 'Diego' }).click();
  for (const digito of '4821') await page.getByRole('button', { name: digito, exact: true }).click();
  await expect(page).toHaveURL(/\/lavador$/);

  // navegação interna (sem recarregar) para manter a sessão em memória
  await page.evaluate(() => {
    window.history.pushState({}, '', '/dono/caixa');
    window.dispatchEvent(new PopStateEvent('popstate'));
  });

  await expect(page.getByRole('heading', { name: 'Sem permissão' })).toBeVisible();
});

test('login do dono mostra a mensagem da API quando a senha está errada', async ({ page }) => {
  await page.route('**/api/v1/auth/refresh', (r) => r.fulfill({ status: 401, json: {} }));
  await page.route('**/api/v1/auth/login', (r) =>
    r.fulfill({ status: 401, contentType: 'application/problem+json', json: { status: 401, detail: 'E-mail ou senha incorretos.' } }),
  );

  await page.goto('/login');
  await page.getByLabel('E-mail').fill('thiago@lavacar.com');
  await page.getByLabel('Senha').fill('errada123');
  await page.getByRole('button', { name: 'Entrar' }).click();

  await expect(page.getByRole('alert')).toHaveText('E-mail ou senha incorretos.');
});
