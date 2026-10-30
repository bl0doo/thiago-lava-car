import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { render, screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { hojeIso } from '@/lib/format';
import { erroHttp } from '@/test/fabrica';
import { Equipe } from './dono/Equipe';

const api = vi.hoisted(() => ({
  listarEquipe: vi.fn(),
  criarLavador: vi.fn(),
  editarMembro: vi.fn(),
  listarDiarias: vi.fn(),
  lancarDiariasDoDia: vi.fn(),
  editarDiaria: vi.fn(),
  excluirDiaria: vi.fn(),
  redefinirPin: vi.fn(),
  alterarAtivo: vi.fn(),
}));
vi.mock('@/api/equipe', () => api);

const diaria = { id: 9, lavadorId: 2, data: '2026-10-20', valor: 100, paga: false };

function renderizar() {
  const cliente = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  render(
    <QueryClientProvider client={cliente}>
      <Equipe />
    </QueryClientProvider>,
  );
}

describe('Equipe e diárias', () => {
  beforeEach(() => {
    vi.resetAllMocks();
    api.listarEquipe.mockResolvedValue([
      { id: 2, nome: 'José da Silva', apelido: 'Zé', perfil: 'Lavador', ativo: true },
      { id: 3, nome: 'Roberto Lima', apelido: 'Beto', perfil: 'Lavador', ativo: true },
      { id: 4, nome: 'Carlos Souza', apelido: 'Carlão', perfil: 'Lavador', ativo: false },
    ]);
    api.listarDiarias.mockResolvedValue({ itens: [diaria], total: 100 });
  });

  it('lista lavadores e as diárias do mês com o apelido e o total', async () => {
    renderizar();

    const lista = await screen.findByRole('region', { name: 'Lavadores' });
    expect(await within(lista).findByText('Beto')).toBeInTheDocument();
    expect(await screen.findByText(/Zé · R\$\s?100,00/)).toBeInTheDocument();
    expect(screen.getByText(/A pagar/)).toBeInTheDocument();
    expect(api.listarDiarias).toHaveBeenCalledWith(hojeIso().slice(0, 7));
  });

  it('mostra desativados com a marca e não os oferece para diária; reativa com um toque', async () => {
    const usuario = userEvent.setup();
    api.alterarAtivo.mockResolvedValue(undefined);
    renderizar();

    expect(await screen.findByText(/Carlos Souza · Desativado/)).toBeInTheDocument();
    expect(screen.queryByRole('checkbox', { name: 'Carlão' })).not.toBeInTheDocument();
    await usuario.click(screen.getByRole('button', { name: 'Reativar Carlão' }));

    await waitFor(() => expect(api.alterarAtivo).toHaveBeenCalled());
    expect(api.alterarAtivo.mock.calls[0]).toEqual([4, true]);
  });

  it('cadastra um lavador novo validando nome, apelido e PIN', async () => {
    const usuario = userEvent.setup();
    api.criarLavador.mockResolvedValue({ id: 5, nome: 'Pedro Alves', apelido: 'Pedrão', perfil: 'Lavador', ativo: true });
    renderizar();

    await usuario.click(await screen.findByRole('button', { name: 'Novo lavador' }));
    const dialogo = await screen.findByRole('dialog', { name: 'Novo lavador', hidden: true });
    await usuario.click(within(dialogo).getByRole('button', { name: 'Cadastrar', hidden: true }));
    expect(await within(dialogo).findByText('Informe o apelido.')).toBeInTheDocument();
    expect(within(dialogo).getByText('O PIN deve ter 4 dígitos.')).toBeInTheDocument();
    expect(api.criarLavador).not.toHaveBeenCalled();

    await usuario.type(within(dialogo).getByLabelText('Nome completo'), 'Pedro Alves');
    await usuario.type(within(dialogo).getByLabelText('Apelido'), 'Pedrão');
    await usuario.type(within(dialogo).getByLabelText('PIN de 4 dígitos'), '4829');
    await usuario.click(within(dialogo).getByRole('button', { name: 'Cadastrar', hidden: true }));

    await waitFor(() => expect(api.criarLavador).toHaveBeenCalled());
    expect(api.criarLavador.mock.calls[0]?.[0]).toEqual({ nome: 'Pedro Alves', apelido: 'Pedrão', pin: '4829' });
    expect(await screen.findByText('Lavador cadastrado.')).toBeInTheDocument();
  });

  it('mostra no campo o erro da API para apelido repetido (409) e PIN óbvio (422)', async () => {
    const usuario = userEvent.setup();
    api.criarLavador.mockRejectedValue(erroHttp(409, { detail: 'Já existe um lavador com este apelido.' }));
    renderizar();

    await usuario.click(await screen.findByRole('button', { name: 'Novo lavador' }));
    const dialogo = await screen.findByRole('dialog', { name: 'Novo lavador', hidden: true });
    await usuario.type(within(dialogo).getByLabelText('Nome completo'), 'Pedro Alves');
    await usuario.type(within(dialogo).getByLabelText('Apelido'), 'Zé');
    await usuario.type(within(dialogo).getByLabelText('PIN de 4 dígitos'), '4829');
    await usuario.click(within(dialogo).getByRole('button', { name: 'Cadastrar', hidden: true }));

    expect(await within(dialogo).findByText('Já existe um lavador com este apelido.')).toBeInTheDocument();
  });

  it('edita nome e apelido de um lavador', async () => {
    const usuario = userEvent.setup();
    api.editarMembro.mockResolvedValue({});
    renderizar();

    await usuario.click(await screen.findByRole('button', { name: 'Editar Zé' }));
    const dialogo = await screen.findByRole('dialog', { name: 'Editar Zé', hidden: true });
    expect(within(dialogo).getByLabelText('Nome completo')).toHaveValue('José da Silva');
    await usuario.clear(within(dialogo).getByLabelText('Apelido'));
    await usuario.type(within(dialogo).getByLabelText('Apelido'), 'Zezinho');
    await usuario.click(within(dialogo).getByRole('button', { name: 'Salvar', hidden: true }));

    await waitFor(() => expect(api.editarMembro).toHaveBeenCalled());
    expect(api.editarMembro.mock.calls[0]).toEqual([2, 'José da Silva', 'Zezinho']);
  });

  it('lança as diárias do dia dos lavadores marcados com a diária padrão', async () => {
    const usuario = userEvent.setup();
    api.lancarDiariasDoDia.mockResolvedValue({ criadas: [diaria], jaLancadas: [3] });
    renderizar();

    await usuario.click(await screen.findByRole('checkbox', { name: 'Zé' }));
    await usuario.click(screen.getByRole('checkbox', { name: 'Beto' }));
    await usuario.click(screen.getByRole('button', { name: 'Lançar diárias' }));

    await waitFor(() => expect(api.lancarDiariasDoDia).toHaveBeenCalled());
    expect(api.lancarDiariasDoDia.mock.calls[0]).toEqual([hojeIso(), [2, 3], null]);
    expect(await screen.findByText(/1 diária lançada\. Já tinham diária: Beto\./)).toBeInTheDocument();
  });

  it('exige ao menos um lavador e valor maior que zero antes de chamar a API', async () => {
    const usuario = userEvent.setup();
    renderizar();

    await usuario.click(await screen.findByRole('button', { name: 'Lançar diárias' }));
    expect(await screen.findByText('Escolha ao menos um lavador.')).toBeInTheDocument();

    await usuario.click(screen.getByRole('checkbox', { name: 'Zé' }));
    await usuario.type(screen.getByLabelText('Valor da diária (opcional)'), '0');
    await usuario.click(screen.getByRole('button', { name: 'Lançar diárias' }));
    expect(await screen.findByText('O valor da diária deve ser maior que zero.')).toBeInTheDocument();
    expect(api.lancarDiariasDoDia).not.toHaveBeenCalled();
  });

  it('envia o valor digitado em reais', async () => {
    const usuario = userEvent.setup();
    api.lancarDiariasDoDia.mockResolvedValue({ criadas: [diaria], jaLancadas: [] });
    renderizar();

    await usuario.click(await screen.findByRole('checkbox', { name: 'Zé' }));
    await usuario.type(screen.getByLabelText('Valor da diária (opcional)'), '130,50');
    await usuario.click(screen.getByRole('button', { name: 'Lançar diárias' }));

    await waitFor(() => expect(api.lancarDiariasDoDia).toHaveBeenCalled());
    expect(api.lancarDiariasDoDia.mock.calls[0]?.[2]).toBe(130.5);
  });

  it('marca a diária como paga mantendo o valor e mostra erro da API (409)', async () => {
    const usuario = userEvent.setup();
    api.editarDiaria.mockRejectedValueOnce(erroHttp(409, { detail: 'Esta diária já foi paga.' }));
    renderizar();

    await usuario.click(await screen.findByRole('button', { name: /Marcar diária de Zé/ }));

    await waitFor(() => expect(api.editarDiaria).toHaveBeenCalled());
    expect(api.editarDiaria.mock.calls[0]).toEqual([9, 100, true]);
    expect(await screen.findByRole('alert')).toHaveTextContent('Esta diária já foi paga.');
  });

  it('exclui a diária só depois de confirmar', async () => {
    const usuario = userEvent.setup();
    api.excluirDiaria.mockResolvedValue(undefined);
    renderizar();

    await usuario.click(await screen.findByRole('button', { name: /Excluir diária de Zé/ }));
    expect(api.excluirDiaria).not.toHaveBeenCalled();
    const dialogo = await screen.findByRole('dialog', { name: 'Excluir esta diária?', hidden: true });
    await usuario.click(within(dialogo).getByRole('button', { name: 'Excluir', hidden: true }));

    await waitFor(() => expect(api.excluirDiaria).toHaveBeenCalled());
    expect(api.excluirDiaria.mock.calls[0]?.[0]).toBe(9);
  });

  it('redefine o PIN só com 4 dígitos', async () => {
    const usuario = userEvent.setup();
    api.redefinirPin.mockResolvedValue(undefined);
    renderizar();

    await usuario.click(await screen.findByRole('button', { name: 'Redefinir PIN de Zé' }));
    const dialogo = await screen.findByRole('dialog', { name: 'Novo PIN de Zé', hidden: true });
    await usuario.type(within(dialogo).getByLabelText('PIN de 4 dígitos'), '12');
    await usuario.click(within(dialogo).getByRole('button', { name: 'Salvar PIN', hidden: true }));
    expect(await within(dialogo).findByText('O PIN deve ter 4 dígitos.')).toBeInTheDocument();
    expect(api.redefinirPin).not.toHaveBeenCalled();

    await usuario.type(within(dialogo).getByLabelText('PIN de 4 dígitos'), '34');
    await usuario.click(within(dialogo).getByRole('button', { name: 'Salvar PIN', hidden: true }));
    await waitFor(() => expect(api.redefinirPin).toHaveBeenCalled());
    expect(api.redefinirPin.mock.calls[0]).toEqual([2, '1234']);
    expect(await screen.findByText(/PIN redefinido/)).toBeInTheDocument();
  });

  it('desativa o acesso depois de confirmar', async () => {
    const usuario = userEvent.setup();
    api.alterarAtivo.mockResolvedValue(undefined);
    renderizar();

    await usuario.click(await screen.findByRole('button', { name: 'Desativar Beto' }));
    const dialogo = await screen.findByRole('dialog', { name: 'Desativar Beto?', hidden: true });
    await usuario.click(within(dialogo).getByRole('button', { name: 'Desativar', hidden: true }));

    await waitFor(() => expect(api.alterarAtivo).toHaveBeenCalled());
    expect(api.alterarAtivo.mock.calls[0]).toEqual([3, false]);
  });

  it('erro ao carregar as diárias oferece Tentar de novo', async () => {
    const usuario = userEvent.setup();
    api.listarDiarias.mockRejectedValueOnce(erroHttp(500)).mockResolvedValue({ itens: [diaria], total: 100 });
    renderizar();

    await usuario.click(await screen.findByRole('button', { name: 'Tentar de novo' }));
    expect(await screen.findByText(/Zé · R\$\s?100,00/)).toBeInTheDocument();
  });
});
