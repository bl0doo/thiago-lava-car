import type { Cartao, Quadro, StatusOS } from '@/api/tipos';

export type Acao = 'iniciar' | 'encerrar' | 'desfazer';

/** Janela em que o servidor aceita "Desfazer". */
export const JANELA_DESFAZER_MS = 2 * 60 * 1000;

type Coluna = 'aguardando' | 'emAndamento' | 'concluido';

const COLUNA_DE_STATUS: Partial<Record<StatusOS, Coluna>> = {
  Aguardando: 'aguardando',
  EmAndamento: 'emAndamento',
  Concluido: 'concluido',
};

export function localizar(quadro: Quadro, id: number): Cartao | undefined {
  return [...quadro.aguardando, ...quadro.emAndamento, ...quadro.concluido].find((c) => c.id === id);
}

/**
 * Previsão local do que o servidor vai responder, para o cartão mudar de coluna na hora do toque.
 * Se a API recusar, o quadro anterior é restaurado; se aceitar, o cartão verdadeiro substitui este.
 */
export function aplicarAcao(quadro: Quadro, id: number, acao: Acao, agora: Date): Quadro {
  const atual = localizar(quadro, id);
  if (!atual) return quadro;

  const instante = agora.toISOString();
  let proximo: Cartao;
  switch (acao) {
    case 'iniciar':
      proximo = { ...atual, status: 'EmAndamento', inicioEm: instante };
      break;
    case 'encerrar': {
      const inicio = atual.inicioEm ? new Date(atual.inicioEm).getTime() : agora.getTime();
      proximo = {
        ...atual,
        status: 'Concluido',
        fimEm: instante,
        duracaoRealMin: Math.round((agora.getTime() - inicio) / 60000),
      };
      break;
    }
    case 'desfazer':
      proximo =
        atual.status === 'Concluido'
          ? { ...atual, status: 'EmAndamento', fimEm: null, duracaoRealMin: null, revisaoNecessaria: false }
          : { ...atual, status: 'Aguardando', inicioEm: null, lavador: null };
      break;
  }
  return substituir(quadro, proximo);
}

/** Troca o cartão pelo que a API devolveu, mudando de coluna se o status mudou. */
export function substituir(quadro: Quadro, cartao: Cartao): Quadro {
  const sem = (lista: Cartao[]) => lista.filter((c) => c.id !== cartao.id);
  const novo: Quadro = {
    ...quadro,
    aguardando: sem(quadro.aguardando),
    emAndamento: sem(quadro.emAndamento),
    concluido: sem(quadro.concluido),
  };
  const coluna = COLUNA_DE_STATUS[cartao.status];
  if (coluna) novo[coluna] = [...novo[coluna], cartao];
  return novo;
}

/** O botão Desfazer só aparece enquanto o último passo tem menos de 2 minutos (o servidor confere de novo). */
export function podeDesfazer(cartao: Cartao, agora: number): boolean {
  const ultimo =
    cartao.status === 'EmAndamento' ? cartao.inicioEm : cartao.status === 'Concluido' ? cartao.fimEm : null;
  return !!ultimo && agora - new Date(ultimo).getTime() <= JANELA_DESFAZER_MS;
}
