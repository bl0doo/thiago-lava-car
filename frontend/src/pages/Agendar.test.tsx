import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { render, screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter } from 'react-router-dom';
import { hojeIso } from '@/lib/format';
import { erroHttp } from '@/test/fabrica';
import { Agendar } from './cliente/Agendar';

const agendamentos = vi.hoisted(() => ({ listarHorarios: vi.fn(), criarAgendamento: vi.fn() }));
const clientes = vi.hoisted(() => ({ listarMeusVeiculos: vi.fn() }));
const servicos = vi.hoisted(() => ({ listarServicos: vi.fn() }));
vi.mock('@/api/agendamentos', () => agendamentos);
vi.mock('@/api/clientes', () => clientes);
vi.mock('@/api/servicos', () => servicos);

const onix = { id: 9, placa: 'ABC1D23', modelo: 'Chevrolet Onix', tipo: 'Carro' as const };
const completa = {
  id: 2,
  nome: 'Lavagem completa',
  categoria: 'Lavagem',
  tempoEstimadoMin: 60,
  ativo: true,
  precos: [{ tipoVeiculo: 'Carro' as const, preco: 80, precoComCera: 110 }],
};

function renderizar() {
  render(
    <QueryClientProvider client={new QueryClient({ defaultOptions: { queries: { retry: false } } })}>
      <MemoryRouter>
        <Agendar />
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

async function escolherServico() {
  await userEvent.selectOptions(await screen.findByLabelText('Veículo'), '9');
  await userEvent.selectOptions(screen.getByLabelText('Serviço'), '2');
}

describe('Agendar (cliente)', () => {
  beforeEach(() => {
    vi.resetAllMocks();
    clientes.listarMeusVeiculos.mockResolvedValue([onix]);
    servicos.listarServicos.mockResolvedValue([completa, { ...completa, id: 3, nome: 'Antigo', ativo: false }]);
    agendamentos.listarHorarios.mockResolvedValue({ preco: 80, duracaoEstimadaMin: 60, horarios: ['09:00', '09:30', '10:00'] });
    agendamentos.criarAgendamento.mockResolvedValue({ id: 1 });
  });

  it('só oferece serviços ativos e mostra horários livres do dia escolhido', async () => {
    renderizar();
    await escolherServico();

    const grupo = await screen.findByRole('group', { name: 'Horários livres' });
    expect(within(grupo).getAllByRole('button').map((b) => b.textContent)).toEqual(['09:00', '09:30', '10:00']);
    expect(screen.queryByRole('option', { name: 'Antigo' })).not.toBeInTheDocument();
    expect(agendamentos.listarHorarios).toHaveBeenCalledWith(hojeIso(), 2, 'Carro', false);
  });

  it('com cera recarrega os horários e o preço', async () => {
    renderizar();
    await escolherServico();
    await screen.findByRole('group', { name: 'Horários livres' });

    await userEvent.click(screen.getByLabelText('Com cera'));

    await waitFor(() => expect(agendamentos.listarHorarios).toHaveBeenCalledWith(hojeIso(), 2, 'Carro', true));
  });

  it('mostra o resumo com preço e duração e confirma com o horário de Brasília', async () => {
    renderizar();
    await escolherServico();
    await userEvent.click(await screen.findByRole('button', { name: '09:30' }));

    const resumo = await screen.findByRole('region', { name: 'Resumo' });
    expect(within(resumo).getByText(/R\$\s80,00/)).toBeInTheDocument();
    await userEvent.click(screen.getByRole('button', { name: 'Confirmar horário' }));

    expect(await screen.findByText('Horário reservado')).toBeInTheDocument();
    expect(agendamentos.criarAgendamento.mock.calls[0]?.[0]).toEqual({ veiculoId: 9, servicoId: 2, comCera: false, inicio: `${hojeIso()}T09:30:00-03:00` });
    expect(screen.getByRole('link', { name: 'Ver meus horários' })).toHaveAttribute('href', '/cliente/agendamentos');
  });

  it('não deixa confirmar sem escolher um horário', async () => {
    renderizar();
    await escolherServico();
    await screen.findByRole('group', { name: 'Horários livres' });

    expect(screen.getByRole('button', { name: 'Confirmar horário' })).toBeDisabled();
  });

  it('se o horário foi ocupado (409), mostra o erro e recarrega a grade', async () => {
    agendamentos.criarAgendamento.mockRejectedValue(erroHttp(409, { detail: 'Este horário acabou de ser ocupado.' }));

    renderizar();
    await escolherServico();
    await userEvent.click(await screen.findByRole('button', { name: '10:00' }));
    await userEvent.click(screen.getByRole('button', { name: 'Confirmar horário' }));

    expect(await screen.findByText('Este horário acabou de ser ocupado.')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Confirmar horário' })).toBeDisabled();
    await waitFor(() => expect(agendamentos.listarHorarios.mock.calls.length).toBeGreaterThan(1));
  });

  it('avisa quando o dia não tem horários livres', async () => {
    agendamentos.listarHorarios.mockResolvedValue({ preco: 80, duracaoEstimadaMin: 60, horarios: [] });

    renderizar();
    await escolherServico();

    expect(await screen.findByText('Não há horários livres neste dia. Tente outro dia.')).toBeInTheDocument();
  });

  it('pede para cadastrar um veículo quando o cliente não tem nenhum', async () => {
    clientes.listarMeusVeiculos.mockResolvedValue([]);

    renderizar();

    expect(await screen.findByText('Cadastre um veículo primeiro')).toBeInTheDocument();
    expect(screen.getByRole('link', { name: 'Meus veículos' })).toHaveAttribute('href', '/cliente/veiculos');
  });
});
