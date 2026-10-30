import { AxiosError, type AxiosResponse, type InternalAxiosRequestConfig } from 'axios';
import type { Cartao, Quadro } from '@/api/tipos';

export function cartao(parcial: Partial<Cartao> = {}): Cartao {
  return {
    id: 1,
    status: 'Aguardando',
    placa: 'ABC1D23',
    modelo: 'Chevrolet Onix',
    servicos: ['Lavagem completa'],
    duracaoEstimadaMin: 90,
    revisaoNecessaria: false,
    inicioEm: null,
    fimEm: null,
    duracaoRealMin: null,
    lavador: null,
    valorTotal: 100,
    cliente: 'Renata Souza',
    telefone: '44999998888',
    ...parcial,
  };
}

export function quadro(parcial: Partial<Quadro> = {}): Quadro {
  return { data: '2026-10-20', aguardando: [], emAndamento: [], concluido: [], ...parcial };
}

/** Erro no formato que o Axios lança para uma resposta ProblemDetails da API. */
export function erroHttp(status: number, corpo: Record<string, unknown> = {}): AxiosError {
  const config = { headers: {} } as InternalAxiosRequestConfig;
  const resposta = { status, statusText: String(status), headers: {}, config, data: corpo } as AxiosResponse;
  return new AxiosError(`Request failed with status code ${status}`, AxiosError.ERR_BAD_REQUEST, config, null, resposta);
}
