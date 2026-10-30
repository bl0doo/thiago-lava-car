import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { render, screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { hojeIso } from '@/lib/format';
import { erroHttp } from '@/test/fabrica';
import { Despesas } from './dono/Despesas';

const api = vi.hoisted(() => ({
  CATEGORIAS_DESPESA: ['Insumos', 'Aluguel', 'Agua', 'Energia', 'Manutencao', 'Outros'],
  listarDespesas: vi.fn(),
  criarDespesa: vi.fn(),
  editarDespesa: vi.fn(),
  excluirDespesa: vi.fn(),
  listarRetiradas: vi.fn(),
  registrarRetirada: vi.fn(),
  excluirRetirada: vi.fn(),
}));
vi.mock('@/api/financeiro', () => api);

const despesa = { id: 3, descricao: 'Shampoo automotivo', categoria: 'Insumos', valor: 80, data: '2026-10-10' };

function renderizar() {
  const cliente = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  render(
    <QueryClientProvider client={cliente}>
      <Despesas />
    </QueryClientProvider>,
  );
}

describe('Despesas e retiradas', () => {
  beforeEach(() => {
    vi.resetAllMocks();
    api.listarDespesas.mockResolvedValue({ itens: [despesa], total: 80 });
    api.listarRetiradas.mockResolvedValue({ itens: [], total: 0 });
  });

  it('lista as despesas do mês com total e pede o primeiro dia do mês', async () => {
    renderizar();

    expect(await screen.findByText('Shampoo automotivo')).toBeInTheDocument();
    expect(screen.getByText(/Insumos · 10\/10\/2026/)).toBeInTheDocument();
    const total = screen.getByText('Total de despesas').parentElement!;
    expect(within(total).getByText(/R\$\s?80,00/)).toBeInTheDocument();
    expect(api.listarDespesas).toHaveBeenCalledWith(`${hojeIso().slice(0, 7)}-01`);
  });

  it('cria uma despesa com valor em reais e categoria', async () => {
    const usuario = userEvent.setup();
    api.criarDespesa.mockResolvedValue(despesa);
    renderizar();
    await screen.findByText('Shampoo automotivo');

    await usuario.type(screen.getByLabelText('Descrição'), 'Conta de luz');
    await usuario.selectOptions(screen.getByLabelText('Categoria'), 'Energia');
    await usuario.type(screen.getByLabelText('Valor'), '152,30');
    await usuario.click(screen.getByRole('button', { name: 'Adicionar despesa' }));

    await waitFor(() => expect(api.criarDespesa).toHaveBeenCalled());
    expect(api.criarDespesa.mock.calls[0]?.[0]).toEqual({ descricao: 'Conta de luz', categoria: 'Energia', valor: 152.3, data: hojeIso() });
  });

  it('recusa descrição vazia e valor inválido sem chamar a API', async () => {
    const usuario = userEvent.setup();
    renderizar();
    await screen.findByText('Shampoo automotivo');

    await usuario.type(screen.getByLabelText('Valor'), '0');
    await usuario.click(screen.getByRole('button', { name: 'Adicionar despesa' }));

    expect(await screen.findByText('Informe a descrição.')).toBeInTheDocument();
    expect(screen.getByText('O valor deve ser maior que zero.')).toBeInTheDocument();
    expect(api.criarDespesa).not.toHaveBeenCalled();
  });

  it('edita uma despesa existente', async () => {
    const usuario = userEvent.setup();
    api.editarDespesa.mockResolvedValue(despesa);
    renderizar();

    await usuario.click(await screen.findByRole('button', { name: 'Editar Shampoo automotivo' }));
    expect(screen.getByLabelText('Descrição')).toHaveValue('Shampoo automotivo');
    expect(screen.getByLabelText('Valor')).toHaveValue('80');
    await usuario.clear(screen.getByLabelText('Valor'));
    await usuario.type(screen.getByLabelText('Valor'), '95');
    await usuario.click(screen.getByRole('button', { name: 'Salvar alterações' }));

    await waitFor(() => expect(api.editarDespesa).toHaveBeenCalled());
    expect(api.editarDespesa.mock.calls[0]?.[0]).toBe(3);
    expect(api.editarDespesa.mock.calls[0]?.[1]).toMatchObject({ valor: 95, descricao: 'Shampoo automotivo' });
  });

  it('exclui só depois de confirmar', async () => {
    const usuario = userEvent.setup();
    api.excluirDespesa.mockResolvedValue(undefined);
    renderizar();

    await usuario.click(await screen.findByRole('button', { name: 'Excluir Shampoo automotivo' }));
    expect(api.excluirDespesa).not.toHaveBeenCalled();
    const dialogo = await screen.findByRole('dialog', { name: 'Excluir esta despesa?', hidden: true });
    await usuario.click(within(dialogo).getByRole('button', { name: 'Excluir', hidden: true }));

    await waitFor(() => expect(api.excluirDespesa).toHaveBeenCalled());
    expect(api.excluirDespesa.mock.calls[0]?.[0]).toBe(3);
  });

  it('mostra erro de validação da API no campo (422) e erro geral no topo', async () => {
    const usuario = userEvent.setup();
    api.criarDespesa.mockRejectedValueOnce(erroHttp(422, { errors: { data: ['A data não pode ser futura.'] } })).mockRejectedValueOnce(erroHttp(500));
    renderizar();
    await screen.findByText('Shampoo automotivo');
    await usuario.type(screen.getByLabelText('Descrição'), 'Teste');
    await usuario.type(screen.getByLabelText('Valor'), '10');

    await usuario.click(screen.getByRole('button', { name: 'Adicionar despesa' }));
    expect(await screen.findByText('A data não pode ser futura.')).toBeInTheDocument();

    await usuario.click(screen.getByRole('button', { name: 'Adicionar despesa' }));
    expect(await screen.findByRole('alert')).toHaveTextContent('Algo deu errado');
  });

  it('na aba Retiradas mostra o estado vazio e registra uma retirada com observação', async () => {
    const usuario = userEvent.setup();
    api.registrarRetirada.mockResolvedValue({ id: 1, valor: 200, data: hojeIso(), observacao: 'Pró-labore' });
    renderizar();

    await usuario.click(screen.getByRole('button', { name: 'Retiradas' }));
    expect(await screen.findByText('Nenhuma retirada neste mês')).toBeInTheDocument();
    await usuario.type(screen.getByLabelText('Valor'), '200');
    await usuario.type(screen.getByLabelText('Observação (opcional)'), 'Pró-labore');
    await usuario.click(screen.getByRole('button', { name: 'Registrar retirada' }));

    await waitFor(() => expect(api.registrarRetirada).toHaveBeenCalled());
    expect(api.registrarRetirada.mock.calls[0]?.[0]).toEqual({ valor: 200, data: hojeIso(), observacao: 'Pró-labore' });
  });

  it('erro ao carregar oferece Tentar de novo', async () => {
    const usuario = userEvent.setup();
    api.listarDespesas.mockRejectedValueOnce(erroHttp(500)).mockResolvedValue({ itens: [despesa], total: 80 });
    renderizar();

    await usuario.click(await screen.findByRole('button', { name: 'Tentar de novo' }));
    expect(await screen.findByText('Shampoo automotivo')).toBeInTheDocument();
  });
});
