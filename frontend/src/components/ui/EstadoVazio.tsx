import type { LucideIcon } from 'lucide-react';
import type { ReactNode } from 'react';

type Props = {
  Icone: LucideIcon;
  titulo: string;
  children?: ReactNode;
  className?: string;
  /** Tag do título; `h1` quando o estado vazio é a tela inteira. */
  nivel?: 'h1' | 'h2' | 'p';
};

/** Estado vazio do Design System: ícone em caixa, título e uma frase que diz o que fazer a seguir. */
export function EstadoVazio({ Icone, titulo, children, className = '', nivel: Titulo = 'p' }: Props) {
  return (
    <div className={`flex flex-col items-center gap-sm rounded-lg border-2 border-dashed border-padrao bg-superficie px-lg py-xl text-center ${className}`}>
      <span className="flex size-16 items-center justify-center rounded-md bg-rebaixado text-primario">
        <Icone aria-hidden="true" className="size-8" strokeWidth={2} />
      </span>
      <Titulo className="texto-h2 text-primario">{titulo}</Titulo>
      {children ? <div className="texto-corpo-m text-secundario">{children}</div> : null}
    </div>
  );
}
