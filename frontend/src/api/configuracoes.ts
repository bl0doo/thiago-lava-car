import { api } from './client';

export type Configuracao = {
  nomeLavaJato: string;
  telefone: string | null;
  abertura: string;
  fechamento: string;
  diasFuncionamento: number[];
  diariaSemana: number;
  diariaSabado: number;
  intervaloAgendaMin: number;
};

export type ModeloMensagem = { tipo: string; texto: string; atualizadoEm: string };
export type Taxa = { formaPagamento: string; percentual: number; valorFixo: number };

export async function obterConfiguracao(): Promise<Configuracao> {
  return (await api.get<Configuracao>('/configuracoes')).data;
}

/** PUT /configuracoes: muda a grade da agenda e a diária sugerida. */
export async function salvarConfiguracao(dados: Configuracao): Promise<Configuracao> {
  return (await api.put<Configuracao>('/configuracoes', dados)).data;
}

export async function listarTaxas(): Promise<Taxa[]> {
  return (await api.get<Taxa[]>('/taxas')).data;
}

/** PUT /taxas/{forma}: vale só para pagamentos futuros. */
export async function alterarTaxa(forma: string, percentual: number, valorFixo: number): Promise<Taxa> {
  return (await api.put<Taxa>(`/taxas/${forma}`, { percentual, valorFixo })).data;
}

/** `tipo`: lavagem-iniciada ou lavagem-concluida. */
export async function listarModelos(): Promise<ModeloMensagem[]> {
  return (await api.get<ModeloMensagem[]>('/modelos-mensagem')).data;
}

export async function salvarModelo(tipo: string, texto: string): Promise<ModeloMensagem> {
  return (await api.put<ModeloMensagem>(`/modelos-mensagem/${tipo}`, { texto })).data;
}
