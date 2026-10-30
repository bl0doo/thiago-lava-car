import { api } from './client';
import type { Pagina } from './paginacao';

export type CanalPreferido = 'WhatsApp' | 'Telefone' | 'Email';

export type VeiculoResumo = { id: number; placa: string; modelo: string; tipo: 'Moto' | 'Carro' | 'Caminhonete'; cor?: string | null };

export type ClienteResumo = { id: number; nome: string; telefone: string; email?: string | null; veiculos?: VeiculoResumo[] };

export type ClienteDetalhe = ClienteResumo & { canalPreferido: CanalPreferido; consentimentoLgpd: boolean; veiculos: VeiculoResumo[] };

export type ClienteEntrada = {
  nome: string;
  telefone: string;
  email: string | null;
  canalPreferido: CanalPreferido;
  consentimentoLgpd: boolean;
};

/** GET /clientes?busca=: nome, telefone ou placa. */
export async function listarClientes(busca: string, pagina = 1): Promise<Pagina<ClienteResumo>> {
  const params = { busca: busca || undefined, pagina, tamanho: 20 };
  return (await api.get<Pagina<ClienteResumo>>('/clientes', { params })).data;
}

export async function obterCliente(id: number): Promise<ClienteDetalhe> {
  return (await api.get<ClienteDetalhe>(`/clientes/${id}`)).data;
}

export async function criarCliente(dados: ClienteEntrada): Promise<ClienteDetalhe> {
  return (await api.post<ClienteDetalhe>('/clientes', dados)).data;
}

export async function atualizarCliente(id: number, dados: ClienteEntrada): Promise<ClienteDetalhe> {
  return (await api.put<ClienteDetalhe>(`/clientes/${id}`, dados)).data;
}

export type MeusDados = { nome: string; telefone: string; email?: string | null; canalPreferido: CanalPreferido };

/** GET /clientes/me: o cliente logado vê os próprios dados (LGPD). */
export async function obterMeusDados(): Promise<ClienteDetalhe> {
  return (await api.get<ClienteDetalhe>('/clientes/me')).data;
}

export async function atualizarMeusDados(dados: MeusDados): Promise<ClienteDetalhe> {
  return (await api.put<ClienteDetalhe>('/clientes/me', dados)).data;
}

export async function listarMeusVeiculos(): Promise<VeiculoResumo[]> {
  return (await api.get<VeiculoResumo[]>('/clientes/me/veiculos')).data;
}
