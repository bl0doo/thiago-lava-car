import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { render, screen } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import { cartao, quadro } from '@/test/fabrica';
import { MinhaFila } from './lavador/MinhaFila';

const api = vi.hoisted(() => ({
  obterQuadro: vi.fn(),
  iniciarLavagem: vi.fn(),
  encerrarLavagem: vi.fn(),
  desfazerUltimoPasso: vi.fn(),
}));
vi.mock('@/api/ordensServico', () => api);
vi.mock('@/auth/AuthContext', () => ({ useAuth: () => ({ usuario: { id: 2, nome: 'Diego Alves', perfil: 'Lavador' }, sair: vi.fn() }) }));

function renderizar() {
  render(
    <QueryClientProvider client={new QueryClient({ defaultOptions: { queries: { retry: false } } })}>
      <MemoryRouter>
        <MinhaFila />
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

describe('Minha fila (lavador)', () => {
  it('no box oferece o atalho para registrar pertences encontrados', async () => {
    api.obterQuadro.mockResolvedValue(quadro({ emAndamento: [cartao({ id: 2, status: 'EmAndamento', inicioEm: new Date().toISOString() })] }));

    renderizar();

    expect(await screen.findByRole('link', { name: 'Achou algo no carro?' })).toHaveAttribute('href', '/lavador/os/2');
  });

  it('lista o que está em andamento antes dos que aguardam e não mostra valores nem cliente', async () => {
    // a API não envia valorTotal, cliente nem telefone para o lavador
    const semFinanceiro = { valorTotal: undefined, cliente: undefined, telefone: undefined };
    api.obterQuadro.mockResolvedValue(
      quadro({
        aguardando: [cartao({ id: 1, placa: 'AAA1A11', ...semFinanceiro })],
        emAndamento: [cartao({ id: 2, placa: 'BBB2B22', status: 'EmAndamento', inicioEm: new Date().toISOString(), ...semFinanceiro })],
      }),
    );

    renderizar();

    const cartoes = await screen.findAllByRole('article');
    expect(cartoes.map((c) => c.getAttribute('aria-label'))).toEqual(['Chevrolet Onix, placa BBB2B22', 'Chevrolet Onix, placa AAA1A11']);
    expect(screen.queryByText(/R\$/)).not.toBeInTheDocument();
    expect(screen.queryByText('Renata Souza')).not.toBeInTheDocument();
  });

  it('usa o botão de 72 px do Design System para iniciar (uso com luva)', async () => {
    api.obterQuadro.mockResolvedValue(quadro({ aguardando: [cartao({ id: 1 })] }));

    renderizar();

    expect(await screen.findByRole('button', { name: 'Iniciar' })).toHaveClass('min-h-alvo-lavador-principal');
  });

  it('mostra uma mensagem quando a fila está vazia', async () => {
    api.obterQuadro.mockResolvedValue(quadro());

    renderizar();

    expect(await screen.findByText('Nenhum carro na fila')).toBeInTheDocument();
  });
});
