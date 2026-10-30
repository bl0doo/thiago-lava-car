import { CircleAlert } from 'lucide-react';
import { useId, type InputHTMLAttributes } from 'react';

type Props = InputHTMLAttributes<HTMLInputElement> & {
  rotulo: string;
  erro?: string | undefined;
  ajuda?: string;
};

/**
 * Campo de texto do Design System: rótulo visível, caixa de 48 px com contorno de 2 px e texto de ajuda.
 * Erro sempre com ícone e mensagem abaixo do campo, nunca só a borda vermelha (WCAG 3.3.1).
 */
export function Campo({ rotulo, erro, ajuda, id, className = '', ...resto }: Props) {
  const gerado = useId();
  const campoId = id ?? gerado;
  const erroId = `${campoId}-erro`;
  const ajudaId = `${campoId}-ajuda`;
  const descricao = [erro ? erroId : null, ajuda ? ajudaId : null].filter(Boolean).join(' ') || undefined;

  return (
    <div className="flex flex-col gap-2xs">
      <label htmlFor={campoId} className="texto-rotulo-m text-primario">
        {rotulo}
      </label>
      <input
        id={campoId}
        aria-invalid={erro ? true : undefined}
        aria-describedby={descricao}
        className={`min-h-alvo w-full rounded-md border-2 bg-superficie px-md texto-corpo-g text-primario placeholder:text-secundario disabled:border-padrao disabled:bg-rebaixado disabled:text-secundario ${erro ? 'border-erro' : 'border-forte'} ${className}`}
        {...resto}
      />
      {erro ? (
        <p id={erroId} className="flex items-start gap-2xs texto-rotulo-p leading-5 text-erro">
          <CircleAlert aria-hidden="true" className="mt-0.5 size-4 shrink-0" strokeWidth={2.5} />
          {erro}
        </p>
      ) : null}
      {ajuda ? (
        <p id={ajudaId} className="texto-corpo-p text-secundario">
          {ajuda}
        </p>
      ) : null}
    </div>
  );
}
