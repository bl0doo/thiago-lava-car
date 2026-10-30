import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { render, screen } from '@testing-library/react';
import { createMemoryRouter, RouterProvider } from 'react-router-dom';
import { erroHttp } from '@/test/fabrica';
import { FichaDoCarro } from './FichaDoCarro';

const api = vi.hoisted(() => ({ obterFicha: vi.fn() }));
vi.mock('@/api/veiculos', () => api);

function renderizar() {
  const router = createMemoryRouter([{ path: '/dono/veiculos/:id', element: <FichaDoCarro /> }], { initialEntries: ['/dono/veiculos/30'] });
  render(
    <QueryClientProvider client={new QueryClient({ defaultOptions: { queries: { retry: false } } })}>
      <RouterProvider router={router} />
    </QueryClientProvider>,
  );
}

const veiculo = { id: 30, clienteId: 12, placa: 'ABC1D23', modelo: 'Chevrolet Onix', tipo: 'Carro', cor: 'Prata' };

describe('Ficha do carro', () => {
  beforeEach(() => vi.resetAllMocks());

  it('mostra o veículo e a linha do tempo de visitas', async () => {
    api.obterFicha.mockResolvedValue({
      veiculo,
      totalServicos: 2,
      historico: [
        { id: 88, data: '2026-10-20T16:05:00Z', status: 'Concluido', servicos: ['Lavagem completa', 'Motor'], valorTotal: 220, duracaoRealMin: 102 },
        { id: 70, data: '2026-09-02T13:00:00Z', status: 'Concluido', servicos: ['Lavagem simples'], valorTotal: 60, duracaoRealMin: null },
      ],
    });
    renderizar();
    expect(await screen.findByText('2 visitas na Thiago Lava Car')).toBeInTheDocument();
    expect(screen.getByText('ABC1D23')).toBeInTheDocument();
    const itens = screen.getAllByRole('listitem');
    expect(itens).toHaveLength(2);
    expect(itens[0]).toHaveTextContent('Lavagem completa, Motor');
    expect(itens[0]).toHaveTextContent('1 h 42 min');
    expect(itens[0]).toHaveTextContent('20/10/2026');
  });

  it('mostra o estado vazio quando não há visitas', async () => {
    api.obterFicha.mockResolvedValue({ veiculo, totalServicos: 0, historico: [] });
    renderizar();
    expect(await screen.findByText('Sem visitas ainda')).toBeInTheDocument();
  });

  it('mostra o 404 como erro com botão de tentar de novo', async () => {
    api.obterFicha.mockRejectedValue(erroHttp(404, { detail: 'Veículo não encontrado.' }));
    renderizar();
    expect(await screen.findByRole('alert')).toHaveTextContent('Veículo não encontrado.');
    expect(screen.getByRole('button', { name: 'Tentar de novo' })).toBeInTheDocument();
  });
});
