import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { render, screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter } from 'react-router-dom';
import { hojeIso } from '@/lib/format';
import { erroHttp } from '@/test/fabrica';
import { Caixa } from './dono/Caixa';

const api = vi.hoisted(() => ({
  obterResumoCaixa: vi.fn(),
  fecharCaixa: vi.fn(),
  reabrirCaixa: vi.fn(),
  listarFechamentos: vi.fn(),
}));
vi.mock('@/api/caixa', () => api);

const resumo = (parcial = {}) => ({
  data: hojeIso(),
  fechado: false,
  porForma: [
    { forma: 'Pix', quantidade: 2, bruto: 200, taxas: 0, liquido: 200 },
    { forma: 'Credito', quantidade: 1, bruto: 100, taxas: 3.5, liquido: 96.5 },
  ],
  totalBruto: 300,
  totalLiquido: 296.5,
  despesas: 40,
  diarias: 100,
  osConcluidasSemPagamento: 0,
  ...parcial,
});

function renderizar() {
  const cliente = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  render(
    <QueryClientProvider client={cliente}>
      <MemoryRouter>
        <Caixa />
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

describe('Caixa do dia', () => {
  beforeEach(() => {
    vi.resetAllMocks();
    api.listarFechamentos.mockResolvedValue([]);
  });

  it('mostra o resumo, a sobra do dia e o aviso de OS sem pagamento', async () => {
    api.obterResumoCaixa.mockResolvedValue(resumo({ osConcluidasSemPagamento: 2 }));
    renderizar();

    expect(await screen.findByText('2 OS concluídas ainda não foram totalmente pagas.')).toBeInTheDocument();
    const sobra = screen.getByText('Sobra do dia').parentElement!;
    expect(within(sobra).getByText(/R\$\s?156,50/)).toBeInTheDocument();
    expect(api.obterResumoCaixa).toHaveBeenCalledWith(hojeIso());
  });

  it('fecha o caixa sem diferença, enviando as quatro formas e sem justificativa', async () => {
    const usuario = userEvent.setup();
    api.obterResumoCaixa.mockResolvedValue(resumo());
    api.fecharCaixa.mockResolvedValue({});
    renderizar();

    await usuario.click(await screen.findByRole('button', { name: 'Fechar o caixa' }));

    await waitFor(() => expect(api.fecharCaixa).toHaveBeenCalled());
    expect(api.fecharCaixa.mock.calls[0]).toEqual([
      hojeIso(),
      [
        { forma: 'Pix', valor: 200 },
        { forma: 'Debito', valor: 0 },
        { forma: 'Credito', valor: 96.5 },
        { forma: 'Dinheiro', valor: 0 },
      ],
      null,
    ]);
  });

  it('com diferença na conferência exige justificativa antes de chamar a API', async () => {
    const usuario = userEvent.setup();
    api.obterResumoCaixa.mockResolvedValue(resumo());
    api.fecharCaixa.mockResolvedValue({});
    renderizar();

    const dinheiro = await screen.findByLabelText(/^Dinheiro/);
    await usuario.clear(dinheiro);
    await usuario.type(dinheiro, '10,00');
    expect(await screen.findByText(/Diferença de \+R\$\s?10,00/)).toBeInTheDocument();
    await usuario.click(screen.getByRole('button', { name: 'Fechar o caixa' }));

    expect(await screen.findByText(/explique em pelo menos 10 caracteres/)).toBeInTheDocument();
    expect(api.fecharCaixa).not.toHaveBeenCalled();

    await usuario.type(screen.getByLabelText('Justificativa da diferença'), 'Troco da manhã que sobrou');
    await usuario.click(screen.getByRole('button', { name: 'Fechar o caixa' }));

    await waitFor(() => expect(api.fecharCaixa).toHaveBeenCalled());
    expect(api.fecharCaixa.mock.calls[0]?.[2]).toBe('Troco da manhã que sobrou');
  });

  it('valor conferido inválido é recusado no campo', async () => {
    const usuario = userEvent.setup();
    api.obterResumoCaixa.mockResolvedValue(resumo());
    renderizar();

    const pix = await screen.findByLabelText(/^Pix/);
    await usuario.clear(pix);
    await usuario.type(pix, 'xx');
    await usuario.click(screen.getByRole('button', { name: 'Fechar o caixa' }));

    expect(await screen.findByText(/Informe o valor conferido em reais/)).toBeInTheDocument();
    expect(api.fecharCaixa).not.toHaveBeenCalled();
  });

  it('mostra a mensagem da API se o caixa já foi fechado (409)', async () => {
    const usuario = userEvent.setup();
    api.obterResumoCaixa.mockResolvedValue(resumo());
    api.fecharCaixa.mockRejectedValue(erroHttp(409, { detail: 'O caixa deste dia já foi fechado.' }));
    renderizar();

    await usuario.click(await screen.findByRole('button', { name: 'Fechar o caixa' }));

    expect(await screen.findByRole('alert')).toHaveTextContent('O caixa deste dia já foi fechado.');
  });

  it('dia fechado mostra a conferência e reabre com justificativa de 10 a 500 caracteres', async () => {
    const usuario = userEvent.setup();
    api.obterResumoCaixa.mockResolvedValue(resumo({ fechado: true }));
    api.listarFechamentos.mockResolvedValue([
      { data: hojeIso(), totalBruto: 300, totalLiquido: 296.5, totalConferido: 290, diferenca: -6.5, justificativa: 'Faltou troco', formas: [], fechadoEm: '2026-10-21T20:00:00Z' },
    ]);
    api.reabrirCaixa.mockResolvedValue(undefined);
    renderizar();

    expect(await screen.findByText(/Os pagamentos deste dia estão bloqueados/)).toBeInTheDocument();
    expect(await screen.findByText(/Justificativa: Faltou troco/)).toBeInTheDocument();
    expect(screen.queryByRole('button', { name: 'Fechar o caixa' })).not.toBeInTheDocument();

    await usuario.click(screen.getByRole('button', { name: 'Reabrir o caixa' }));
    const dialogo = await screen.findByRole('dialog', { name: 'Reabrir o caixa?', hidden: true });
    await usuario.type(within(dialogo).getByLabelText('Justificativa'), 'curta');
    await usuario.click(within(dialogo).getByRole('button', { name: 'Reabrir', hidden: true }));
    expect(await within(dialogo).findByText('A justificativa deve ter de 10 a 500 caracteres.')).toBeInTheDocument();
    expect(api.reabrirCaixa).not.toHaveBeenCalled();

    await usuario.type(within(dialogo).getByLabelText('Justificativa'), ' demais, lancei errado');
    await usuario.click(within(dialogo).getByRole('button', { name: 'Reabrir', hidden: true }));
    await waitFor(() => expect(api.reabrirCaixa).toHaveBeenCalled());
    expect(api.reabrirCaixa.mock.calls[0]).toEqual([hojeIso(), 'curta demais, lancei errado']);
  });

  it('erro ao carregar o resumo oferece Tentar de novo', async () => {
    const usuario = userEvent.setup();
    api.obterResumoCaixa.mockRejectedValueOnce(erroHttp(500)).mockResolvedValue(resumo());
    renderizar();

    expect(await screen.findByRole('alert')).toBeInTheDocument();
    await usuario.click(screen.getByRole('button', { name: 'Tentar de novo' }));
    expect(await screen.findByText('Sobra do dia')).toBeInTheDocument();
  });
});
