import { render, screen } from '@testing-library/react';
import { createMemoryRouter, RouterProvider } from 'react-router-dom';
import type { Perfil } from '@/api/tipos';
import { RotaProtegida } from './RotaProtegida';

const mockUseAuth = vi.hoisted(() => vi.fn());
vi.mock('./AuthContext', () => ({ useAuth: mockUseAuth }));

function renderizarEm(caminho: string) {
  const router = createMemoryRouter(
    [
      { path: '/login', element: <p>tela de login</p> },
      { path: '/login/lavador', element: <p>tela de login do lavador</p> },
      { path: '/sem-permissao', element: <p>sem permissão</p> },
      { element: <RotaProtegida perfis={['Dono']} />, children: [{ path: '/dono', element: <p>área do dono</p> }] },
      { element: <RotaProtegida perfis={['Lavador']} />, children: [{ path: '/lavador', element: <p>área do lavador</p> }] },
    ],
    { initialEntries: [caminho] },
  );
  render(<RouterProvider router={router} />);
}

const logadoComo = (perfil: Perfil) =>
  mockUseAuth.mockReturnValue({ status: 'autenticado', usuario: { id: 1, nome: 'X', perfil } });

describe('RotaProtegida', () => {
  it('mostra a área quando o perfil é permitido', () => {
    logadoComo('Dono');
    renderizarEm('/dono');
    expect(screen.getByText('área do dono')).toBeInTheDocument();
  });

  it('manda o Lavador que digita /dono para "Sem permissão"', () => {
    logadoComo('Lavador');
    renderizarEm('/dono');
    expect(screen.getByText('sem permissão')).toBeInTheDocument();
  });

  it('manda quem não entrou para o login (o do lavador, na área do lavador)', () => {
    mockUseAuth.mockReturnValue({ status: 'anonimo', usuario: null });
    renderizarEm('/dono');
    expect(screen.getByText('tela de login')).toBeInTheDocument();
  });

  it('usa a tela de PIN quando a rota é só do lavador', () => {
    mockUseAuth.mockReturnValue({ status: 'anonimo', usuario: null });
    renderizarEm('/lavador');
    expect(screen.getByText('tela de login do lavador')).toBeInTheDocument();
  });

  it('espera a sessão ser recuperada antes de decidir', () => {
    mockUseAuth.mockReturnValue({ status: 'carregando', usuario: null });
    renderizarEm('/dono');
    expect(screen.getByRole('status')).toHaveTextContent('Carregando');
    expect(screen.queryByText('tela de login')).not.toBeInTheDocument();
  });
});
