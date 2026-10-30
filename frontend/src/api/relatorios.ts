import { api } from './client';

export type LucroServico = {
  servico: string;
  quantidade: number;
  receitaLiquida: number;
  insumos: number;
  maoDeObra: number;
  lucro: number;
  lucroMedio: number;
  tempoMedioMin: number;
};

export type PontoEquilibrio = {
  mes: string;
  despesas: number;
  diarias: number;
  contas: number;
  margemPorCarro: number | null;
  margemEstimada: boolean;
  carrosNecessarios: number | null;
  carrosFeitos: number;
  faltam: number | null;
};

export type FaturamentoDia = { data: string; bruto: number; liquido: number; osPagas: number; ticketMedio: number };

export type Faturamento = { dias: FaturamentoDia[]; totalBruto: number; totalLiquido: number; osPagas: number; ticketMedio: number };

export type TempoServico = { servico: string; quantidade: number; tempoMedioMin: number; estimadoMedioMin: number };
export type TempoLavador = { lavador: string; quantidade: number; tempoMedioMin: number };
export type Produtividade = { porServico: TempoServico[]; porLavador: TempoLavador[] };

export type Periodo = { inicio: string; fim: string };

export const lucroPorServico = (p: Periodo) => api.get<LucroServico[]>('/relatorios/lucro-por-servico', { params: p }).then((r) => r.data);
export const faturamento = (p: Periodo) => api.get<Faturamento>('/relatorios/faturamento', { params: p }).then((r) => r.data);
export const produtividade = (p: Periodo) => api.get<Produtividade>('/relatorios/produtividade', { params: p }).then((r) => r.data);
export const pontoEquilibrio = (mes: string) => api.get<PontoEquilibrio>('/relatorios/ponto-equilibrio', { params: { mes } }).then((r) => r.data);
