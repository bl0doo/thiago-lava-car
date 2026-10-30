import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { render, screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter } from 'react-router-dom';
import type { Agendamento } from '@/api/agendamentos';
import { hojeIso } from '@/lib/format';
import { erroHttp } from '@/test/fabrica';
import { Agenda } from './dono/Agenda';

const api = vi.hoisted(() => ({
  obterAgenda: vi.fn(),
  cancelarAgendamento: vi.fn(),
  marcarNaoCompareceu: vi.fn(),
  fazerCheckIn: vi.fn(),
}));
vi.mock('@/api/agendamentos', () => api);

const agendamento = (parcial: Partial<Agendamento> = {}): Agendamento => ({
  id: 1,
  clienteId: 5,
  veiculoId: 9,
  placa: 'ABC1D23',
  servicoId: 2,
  servico: 'Lavagem completa',
  comCera: false,
  inicio: '2026-10-23T13:00:00-03:00',
  duracaoEstimadaMin: 60,
  precoEstimado: 80,
  status: 'Agendado',
  cliente: 'Renata Souza',
  ...parcial,
});

const agenda = (...itens: Agendamento[]) => ({ data: hojeIso(), agendamentos: itens });

function renderizar() {
  render(
    <QueryClientProvider client={new QueryClient({ defaultOptions: { queries: { retry: false } } })}>
      <MemoryRouter>
        <Agenda />
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

describe('Agenda do dono', () => {
  beforeEach(() => {
    vi.resetAllMocks();
    api.obterAgenda.mockResolvedValue(agenda(agendamento(), agendamento({ id: 2, placa: 'XYZ9K88', status: 'Cancelado' })));
  });

  it('lista os horários de hoje e só oferece ações enquanto está agendado', async () => {
    renderizar();

    const lista = await screen.findByRole('list', { name: 'Horários do dia' });
    const [primeiro, segundo] = within(lista).getAllByRole('listitem');
    expect(api.obterAgenda).toHaveBeenCalledWith(hojeIso());
    expect(within(primeiro!).getByText('Agendado')).toBeInTheDocument();
    expect(within(primeiro!).getByRole('button', { name: 'Fazer check-in' })).toBeInTheDocument();
    expect(within(segundo!).getByText('Cancelado')).toBeInTheDocument();
    expect(within(segundo!).queryByRole('button')).not.toBeInTheDocument();
  });

  it('mostra estado vazio quando o dia não tem horários', async () => {
    api.obterAgenda.mockResolvedValue(agenda());

    renderizar();

    expect(await screen.findByText('Nenhum horário neste dia')).toBeInTheDocument();
  });

  it('marca falta depois de confirmar', async () => {
    api.marcarNaoCompareceu.mockResolvedValue(agendamento({ status: 'NaoCompareceu' }));
    api.obterAgenda.mockResolvedValueOnce(agenda(agendamento())).mockResolvedValue(agenda(agendamento({ status: 'NaoCompareceu' })));

    renderizar();
    const lista = await screen.findByRole('list', { name: 'Horários do dia' });
    await userEvent.click(within(lista).getByRole('button', { name: 'Não compareceu' }));
    const dialogo = await screen.findByRole('dialog', { name: 'Marcar falta?', hidden: true });
    await userEvent.click(within(dialogo).getByRole('button', { name: 'Não compareceu', hidden: true }));

    await waitFor(() => expect(api.marcarNaoCompareceu.mock.calls[0]?.[0]).toBe(1));
    expect(await within(lista).findByText('Não compareceu')).toBeInTheDocument();
  });

  it('cancela o horário depois de confirmar', async () => {
    api.cancelarAgendamento.mockResolvedValue(agendamento({ status: 'Cancelado' }));

    renderizar();
    const lista = await screen.findByRole('list', { name: 'Horários do dia' });
    await userEvent.click(within(lista).getByRole('button', { name: 'Cancelar horário' }));
    const dialogo = await screen.findByRole('dialog', { name: 'Cancelar este horário?', hidden: true });
    await userEvent.click(within(dialogo).getByRole('button', { name: 'Cancelar horário', hidden: true }));

    await waitFor(() => expect(api.cancelarAgendamento.mock.calls[0]?.[0]).toBe(1));
  });

  it('mostra o erro 409 da API ao cancelar', async () => {
    api.cancelarAgendamento.mockRejectedValue(erroHttp(409, { detail: 'Este agendamento já foi atendido.' }));

    renderizar();
    const lista = await screen.findByRole('list', { name: 'Horários do dia' });
    await userEvent.click(within(lista).getByRole('button', { name: 'Cancelar horário' }));
    const dialogo = await screen.findByRole('dialog', { name: 'Cancelar este horário?', hidden: true });
    await userEvent.click(within(dialogo).getByRole('button', { name: 'Cancelar horário', hidden: true }));

    expect(await screen.findByText('Este agendamento já foi atendido.')).toBeInTheDocument();
  });

  it('check-in exige a vistoria e o aceite antes de chamar a API', async () => {
    api.fazerCheckIn.mockResolvedValue({ id: 77 });

    renderizar();
    await userEvent.click(await screen.findByRole('button', { name: 'Fazer check-in' }));
    const secao = await screen.findByRole('region', { name: 'Check-in de ABC1D23' });

    await userEvent.click(within(secao).getByRole('button', { name: 'Registrar entrada' }));
    expect(await within(secao).findByText('O cliente precisa concordar com a vistoria.')).toBeInTheDocument();
    expect(api.fazerCheckIn).not.toHaveBeenCalled();

    await userEvent.click(within(secao).getByLabelText(/Sem avarias/));
    await userEvent.click(within(secao).getByLabelText(/O cliente viu a vistoria/));
    await userEvent.click(within(secao).getByRole('button', { name: 'Registrar entrada' }));

    await waitFor(() => expect(api.fazerCheckIn).toHaveBeenCalled());
    expect(api.fazerCheckIn.mock.calls[0]).toEqual([1, { semAvarias: true, avarias: [], pertencesEntrada: null, nomeQuemRetira: null, aceiteCliente: true }]);
    expect(await screen.findByText(/Check-in feito/)).toBeInTheDocument();
  });

  it('mostra o erro e deixa tentar de novo quando a agenda não carrega', async () => {
    api.obterAgenda.mockRejectedValueOnce(erroHttp(500, { detail: 'Erro ao carregar.' })).mockResolvedValue(agenda(agendamento()));

    renderizar();
    expect(await screen.findByText('Erro ao carregar.')).toBeInTheDocument();
    await userEvent.click(screen.getByRole('button', { name: 'Tentar de novo' }));

    expect(await screen.findByRole('list', { name: 'Horários do dia' })).toBeInTheDocument();
  });
});
