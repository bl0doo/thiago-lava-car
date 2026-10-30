import { api } from './client';
import type { Schemas } from './tipos';

export type CaixaResumo = Schemas['CaixaResumoResponse'];

export type Fechamento = Schemas['FechamentoResponse'];

export type ValorConferido = Schemas['ValorConferidoDto'];

/** GET /caixa/{data}/resumo: pagamentos não estornados do dia, para conferir com o dinheiro e a maquininha. */
export async function obterResumoCaixa(data: string): Promise<CaixaResumo> {
  return (await api.get<CaixaResumo>(`/caixa/${data}/resumo`)).data;
}

/** POST /caixa/{data}/fechamento: com diferença na conferência, a justificativa (10 a 500 caracteres) é obrigatória. */
export async function fecharCaixa(data: string, conferido: ValorConferido[], justificativa: string | null): Promise<Fechamento> {
  return (await api.post<Fechamento>(`/caixa/${data}/fechamento`, { conferido, justificativa })).data;
}

/** POST /caixa/{data}/reabrir: devolve 204; fica na auditoria. */
export async function reabrirCaixa(data: string, justificativa: string): Promise<void> {
  await api.post(`/caixa/${data}/reabrir`, { justificativa });
}

/** GET /caixa/fechamentos?mes=AAAA-MM */
export async function listarFechamentos(mes: string): Promise<Fechamento[]> {
  return (await api.get<Fechamento[]>('/caixa/fechamentos', { params: { mes } })).data;
}
