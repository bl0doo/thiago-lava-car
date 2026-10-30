import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { render, screen, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { createMemoryRouter, RouterProvider } from 'react-router-dom';
import { erroHttp } from '@/test/fabrica';
import { ServicoForm } from './dono/ServicoForm';
import { Servicos } from './dono/Servicos';

const api = vi.hoisted(() => ({ listarServicos: vi.fn(), criarServico: vi.fn(), atualizarServico: vi.fn(), alterarAtivoServico: vi.fn() }));
vi.mock('@/api/servicos', () => api);

function renderizar(inicial: string) {
  const router = createMemoryRouter(
    [
      { path: '/dono/servicos', element: <Servicos /> },
      { path: '/dono/servicos/novo', element: <ServicoForm /> },
      { path: '/dono/servicos/:id', element: <ServicoForm /> },
    ],
    { initialEntries: [inicial] },
  );
  render(
    <QueryClientProvider client={new QueryClient({ defaultOptions: { queries: { retry: false } } })}>
      <RouterProvider router={router} />
    </QueryClientProvider>,
  );
}

const lavagem = {
  id: 1,
  nome: 'Lavagem completa',
  categoria: 'Lavagem',
  tempoEstimadoMin: 90,
  ativo: true,
  custoInsumos: 8,
  precos: [
    { tipoVeiculo: 'Carro', preco: 80, precoComCera: 100 },
    { tipoVeiculo: 'Moto', preco: 40, precoComCera: null },
  ],
};

describe('Serviços e preços', () => {
  beforeEach(() => vi.resetAllMocks());

  it('lista o catálogo com preços por tipo, tempo e insumos', async () => {
    api.listarServicos.mockResolvedValue([lavagem]);
    renderizar('/dono/servicos');
    expect(await screen.findByText('Lavagem completa')).toBeInTheDocument();
    expect(screen.getByText(/1 h 30 min/)).toBeInTheDocument();
    expect(screen.getByText(/Carro: R\$\s?80,00 · cera R\$\s?100,00/)).toBeInTheDocument();
  });

  it('desativa um serviço', async () => {
    const u = userEvent.setup();
    api.listarServicos.mockResolvedValue([lavagem]);
    api.alterarAtivoServico.mockResolvedValue(undefined);
    renderizar('/dono/servicos');
    await u.click(await screen.findByRole('button', { name: 'Desativar' }));
    expect(api.alterarAtivoServico).toHaveBeenCalledWith(1, false);
  });

  it('mostra o erro da API', async () => {
    api.listarServicos.mockRejectedValue(erroHttp(500));
    renderizar('/dono/servicos');
    expect(await screen.findByRole('alert')).toBeInTheDocument();
  });

  it('valida o formulário antes de enviar', async () => {
    const u = userEvent.setup();
    renderizar('/dono/servicos/novo');
    await u.click(screen.getByRole('button', { name: 'Salvar serviço' }));
    expect(await screen.findByText('O nome deve ter ao menos 2 caracteres.')).toBeInTheDocument();
    expect(screen.getByText('Informe o tempo em minutos, entre 5 e 600.')).toBeInTheDocument();
    expect(api.criarServico).not.toHaveBeenCalled();
  });

  it('exige preço de ao menos um tipo e cria o serviço', async () => {
    const u = userEvent.setup();
    api.criarServico.mockResolvedValue(lavagem);
    api.listarServicos.mockResolvedValue([]);
    renderizar('/dono/servicos/novo');
    await u.type(screen.getByLabelText('Nome'), 'Polimento');
    await u.type(screen.getByLabelText('Categoria'), 'Polimento');
    await u.type(screen.getByLabelText('Tempo estimado (minutos)'), '120');
    await u.type(screen.getByLabelText('Custo de insumos (R$)'), '15,50');
    await u.click(screen.getByRole('button', { name: 'Salvar serviço' }));
    expect(await screen.findByText('Informe o preço de ao menos um tipo de veículo.')).toBeInTheDocument();

    await u.type(screen.getByLabelText('Carro (R$)'), '250');
    await u.type(screen.getAllByLabelText('Com cera (R$)')[1]!, '300,00');
    await u.click(screen.getByRole('button', { name: 'Salvar serviço' }));
    expect(await screen.findByRole('heading', { name: 'Serviços e preços' })).toBeInTheDocument();
    expect(api.criarServico.mock.calls[0]?.[0]).toEqual({
      nome: 'Polimento',
      categoria: 'Polimento',
      tempoEstimadoMin: 120,
      custoInsumos: 15.5,
      precos: [{ tipoVeiculo: 'Carro', preco: 250, precoComCera: 300 }],
    });
  });

  it('edita carregando os valores atuais', async () => {
    const u = userEvent.setup();
    api.listarServicos.mockResolvedValue([lavagem]);
    api.atualizarServico.mockResolvedValue(lavagem);
    renderizar('/dono/servicos/1');
    expect(await screen.findByDisplayValue('Lavagem completa')).toBeInTheDocument();
    expect(screen.getByDisplayValue('80,00')).toBeInTheDocument();
    await u.clear(screen.getByLabelText('Tempo estimado (minutos)'));
    await u.type(screen.getByLabelText('Tempo estimado (minutos)'), '100');
    await u.click(screen.getByRole('button', { name: 'Salvar serviço' }));
    await vi.waitFor(() => expect(api.atualizarServico).toHaveBeenCalled());
    expect(api.atualizarServico.mock.calls[0]?.[1]).toMatchObject({ tempoEstimadoMin: 100, precos: [{ tipoVeiculo: 'Moto', preco: 40, precoComCera: null }, { tipoVeiculo: 'Carro', preco: 80, precoComCera: 100 }] });
    void within;
  });
});
