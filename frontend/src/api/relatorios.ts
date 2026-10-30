import { api } from './client';
import type { Schemas } from './tipos';

export type LucroServico = Schemas['LucroPorServicoResponse'];

export type PontoEquilibrio = Schemas['PontoEquilibrioResponse'];

export type Faturamento = Schemas['FaturamentoResponse'];

export type Produtividade = Schemas['ProdutividadeResponse'];

export type Periodo = { inicio: string; fim: string };

export const lucroPorServico = (p: Periodo) => api.get<LucroServico[]>('/relatorios/lucro-por-servico', { params: p }).then((r) => r.data);
export const faturamento = (p: Periodo) => api.get<Faturamento>('/relatorios/faturamento', { params: p }).then((r) => r.data);
export const produtividade = (p: Periodo) => api.get<Produtividade>('/relatorios/produtividade', { params: p }).then((r) => r.data);
export const pontoEquilibrio = (mes: string) => api.get<PontoEquilibrio>('/relatorios/ponto-equilibrio', { params: { mes } }).then((r) => r.data);
