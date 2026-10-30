import axios from 'axios';
import { api } from './client';
import type { Schemas } from './tipos';

export type TipoVeiculo = 'Moto' | 'Carro' | 'Caminhonete';

export type Veiculo = Omit<Schemas['VeiculoResponse'], 'tipo'> & {
  tipo: TipoVeiculo;
};

export type VeiculoEntrada = Omit<Schemas['CriarVeiculoRequest'], 'modeloDescricao' | 'tipoVeiculo'> & {
  modeloDescricao: string;
  tipoVeiculo: TipoVeiculo;
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

export type Ficha = Omit<Schemas['FichaVeiculoResponse'], 'veiculo'> & { veiculo: Veiculo };

/** GET /veiculos/{id}/ficha: a Ficha do carro (histórico de visitas). O cliente só abre a dos próprios veículos. */
export async function obterFicha(id: number): Promise<Ficha> {
  return (await api.get<Ficha>(`/veiculos/${id}/ficha`)).data;
}
