import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter } from 'react-router-dom';
import { MinhaConta } from './cliente/MinhaConta';

const clientes = vi.hoisted(() => ({ obterMeusDados: vi.fn(), atualizarMeusDados: vi.fn(), listarMeusVeiculos: vi.fn() }));
const agendamentos = vi.hoisted(() => ({ listarMeusAgendamentos: vi.fn(), cancelarAgendamento: vi.fn() }));
vi.mock('@/api/clientes', () => clientes);
vi.mock('@/api/agendamentos', () => agendamentos);
vi.mock('@/api/veiculos', () => ({ criarVeiculo: vi.fn() }));
vi.mock('@/auth/AuthContext', () => ({ useAuth: () => ({ sair: vi.fn() }) }));

function renderizar(url = '/cliente/conta') {
  render(
    <QueryClientProvider client={new QueryClient({ defaultOptions: { queries: { retry: false } } })}>
      <MemoryRouter initialEntries={[url]}>
        <MinhaConta />
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

describe('Minha conta (cliente)', () => {
  beforeEach(() => {
    vi.resetAllMocks();
    clientes.listarMeusVeiculos.mockResolvedValue([]);
    clientes.obterMeusDados.mockResolvedValue({ id: 1, nome: 'Renata', telefone: '44999998888', email: null, canalPreferido: 'WhatsApp', consentimentoLgpd: true, veiculos: [] });
    agendamentos.listarMeusAgendamentos.mockResolvedValue([]);
  });

  it('abre em Veículos e troca de seção pelas abas', async () => {
    const u = userEvent.setup();
    renderizar();
    expect(await screen.findByText('Nenhum carro cadastrado')).toBeInTheDocument();

    await u.click(screen.getByRole('tab', { name: 'Histórico' }));
    expect(await screen.findByText('Nenhum atendimento ainda')).toBeInTheDocument();

    await u.click(screen.getByRole('tab', { name: 'Meus dados' }));
    expect(await screen.findByDisplayValue('Renata')).toBeInTheDocument();
  });

  it('respeita a aba pedida na URL', async () => {
    renderizar('/cliente/conta?aba=dados');
    expect(await screen.findByDisplayValue('Renata')).toBeInTheDocument();
    expect(screen.getByRole('tab', { name: 'Meus dados' })).toHaveAttribute('aria-selected', 'true');
  });
});
