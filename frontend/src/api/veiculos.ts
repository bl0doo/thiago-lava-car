import axios from 'axios';
import { api } from './client';

export type TipoVeiculo = 'Moto' | 'Carro' | 'Caminhonete';

export type Veiculo = {
  id: number;
  clienteId: number;
  placa: string;
  modelo: string;
  tipo: TipoVeiculo;
  cor?: string | null;
};

export type VeiculoEntrada = {
  /** Só o dono informa; para o cliente a API usa o do token. */
  clienteId?: number;
  placa: string;
  modeloDescricao: string;
  tipoVeiculo: TipoVeiculo;
  cor: string | null;
};

/** GET /veiculos?placa=. Devolve null quando a placa ainda não está cadastrada (404). */
export async function buscarVeiculoPorPlaca(placa: string): Promise<Veiculo | null> {
  try {
    return (await api.get<Veiculo>('/veiculos', { params: { placa } })).data;
  } catch (e) {
    if (axios.isAxiosError(e) && e.response?.status === 404) return null;
    throw e;
  }
}

export async function criarVeiculo(dados: VeiculoEntrada): Promise<Veiculo> {
  return (await api.post<Veiculo>('/veiculos', dados)).data;
}

export type VisitaFicha = { id: number; data: string; status: string; servicos: string[]; valorTotal: number; duracaoRealMin: number | null };

export type Ficha = { veiculo: Veiculo; totalServicos: number; historico: VisitaFicha[] };

/** GET /veiculos/{id}/ficha: a Ficha do carro (histórico de visitas). O cliente só abre a dos próprios veículos. */
export async function obterFicha(id: number): Promise<Ficha> {
  return (await api.get<Ficha>(`/veiculos/${id}/ficha`)).data;
}
