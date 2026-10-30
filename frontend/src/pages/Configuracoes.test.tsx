import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { render, screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter } from 'react-router-dom';
import { erroHttp } from '@/test/fabrica';
import { Configuracoes } from './dono/Configuracoes';

const api = vi.hoisted(() => ({
  obterConfiguracao: vi.fn(),
  salvarConfiguracao: vi.fn(),
  listarTaxas: vi.fn(),
  alterarTaxa: vi.fn(),
  listarModelos: vi.fn(),
  salvarModelo: vi.fn(),
}));
vi.mock('@/api/configuracoes', () => api);

const config = {
  nomeLavaJato: 'Thiago Lava Car',
  telefone: '11999990000',
  abertura: '08:00',
  fechamento: '18:00',
  diasFuncionamento: [1, 2, 3, 4, 5, 6],
  diariaSemana: 100,
  diariaSabado: 120,
  intervaloAgendaMin: 30,
};

function renderizar() {
  render(
    <QueryClientProvider client={new QueryClient({ defaultOptions: { queries: { retry: false } } })}>
      <MemoryRouter>
        <Configuracoes />
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

describe('Configurações', () => {
  beforeEach(() => {
    vi.resetAllMocks();
    api.obterConfiguracao.mockResolvedValue(config);
    api.salvarConfiguracao.mockImplementation(async (d) => d);
    api.listarTaxas.mockResolvedValue([
      { formaPagamento: 'Pix', percentual: 0, valorFixo: 0 },
      { formaPagamento: 'Credito', percentual: 3.5, valorFixo: 0.5 },
    ]);
    api.alterarTaxa.mockImplementation(async (forma, percentual, valorFixo) => ({ formaPagamento: forma, percentual, valorFixo }));
    api.listarModelos.mockResolvedValue([
      { tipo: 'lavagem-iniciada', texto: 'Oi {nome}!', atualizadoEm: '2026-10-01T10:00:00Z' },
      { tipo: 'lavagem-concluida', texto: 'Pronto, {nome}.', atualizadoEm: '2026-10-01T10:00:00Z' },
    ]);
    api.salvarModelo.mockImplementation(async (tipo, texto) => ({ tipo, texto, atualizadoEm: '2026-10-23T10:00:00Z' }));
  });

  it('carrega os dados do lava-jato e salva com os dias em números', async () => {
    renderizar();
    const nome = await screen.findByLabelText('Nome do lava-jato');
    expect(nome).toHaveValue('Thiago Lava Car');
    expect(screen.getByLabelText('Domingo')).not.toBeChecked();
    expect(screen.getByLabelText('Sábado')).toBeChecked();

    await userEvent.clear(screen.getByLabelText('Diária de sábado (R$)'));
    await userEvent.type(screen.getByLabelText('Diária de sábado (R$)'), '130,50');
    await userEvent.click(screen.getByLabelText('Domingo'));
    await userEvent.click(screen.getByRole('button', { name: 'Salvar' }));

    expect(await screen.findByText('Configurações salvas.')).toBeInTheDocument();
    expect(api.salvarConfiguracao.mock.calls[0]?.[0]).toEqual({ ...config, diariaSabado: 130.5, diasFuncionamento: [0, 1, 2, 3, 4, 5, 6] });
  });

  it('valida sem chamar a API: nenhum dia e intervalo fora do limite', async () => {
    renderizar();
    await screen.findByLabelText('Nome do lava-jato');

    for (const dia of [1, 2, 3, 4, 5, 6]) {
      await userEvent.click(screen.getByLabelText(['Domingo', 'Segunda', 'Terça', 'Quarta', 'Quinta', 'Sexta', 'Sábado'][dia]!));
    }
    await userEvent.clear(screen.getByLabelText('Intervalo da agenda (minutos)'));
    await userEvent.type(screen.getByLabelText('Intervalo da agenda (minutos)'), '2');
    await userEvent.click(screen.getByRole('button', { name: 'Salvar' }));

    expect(await screen.findByText('Informe ao menos um dia de funcionamento.')).toBeInTheDocument();
    expect(screen.getByText('O intervalo da agenda deve ficar entre 5 e 240 minutos.')).toBeInTheDocument();
    expect(api.salvarConfiguracao).not.toHaveBeenCalled();
  });

  it('mostra o erro de campo devolvido pela API', async () => {
    api.salvarConfiguracao.mockRejectedValue(erroHttp(422, { errors: { NomeLavaJato: ['Nome já usado.'] } }));

    renderizar();
    await screen.findByLabelText('Nome do lava-jato');
    await userEvent.click(screen.getByRole('button', { name: 'Salvar' }));

    expect(await screen.findByText('Nome já usado.')).toBeInTheDocument();
  });

  it('taxas: salva percentual e valor fixo de uma forma de pagamento', async () => {
    renderizar();
    await userEvent.click(screen.getByRole('button', { name: 'Taxas' }));

    const credito = await screen.findByRole('form', { name: 'Taxa de Crédito' });
    expect(within(credito).getByLabelText('Percentual (%)')).toHaveValue('3,5');
    await userEvent.clear(within(credito).getByLabelText('Percentual (%)'));
    await userEvent.type(within(credito).getByLabelText('Percentual (%)'), '4,2');
    await userEvent.click(within(credito).getByRole('button', { name: /Salvar taxa/ }));

    expect(await within(credito).findByText(/Taxa salva/)).toBeInTheDocument();
    expect(api.alterarTaxa).toHaveBeenCalledWith('Credito', 4.2, 0.5);
  });

  it('taxas: percentual acima de 100 não vai para a API', async () => {
    renderizar();
    await userEvent.click(screen.getByRole('button', { name: 'Taxas' }));

    const pix = await screen.findByRole('form', { name: 'Taxa de Pix' });
    await userEvent.clear(within(pix).getByLabelText('Percentual (%)'));
    await userEvent.type(within(pix).getByLabelText('Percentual (%)'), '120');
    await userEvent.click(within(pix).getByRole('button', { name: /Salvar taxa/ }));

    expect(await within(pix).findByText('O percentual deve estar entre 0 e 100.')).toBeInTheDocument();
    expect(api.alterarTaxa).not.toHaveBeenCalled();
  });

  it('mensagens: edita e salva o texto do modelo', async () => {
    renderizar();
    await userEvent.click(screen.getByRole('button', { name: 'Mensagens' }));

    const iniciada = await screen.findByRole('region', { name: 'Lavagem iniciada' });
    const campo = within(iniciada).getByLabelText('Texto de lavagem iniciada');
    await userEvent.clear(campo);
    await userEvent.type(campo, 'Começamos, {{nome}!');
    await userEvent.click(within(iniciada).getByRole('button', { name: /Salvar mensagem/ }));

    await waitFor(() => expect(api.salvarModelo).toHaveBeenCalledWith('lavagem-iniciada', 'Começamos, {nome}!'));
    expect(await within(iniciada).findByText('Mensagem salva.')).toBeInTheDocument();
  });

  it('mensagens: texto vazio não é enviado', async () => {
    renderizar();
    await userEvent.click(screen.getByRole('button', { name: 'Mensagens' }));

    const concluida = await screen.findByRole('region', { name: 'Lavagem concluída' });
    await userEvent.clear(within(concluida).getByLabelText('Texto de lavagem concluída'));
    await userEvent.click(within(concluida).getByRole('button', { name: /Salvar mensagem/ }));

    expect(await within(concluida).findByText('Informe o texto da mensagem.')).toBeInTheDocument();
    expect(api.salvarModelo).not.toHaveBeenCalled();
  });
});
