import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter } from 'react-router-dom';
import type { Servico } from '@/api/servicos';
import { erroHttp } from '@/test/fabrica';
import { NovaEntrada } from './dono/NovaEntrada';
import { resumo } from './dono/entrada/PassoServicos';

const veiculos = vi.hoisted(() => ({ buscarVeiculoPorPlaca: vi.fn(), criarVeiculo: vi.fn() }));
const servicos = vi.hoisted(() => ({ listarServicos: vi.fn() }));
const os = vi.hoisted(() => ({ registrarEntrada: vi.fn() }));
vi.mock('@/api/veiculos', () => veiculos);
vi.mock('@/api/servicos', () => servicos);
vi.mock('@/api/ordensServico', () => os);
const clientes = vi.hoisted(() => ({ listarClientes: vi.fn(), obterCliente: vi.fn() }));
vi.mock('@/api/clientes', () => clientes);

const catalogo: Servico[] = [
  { id: 1, nome: 'Lavagem completa', categoria: 'Lavagem', tempoEstimadoMin: 90, ativo: true, precos: [{ tipoVeiculo: 'Carro', preco: 80, precoComCera: 100 }, { tipoVeiculo: 'Moto', preco: 40, precoComCera: null }] },
  { id: 4, nome: 'Motor', categoria: 'Lavagem', tempoEstimadoMin: 30, ativo: true, precos: [{ tipoVeiculo: 'Carro', preco: 120, precoComCera: null }] },
];
const onix = { id: 30, clienteId: 12, placa: 'ABC1D23', modelo: 'Chevrolet Onix', tipo: 'Carro', cor: 'Prata' };

function renderizar() {
  render(
    <QueryClientProvider client={new QueryClient({ defaultOptions: { queries: { retry: false } } })}>
      <MemoryRouter>
        <NovaEntrada />
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

async function ate(u: ReturnType<typeof userEvent.setup>) {
  veiculos.buscarVeiculoPorPlaca.mockResolvedValue(onix);
  servicos.listarServicos.mockResolvedValue(catalogo);
  renderizar();
  await u.type(screen.getByLabelText('Placa do carro'), 'abc-1d23');
  await u.click(screen.getByRole('button', { name: 'Procurar placa' }));
  await u.click(await screen.findByRole('button', { name: 'Continuar com este veículo' }));
}

describe('Nova entrada', () => {
  beforeEach(() => {
    vi.resetAllMocks();
    clientes.listarClientes.mockResolvedValue({ itens: [], total: 0, pagina: 1, tamanho: 20 });
    clientes.obterCliente.mockResolvedValue({ id: 12, nome: 'Renata Souza', telefone: '44999998888', veiculos: [] });
  });

  it('placa inválida mostra o erro sem chamar a API', async () => {
    const u = userEvent.setup();
    renderizar();
    await u.type(screen.getByLabelText('Placa do carro'), 'AB123');
    await u.click(screen.getByRole('button', { name: 'Procurar placa' }));
    expect(await screen.findByText(/Placa inválida/)).toBeInTheDocument();
    expect(veiculos.buscarVeiculoPorPlaca).not.toHaveBeenCalled();
  });

  it('placa não cadastrada abre o cadastro do veículo', async () => {
    const u = userEvent.setup();
    veiculos.buscarVeiculoPorPlaca.mockResolvedValue(null);
    renderizar();
    await u.type(screen.getByLabelText('Placa do carro'), 'ABC1D23');
    await u.click(screen.getByRole('button', { name: 'Procurar placa' }));
    expect(await screen.findByText(/ainda não está cadastrada/)).toBeInTheDocument();
    await u.click(screen.getByRole('button', { name: 'Cadastrar veículo' }));
    expect(await screen.findByText('Escolha o dono do veículo.')).toBeInTheDocument();
    expect(veiculos.criarVeiculo).not.toHaveBeenCalled();
  });

  it('fluxo completo: placa, serviços com cera, vistoria e registro', async () => {
    const u = userEvent.setup();
    os.registrarEntrada.mockResolvedValue({ id: 88, status: 'Aguardando', valorTotal: 220, duracaoEstimadaMin: 120, previsaoConclusao: '2026-10-20T17:00:00Z' });
    await ate(u);

    await u.click(await screen.findByRole('checkbox', { name: /Lavagem completa/ }));
    await u.click(screen.getByRole('checkbox', { name: /Com cera/ }));
    await u.click(screen.getByRole('checkbox', { name: /Motor/ }));
    expect(screen.getByText(/R\$\s?220,00/)).toBeInTheDocument();
    await u.click(screen.getByRole('button', { name: 'Continuar' }));

    await u.click(screen.getByRole('button', { name: 'Registrar entrada' }));
    expect(await screen.findByText('Anote ao menos uma avaria ou marque "Sem avarias".')).toBeInTheDocument();
    expect(screen.getByText('O cliente precisa concordar com a vistoria.')).toBeInTheDocument();

    await u.click(screen.getByLabelText(/Sem avarias/));
    await u.type(screen.getByLabelText(/Pertences/), 'Cadeirinha infantil');
    await u.click(screen.getByLabelText(/O cliente viu a vistoria/));
    await u.click(screen.getByRole('button', { name: 'Registrar entrada' }));

    expect(await screen.findByText('Entrada registrada')).toBeInTheDocument();
    expect(os.registrarEntrada.mock.calls[0]?.[0]).toEqual({
      veiculoId: 30,
      itens: [
        { servicoId: 1, comCera: true, precoManual: null },
        { servicoId: 4, comCera: false, precoManual: null },
      ],
      semAvarias: true,
      avarias: [],
      pertencesEntrada: 'Cadeirinha infantil',
      nomeQuemRetira: null,
      aceiteCliente: true,
    });

    await u.click(screen.getByRole('button', { name: 'Registrar outro carro' }));
    expect(screen.getByLabelText('Placa do carro')).toHaveValue('');
    expect(screen.queryByText('Entrada registrada')).not.toBeInTheDocument();
  });

  it('mostra o 409 de veículo que já está no pátio', async () => {
    const u = userEvent.setup();
    os.registrarEntrada.mockRejectedValue(erroHttp(409, { detail: 'Este veículo já está no pátio.' }));
    await ate(u);
    await u.click(await screen.findByRole('checkbox', { name: /Motor/ }));
    await u.click(screen.getByRole('button', { name: 'Continuar' }));
    await u.click(screen.getByLabelText(/Sem avarias/));
    await u.click(screen.getByLabelText(/O cliente viu a vistoria/));
    await u.click(screen.getByRole('button', { name: 'Registrar entrada' }));
    expect(await screen.findByRole('alert')).toHaveTextContent('Este veículo já está no pátio.');
  });

  it('não deixa seguir sem serviço', async () => {
    const u = userEvent.setup();
    await ate(u);
    await u.click(await screen.findByRole('button', { name: 'Continuar' }));
    expect(await screen.findByText('Escolha ao menos um serviço.')).toBeInTheDocument();
  });
});

describe('resumo de preços', () => {
  it('usa o preço corrigido pelo dono e soma a duração', () => {
    const r = resumo(catalogo, [{ servicoId: 1, comCera: false, precoManual: '70,00' }, { servicoId: 4, comCera: false, precoManual: '' }], 'Carro');
    expect(r).toEqual({ total: 190, minutos: 120 });
  });
});
