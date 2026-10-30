import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter } from 'react-router-dom';
import { erroHttp } from '@/test/fabrica';
import { Painel } from './dono/Painel';

const api = vi.hoisted(() => ({ obterPainelHoje: vi.fn() }));
vi.mock('@/api/painel', () => api);

function renderizar() {
  render(
    <QueryClientProvider client={new QueryClient({ defaultOptions: { queries: { retry: false } } })}>
      <MemoryRouter>
        <Painel />
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

describe('Painel do dono', () => {
  beforeEach(() => vi.resetAllMocks());

  it('mostra Entrou hoje, Sobrou de verdade, carros que faltam e pendências', async () => {
    api.obterPainelHoje.mockResolvedValue({
      entrouHoje: 640,
      liquidoHoje: 633.02,
      sobrouDeVerdade: 256.02,
      carrosNoMes: 95,
      carrosFaltamParaPagarContas: 25,
      pendencias: ['Caixa de 18/10 não fechado'],
    });
    renderizar();
    expect(await screen.findByText(/R\$\s?640,00/)).toBeInTheDocument();
    expect(screen.getByText(/R\$\s?256,02/)).toBeInTheDocument();
    expect(screen.getByText('Faltam 25 carros para pagar as contas')).toBeInTheDocument();
    expect(screen.getByText('Caixa de 18/10 não fechado')).toBeInTheDocument();
    expect(screen.getByRole('link', { name: 'Nova entrada' })).toHaveAttribute('href', '/dono/entrada');
  });

  it('quando a API não consegue estimar as contas (null), diz isso em vez de mostrar um número', async () => {
    api.obterPainelHoje.mockResolvedValue({ entrouHoje: 0, liquidoHoje: 0, sobrouDeVerdade: 0, carrosNoMes: 3, carrosFaltamParaPagarContas: null, pendencias: [] });
    renderizar();

    expect(await screen.findByText('Ainda não dá para estimar as contas do mês.')).toBeInTheDocument();
    expect(screen.queryByText(/Faltam/)).not.toBeInTheDocument();
  });

  it('diz que as contas estão pagas quando não faltam carros', async () => {
    api.obterPainelHoje.mockResolvedValue({ entrouHoje: 0, liquidoHoje: 0, sobrouDeVerdade: 0, carrosNoMes: 130, carrosFaltamParaPagarContas: 0, pendencias: [] });
    renderizar();
    expect(await screen.findByText('Contas do mês pagas.')).toBeInTheDocument();
    expect(screen.queryByRole('list', { name: 'Pendências' })).not.toBeInTheDocument();
  });

  it('mostra erro e tenta de novo', async () => {
    const u = userEvent.setup();
    api.obterPainelHoje.mockRejectedValueOnce(erroHttp(500)).mockResolvedValue({ entrouHoje: 10, liquidoHoje: 9, sobrouDeVerdade: 5, carrosNoMes: 1, carrosFaltamParaPagarContas: 1, pendencias: [] });
    renderizar();
    expect(await screen.findByRole('alert')).toHaveTextContent('Algo deu errado');
    await u.click(screen.getByRole('button', { name: 'Tentar de novo' }));
    expect(await screen.findByText(/R\$\s?10,00/)).toBeInTheDocument();
  });
});
