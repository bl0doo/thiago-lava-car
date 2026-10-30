import { CircleAlert } from 'lucide-react';
import { useId, type TextareaHTMLAttributes } from 'react';

type Props = TextareaHTMLAttributes<HTMLTextAreaElement> & { rotulo: string; erro?: string | undefined; ajuda?: string };

/** Campo de texto longo (pertences, avarias, justificativas): o limite de 500 caracteres vem em `maxLength`. */
export function AreaTexto({ rotulo, erro, ajuda, id, className = '', ...resto }: Props) {
  const gerado = useId();
  const campoId = id ?? gerado;
  const descricao = [erro ? `${campoId}-erro` : null, ajuda ? `${campoId}-ajuda` : null].filter(Boolean).join(' ') || undefined;
  return (
    <div className="flex flex-col gap-2xs">
      <label htmlFor={campoId} className="texto-rotulo-m text-primario">
        {rotulo}
      </label>
      <textarea
        id={campoId}
        rows={3}
        aria-invalid={erro ? true : undefined}
        aria-describedby={descricao}
        className={`w-full rounded-md border-2 bg-superficie px-md py-sm texto-corpo-g text-primario placeholder:text-secundario ${erro ? 'border-erro' : 'border-forte'} ${className}`}
        {...resto}
      />
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
