import { CircleAlert } from 'lucide-react';
import { useId, type SelectHTMLAttributes } from 'react';

type Props = SelectHTMLAttributes<HTMLSelectElement> & { rotulo: string; erro?: string | undefined; ajuda?: string };

/** Lista de opções no mesmo desenho do Campo de texto (48 px, contorno de 2 px, erro com ícone e texto). */
export function Select({ rotulo, erro, ajuda, id, className = '', children, ...resto }: Props) {
  const gerado = useId();
  const campoId = id ?? gerado;
  const descricao = [erro ? `${campoId}-erro` : null, ajuda ? `${campoId}-ajuda` : null].filter(Boolean).join(' ') || undefined;
  return (
    <div className="flex flex-col gap-2xs">
      <label htmlFor={campoId} className="texto-rotulo-m text-primario">
        {rotulo}
      </label>
      <select
        id={campoId}
        aria-invalid={erro ? true : undefined}
        aria-describedby={descricao}
        className={`min-h-alvo w-full rounded-md border-2 bg-superficie px-md texto-corpo-g text-primario disabled:border-padrao disabled:bg-rebaixado disabled:text-secundario ${erro ? 'border-erro' : 'border-forte'} ${className}`}
        {...resto}
      >
        {children}
      </select>
      {erro ? (
        <p id={`${campoId}-erro`} className="flex items-start gap-2xs texto-rotulo-p leading-5 text-erro">
          <CircleAlert aria-hidden="true" className="mt-0.5 size-4 shrink-0" strokeWidth={2.5} />
          {erro}
        </p>
      ) : null}
      {ajuda ? (
        <p id={`${campoId}-ajuda`} className="texto-corpo-p text-secundario">
          {ajuda}
        </p>
      ) : null}
    </div>
  );
}
