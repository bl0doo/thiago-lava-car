import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { render, screen, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter } from 'react-router-dom';
import { hojeIso } from '@/lib/format';
import { erroHttp } from '@/test/fabrica';
import { Relatorios } from './dono/Relatorios';

const api = vi.hoisted(() => ({
  lucroPorServico: vi.fn(),
  faturamento: vi.fn(),
  produtividade: vi.fn(),
  pontoEquilibrio: vi.fn(),
}));
vi.mock('@/api/relatorios', () => api);

const linhaLucro = (parcial = {}) => ({
  servico: 'Lavagem completa',
  quantidade: 4,
  receitaLiquida: 400,
  insumos: 40,
  maoDeObra: 60,
  lucro: 300,
  lucroMedio: 75,
  tempoMedioMin: 45,
  ...parcial,
});

const equilibrio = (parcial = {}) => ({
  mes: hojeIso().slice(0, 7),
  despesas: 500,
  diarias: 300,
  contas: 800,
  margemPorCarro: 80,
  margemEstimada: false,
  carrosNecessarios: 10,
  carrosFeitos: 7,
  faltam: 3,
  ...parcial,
});

function renderizar() {
  render(
    <QueryClientProvider client={new QueryClient({ defaultOptions: { queries: { retry: false } } })}>
      <MemoryRouter>
        <Relatorios />
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

describe('Relatórios', () => {
  beforeEach(() => {
    vi.resetAllMocks();
    api.lucroPorServico.mockResolvedValue([linhaLucro(), linhaLucro({ servico: 'Lavagem simples', quantidade: 1, lucro: 20 })]);
  });

  it('abre no lucro por serviço do mês corrente até hoje', async () => {
    renderizar();

    const lista = await screen.findByRole('list', { name: 'Serviços do mais para o menos lucrativo' });
    expect(within(lista).getAllByRole('listitem')).toHaveLength(2);
    expect(within(lista).getByRole('heading', { name: '1. Lavagem completa' })).toBeInTheDocument();
    const hoje = hojeIso();
    expect(api.lucroPorServico).toHaveBeenCalledWith({ inicio: `${hoje.slice(0, 8)}01`, fim: hoje });
  });

  it('não consulta quando a data inicial é depois da final', async () => {
    renderizar();
    await screen.findByRole('list', { name: /Serviços/ });
    api.lucroPorServico.mockClear();

    await userEvent.clear(screen.getByLabelText('Até'));
    await userEvent.type(screen.getByLabelText('Até'), '2000-01-01');

    expect(await screen.findByText('A data inicial precisa ser igual ou anterior à final.')).toBeInTheDocument();
    expect(api.lucroPorServico).not.toHaveBeenCalled();
  });

  it('mostra estado vazio quando não há serviços no período', async () => {
    api.lucroPorServico.mockResolvedValue([]);

    renderizar();

    expect(await screen.findByText('Sem dados neste período')).toBeInTheDocument();
  });

  it('mostra o erro da API e deixa tentar de novo', async () => {
    api.lucroPorServico.mockRejectedValueOnce(erroHttp(500, { detail: 'Falha no relatório.' })).mockResolvedValue([linhaLucro()]);

    renderizar();
    expect(await screen.findByText('Falha no relatório.')).toBeInTheDocument();
    await userEvent.click(screen.getByRole('button', { name: 'Tentar de novo' }));

    expect(await screen.findByRole('heading', { name: '1. Lavagem completa' })).toBeInTheDocument();
  });

  it('faturamento mostra totais e os dias com movimento', async () => {
    api.faturamento.mockResolvedValue({
      dias: [{ data: '2026-10-20', bruto: 300, liquido: 290, osPagas: 3, ticketMedio: 100 }],
      totalBruto: 300,
      totalLiquido: 290,
      osPagas: 3,
      ticketMedio: 100,
    });

    renderizar();
    await userEvent.click(screen.getByRole('button', { name: 'Faturamento' }));

    const total = await screen.findByRole('region', { name: 'Total do período' });
    expect(within(total).getByText('R$ 290,00')).toBeInTheDocument();
    expect(within(screen.getByRole('list', { name: 'Por dia' })).getAllByRole('listitem')).toHaveLength(1);
  });

  it('equilíbrio diz quantos carros faltam e avisa quando a margem é estimada', async () => {
    api.pontoEquilibrio.mockResolvedValue(equilibrio({ margemEstimada: true }));

    renderizar();
    await userEvent.click(screen.getByRole('button', { name: 'Equilíbrio' }));

    expect(await screen.findByText('Faltam 3 carros para pagar as contas.')).toBeInTheDocument();
    expect(screen.getByText(/Margem estimada pelo catálogo/)).toBeInTheDocument();
    expect(api.pontoEquilibrio).toHaveBeenCalledWith(hojeIso().slice(0, 7));
  });

  it('equilíbrio sem estimativa possível explica em vez de mostrar zero', async () => {
    api.pontoEquilibrio.mockResolvedValue(equilibrio({ margemPorCarro: null, carrosNecessarios: null, faltam: null }));

    renderizar();
    await userEvent.click(screen.getByRole('button', { name: 'Equilíbrio' }));

    expect(await screen.findByText('Ainda não dá para estimar as contas do mês.')).toBeInTheDocument();
  });

  it('tempos compara o real com o previsto por serviço e por lavador', async () => {
    api.produtividade.mockResolvedValue({
      porServico: [{ servico: 'Lavagem completa', quantidade: 5, tempoMedioMin: 50, estimadoMedioMin: 45 }],
      porLavador: [{ lavador: 'Diego Alves', quantidade: 5, tempoMedioMin: 50 }],
    });

    renderizar();
    await userEvent.click(screen.getByRole('button', { name: 'Tempos' }));

    expect(await screen.findByText('Diego Alves')).toBeInTheDocument();
    expect(screen.getByText('Previsto')).toBeInTheDocument();
  });
});
