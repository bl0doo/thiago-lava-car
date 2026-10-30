import { CircleAlert } from 'lucide-react';
import { useId, type InputHTMLAttributes, type ReactNode } from 'react';

type Props = Omit<InputHTMLAttributes<HTMLInputElement>, 'type'> & { children: ReactNode; erro?: string | undefined };

/** Caixa de seleção com alvo de 48 px; o texto inteiro é clicável. */
export function Caixinha({ children, erro, id, className = '', ...resto }: Props) {
  const gerado = useId();
  const campoId = id ?? gerado;
  return (
    <div className="flex flex-col gap-2xs">
      <label htmlFor={campoId} className={`flex min-h-alvo cursor-pointer items-start gap-sm py-xs texto-corpo-m ${className}`}>
        <input id={campoId} type="checkbox" aria-invalid={erro ? true : undefined} aria-describedby={erro ? `${campoId}-erro` : undefined} className="mt-0.5 size-6 shrink-0 accent-[var(--acao-secundaria-bg)]" {...resto} />
        <span>{children}</span>
      </label>
      {erro ? (
        <p id={`${campoId}-erro`} className="flex items-start gap-2xs texto-rotulo-p leading-5 text-erro">
          <CircleAlert aria-hidden="true" className="mt-0.5 size-4 shrink-0" strokeWidth={2.5} />
          {erro}
        </p>
      ) : null}
    </div>
  );
}
