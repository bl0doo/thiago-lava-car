import { api } from './client';
import type { Cartao, Quadro } from './tipos';

export async function obterQuadro(data?: string): Promise<Quadro> {
  const { data: quadro } = await api.get<Quadro>('/ordens-servico/quadro', { params: data ? { data } : undefined });
  return quadro;
}

export async function iniciarLavagem(id: number): Promise<Cartao> {
  return (await api.post<Cartao>(`/ordens-servico/${id}/iniciar`)).data;
}

export async function encerrarLavagem(id: number): Promise<Cartao> {
  return (await api.post<Cartao>(`/ordens-servico/${id}/encerrar`)).data;
}

export async function desfazerUltimoPasso(id: number): Promise<Cartao> {
  return (await api.post<Cartao>(`/ordens-servico/${id}/desfazer`)).data;
}

export type AvariaEntrada = { local: string; tipo: string; descricao: string };

export type EntradaOS = {
  veiculoId: number;
  itens: { servicoId: number; comCera: boolean; precoManual: number | null }[];
  semAvarias: boolean;
  avarias: AvariaEntrada[];
  pertencesEntrada: string | null;
  nomeQuemRetira: string | null;
  aceiteCliente: boolean;
};

export type OSCriada = { id: number; status: string; valorTotal: number; duracaoEstimadaMin: number; previsaoConclusao: string };

/** POST /ordens-servico: registra a entrada do carro com a vistoria (o cartão entra em Aguardando). */
export async function registrarEntrada(dados: EntradaOS): Promise<OSCriada> {
  return (await api.post<OSCriada>('/ordens-servico', dados)).data;
}

export type ItemDetalhe = { servicoId: number; nome: string; comCera: boolean; precoFinal: number };
export type AvariaDetalhe = { local: string; tipo: string; descricao: string | null };
export type PagamentoDaOS = {
  id: number;
  ordemServicoId: number;
  formaPagamento: string;
  valorBruto: number;
  percentualTaxa: number;
  valorTaxa: number;
  valorLiquido: number;
  dataCaixa: string;
  criadoEm: string;
  estornado: boolean;
};

type RespostaDetalhe = {
  cartao: Cartao;
  itens: ItemDetalhe[];
  pertencesEntrada: string | null;
  pertencesEncontrados: string | null;
  nomeQuemRetira: string | null;
  semAvarias: boolean;
  avarias: AvariaDetalhe[];
  avaliacao: number | null;
  valorPago: number;
  saldoPendente: number;
  pagamentos: PagamentoDaOS[];
};

/** O cartão do Pit Stop com itens, vistoria, avaliação, pagamentos e saldo (GET /ordens-servico/{id}/detalhe, só o Dono). */
export type DetalheOrdem = Cartao & Omit<RespostaDetalhe, 'cartao'>;

export async function obterOS(id: number): Promise<DetalheOrdem> {
  const { cartao, ...resto } = (await api.get<RespostaDetalhe>(`/ordens-servico/${id}/detalhe`)).data;
  return { ...cartao, ...resto };
}

/** POST /ordens-servico/{id}/entregar: Concluído para Entregue; o saldo pendente não impede, vira pendência no caixa. */
export async function entregarOS(id: number, nomeQuemRetira: string | null): Promise<Cartao> {
  return (await api.post<Cartao>(`/ordens-servico/${id}/entregar`, { nomeQuemRetira })).data;
}

/** POST /ordens-servico/{id}/cancelar: só vale em Aguardando. */
export async function cancelarOS(id: number): Promise<Cartao> {
  return (await api.post<Cartao>(`/ordens-servico/${id}/cancelar`)).data;
}

/** PATCH /ordens-servico/{id}/avaliacao: nota de 1 a 5 dada pelo cliente. */
export async function avaliarOS(id: number, nota: number): Promise<Cartao> {
  return (await api.patch<Cartao>(`/ordens-servico/${id}/avaliacao`, { nota })).data;
}

/** GET /ordens-servico/{id}: o cartão simples, que o lavador também pode ler (o /detalhe é só do dono). */
export async function obterCartao(id: number): Promise<Cartao> {
  return (await api.get<Cartao>(`/ordens-servico/${id}`)).data;
}

/** PATCH /ordens-servico/{id}/pertences-encontrados: texto de até 500 caracteres; vazio limpa o registro. */
export async function registrarPertencesEncontrados(id: number, texto: string | null): Promise<Cartao> {
  return (await api.patch<Cartao>(`/ordens-servico/${id}/pertences-encontrados`, { texto })).data;
}
