import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { render, screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter } from 'react-router-dom';
import type { Agendamento } from '@/api/agendamentos';
import { erroHttp } from '@/test/fabrica';
import { ListaAgendamentos } from './cliente/ListaAgendamentos';

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

function renderizar(mostrar: 'proximos' | 'anteriores' = 'proximos') {
  return render(
    <QueryClientProvider client={new QueryClient({ defaultOptions: { queries: { retry: false } } })}>
      <MemoryRouter>
        <ListaAgendamentos mostrar={mostrar} />
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

describe('Lista de agendamentos (cliente)', () => {
  beforeEach(() => {
    vi.resetAllMocks();
    api.listarMeusAgendamentos.mockResolvedValue([
      agendamento({ id: 3, inicio: '2026-11-02T10:00:00-03:00' }),
      agendamento({ id: 1 }),
      agendamento({ id: 2, status: 'Atendido', inicio: '2026-10-01T09:00:00-03:00' }),
    ]);
  });

  it('lista os próximos do mais perto para o mais longe, sem os anteriores', async () => {
    renderizar();

    const proximos = await screen.findByRole('region', { name: 'Próximos' });
    const itens = within(proximos).getAllByRole('listitem');
    expect(itens).toHaveLength(2);
    expect(within(itens[0]!).getByText(/30\/10/)).toBeInTheDocument();
    expect(within(itens[1]!).getByText(/02\/11/)).toBeInTheDocument();
    expect(screen.queryByText('Atendido')).not.toBeInTheDocument();
  });

  it('mostra os anteriores sem botão de cancelar', async () => {
    renderizar('anteriores');

    const anteriores = await screen.findByRole('region', { name: 'Anteriores' });
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

  it('sem próximos, não mostra nada', async () => {
    api.listarMeusAgendamentos.mockResolvedValue([]);

    const { container } = renderizar();

    await waitFor(() => expect(api.listarMeusAgendamentos).toHaveBeenCalled());
    expect(screen.queryByRole('region')).not.toBeInTheDocument();
    expect(container).not.toHaveTextContent('horários');
  });

  it('sem histórico, explica que está vazio', async () => {
    api.listarMeusAgendamentos.mockResolvedValue([]);

    renderizar('anteriores');

    expect(await screen.findByText('Nenhum atendimento ainda')).toBeInTheDocument();
  });

  it('deixa tentar de novo quando o histórico não carrega', async () => {
    api.listarMeusAgendamentos.mockRejectedValueOnce(erroHttp(500, { detail: 'Falha.' })).mockResolvedValue([agendamento({ status: 'Atendido' })]);

    renderizar('anteriores');
    expect(await screen.findByText('Falha.')).toBeInTheDocument();
    await userEvent.click(screen.getByRole('button', { name: 'Tentar de novo' }));

    expect(await screen.findByRole('region', { name: 'Anteriores' })).toBeInTheDocument();
  });
});
