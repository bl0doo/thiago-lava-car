import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { createMemoryRouter, RouterProvider } from 'react-router-dom';
import { erroHttp } from '@/test/fabrica';
import { Cadastro } from './publico/Cadastro';

const api = vi.hoisted(() => ({ cadastrarCliente: vi.fn() }));
vi.mock('@/api/auth', () => api);
vi.mock('@/auth/AuthContext', () => ({
  useAuth: () => ({ status: 'anonimo', usuario: null }),
  rotaInicial: () => '/cliente/agendar',
}));

function renderizar() {
  const router = createMemoryRouter(
    [
      { path: '/cadastro', element: <Cadastro /> },
      { path: '/login', element: <p>login</p> },
      { path: '/cliente/agendar', element: <p>agendar</p> },
    ],
    { initialEntries: ['/cadastro'] },
  );
  render(<RouterProvider router={router} />);
}

async function preencher(u: ReturnType<typeof userEvent.setup>, senha = 'Senha123', confirmacao = senha) {
  await u.type(screen.getByLabelText('Nome'), 'Renata Souza');
  await u.type(screen.getByLabelText('Celular com DDD'), '(44) 99999-8888');
  await u.type(screen.getByLabelText('E-mail'), 'renata@email.com');
  await u.type(screen.getByLabelText('Senha'), senha);
  await u.type(screen.getByLabelText('Repita a senha'), confirmacao);
}

describe('Cadastro do cliente', () => {
  beforeEach(() => vi.resetAllMocks());

  it('valida no navegador: campos vazios, senha fraca e LGPD', async () => {
    const u = userEvent.setup();
    renderizar();
    await u.click(screen.getByRole('button', { name: 'Criar conta' }));
    expect(await screen.findByText('O nome deve ter ao menos 2 caracteres.')).toBeInTheDocument();
    expect(screen.getByText('Informe o e-mail.')).toBeInTheDocument();
    expect(screen.getByText('Para criar a conta, aceite o uso dos seus dados (LGPD).')).toBeInTheDocument();

    await u.type(screen.getByLabelText('Senha'), 'abcdefgh');
    await u.click(screen.getByRole('button', { name: 'Criar conta' }));
    expect(await screen.findByText('A senha deve ter letras e números.')).toBeInTheDocument();
    expect(api.cadastrarCliente).not.toHaveBeenCalled();
  });

  it('exige senhas iguais', async () => {
    const u = userEvent.setup();
    renderizar();
    await preencher(u, 'Senha123', 'Senha124');
    await u.click(screen.getByLabelText(/Concordo/));
    await u.click(screen.getByRole('button', { name: 'Criar conta' }));
    expect(await screen.findByText('As senhas não são iguais.')).toBeInTheDocument();
  });

  it('cadastra com o telefone só em dígitos e entra na área do cliente', async () => {
    const u = userEvent.setup();
    api.cadastrarCliente.mockResolvedValue({ id: 9, nome: 'Renata Souza', perfil: 'Cliente' });
    renderizar();
    await preencher(u);
    await u.click(screen.getByLabelText(/Concordo/));
    await u.click(screen.getByRole('button', { name: 'Criar conta' }));
    expect(await screen.findByText('agendar')).toBeInTheDocument();
    expect(api.cadastrarCliente).toHaveBeenCalledWith({
      nome: 'Renata Souza',
      telefone: '44999998888',
      email: 'renata@email.com',
      senha: 'Senha123',
      canalPreferido: 'WhatsApp',
      consentimentoLgpd: true,
    });
  });

  it('mostra no topo o 409 de e-mail repetido, com caminho para o login', async () => {
    const u = userEvent.setup();
    api.cadastrarCliente.mockRejectedValue(erroHttp(409, { detail: 'Já existe uma conta com este e-mail. Entre com a senha ou use outro e-mail.' }));
    renderizar();
    await preencher(u);
    await u.click(screen.getByLabelText(/Concordo/));
    await u.click(screen.getByRole('button', { name: 'Criar conta' }));
    expect(await screen.findByRole('alert')).toHaveTextContent('Já existe uma conta com este e-mail');
    expect(screen.getByRole('link', { name: 'Ir para o login' })).toBeInTheDocument();
  });

  it('aponta o campo quando a API devolve 422', async () => {
    const u = userEvent.setup();
    api.cadastrarCliente.mockRejectedValue(erroHttp(422, { errors: { telefone: ['Telefone inválido. Use DDD + número (10 ou 11 dígitos).'] } }));
    renderizar();
    await preencher(u);
    await u.click(screen.getByLabelText(/Concordo/));
    await u.click(screen.getByRole('button', { name: 'Criar conta' }));
    expect(await screen.findByText(/Telefone inválido/)).toBeInTheDocument();
    expect(screen.getByLabelText('Celular com DDD')).toHaveAttribute('aria-invalid', 'true');
  });
});
