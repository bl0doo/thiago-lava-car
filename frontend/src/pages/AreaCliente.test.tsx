import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter } from 'react-router-dom';
import { erroHttp } from '@/test/fabrica';
import { MeusDados } from './cliente/MeusDados';
import { MeusVeiculos } from './cliente/MeusVeiculos';

const clientes = vi.hoisted(() => ({ obterMeusDados: vi.fn(), atualizarMeusDados: vi.fn(), listarMeusVeiculos: vi.fn() }));
const veiculos = vi.hoisted(() => ({ criarVeiculo: vi.fn() }));
vi.mock('@/api/clientes', () => clientes);
vi.mock('@/api/veiculos', () => veiculos);
vi.mock('@/auth/AuthContext', () => ({ useAuth: () => ({ sair: vi.fn() }) }));

function renderizar(tela: React.ReactNode) {
  render(
    <QueryClientProvider client={new QueryClient({ defaultOptions: { queries: { retry: false } } })}>
      <MemoryRouter>{tela}</MemoryRouter>
    </QueryClientProvider>,
  );
}

const eu = { id: 12, nome: 'Renata', telefone: '44999998888', email: null, canalPreferido: 'WhatsApp', consentimentoLgpd: true, veiculos: [] };

describe('Meus dados', () => {
  beforeEach(() => vi.resetAllMocks());

  it('carrega, valida e salva', async () => {
    const u = userEvent.setup();
    clientes.obterMeusDados.mockResolvedValue(eu);
    clientes.atualizarMeusDados.mockResolvedValue({ ...eu, nome: 'Renata Souza' });
    renderizar(<MeusDados />);
    const nome = await screen.findByDisplayValue('Renata');
    await u.clear(nome);
    await u.type(nome, 'R');
    await u.click(screen.getByRole('button', { name: 'Salvar' }));
    expect(await screen.findByText('O nome deve ter ao menos 2 caracteres.')).toBeInTheDocument();
    expect(clientes.atualizarMeusDados).not.toHaveBeenCalled();

    await u.type(nome, 'enata Souza');
    await u.click(screen.getByRole('button', { name: 'Salvar' }));
    expect(await screen.findByText('Dados salvos.')).toBeInTheDocument();
    expect(clientes.atualizarMeusDados.mock.calls[0]?.[0]).toEqual({ nome: 'RRenata Souza'.slice(1), telefone: '44999998888', email: null, canalPreferido: 'WhatsApp' });
  });

  it('mostra 409 de telefone repetido', async () => {
    const u = userEvent.setup();
    clientes.obterMeusDados.mockResolvedValue(eu);
    clientes.atualizarMeusDados.mockRejectedValue(erroHttp(409, { detail: 'Já existe cliente com este telefone.' }));
    renderizar(<MeusDados />);
    await screen.findByDisplayValue('Renata');
    await u.click(screen.getByRole('button', { name: 'Salvar' }));
    expect(await screen.findByRole('alert')).toHaveTextContent('Já existe cliente com este telefone.');
  });
});

describe('Meus veículos', () => {
  beforeEach(() => vi.resetAllMocks());

  it('mostra estado vazio com chamada para cadastrar', async () => {
    clientes.listarMeusVeiculos.mockResolvedValue([]);
    renderizar(<MeusVeiculos />);
    expect(await screen.findByText('Nenhum carro cadastrado')).toBeInTheDocument();
  });

  it('lista com link para a ficha e cadastra um carro novo', async () => {
    const u = userEvent.setup();
    clientes.listarMeusVeiculos.mockResolvedValue([{ id: 3, placa: 'ABC1D23', modelo: 'Chevrolet Onix', tipo: 'Carro' }]);
    veiculos.criarVeiculo.mockResolvedValue({ id: 4 });
    renderizar(<MeusVeiculos />);
    expect(await screen.findByRole('link', { name: /ABC1D23/ })).toHaveAttribute('href', '/cliente/veiculos/3');

    await u.click(screen.getByRole('button', { name: 'Cadastrar carro' }));
    await u.click(screen.getByRole('button', { name: 'Cadastrar' }));
    expect(await screen.findByText(/Placa inválida/)).toBeInTheDocument();
    expect(screen.getByText('Informe o modelo do carro.')).toBeInTheDocument();

    await u.type(screen.getByLabelText('Placa'), 'xyz-9a87');
    await u.type(screen.getByLabelText('Modelo'), 'Fiat Argo');
    await u.click(screen.getByRole('button', { name: 'Cadastrar' }));
    await vi.waitFor(() => expect(veiculos.criarVeiculo).toHaveBeenCalled());
    expect(veiculos.criarVeiculo.mock.calls[0]?.[0]).toEqual({ placa: 'XYZ9A87', modeloDescricao: 'Fiat Argo', tipoVeiculo: 'Carro', cor: null });
  });

  it('mostra 409 de placa já cadastrada no topo', async () => {
    const u = userEvent.setup();
    clientes.listarMeusVeiculos.mockResolvedValue([]);
    veiculos.criarVeiculo.mockRejectedValue(erroHttp(409, { detail: 'Placa já cadastrada.' }));
    renderizar(<MeusVeiculos />);
    await u.click(await screen.findByRole('button', { name: 'Cadastrar carro' }));
    await u.type(screen.getByLabelText('Placa'), 'ABC1D23');
    await u.type(screen.getByLabelText('Modelo'), 'Onix');
    await u.click(screen.getByRole('button', { name: 'Cadastrar' }));
    expect(await screen.findByRole('alert')).toHaveTextContent('Placa já cadastrada.');
  });
});
