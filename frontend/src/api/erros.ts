import axios from 'axios';
import type { ProblemDetails } from './tipos';

export type ErroDaApi = {
  /** 0 quando não houve resposta (sem rede, API fora do ar). */
  status: number;
  mensagem: string;
  /** Mensagens por campo (422), com o nome do campo em camelCase. */
  campos: Record<string, string>;
  traceId?: string;
};

const MENSAGEM_PADRAO: Record<number, string> = {
  401: 'Sessão expirada. Entre novamente.',
  403: 'Seu perfil não tem permissão para esta ação.',
  404: 'Não encontrado.',
  429: 'Muitas tentativas. Aguarde um minuto e tente de novo.',
  500: 'Algo deu errado, tente de novo.',
};

/** Converte qualquer erro do Axios no formato ProblemDetails (RFC 9457) da API em algo pronto para a tela. */
export function interpretarErro(erro: unknown): ErroDaApi {
  if (!axios.isAxiosError<ProblemDetails>(erro) || !erro.response) {
    return { status: 0, mensagem: 'Sem conexão com o servidor. Confira a internet e tente de novo.', campos: {} };
  }
  const { status, data } = erro.response;
  const campos: Record<string, string> = {};
  for (const [campo, mensagens] of Object.entries(data?.errors ?? {})) {
    if (mensagens[0]) campos[campo] = mensagens[0];
  }
  return {
    status,
    mensagem: data?.detail ?? MENSAGEM_PADRAO[status] ?? 'Algo deu errado, tente de novo.',
    campos,
    traceId: data?.traceId,
  };
}
