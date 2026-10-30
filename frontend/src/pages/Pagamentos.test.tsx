import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { render, screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { erroHttp } from '@/test/fabrica';
import { PagamentosOS } from './dono/os/PagamentosOS';

const api = vi.hoisted(() => ({ registrarPagamento: vi.fn(), estornarPagamento: vi.fn() }));
vi.mock('@/api/pagamentos', () => api);

const pagamento = (parcial = {}) => ({
  id: 5,
  ordemServicoId: 7,
  formaPagamento: 'Pix',
  valorBruto: 100,
  percentualTaxa: 0,
  valorTaxa: 0,
  valorLiquido: 100,
  dataCaixa: '2026-10-21',
  criadoEm: '2026-10-21T14:00:00Z',
  estornado: false,
  ...parcial,
});

function renderizar(pagamentos = [] as ReturnType<typeof pagamento>[], saldoPendente = 180) {
  const cliente = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  render(
    <QueryClientProvider client={cliente}>
      <PagamentosOS osId={7} pagamentos={pagamentos} saldoPendente={saldoPendente} />
    </QueryClientProvider>,
  );
}

describe('Pagamentos da OS', () => {
  beforeEach(() => vi.resetAllMocks());

  it('já vem com o saldo no valor e registra o pagamento na forma escolhida', async () => {
    const usuario = userEvent.setup();
    api.registrarPagamento.mockResolvedValue({ id: 1, saldoPendente: 80 });
    renderizar();

    expect(screen.getByLabelText('Valor recebido')).toHaveValue('180');
    await usuario.selectOptions(screen.getByLabelText('Forma de pagamento'), 'Dinheiro');
    await usuario.clear(screen.getByLabelText('Valor recebido'));
    await usuario.type(screen.getByLabelText('Valor recebido'), '100,00');
    await usuario.click(screen.getByRole('button', { name: 'Registrar pagamento' }));

    await waitFor(() => expect(api.registrarPagamento).toHaveBeenCalled());
    expect(api.registrarPagamento.mock.calls[0]).toEqual([7, 'Dinheiro', 100]);
  });

  it('recusa valor inválido sem chamar a API', async () => {
    const usuario = userEvent.setup();
    renderizar();

    await usuario.clear(screen.getByLabelText('Valor recebido'));
    await usuario.type(screen.getByLabelText('Valor recebido'), 'abc');
    await usuario.click(screen.getByRole('button', { name: 'Registrar pagamento' }));

    expect(await screen.findByText(/Informe o valor recebido em reais/)).toBeInTheDocument();
    expect(api.registrarPagamento).not.toHaveBeenCalled();
  });

  it('mostra o erro da API quando o valor passa do saldo (422 no campo)', async () => {
    const usuario = userEvent.setup();
    api.registrarPagamento.mockRejectedValue(erroHttp(422, { errors: { valorBruto: ['O valor passa do saldo pendente.'] } }));
    renderizar();

    await usuario.click(screen.getByRole('button', { name: 'Registrar pagamento' }));

    expect(await screen.findByText('O valor passa do saldo pendente.')).toBeInTheDocument();
  });

  it('mostra erro de caixa fechado (409) no topo', async () => {
    const usuario = userEvent.setup();
    api.registrarPagamento.mockRejectedValue(erroHttp(409, { detail: 'O caixa deste dia já está fechado.' }));
    renderizar();

    await usuario.click(screen.getByRole('button', { name: 'Registrar pagamento' }));

    expect(await screen.findByRole('alert')).toHaveTextContent('O caixa deste dia já está fechado.');
  });

  it('lista pagamentos com taxa, marca os estornados e some o formulário quando está pago', () => {
    renderizar([pagamento({ valorTaxa: 3.5, valorLiquido: 96.5 }), pagamento({ id: 6, estornado: true })], 0);

    expect(screen.getByText(/Líquido R\$\s?96,50/)).toBeInTheDocument();
    expect(screen.getByText(/Estornado/)).toBeInTheDocument();
    expect(screen.getAllByRole('button', { name: /^Estornar/ })).toHaveLength(1);
    expect(screen.getByText('Pago por completo.')).toBeInTheDocument();
    expect(screen.queryByRole('button', { name: 'Registrar pagamento' })).not.toBeInTheDocument();
  });

  it('estorna só depois de confirmar, enviando o motivo', async () => {
    const usuario = userEvent.setup();
    api.estornarPagamento.mockResolvedValue(pagamento({ estornado: true }));
    renderizar([pagamento()], 0);

    await usuario.click(screen.getByRole('button', { name: /^Estornar Pix/ }));
    expect(api.estornarPagamento).not.toHaveBeenCalled();
    const dialogo = await screen.findByRole('dialog', { name: 'Estornar este pagamento?', hidden: true });
    await usuario.type(within(dialogo).getByLabelText('Motivo (opcional)'), 'Valor errado');
    await usuario.click(within(dialogo).getByRole('button', { name: 'Estornar', hidden: true }));

    await waitFor(() => expect(api.estornarPagamento).toHaveBeenCalled());
    expect(api.estornarPagamento.mock.calls[0]).toEqual([5, 'Valor errado']);
  });
});
