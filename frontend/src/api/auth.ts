import { api } from './client';
import type { AuthResponse, LavadorResumo, Schemas, Usuario } from './tipos';
import { sessao } from '@/auth/sessao';

function iniciarSessao(resposta: AuthResponse): Usuario {
  sessao.definir(resposta.accessToken, resposta.usuario);
  return resposta.usuario;
}

export async function entrar(email: string, senha: string): Promise<Usuario> {
  const { data } = await api.post<AuthResponse>('/auth/login', { email, senha }, { semRenovacao: true });
  return iniciarSessao(data);
}

export async function entrarComPin(usuarioId: number, pin: string): Promise<Usuario> {
  const { data } = await api.post<AuthResponse>('/auth/login-pin', { usuarioId, pin }, { semRenovacao: true });
  return iniciarSessao(data);
}

export async function sair(): Promise<void> {
  try {
    await api.post('/auth/logout', undefined, { semRenovacao: true });
  } finally {
    sessao.limpar();
  }
}

export async function listarLavadores(): Promise<LavadorResumo[]> {
  const { data } = await api.get<LavadorResumo[]>('/auth/lavadores', { semRenovacao: true });
  return data;
}

export type CadastroCliente = Schemas['CadastroClienteRequest'];

/** Cria a conta do cliente e já deixa a sessão aberta (a API responde 201 com token e cookie de renovação). */
export async function cadastrarCliente(dados: CadastroCliente): Promise<Usuario> {
  const { data } = await api.post<AuthResponse>('/auth/cadastro-cliente', dados, { semRenovacao: true });
  return iniciarSessao(data);
}
