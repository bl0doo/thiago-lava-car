import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { createMemoryRouter, RouterProvider } from 'react-router-dom';
import { erroHttp } from '@/test/fabrica';
import { ClienteForm } from './dono/ClienteForm';
import { Clientes } from './dono/Clientes';

const api = vi.hoisted(() => ({ listarClientes: vi.fn(), obterCliente: vi.fn(), criarCliente: vi.fn(), atualizarCliente: vi.fn() }));
vi.mock('@/api/clientes', () => api);

function renderizar(inicial: string) {
  const router = createMemoryRouter(
    [
      { path: '/dono/clientes', element: <Clientes /> },
      { path: '/dono/clientes/novo', element: <ClienteForm /> },
      { path: '/dono/clientes/:id', element: <ClienteForm /> },
    ],
    { initialEntries: [inicial] },
  );
  render(
    <QueryClientProvider client={new QueryClient({ defaultOptions: { queries: { retry: false } } })}>
      <RouterProvider router={router} />
    </QueryClientProvider>,
  );
}

const lista = (itens: unknown[]) => ({ itens, total: itens.length, pagina: 1, tamanho: 20 });

describe('Clientes', () => {
  beforeEach(() => vi.resetAllMocks());

  it('lista e busca pelo texto digitado', async () => {
    const u = userEvent.setup();
    api.listarClientes.mockResolvedValue(lista([{ id: 12, nome: 'Renata Souza', telefone: '44999998888' }]));
    renderizar('/dono/clientes');
    expect(await screen.findByText('Renata Souza')).toBeInTheDocument();
    expect(screen.getByText('(44) 99999-8888')).toBeInTheDocument();
    await u.type(screen.getByLabelText('Buscar'), 'ABC1D23');
    await vi.waitFor(() => expect(api.listarClientes).toHaveBeenCalledWith('ABC1D23', 1));
  });

  it('mostra estado de sem resultado quando a busca não acha ninguém', async () => {
    const u = userEvent.setup();
    api.listarClientes.mockResolvedValue(lista([]));
    renderizar('/dono/clientes');
    await u.type(await screen.findByLabelText('Buscar'), 'zzz');
    expect(await screen.findByText('Ninguém encontrado')).toBeInTheDocument();
  });

  it('mostra erro da API', async () => {
    api.listarClientes.mockRejectedValue(erroHttp(500));
    renderizar('/dono/clientes');
    expect(await screen.findByRole('alert')).toBeInTheDocument();
  });
});

describe('Cliente (formulário)', () => {
  beforeEach(() => vi.resetAllMocks());

  it('valida antes de enviar', async () => {
    const u = userEvent.setup();
    renderizar('/dono/clientes/novo');
    await u.click(screen.getByRole('button', { name: 'Salvar' }));
    expect(await screen.findByText('O nome deve ter ao menos 2 caracteres.')).toBeInTheDocument();
    expect(screen.getByText(/10 ou 11 dígitos/)).toBeInTheDocument();
    expect(screen.getByText('Sem o consentimento LGPD o cadastro não é gravado.')).toBeInTheDocument();
    expect(api.criarCliente).not.toHaveBeenCalled();
  });

  it('cadastra no balcão e abre o cliente criado', async () => {
    const u = userEvent.setup();
    api.criarCliente.mockResolvedValue({ id: 31, nome: 'Renata Souza', telefone: '44999998888', email: null, canalPreferido: 'WhatsApp', consentimentoLgpd: true, veiculos: [] });
    api.obterCliente.mockResolvedValue({ id: 31, nome: 'Renata Souza', telefone: '44999998888', email: null, canalPreferido: 'WhatsApp', consentimentoLgpd: true, veiculos: [] });
    renderizar('/dono/clientes/novo');
    await u.type(screen.getByLabelText('Nome'), 'Renata Souza');
    await u.type(screen.getByLabelText('Celular com DDD'), '44999998888');
    await u.click(screen.getByLabelText(/concordou/));
    await u.click(screen.getByRole('button', { name: 'Salvar' }));
    expect(api.criarCliente).toHaveBeenCalledWith({ nome: 'Renata Souza', telefone: '44999998888', email: null, canalPreferido: 'WhatsApp', consentimentoLgpd: true });
    expect(await screen.findByRole('heading', { name: 'Editar cliente' })).toBeInTheDocument();
  });

  it('oferece o cadastro existente no 409 de telefone repetido', async () => {
    const u = userEvent.setup();
    api.criarCliente.mockRejectedValue(erroHttp(409, { detail: 'Já existe cliente com este telefone', clienteId: 7 }));
    renderizar('/dono/clientes/novo');
    await u.type(screen.getByLabelText('Nome'), 'Renata Souza');
    await u.type(screen.getByLabelText('Celular com DDD'), '44999998888');
    await u.click(screen.getByLabelText(/concordou/));
    await u.click(screen.getByRole('button', { name: 'Salvar' }));
    expect(await screen.findByRole('link', { name: 'Abrir o cliente encontrado' })).toHaveAttribute('href', '/dono/clientes/7');
  });

  it('edita e mostra os veículos do cliente', async () => {
    const u = userEvent.setup();
    const cliente = { id: 12, nome: 'Renata', telefone: '44999998888', email: 'r@x.com', canalPreferido: 'Email', consentimentoLgpd: true, veiculos: [{ id: 3, placa: 'ABC1D23', modelo: 'Chevrolet Onix', tipo: 'Carro' }] };
    api.obterCliente.mockResolvedValue(cliente);
    api.atualizarCliente.mockResolvedValue({ ...cliente, nome: 'Renata Souza' });
    renderizar('/dono/clientes/12');
    expect(await screen.findByDisplayValue('Renata')).toBeInTheDocument();
    expect(screen.getByText('ABC1D23')).toBeInTheDocument();
    await u.type(screen.getByLabelText('Nome'), ' Souza');
    await u.click(screen.getByRole('button', { name: 'Salvar' }));
    expect(await screen.findByText('Dados salvos.')).toBeInTheDocument();
    expect(api.atualizarCliente).toHaveBeenCalledWith(12, expect.objectContaining({ nome: 'Renata Souza', email: 'r@x.com', canalPreferido: 'Email' }));
  });
});
