import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { createMemoryRouter, RouterProvider } from 'react-router-dom';
import { erroHttp } from '@/test/fabrica';
import { Login } from './publico/Login';

const auth = vi.hoisted(() => ({ entrar: vi.fn(), usuario: null as unknown }));
vi.mock('@/auth/AuthContext', () => ({
  useAuth: () => ({ status: 'anonimo', usuario: auth.usuario, entrar: auth.entrar }),
  rotaInicial: (perfil: string) => (perfil === 'Dono' ? '/dono' : '/outro'),
}));

function renderizar() {
  const router = createMemoryRouter(
    [
      { path: '/login', element: <Login /> },
      { path: '/dono', element: <p>painel do dono</p> },
      { path: '/login/lavador', element: <p>login lavador</p> },
    ],
    { initialEntries: ['/login'] },
  );
  render(<RouterProvider router={router} />);
}

describe('Login', () => {
  beforeEach(() => vi.resetAllMocks());

  it('valida no navegador sem chamar a API', async () => {
    const usuario = userEvent.setup();
    renderizar();

    await usuario.type(screen.getByLabelText('E-mail'), 'nao-e-email');
    await usuario.click(screen.getByRole('button', { name: 'Entrar' }));

    expect(await screen.findByText('E-mail inválido.')).toBeInTheDocument();
    expect(screen.getByText('Informe a senha.')).toBeInTheDocument();
    expect(screen.getByLabelText('E-mail')).toHaveAttribute('aria-invalid', 'true');
    expect(auth.entrar).not.toHaveBeenCalled();
  });

  it('entra e vai para a área do perfil', async () => {
    const usuario = userEvent.setup();
    auth.entrar.mockResolvedValue({ id: 1, nome: 'Thiago', perfil: 'Dono' });
    renderizar();

    await usuario.type(screen.getByLabelText('E-mail'), 'thiago@lavacar.com');
    await usuario.type(screen.getByLabelText('Senha'), 'Senha123');
    await usuario.click(screen.getByRole('button', { name: 'Entrar' }));

    expect(await screen.findByText('painel do dono')).toBeInTheDocument();
    expect(auth.entrar).toHaveBeenCalledWith('thiago@lavacar.com', 'Senha123');
  });

  it.each([
    [401, 'E-mail ou senha incorretos.'],
    [423, 'Acesso bloqueado por 15 minutos após várias tentativas.'],
  ])('mostra no topo a mensagem da API no erro %i', async (status, detail) => {
    const usuario = userEvent.setup();
    auth.entrar.mockRejectedValue(erroHttp(status, { detail }));
    renderizar();

    await usuario.type(screen.getByLabelText('E-mail'), 'thiago@lavacar.com');
    await usuario.type(screen.getByLabelText('Senha'), 'errada');
    await usuario.click(screen.getByRole('button', { name: 'Entrar' }));

    expect(await screen.findByRole('alert')).toHaveTextContent(detail);
  });

  it('liga o erro 422 da API ao campo certo', async () => {
    const usuario = userEvent.setup();
    auth.entrar.mockRejectedValue(erroHttp(422, { detail: 'Dados inválidos', errors: { email: ['E-mail inválido.'] } }));
    renderizar();

    await usuario.type(screen.getByLabelText('E-mail'), 'a@b.co');
    await usuario.type(screen.getByLabelText('Senha'), 'x');
    await usuario.click(screen.getByRole('button', { name: 'Entrar' }));

    expect(await screen.findByText('E-mail inválido.')).toBeInTheDocument();
    expect(screen.queryByRole('alert')).not.toBeInTheDocument();
  });

  it('oferece o caminho do lavador', () => {
    renderizar();
    expect(screen.getByRole('link', { name: 'Sou lavador' })).toHaveAttribute('href', '/login/lavador');
  });
});
