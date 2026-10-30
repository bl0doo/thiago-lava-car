import { api } from './client';

export type TipoMensagem = 'lavagem-iniciada' | 'lavagem-concluida';
export type MensagemGerada = { texto: string; canal: string; linkWhatsApp: string };
export type MensagemEnviada = { id: number; tipo: string; texto: string; enviadaEm: string };

/** GET /ordens-servico/{id}/mensagens/{tipo}: texto pronto e link do WhatsApp. 409 se a OS não está na etapa certa. */
export async function gerarMensagem(osId: number, tipo: TipoMensagem): Promise<MensagemGerada> {
  return (await api.get<MensagemGerada>(`/ordens-servico/${osId}/mensagens/${tipo}`)).data;
}

/** POST /ordens-servico/{id}/mensagens/{tipo}/enviada: guarda o texto final no histórico. */
export async function registrarEnvio(osId: number, tipo: TipoMensagem): Promise<MensagemEnviada> {
  return (await api.post<MensagemEnviada>(`/ordens-servico/${osId}/mensagens/${tipo}/enviada`)).data;
}
