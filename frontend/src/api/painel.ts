import { api } from './client';
import type { Schemas } from './tipos';

export type PainelHoje = Schemas['PainelHojeResponse'];

export async function obterPainelHoje(): Promise<PainelHoje> {
  return (await api.get<PainelHoje>('/painel/hoje')).data;
}
