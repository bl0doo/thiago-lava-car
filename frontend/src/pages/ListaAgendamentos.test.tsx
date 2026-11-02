import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { render, screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter } from 'react-router-dom';
import type { Agendamento } from '@/api/agendamentos';
import { erroHttp } from '@/test/fabrica';
import { MeusAgendamentos } from './cliente/MeusAgendamentos';

const api = vi.hoisted(() => ({ listarMeusAgendamentos: vi.fn(), cancelarAgendamento: vi.fn() }));
vi.mock('@/api/agendamentos', () => api);

const agendamento = (parcial: Partial<Agendamento> = {}): Agendamento => ({
  id: 1,
  clienteId: 5,
  veiculoId: 9,
  placa: 'ABC1D23',
  servicoId: 2,
  servico: 'Lavagem completa',
  comCera: true,
  inicio: '2026-10-30T13:00:00-03:00',
  duracaoEstimadaMin: 60,
  precoEstimado: 80,
  status: 'Agendado',
  ...parcial,
});

function renderizar() {
  render(
    <QueryClientProvider client={new QueryClient({ defaultOptions: { queries: { retry: false } } })}>
      <MemoryRouter>
        <MeusAgendamentos />
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

describe('Meus agendamentos (cliente)', () => {
  beforeEach(() => {
    vi.resetAllMocks();
    api.listarMeusAgendamentos.mockResolvedValue([
      agendamento({ id: 3, inicio: '2026-11-02T10:00:00-03:00' }),
      agendamento({ id: 1 }),
      agendamento({ id: 2, status: 'Atendido', inicio: '2026-10-01T09:00:00-03:00' }),
    ]);
  });

  it('separa próximos (do mais perto para o mais longe) dos anteriores', async () => {
    renderizar();

    const proximos = await screen.findByRole('region', { name: 'Próximos' });
    const itens = within(proximos).getAllByRole('listitem');
    expect(itens).toHaveLength(2);
    expect(within(itens[0]!).getByText(/30\/10/)).toBeInTheDocument();
    expect(within(itens[1]!).getByText(/02\/11/)).toBeInTheDocument();
    const anteriores = screen.getByRole('region', { name: 'Anteriores' });
    expect(within(anteriores).getByText('Atendido')).toBeInTheDocument();
    expect(within(anteriores).queryByRole('button')).not.toBeInTheDocument();
  });

  it('cancela um horário depois de confirmar', async () => {
    api.cancelarAgendamento.mockResolvedValue(agendamento({ status: 'Cancelado' }));

    renderizar();
    const proximos = await screen.findByRole('region', { name: 'Próximos' });
    await userEvent.click(within(within(proximos).getAllByRole('listitem')[0]!).getByRole('button', { name: 'Cancelar horário' }));
    const dialogo = await screen.findByRole('dialog', { name: 'Cancelar este horário?', hidden: true });
    await userEvent.click(within(dialogo).getByRole('button', { name: 'Cancelar horário', hidden: true }));

    await waitFor(() => expect(api.cancelarAgendamento.mock.calls[0]?.[0]).toBe(1));
  });

  it('mostra o erro da API ao cancelar', async () => {
    api.cancelarAgendamento.mockRejectedValue(erroHttp(409, { detail: 'Agendamento já atendido.' }));

    renderizar();
    const proximos = await screen.findByRole('region', { name: 'Próximos' });
    await userEvent.click(within(within(proximos).getAllByRole('listitem')[0]!).getByRole('button', { name: 'Cancelar horário' }));
    const dialogo = await screen.findByRole('dialog', { name: 'Cancelar este horário?', hidden: true });
    await userEvent.click(within(dialogo).getByRole('button', { name: 'Cancelar horário', hidden: true }));

    expect(await screen.findByText('Agendamento já atendido.')).toBeInTheDocument();
  });

  it('sem horários, convida a agendar', async () => {
    api.listarMeusAgendamentos.mockResolvedValue([]);

    renderizar();

    expect(await screen.findByText('Você ainda não tem horários')).toBeInTheDocument();
    expect(screen.getByRole('link', { name: 'Agendar um horário' })).toHaveAttribute('href', '/cliente/agendar');
  });

  it('deixa tentar de novo quando a lista não carrega', async () => {
    api.listarMeusAgendamentos.mockRejectedValueOnce(erroHttp(500, { detail: 'Falha.' })).mockResolvedValue([agendamento()]);

    renderizar();
    expect(await screen.findByText('Falha.')).toBeInTheDocument();
    await userEvent.click(screen.getByRole('button', { name: 'Tentar de novo' }));

    expect(await screen.findByRole('region', { name: 'Próximos' })).toBeInTheDocument();
  });
});
