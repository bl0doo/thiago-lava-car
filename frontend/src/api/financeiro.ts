import { api } from './client';
import type { Schemas } from './tipos';

export const CATEGORIAS_DESPESA = ['Insumos', 'Aluguel', 'Agua', 'Energia', 'Manutencao', 'Outros'] as const;
export type Despesa = Schemas['DespesaResponse'];
export type Despesas = Schemas['DespesasResponse'];
export type DespesaEntrada = Schemas['SalvarDespesaRequest'];

export type Retirada = Schemas['RetiradaResponse'];
export type Retiradas = Schemas['RetiradasResponse'];
export type RetiradaEntrada = Schemas['RegistrarRetiradaRequest'];

/** `de` = primeiro dia do período (AAAA-MM-DD); sem `ate`, a API vai até o fim desse mês. */
export async function listarDespesas(de: string): Promise<Despesas> {
  return (await api.get<Despesas>('/despesas', { params: { de } })).data;
}
export async function criarDespesa(dados: DespesaEntrada): Promise<Despesa> {
  return (await api.post<Despesa>('/despesas', dados)).data;
}
export async function editarDespesa(id: number, dados: DespesaEntrada): Promise<Despesa> {
  return (await api.put<Despesa>(`/despesas/${id}`, dados)).data;
}
export async function excluirDespesa(id: number): Promise<void> {
  await api.delete(`/despesas/${id}`);
}

export async function listarRetiradas(de: string): Promise<Retiradas> {
  return (await api.get<Retiradas>('/retiradas', { params: { de } })).data;
}
export async function registrarRetirada(dados: RetiradaEntrada): Promise<Retirada> {
  return (await api.post<Retirada>('/retiradas', dados)).data;
}
export async function excluirRetirada(id: number): Promise<void> {
  await api.delete(`/retiradas/${id}`);
}
