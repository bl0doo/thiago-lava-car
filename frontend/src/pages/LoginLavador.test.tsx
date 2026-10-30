import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter } from 'react-router-dom';
import { erroHttp } from '@/test/fabrica';
import { LoginLavador } from './publico/LoginLavador';

const auth = vi.hoisted(() => ({ entrarComPin: vi.fn() }));
vi.mock('@/auth/AuthContext', () => ({
  useAuth: () => ({ usuario: null, entrarComPin: auth.entrarComPin }),
  rotaInicial: () => '/lavador',
}));
vi.mock('@/api/auth', () => ({ listarLavadores: () => Promise.resolve([{ id: 2, apelido: 'Diego' }]) }));

function renderizar() {
  render(
    <QueryClientProvider client={new QueryClient({ defaultOptions: { queries: { retry: false } } })}>
      <MemoryRouter>
        <LoginLavador />
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

async function digitarPin(usuario: ReturnType<typeof userEvent.setup>, pin: string) {
  await usuario.click(await screen.findByRole('button', { name: 'Diego' }));
  for (const d of pin) await usuario.click(screen.getByRole('button', { name: d }));
}

describe('Login do lavador', () => {
  beforeEach(() => vi.resetAllMocks());

  it('mostra o erro e limpa o PIN quando ele está incorreto', async () => {
    const usuario = userEvent.setup();
    auth.entrarComPin.mockRejectedValue(erroHttp(401, { detail: 'PIN incorreto.' }));
    renderizar();

    await digitarPin(usuario, '1234');

    expect(await screen.findByRole('alert')).toHaveTextContent('PIN incorreto.');
    expect(screen.getByRole('status', { name: '0 de 4 dígitos' })).toBeInTheDocument();
  });

  it('depois de 5 erros mostra a tela de acesso travado, com o caminho de volta', async () => {
    const usuario = userEvent.setup();
    auth.entrarComPin.mockRejectedValue(erroHttp(423, { detail: 'Acesso bloqueado por 15 minutos.' }));
    renderizar();

    await digitarPin(usuario, '1234');

    expect(await screen.findByRole('heading', { name: 'Bloqueado' })).toBeInTheDocument();
    expect(screen.getByRole('alert')).toHaveTextContent('Acesso bloqueado por 15 minutos.');
    // o botão grande do rodapé (a barra de título também tem um Voltar)
    await usuario.click(screen.getAllByRole('button', { name: 'Voltar' }).at(-1)!);
    expect(await screen.findByRole('heading', { name: 'Quem é você?' })).toBeInTheDocument();
  });
});
