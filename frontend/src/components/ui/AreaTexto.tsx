import { useId, type TextareaHTMLAttributes } from 'react';
import { DescricaoCampo, descricaoCampo } from './DescricaoCampo';

type Props = TextareaHTMLAttributes<HTMLTextAreaElement> & { rotulo: string; erro?: string; ajuda?: string };

/** Campo de texto longo (pertences, avarias, justificativas): o limite de 500 caracteres vem em `maxLength`. */
export function AreaTexto({ rotulo, erro, ajuda, id, className = '', ...resto }: Props) {
  const gerado = useId();
  const campoId = id ?? gerado;
  return (
    <div className="flex flex-col gap-2xs">
      <label htmlFor={campoId} className="texto-rotulo-m text-primario">
        {rotulo}
      </label>
      <textarea
        id={campoId}
        rows={3}
        aria-invalid={erro ? true : undefined}
        aria-describedby={descricaoCampo({ campoId, erro, ajuda })}
        className={`w-full rounded-md border-2 bg-superficie px-md py-sm texto-corpo-g text-primario placeholder:text-secundario ${erro ? 'border-erro' : 'border-forte'} ${className}`}
        {...resto}
      />
      <DescricaoCampo campoId={campoId} erro={erro} ajuda={ajuda} />
    </div>
  );
}
