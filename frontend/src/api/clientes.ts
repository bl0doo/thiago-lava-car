import { api } from './client';
import type { Schemas } from './tipos';
import type { TipoVeiculo } from './veiculos';

export type CanalPreferido = 'WhatsApp' | 'Telefone' | 'Email';

export type VeiculoResumo = Omit<Schemas['VeiculoResumoResponse'], 'tipo'> & { tipo: TipoVeiculo };

export type ClienteDetalhe = Omit<Schemas['ClienteDetalheResponse'], 'canalPreferido' | 'veiculos'> & {
  canalPreferido: CanalPreferido;
  veiculos: VeiculoResumo[];
};

export type ClienteEntrada = Omit<Schemas['CriarClienteRequest'], 'canalPreferido'> & {
  canalPreferido: CanalPreferido;
};

/** GET /clientes?busca=: nome, telefone ou placa. */
export async function listarClientes(busca: string, pagina = 1): Promise<Schemas['ClienteResponsePaginaResponse']> {
  const params = { busca: busca || undefined, pagina, tamanho: 20 };
  return (await api.get<Schemas['ClienteResponsePaginaResponse']>('/clientes', { params })).data;
}

export async function obterCliente(id: number): Promise<ClienteDetalhe> {
  return (await api.get<ClienteDetalhe>(`/clientes/${id}`)).data;
}

export async function criarCliente(dados: ClienteEntrada): Promise<Schemas['ClienteResponse']> {
  return (await api.post<Schemas['ClienteResponse']>('/clientes', dados)).data;
}

export async function atualizarCliente(id: number, dados: ClienteEntrada): Promise<Schemas['ClienteResponse']> {
  return (await api.put<Schemas['ClienteResponse']>(`/clientes/${id}`, dados)).data;
}

export type MeusDados = Omit<Schemas['AtualizarClienteRequest'], 'canalPreferido'> & { canalPreferido: CanalPreferido };

/** GET /clientes/me: o cliente logado vê os próprios dados (LGPD). */
export async function obterMeusDados(): Promise<ClienteDetalhe> {
  return (await api.get<ClienteDetalhe>('/clientes/me')).data;
}

export async function atualizarMeusDados(dados: MeusDados): Promise<Schemas['ClienteResponse']> {
  return (await api.put<Schemas['ClienteResponse']>('/clientes/me', dados)).data;
}

export async function listarMeusVeiculos(): Promise<VeiculoResumo[]> {
  return (await api.get<VeiculoResumo[]>('/clientes/me/veiculos')).data;
}
