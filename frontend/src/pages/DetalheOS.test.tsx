import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { render, screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { createMemoryRouter, RouterProvider } from 'react-router-dom';
import { cartao, erroHttp } from '@/test/fabrica';
import { DetalheOS } from './dono/DetalheOS';

const api = vi.hoisted(() => ({
  obterOS: vi.fn(),
  iniciarLavagem: vi.fn(),
  encerrarLavagem: vi.fn(),
  desfazerUltimoPasso: vi.fn(),
  entregarOS: vi.fn(),
  cancelarOS: vi.fn(),
  avaliarOS: vi.fn(),
}));
vi.mock('@/api/ordensServico', () => api);

function renderizar(id = '7') {
  const cliente = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  const roteador = createMemoryRouter(
    [
      { path: '/dono/os/:id', element: <DetalheOS /> },
      { path: '/dono', element: <p>Painel</p> },
    ],
    { initialEntries: [`/dono/os/${id}`] },
  );
  render(
    <QueryClientProvider client={cliente}>
      <RouterProvider router={roteador} />
    </QueryClientProvider>,
  );
}

const detalhe = (parcial = {}) => ({
  ...cartao({ id: 7, valorTotal: 180 }),
  itens: [
    { servicoId: 1, nome: 'Lavagem completa', comCera: true, precoFinal: 100 },
    { servicoId: 2, nome: 'Higienização', comCera: false, precoFinal: 80 },
  ],
  pertencesEntrada: 'Óculos no painel',
  nomeQuemRetira: 'Marido da Renata',
  semAvarias: false,
  avarias: [{ local: 'Porta', tipo: 'Risco', descricao: 'lado direito' }],
  pertencesEncontrados: null,
  avaliacao: null,
  valorPago: 100,
  saldoPendente: 80,
  pagamentos: [],
  ...parcial,
});

describe('Detalhe da OS (dono)', () => {
  beforeEach(() => vi.resetAllMocks());

  it('mostra serviços, avarias, pertences e o saldo a receber', async () => {
    api.obterOS.mockResolvedValue(detalhe());
    renderizar();

    expect(await screen.findByText('Lavagem completa + cera')).toBeInTheDocument();
    expect(screen.getByText('Higienização')).toBeInTheDocument();
    expect(screen.getByText(/Porta: Risco/)).toBeInTheDocument();
    expect(screen.getByText(/Óculos no painel/)).toBeInTheDocument();
    expect(screen.getByText(/Marido da Renata/)).toBeInTheDocument();
    const falta = screen.getByText('Falta receber').parentElement!;
    expect(within(falta).getByText(/R\$\s?80,00/)).toBeInTheDocument();
    expect(api.obterOS).toHaveBeenCalledWith(7);
  });

  it('funciona sem os campos extras do contrato: cai nos nomes dos serviços do cartão', async () => {
    api.obterOS.mockResolvedValue(cartao({ id: 7, servicos: ['Lavagem completa', 'Cera'] }));
    renderizar();

    expect(await screen.findByText('Lavagem completa + Cera')).toBeInTheDocument();
    expect(screen.queryByText('Falta receber')).not.toBeInTheDocument();
  });

  it('em Aguardando inicia a lavagem e cancela só depois de confirmar', async () => {
    const usuario = userEvent.setup();
    const inicio = new Date().toISOString();
    api.obterOS.mockResolvedValueOnce(detalhe()).mockResolvedValue(detalhe({ status: 'EmAndamento', inicioEm: inicio }));
    api.iniciarLavagem.mockResolvedValue(cartao({ id: 7, status: 'EmAndamento', inicioEm: inicio }));
    renderizar();

    await usuario.click(await screen.findByRole('button', { name: 'Iniciar' }));
    await waitFor(() => expect(api.iniciarLavagem.mock.calls[0]?.[0]).toBe(7));
    expect(await screen.findByRole('button', { name: 'Encerrar' })).toBeInTheDocument();
    expect(screen.queryByRole('button', { name: 'Cancelar OS' })).not.toBeInTheDocument();
  });

  it('cancelar pede confirmação e leva a tela para Cancelada', async () => {
    const usuario = userEvent.setup();
    api.obterOS.mockResolvedValueOnce(detalhe()).mockResolvedValue(detalhe({ status: 'Cancelada' }));
    api.cancelarOS.mockResolvedValue(cartao({ id: 7, status: 'Cancelada' }));
    renderizar();

    await usuario.click(await screen.findByRole('button', { name: 'Cancelar OS' }));
    expect(api.cancelarOS).not.toHaveBeenCalled();
    const dialogo = await screen.findByRole('dialog', { name: 'Cancelar esta OS?', hidden: true });
    await usuario.click(within(dialogo).getByRole('button', { name: 'Cancelar OS', hidden: true }));

    await waitFor(() => expect(api.cancelarOS.mock.calls[0]?.[0]).toBe(7));
    expect(await screen.findByText('Cancelada')).toBeInTheDocument();
  });

  it('entrega o carro informando quem retirou e avisa do saldo pendente', async () => {
    const usuario = userEvent.setup();
    const concluida = { status: 'Concluido', fimEm: '2020-01-01T10:00:00Z', duracaoRealMin: 80 };
    api.obterOS.mockResolvedValueOnce(detalhe(concluida)).mockResolvedValue(detalhe({ status: 'Entregue' }));
    api.entregarOS.mockResolvedValue(cartao({ id: 7, status: 'Entregue' }));
    renderizar();

    await usuario.click(await screen.findByRole('button', { name: 'Entregar o carro' }));
    const dialogo = await screen.findByRole('dialog', { name: 'Entregar o carro?', hidden: true });
    expect(within(dialogo).getByText(/vira pendência no caixa/)).toBeInTheDocument();
    await usuario.type(within(dialogo).getByLabelText('Quem retirou (opcional)'), 'João');
    await usuario.click(within(dialogo).getByRole('button', { name: 'Entregar', hidden: true }));

    await waitFor(() => expect(api.entregarOS).toHaveBeenCalled());
    expect(api.entregarOS.mock.calls[0]).toEqual([7, 'João']);
    expect(await screen.findByText('Entregue')).toBeInTheDocument();
  });

  it('registra a nota do cliente depois de concluído', async () => {
    const usuario = userEvent.setup();
    api.obterOS.mockResolvedValue(detalhe({ status: 'Concluido', fimEm: '2020-01-01T10:00:00Z' }));
    api.avaliarOS.mockResolvedValue(cartao({ id: 7, status: 'Concluido' }));
    renderizar();

    await usuario.click(await screen.findByRole('button', { name: 'Nota 5' }));

    await waitFor(() => expect(api.avaliarOS).toHaveBeenCalled());
    expect(api.avaliarOS.mock.calls[0]).toEqual([7, 5]);
  });

  it('mostra a mensagem da API quando a ação é recusada (409)', async () => {
    const usuario = userEvent.setup();
    api.obterOS.mockResolvedValue(detalhe());
    api.iniciarLavagem.mockRejectedValue(erroHttp(409, { detail: 'Esta OS já foi iniciada.' }));
    renderizar();

    await usuario.click(await screen.findByRole('button', { name: 'Iniciar' }));

    expect(await screen.findByRole('alert')).toHaveTextContent('Esta OS já foi iniciada.');
  });

  it('mostra erro de carregamento com Tentar de novo, e 404 sem repetir', async () => {
    const usuario = userEvent.setup();
    api.obterOS.mockRejectedValueOnce(erroHttp(500)).mockResolvedValue(detalhe());
    renderizar();

    expect(await screen.findByRole('alert')).toBeInTheDocument();
    await usuario.click(screen.getByRole('button', { name: 'Tentar de novo' }));
    expect(await screen.findByText('Lavagem completa + cera')).toBeInTheDocument();
  });

  it('404 diz que o cartão não existe mais', async () => {
    api.obterOS.mockRejectedValue(erroHttp(404));
    renderizar();

    expect(await screen.findByText('Este cartão não existe mais.')).toBeInTheDocument();
    expect(screen.queryByRole('button', { name: 'Tentar de novo' })).not.toBeInTheDocument();
  });

  it('id inválido na rota não chama a API', () => {
    renderizar('abc');

    expect(screen.getByText('Cartão não encontrado')).toBeInTheDocument();
    expect(api.obterOS).not.toHaveBeenCalled();
  });

  it('oferece a mensagem de WhatsApp conforme a etapa da OS', async () => {
    api.obterOS.mockResolvedValue(detalhe({ status: 'EmAndamento', inicioEm: new Date().toISOString() }));

    renderizar();

    expect(await screen.findByRole('button', { name: 'Avisar que começou' })).toBeInTheDocument();
    expect(screen.queryByRole('button', { name: 'Avisar que ficou pronto' })).not.toBeInTheDocument();
  });
});
