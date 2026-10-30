import { api } from './client';
import type { Schemas } from './tipos';
import type { TipoVeiculo } from './veiculos';

export type PrecoServico = Omit<Schemas['PrecoServicoDto'], 'tipoVeiculo'> & { tipoVeiculo: TipoVeiculo };

export type Servico = Omit<Schemas['ServicoResponse'], 'precos'> & { precos: PrecoServico[] };

/** GET /servicos: catálogo. O dono também recebe o custo de insumos; o cliente, não. */
export async function listarServicos(): Promise<Servico[]> {
  return (await api.get<Servico[]>('/servicos')).data;
}

export type ServicoEntrada = Omit<Schemas['SalvarServicoRequest'], 'precos'> & {
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
