import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { createMemoryRouter, RouterProvider } from 'react-router-dom';
import { cartao, erroHttp } from '@/test/fabrica';
import { PertencesEncontrados } from './lavador/PertencesEncontrados';

const api = vi.hoisted(() => ({ obterCartao: vi.fn(), registrarPertencesEncontrados: vi.fn() }));
vi.mock('@/api/ordensServico', () => api);

function renderizar(id = '7') {
  const cliente = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  const roteador = createMemoryRouter(
    [
      { path: '/lavador/os/:id', element: <PertencesEncontrados /> },
      { path: '/lavador', element: <p>Fila</p> },
    ],
    { initialEntries: [`/lavador/os/${id}`] },
  );
  render(
    <QueryClientProvider client={cliente}>
      <RouterProvider router={roteador} />
    </QueryClientProvider>,
  );
}

describe('Pertences encontrados (lavador)', () => {
  beforeEach(() => {
    vi.resetAllMocks();
    api.obterCartao.mockResolvedValue(cartao({ id: 7, status: 'EmAndamento' }));
  });

  it('mostra a placa do carro e salva o texto digitado', async () => {
    const usuario = userEvent.setup();
    api.registrarPertencesEncontrados.mockResolvedValue(cartao({ id: 7 }));
    renderizar();

    expect(await screen.findByText('ABC1D23')).toBeInTheDocument();
    await usuario.type(screen.getByLabelText('O que você encontrou?'), 'Óculos no porta-luvas');
    await usuario.click(screen.getByRole('button', { name: 'Salvar' }));

    await waitFor(() => expect(api.registrarPertencesEncontrados).toHaveBeenCalled());
    expect(api.registrarPertencesEncontrados.mock.calls[0]).toEqual([7, 'Óculos no porta-luvas']);
    expect(await screen.findByText('Salvo. O dono já pode ver.')).toBeInTheDocument();
  });

  it('texto vazio envia nulo para limpar o registro', async () => {
    const usuario = userEvent.setup();
    api.registrarPertencesEncontrados.mockResolvedValue(cartao({ id: 7 }));
    renderizar();

    await usuario.click(await screen.findByRole('button', { name: 'Salvar' }));

    await waitFor(() => expect(api.registrarPertencesEncontrados).toHaveBeenCalled());
    expect(api.registrarPertencesEncontrados.mock.calls[0]).toEqual([7, null]);
  });

  it('mostra o erro da API (OS já entregue, 409) sem fingir que salvou', async () => {
    const usuario = userEvent.setup();
    api.registrarPertencesEncontrados.mockRejectedValue(erroHttp(409, { detail: 'Esta OS já foi entregue.' }));
    renderizar();

    await usuario.type(await screen.findByLabelText('O que você encontrou?'), 'Carregador');
    await usuario.click(screen.getByRole('button', { name: 'Salvar' }));

    expect(await screen.findByRole('alert')).toHaveTextContent('Esta OS já foi entregue.');
    expect(screen.queryByText(/Salvo/)).not.toBeInTheDocument();
  });

  it('cartão inexistente mostra aviso e volta para a fila', async () => {
    const usuario = userEvent.setup();
    api.obterCartao.mockRejectedValue(erroHttp(404));
    renderizar();

    expect(await screen.findByText('Este cartão não existe mais.')).toBeInTheDocument();
    await usuario.click(screen.getByRole('button', { name: 'Voltar à fila' }));
    expect(await screen.findByText('Fila')).toBeInTheDocument();
  });

  it('id inválido não chama a API', () => {
    renderizar('abc');

    expect(screen.getByText(/Cartão não encontrado/)).toBeInTheDocument();
    expect(api.obterCartao).not.toHaveBeenCalled();
  });
});
