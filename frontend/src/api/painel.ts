import { api } from './client';

/** GET /painel/hoje (Entrega 2, seção 3). */
export type PainelHoje = {
  entrouHoje: number;
  liquidoHoje: number;
  sobrouDeVerdade: number;
  carrosNoMes: number;
  /** `null` quando ainda não há como estimar (sem despesas ou sem preço médio no mês). */
  carrosFaltamParaPagarContas: number | null;
  pendencias: string[];
};

export async function obterPainelHoje(): Promise<PainelHoje> {
  return (await api.get<PainelHoje>('/painel/hoje')).data;
}
