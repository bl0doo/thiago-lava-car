import { AxiosError, type AxiosAdapter, type AxiosResponse, type InternalAxiosRequestConfig } from 'axios';
import { sessao } from '@/auth/sessao';
import { api, renovarSessao } from './client';

type Chamada = { url: string; autorizacao: string | undefined };

const usuarioDono = { id: 1, nome: 'Thiago', perfil: 'Dono' } as const;

function resposta(config: InternalAxiosRequestConfig, status: number, data: unknown): AxiosResponse {
  return { status, statusText: String(status), headers: {}, config, data };
}

/** Adaptador falso: registra as chamadas e responde conforme o script. Nenhuma rede é usada. */
function simularApi(roteiro: (url: string, tentativa: number, autorizacao?: string) => { status: number; data?: unknown }) {
  const chamadas: Chamada[] = [];
  const contagem = new Map<string, number>();
  const adaptador: AxiosAdapter = async (config) => {
    const url = config.url ?? '';
    const autorizacao = config.headers.get('Authorization')?.toString();
    chamadas.push({ url, autorizacao });
    const tentativa = (contagem.get(url) ?? 0) + 1;
    contagem.set(url, tentativa);
    const { status, data } = roteiro(url, tentativa, autorizacao);
    const res = resposta(config, status, data ?? {});
    if (status >= 400) throw new AxiosError('falhou', AxiosError.ERR_BAD_REQUEST, config, null, res);
    return res;
  };
  api.defaults.adapter = adaptador;
  return chamadas;
}

beforeEach(() => sessao.limpar());

describe('cliente HTTP', () => {
  it('envia o access token em Authorization: Bearer', async () => {
    sessao.definir('token-1', usuarioDono);
    const chamadas = simularApi(() => ({ status: 200 }));

    await api.get('/ordens-servico/quadro');

    expect(chamadas[0]?.autorizacao).toBe('Bearer token-1');
  });

  it('num 401 renova o token uma vez e repete a requisição com o token novo', async () => {
    sessao.definir('velho', usuarioDono);
    const chamadas = simularApi((url, _t, auth) => {
      if (url === '/auth/refresh') return { status: 200, data: { accessToken: 'novo', expiraEm: 'x', usuario: usuarioDono } };
      return auth === 'Bearer novo' ? { status: 200, data: { ok: true } } : { status: 401 };
    });

    const { data } = await api.get('/ordens-servico/quadro');

    expect(data).toEqual({ ok: true });
    expect(chamadas.map((c) => c.url)).toEqual(['/ordens-servico/quadro', '/auth/refresh', '/ordens-servico/quadro']);
    expect(sessao.token()).toBe('novo');
  });

  it('vários 401 ao mesmo tempo disparam uma única renovação (o refresh token gira a cada uso)', async () => {
    sessao.definir('velho', usuarioDono);
    const chamadas = simularApi((url, _t, auth) => {
      if (url === '/auth/refresh') return { status: 200, data: { accessToken: 'novo', expiraEm: 'x', usuario: usuarioDono } };
      return auth === 'Bearer novo' ? { status: 200 } : { status: 401 };
    });

    await Promise.all([api.get('/a'), api.get('/b'), api.get('/c')]);

    expect(chamadas.filter((c) => c.url === '/auth/refresh')).toHaveLength(1);
  });

  it('se a renovação falha, limpa a sessão e devolve o erro original', async () => {
    sessao.definir('velho', usuarioDono);
    simularApi(() => ({ status: 401 }));

    await expect(api.get('/ordens-servico/quadro')).rejects.toMatchObject({ response: { status: 401 } });

    expect(sessao.token()).toBeNull();
  });

  it('o lavador também renova pelo cookie, sem digitar o PIN de novo', async () => {
    const diego = { id: 2, nome: 'Diego', perfil: 'Lavador' } as const;
    sessao.definir('velho', diego);
    const chamadas = simularApi((url, _t, auth) => {
      if (url === '/auth/refresh') return { status: 200, data: { accessToken: 'novo', expiraEm: 'x', usuario: diego } };
      return auth === 'Bearer novo' ? { status: 200 } : { status: 401 };
    });

    await api.get('/ordens-servico/quadro');

    expect(chamadas.map((c) => c.url)).toEqual(['/ordens-servico/quadro', '/auth/refresh', '/ordens-servico/quadro']);
  });

  it('um 401 no login (senha errada) não tenta renovar nada', async () => {
    const chamadas = simularApi(() => ({ status: 401 }));

    await expect(api.post('/auth/login', {}, { semRenovacao: true })).rejects.toBeDefined();

    expect(chamadas).toHaveLength(1);
  });

  it('renovarSessao compartilha a mesma chamada entre quem pede ao mesmo tempo', async () => {
    const chamadas = simularApi(() => ({ status: 200, data: { accessToken: 't', expiraEm: 'x', usuario: usuarioDono } }));

    await Promise.all([renovarSessao(), renovarSessao()]);

    expect(chamadas).toHaveLength(1);
  });
});
