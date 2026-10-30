import { api } from './client';
import type { Schemas } from './tipos';

export type TipoMensagem = 'lavagem-iniciada' | 'lavagem-concluida';
export type MensagemGerada = Schemas['MensagemGeradaResponse'];
export type MensagemEnviada = Schemas['MensagemEnviadaResponse'];

/** GET /ordens-servico/{id}/mensagens/{tipo}: texto pronto e link do WhatsApp. 409 se a OS não está na etapa certa. */
export async function gerarMensagem(osId: number, tipo: TipoMensagem): Promise<MensagemGerada> {
  return (await api.get<MensagemGerada>(`/ordens-servico/${osId}/mensagens/${tipo}`)).data;
}

/** POST /ordens-servico/{id}/mensagens/{tipo}/enviada: guarda o texto final no histórico. */
export async function registrarEnvio(osId: number, tipo: TipoMensagem): Promise<MensagemEnviada> {
  return (await api.post<MensagemEnviada>(`/ordens-servico/${osId}/mensagens/${tipo}/enviada`)).data;
}
