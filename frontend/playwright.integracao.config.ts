import { defineConfig, devices } from '@playwright/test';

// Ponta a ponta com a API e o banco de verdade.
// A API precisa estar de pé em VITE_PROXY_API (padrão http://localhost:5205) com o seed de desenvolvimento;
// o Vite repassa /api para ela, então o navegador fala só com a mesma origem e o cookie de renovação funciona.
export default defineConfig({
  testDir: './e2e-integracao',
  fullyParallel: false,
  workers: 1,
  forbidOnly: !!process.env.CI,
  retries: 0,
  reporter: process.env.CI ? [['github'], ['html', { open: 'never' }]] : 'list',
  use: {
    baseURL: 'http://localhost:5173',
    trace: 'retain-on-failure',
    screenshot: 'only-on-failure',
    launchOptions: process.env.PW_CHROMIUM_PATH ? { executablePath: process.env.PW_CHROMIUM_PATH } : {},
  },
  projects: [{ name: 'celular', use: { ...devices['Pixel 7'] } }],
  webServer: {
    command: 'npm run dev',
    url: 'http://localhost:5173',
    reuseExistingServer: !process.env.CI,
    timeout: 60_000,
  },
});
