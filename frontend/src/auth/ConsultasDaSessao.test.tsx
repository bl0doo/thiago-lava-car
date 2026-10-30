import { useQuery, useQueryClient, type QueryClient } from '@tanstack/react-query';
import { act, render, screen } from '@testing-library/react';
import { StrictMode, useEffect, type ReactNode } from 'react';
import { ConsultasDaSessao } from './ConsultasDaSessao';
import { sessao } from './sessao';

const dono = { id: 1, nome: 'Thiago', perfil: 'Dono' } as const;
const cliente = { id: 2, nome: 'Renata', perfil: 'Cliente' } as const;

function CapturarCache({ aoMontar }: { aoMontar: (cliente: QueryClient) => void }) {
  const queryClient = useQueryClient();
  useEffect(() => aoMontar(queryClient), [aoMontar, queryClient]);
  return null;
}

function renderizar(children?: ReactNode) {
  let atual!: QueryClient;
  render(
    <StrictMode>
      <ConsultasDaSessao>
        <CapturarCache aoMontar={(cache) => { atual = cache; }} />
        {children}
      </ConsultasDaSessao>
    </StrictMode>,
  );
  return () => atual;
}

beforeEach(() => sessao.limpar());

describe('cache da sessão', () => {
  it('mantém as consultas no cache com StrictMode ativo', async () => {
    function Consulta() {
      const { data } = useQuery({ queryKey: ['consulta'], queryFn: async () => 'carregado' });
      return <p>{data}</p>;
    }
    const obterCache = renderizar(<Consulta />);

    await screen.findByText('carregado');

    expect(obterCache().getQueryData(['consulta'])).toBe('carregado');
  });

  it('descarta consultas e mutações ao sair', () => {
    sessao.definir('token-dono', dono);
    const obterCache = renderizar();
    const anterior = obterCache();
    anterior.setQueryData(['clientes'], ['dados do dono']);
    anterior.getMutationCache().build(anterior, { mutationKey: ['salvar-cliente'] });

    act(() => sessao.limpar());

    expect(obterCache()).not.toBe(anterior);
    expect(anterior.getQueryCache().getAll()).toHaveLength(0);
    expect(anterior.getMutationCache().getAll()).toHaveLength(0);
  });

  it('usa outro cache quando outra pessoa entra sem recarregar a página', () => {
    sessao.definir('token-dono', dono);
    const obterCache = renderizar();
    const anterior = obterCache();
    anterior.setQueryData(['servicos'], [{ custoInsumos: 8 }]);

    act(() => sessao.definir('token-cliente', cliente));

    expect(obterCache()).not.toBe(anterior);
    expect(obterCache().getQueryData(['servicos'])).toBeUndefined();
  });

  it('mantém o cache ao renovar o token do mesmo usuário', () => {
    sessao.definir('token-antigo', dono);
    const obterCache = renderizar();
    const anterior = obterCache();
    anterior.setQueryData(['clientes'], ['dados do dono']);

    act(() => sessao.definir('token-novo', { ...dono }));

    expect(obterCache()).toBe(anterior);
    expect(obterCache().getQueryData(['clientes'])).toEqual(['dados do dono']);
  });

  it('uma consulta da sessão anterior não repovoa o cache ao terminar', async () => {
    sessao.definir('token-dono', dono);
    const obterCache = renderizar();
    let concluir!: (dados: string[]) => void;
    const pendente = obterCache().fetchQuery({
      queryKey: ['clientes'],
      queryFn: () => new Promise<string[]>((resolve) => { concluir = resolve; }),
    }).catch(() => undefined);

    act(() => sessao.definir('token-cliente', cliente));
    concluir(['dados do dono']);
    await pendente;

    expect(obterCache().getQueryData(['clientes'])).toBeUndefined();
  });

  it('uma mutação anterior não grava dados no cache da próxima sessão', async () => {
    sessao.definir('token-dono', dono);
    const obterCache = renderizar();
    const anterior = obterCache();
    let concluir!: (dados: string[]) => void;
    const mutacao = anterior.getMutationCache().build(anterior, {
      mutationFn: () => new Promise<string[]>((resolve) => { concluir = resolve; }),
      onSuccess: (dados) => { anterior.setQueryData(['clientes'], dados); },
    });
    const pendente = mutacao.execute(undefined);
    await vi.waitFor(() => expect(concluir).toBeTypeOf('function'));

    act(() => sessao.definir('token-cliente', cliente));
    concluir(['dados do dono']);
    await pendente;

    expect(obterCache().getQueryData(['clientes'])).toBeUndefined();
  });
});
