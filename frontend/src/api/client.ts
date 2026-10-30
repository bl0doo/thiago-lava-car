import axios, { type AxiosError, type InternalAxiosRequestConfig } from 'axios';
import { sessao } from '@/auth/sessao';
import type { AuthResponse, Usuario } from './tipos';

declare module 'axios' {
  interface AxiosRequestConfig {
    /** Não tenta renovar o token num 401 (rotas de login e a própria renovação). */
    semRenovacao?: boolean;
    _jaRenovou?: boolean;
  }
}

export const api = axios.create({
  baseURL: import.meta.env.VITE_API_URL || '/api/v1',
  withCredentials: true, // cookie HttpOnly do refresh token
  headers: { 'Content-Type': 'application/json' },
});

api.interceptors.request.use((config) => {
  const token = sessao.token();
  if (token) config.headers.set('Authorization', `Bearer ${token}`);
  return config;
});

let renovacaoEmCurso: Promise<Usuario> | null = null;

/**
 * Troca o refresh token do cookie por um novo par. Chamadas simultâneas compartilham a mesma requisição:
 * o refresh token gira a cada uso, e um token repetido derrubaria a sessão inteira.
 */
export function renovarSessao(): Promise<Usuario> {
  renovacaoEmCurso ??= api
    .post<AuthResponse>('/auth/refresh', undefined, { semRenovacao: true })
    .then(({ data }) => {
      sessao.definir(data.accessToken, data.usuario);
      return data.usuario;
    })
    .finally(() => {
      renovacaoEmCurso = null;
    });
  return renovacaoEmCurso;
}

api.interceptors.response.use(undefined, async (erro: AxiosError) => {
  const config = erro.config as InternalAxiosRequestConfig | undefined;
  if (erro.response?.status !== 401 || !config || config.semRenovacao || config._jaRenovou) {
    throw erro;
  }
  try {
    await renovarSessao();
  } catch {
    sessao.limpar();
    throw erro;
  }
  config._jaRenovou = true;
  return api.request(config);
});
