import { api } from './client';
import type { TipoVeiculo } from './veiculos';

export type PrecoServico = { tipoVeiculo: TipoVeiculo; preco: number; precoComCera: number | null };

export type Servico = { id: number; nome: string; categoria: string; tempoEstimadoMin: number; ativo: boolean; precos: PrecoServico[]; custoInsumos?: number };

/** GET /servicos: catálogo. O dono também recebe o custo de insumos; o cliente, não. */
export async function listarServicos(): Promise<Servico[]> {
  return (await api.get<Servico[]>('/servicos')).data;
}

export type ServicoEntrada = {
  nome: string;
  categoria: string;
  tempoEstimadoMin: number;
  custoInsumos: number;
  precos: PrecoServico[];
};

export async function criarServico(dados: ServicoEntrada): Promise<Servico> {
  return (await api.post<Servico>('/servicos', dados)).data;
}

export async function atualizarServico(id: number, dados: ServicoEntrada): Promise<Servico> {
  return (await api.put<Servico>(`/servicos/${id}`, dados)).data;
}

export async function alterarAtivoServico(id: number, ativo: boolean): Promise<void> {
  await api.patch(`/servicos/${id}/ativo`, { ativo });
}
