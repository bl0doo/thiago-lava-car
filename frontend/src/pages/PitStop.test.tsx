import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { render, screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter } from 'react-router-dom';
import { cartao, erroHttp, quadro } from '@/test/fabrica';
import { PitStop } from './dono/PitStop';

const api = vi.hoisted(() => ({
  obterQuadro: vi.fn(),
  iniciarLavagem: vi.fn(),
  encerrarLavagem: vi.fn(),
  desfazerUltimoPasso: vi.fn(),
}));
vi.mock('@/api/ordensServico', () => api);

function renderizar() {
  const cliente = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  render(
    <QueryClientProvider client={cliente}>
      <MemoryRouter>
        <PitStop />
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

const coluna = (nome: string) => screen.getByRole('region', { name: nome });

describe('Pit Stop (dono)', () => {
  beforeEach(() => vi.resetAllMocks());

  it('mostra os cartões nas três colunas, com valor e cliente para o dono', async () => {
    api.obterQuadro.mockResolvedValue(
      quadro({
        aguardando: [cartao({ id: 1 })],
        concluido: [cartao({ id: 2, placa: 'XYZ9876', modelo: 'Toyota Hilux', status: 'Concluido', fimEm: '2020-01-01T10:00:00Z', duracaoRealMin: 102 })],
      }),
    );

    renderizar();

    const aguardando = await screen.findByRole('region', { name: 'Fila de largada' });
    expect(within(aguardando).getByText('ABC1D23')).toBeInTheDocument();
    expect(within(aguardando).getByText(/R\$\s?100,00/)).toBeInTheDocument();
    expect(within(aguardando).getByText('Renata Souza')).toBeInTheDocument();
    expect(within(coluna('Bandeirada')).getByText(/Levou 1 h 42 min/)).toBeInTheDocument();
    expect(within(coluna('No box')).getByText('Nenhum carro no box.')).toBeInTheDocument();
  });

  it('ao tocar em Iniciar o cartão muda de coluna na hora, antes de a API responder', async () => {
    const usuario = userEvent.setup();
    api.obterQuadro.mockResolvedValue(quadro({ aguardando: [cartao({ id: 1 })] }));
    let concluir!: (c: ReturnType<typeof cartao>) => void;
    api.iniciarLavagem.mockReturnValue(new Promise((resolve) => (concluir = resolve)));
    renderizar();

    await usuario.click(await screen.findByRole('button', { name: 'Iniciar' }));

    expect(within(coluna('No box')).getByText('ABC1D23')).toBeInTheDocument();
    expect(within(coluna('Fila de largada')).queryByText('ABC1D23')).not.toBeInTheDocument();
    expect(api.iniciarLavagem).toHaveBeenCalledWith(1);

    api.obterQuadro.mockResolvedValue(quadro({ emAndamento: [cartao({ status: 'EmAndamento', inicioEm: new Date().toISOString(), lavador: 'Diego' })] }));
    concluir(cartao({ status: 'EmAndamento', inicioEm: new Date().toISOString(), lavador: 'Diego' }));
    await waitFor(() => expect(within(coluna('No box')).getByText(/Diego/)).toBeInTheDocument());
  });

  it('se a API recusar, o cartão volta para a coluna de origem e a mensagem aparece', async () => {
    const usuario = userEvent.setup();
    api.obterQuadro.mockResolvedValue(quadro({ aguardando: [cartao({ id: 1 })] }));
    api.iniciarLavagem.mockRejectedValue(erroHttp(409, { detail: 'Esta lavagem já foi iniciada.' }));
    renderizar();

    await usuario.click(await screen.findByRole('button', { name: 'Iniciar' }));

    expect(await screen.findByRole('alert')).toHaveTextContent('Esta lavagem já foi iniciada.');
    expect(within(coluna('Fila de largada')).getByText('ABC1D23')).toBeInTheDocument();
  });

  it('avisa quando não consegue carregar o quadro', async () => {
    api.obterQuadro.mockRejectedValue(erroHttp(500));

    renderizar();

    expect(await screen.findByRole('alert')).toHaveTextContent('Algo deu errado, tente de novo.');
  });

  it('no celular as abas Fila / No box / Bandeirada dizem quantos carros há e qual coluna está à vista', async () => {
    const usuario = userEvent.setup();
    api.obterQuadro.mockResolvedValue(quadro({ aguardando: [cartao({ id: 1 }), cartao({ id: 3, placa: 'DEF4G56' })] }));
    renderizar();

    const fila = await screen.findByRole('button', { name: /2\s*Fila/ });
    expect(screen.getByRole('button', { name: /0\s*No box/ })).toHaveAttribute('aria-pressed', 'true');

    await usuario.click(fila);

    expect(fila).toHaveAttribute('aria-pressed', 'true');
    expect(coluna('Fila de largada')).toHaveClass('flex');
    expect(coluna('No box')).toHaveClass('hidden');
  });
});
