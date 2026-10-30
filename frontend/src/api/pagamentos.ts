import { api } from './client';
import type { PagamentoDaOS } from './ordensServico';

export type PagamentoRegistrado = {
  id: number;
  valorBruto: number;
  percentualTaxa: number;
  valorTaxa: number;
  valorLiquido: number;
  saldoPendente: number;
  dataCaixa: string;
};

/** POST /ordens-servico/{id}/pagamentos: o valor não passa do saldo e o caixa do dia não pode estar fechado. */
export async function registrarPagamento(osId: number, formaPagamento: string, valorBruto: number): Promise<PagamentoRegistrado> {
  return (await api.post<PagamentoRegistrado>(`/ordens-servico/${osId}/pagamentos`, { formaPagamento, valorBruto })).data;
}

/** POST /pagamentos/{id}/estorno: só com o caixa do dia aberto; fica na auditoria. */
export async function estornarPagamento(id: number, motivo: string | null): Promise<PagamentoDaOS> {
  return (await api.post<PagamentoDaOS>(`/pagamentos/${id}/estorno`, { motivo })).data;
}
