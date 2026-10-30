import { useId, type InputHTMLAttributes, type ReactNode } from 'react';
import { DescricaoCampo, descricaoCampo } from './DescricaoCampo';

type Props = Omit<InputHTMLAttributes<HTMLInputElement>, 'type'> & { children: ReactNode; erro?: string };

/** Caixa de seleção com alvo de 48 px; o texto inteiro é clicável. */
export function Caixinha({ children, erro, id, className = '', ...resto }: Props) {
  const gerado = useId();
  const campoId = id ?? gerado;
  return (
    <div className="flex flex-col gap-2xs">
      <label htmlFor={campoId} className={`flex min-h-alvo cursor-pointer items-start gap-sm py-xs texto-corpo-m ${className}`}>
        <input id={campoId} type="checkbox" aria-invalid={erro ? true : undefined} aria-describedby={descricaoCampo({ campoId, erro })} className="mt-0.5 size-6 shrink-0 accent-[var(--acao-secundaria-bg)]" {...resto} />
        <span>{children}</span>
      </label>
      <DescricaoCampo campoId={campoId} erro={erro} />
    </div>
  );
}
