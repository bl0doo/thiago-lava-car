import { ChevronLeft } from 'lucide-react';
import type { ReactNode } from 'react';

type Props = {
  titulo: string;
  /** Linha pequena acima do título (estilo Eyebrow, em caixa alta). */
  sobretitulo?: ReactNode;
  /** Botão Voltar de 48 px à esquerda. */
  onVoltar?: () => void;
  /** Ação à direita (ex.: Nova entrada no tablet, Sair na fila do lavador). */
  acao?: ReactNode;
};

/** Barra de título das telas do protótipo: eyebrow + título H1, com Voltar e ação opcionais. */
export function BarraTitulo({ titulo, sobretitulo, onVoltar, acao }: Props) {
  return (
    <header className="flex min-h-16 items-center gap-xs px-md pt-sm pb-xs">
      {onVoltar ? (
        <button type="button" onClick={onVoltar} aria-label="Voltar" className="-ml-xs flex size-alvo shrink-0 items-center justify-center rounded-md text-primario hover:bg-rebaixado">
          <ChevronLeft aria-hidden="true" className="size-6" strokeWidth={2.5} />
        </button>
      ) : null}
      <div className="flex min-w-0 flex-1 flex-col">
        {sobretitulo ? <p className="texto-eyebrow text-secundario">{sobretitulo}</p> : null}
        <h1 className="texto-h1 text-primario">{titulo}</h1>
      </div>
      {acao}
    </header>
  );
}
