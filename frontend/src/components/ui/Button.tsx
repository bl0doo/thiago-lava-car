import type { ButtonHTMLAttributes } from 'react';

/** Os quatro tipos do componente Botão do Design System. */
export type Variante = 'primario' | 'secundario' | 'contorno' | 'perigo';
/** `padrao` = 48 px; `lavador` = 72 px com rótulo de 28 px, para uso com luva e mão molhada. */
export type Tamanho = 'padrao' | 'lavador';

const VARIANTES: Record<Variante, string> = {
  primario: 'bg-acao-primaria text-sobre-destaque hover:bg-acao-primaria-hover',
  // em fundo azul o anel azul some: usa anel escuro (pendência 2 do Design System)
  secundario: 'bg-acao-secundaria text-texto-inverso hover:bg-acao-secundaria-hover focus-visible:outline-forte',
  contorno: 'bg-superficie text-primario hover:bg-rebaixado',
  perigo: 'bg-acao-perigo text-texto-inverso hover:brightness-90',
};

const TAMANHOS: Record<Tamanho, string> = {
  padrao: 'min-h-alvo rounded-md px-lg texto-rotulo-m',
  lavador: 'min-h-alvo-lavador-principal rounded-lg px-xl texto-botao-lavador',
};

/** Classes do botão, para reaproveitar em links que têm cara de botão. */
export function classesBotao(variante: Variante = 'primario', tamanho: Tamanho = 'padrao') {
  return [
    'inline-flex items-center justify-center gap-xs border-2 border-forte shadow-seca transition-[transform,box-shadow,background-color]',
    'active:translate-y-[3px] active:shadow-none',
    'disabled:cursor-not-allowed disabled:border-padrao disabled:bg-rebaixado disabled:text-secundario disabled:shadow-none disabled:active:translate-y-0',
    VARIANTES[variante],
    TAMANHOS[tamanho],
  ].join(' ');
}

type Props = ButtonHTMLAttributes<HTMLButtonElement> & {
  variante?: Variante;
  tamanho?: Tamanho;
  carregando?: boolean;
};

export function Button({ variante = 'primario', tamanho = 'padrao', carregando = false, className = '', children, disabled, type = 'button', ...resto }: Props) {
  return (
    <button
      type={type}
      disabled={disabled || carregando}
      aria-busy={carregando || undefined}
      className={`${classesBotao(variante, tamanho)} ${className}`}
      {...resto}
    >
      {children}
    </button>
  );
}
