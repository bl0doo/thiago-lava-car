import { cartao, quadro } from '@/test/fabrica';
import { aplicarAcao, localizar, podeDesfazer, substituir } from './quadro';

const agora = new Date('2026-10-20T16:05:00Z');

describe('aplicarAcao (previsão otimista)', () => {
  it('iniciar leva o cartão de Aguardando para Em andamento com o horário atual', () => {
    const antes = quadro({ aguardando: [cartao({ id: 1 }), cartao({ id: 2 })] });

    const depois = aplicarAcao(antes, 1, 'iniciar', agora);

    expect(depois.aguardando.map((c) => c.id)).toEqual([2]);
    expect(depois.emAndamento[0]).toMatchObject({ id: 1, status: 'EmAndamento', inicioEm: agora.toISOString() });
  });

  it('encerrar leva para Concluído e estima a duração em minutos', () => {
    const inicio = new Date(agora.getTime() - 102 * 60_000).toISOString();
    const antes = quadro({ emAndamento: [cartao({ status: 'EmAndamento', inicioEm: inicio })] });

    const depois = aplicarAcao(antes, 1, 'encerrar', agora);

    expect(depois.emAndamento).toHaveLength(0);
    expect(depois.concluido[0]).toMatchObject({ status: 'Concluido', duracaoRealMin: 102 });
  });

  it('desfazer volta um passo: Concluído vira Em andamento e Em andamento vira Aguardando', () => {
    const concluido = quadro({ concluido: [cartao({ status: 'Concluido', inicioEm: agora.toISOString(), fimEm: agora.toISOString(), duracaoRealMin: 3 })] });
    const andamento = aplicarAcao(concluido, 1, 'desfazer', agora);
    expect(andamento.emAndamento[0]).toMatchObject({ status: 'EmAndamento', fimEm: null, duracaoRealMin: null });

    const aguardando = aplicarAcao(andamento, 1, 'desfazer', agora);
    expect(aguardando.aguardando[0]).toMatchObject({ status: 'Aguardando', inicioEm: null, lavador: null });
  });

  it('não altera o quadro se o cartão não existe', () => {
    const antes = quadro({ aguardando: [cartao()] });
    expect(aplicarAcao(antes, 99, 'iniciar', agora)).toBe(antes);
  });

  it('não muda o quadro original (imutável)', () => {
    const antes = quadro({ aguardando: [cartao()] });
    aplicarAcao(antes, 1, 'iniciar', agora);
    expect(antes.aguardando).toHaveLength(1);
    expect(antes.emAndamento).toHaveLength(0);
  });
});

describe('substituir', () => {
  it('troca o cartão previsto pelo que a API devolveu, na coluna certa', () => {
    const previsto = aplicarAcao(quadro({ aguardando: [cartao()] }), 1, 'iniciar', agora);

    const final = substituir(previsto, cartao({ status: 'EmAndamento', inicioEm: '2026-10-20T16:05:01Z', lavador: 'Diego' }));

    expect(localizar(final, 1)).toMatchObject({ lavador: 'Diego', inicioEm: '2026-10-20T16:05:01Z' });
    expect(final.emAndamento).toHaveLength(1);
  });
});

describe('podeDesfazer', () => {
  it('vale por 2 minutos depois do último passo', () => {
    const c = cartao({ status: 'EmAndamento', inicioEm: agora.toISOString() });
    expect(podeDesfazer(c, agora.getTime() + 119_000)).toBe(true);
    expect(podeDesfazer(c, agora.getTime() + 121_000)).toBe(false);
  });

  it('não vale para cartão que ainda aguarda', () => {
    expect(podeDesfazer(cartao(), agora.getTime())).toBe(false);
  });
});
