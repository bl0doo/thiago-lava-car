import AxeBuilder from '@axe-core/playwright';
import { expect, test, type Page } from '@playwright/test';

// Auditoria automática WCAG 2.1 AA com axe-core nas telas principais, em tela de celular.
// Ela pega contraste, nome acessível, rótulo de campo, hierarquia de títulos e uso de landmarks. Não substitui o
// teste manual com leitor de tela e teclado.
const REGRAS = ['wcag2a', 'wcag2aa', 'wcag21a', 'wcag21aa'];

async function auditar(page: Page) {
  const resultado = await new AxeBuilder({ page }).withTags(REGRAS).analyze();
  const resumo = resultado.violations.map((v) => `${v.id} (${v.impact}): ${v.nodes.map((n) => n.target.join(' ')).join(' | ')}`);
  expect(resumo, 'violações de acessibilidade').toEqual([]);
}

async function semSessao(page: Page) {
  await page.route('**/api/v1/auth/refresh', (r) => r.fulfill({ status: 401, json: { detail: 'Sessão expirada.' } }));
}

test('login do dono não tem violações WCAG AA', async ({ page }) => {
  await semSessao(page);
  await page.goto('/login');
  await expect(page.getByRole('button', { name: /entrar/i }).first()).toBeVisible();
  await auditar(page);
});

test('cadastro do cliente não tem violações WCAG AA', async ({ page }) => {
  await semSessao(page);
  await page.goto('/cadastro');
  await expect(page.getByRole('heading').first()).toBeVisible();
  await auditar(page);
});

test('login do lavador e teclado de PIN não têm violações WCAG AA', async ({ page }) => {
  await semSessao(page);
  await page.route('**/api/v1/auth/lavadores', (r) => r.fulfill({ json: [{ id: 2, apelido: 'Diego' }, { id: 3, apelido: 'Marcos' }] }));
  await page.goto('/login/lavador');
  await auditar(page);
  await page.getByRole('button', { name: 'Diego' }).click();
  await expect(page.getByRole('button', { name: '4', exact: true })).toBeVisible();
  await auditar(page);
});

test('fila do lavador não tem violações WCAG AA', async ({ page }) => {
  const cartao = {
    id: 88, status: 'Aguardando', placa: 'ABC1D23', modelo: 'Chevrolet Onix', servicos: ['Lavagem completa'],
    duracaoEstimadaMin: 90, revisaoNecessaria: false, inicioEm: null, fimEm: null, duracaoRealMin: null, lavador: null,
  };
  await semSessao(page);
  await page.route('**/api/v1/auth/lavadores', (r) => r.fulfill({ json: [{ id: 2, apelido: 'Diego' }] }));
  await page.route('**/api/v1/auth/login-pin', (r) =>
    r.fulfill({ json: { accessToken: 'jwt-de-teste', expiraEm: '2099-01-01T00:00:00Z', usuario: { id: 2, nome: 'Diego Alves', perfil: 'Lavador' } } }),
  );
  await page.route('**/api/v1/ordens-servico/quadro*', (r) =>
    r.fulfill({ json: { data: '2026-10-20', aguardando: [cartao], emAndamento: [], concluido: [] } }),
  );
  await page.goto('/login/lavador');
  await page.getByRole('button', { name: 'Diego' }).click();
  for (const digito of '4821') await page.getByRole('button', { name: digito, exact: true }).click();
  await expect(page.getByRole('article', { name: /ABC1D23/ })).toBeVisible();
  await auditar(page);
});

test('painel do dono não tem violações WCAG AA', async ({ page }) => {
  await semSessao(page);
  await page.route('**/api/v1/auth/login', (r) =>
    r.fulfill({ json: { accessToken: 'jwt-de-teste', expiraEm: '2099-01-01T00:00:00Z', usuario: { id: 1, nome: 'Thiago', perfil: 'Dono' } } }),
  );
  await page.route('**/api/v1/painel/hoje', (r) =>
    r.fulfill({
      json: { entrouHoje: 450, liquidoHoje: 432.5, sobrouDeVerdade: 256.02, carrosNoMes: 38, carrosFaltamParaPagarContas: 25, pendencias: ['2 OS sem pagamento'] },
    }),
  );
  await page.goto('/login');
  await page.getByLabel(/e-mail/i).fill('thiago@lavacar.local');
  await page.getByLabel(/senha/i).fill('Senha123');
  await page.getByRole('button', { name: /entrar/i }).first().click();
  await expect(page.getByRole('heading', { name: 'Painel' })).toBeVisible();
  await expect(page.getByText(/256,02/)).toBeVisible();
  await auditar(page);
});
