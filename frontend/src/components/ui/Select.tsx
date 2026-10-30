import { useId, type SelectHTMLAttributes } from 'react';
import { DescricaoCampo, descricaoCampo } from './DescricaoCampo';

type Props = SelectHTMLAttributes<HTMLSelectElement> & { rotulo: string; erro?: string; ajuda?: string };

/** Lista de opções no mesmo desenho do Campo de texto (48 px, contorno de 2 px, erro com ícone e texto). */
export function Select({ rotulo, erro, ajuda, id, className = '', children, ...resto }: Props) {
  const gerado = useId();
  const campoId = id ?? gerado;
  return (
    <div className="flex flex-col gap-2xs">
      <label htmlFor={campoId} className="texto-rotulo-m text-primario">
        {rotulo}
      </label>
      <select
        id={campoId}
        aria-invalid={erro ? true : undefined}
        aria-describedby={descricaoCampo({ campoId, erro, ajuda })}
        className={`min-h-alvo w-full rounded-md border-2 bg-superficie px-md texto-corpo-g text-primario disabled:border-padrao disabled:bg-rebaixado disabled:text-secundario ${erro ? 'border-erro' : 'border-forte'} ${className}`}
        {...resto}
      >
        {children}
      </select>
      <DescricaoCampo campoId={campoId} erro={erro} ajuda={ajuda} />
    </div>
  );
}
