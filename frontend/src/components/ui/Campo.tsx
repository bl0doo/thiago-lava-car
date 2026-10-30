import { useId, type InputHTMLAttributes } from 'react';
import { DescricaoCampo, descricaoCampo } from './DescricaoCampo';

type Props = InputHTMLAttributes<HTMLInputElement> & {
  rotulo: string;
  erro?: string;
  ajuda?: string;
};

/**
 * Campo de texto do Design System: rótulo visível, caixa de 48 px com contorno de 2 px e texto de ajuda.
 * Erro sempre com ícone e mensagem abaixo do campo, nunca só a borda vermelha (WCAG 3.3.1).
 */
export function Campo({ rotulo, erro, ajuda, id, className = '', ...resto }: Props) {
  const gerado = useId();
  const campoId = id ?? gerado;

  return (
    <div className="flex flex-col gap-2xs">
      <label htmlFor={campoId} className="texto-rotulo-m text-primario">
        {rotulo}
      </label>
      <input
        id={campoId}
        aria-invalid={erro ? true : undefined}
        aria-describedby={descricaoCampo({ campoId, erro, ajuda })}
        className={`min-h-alvo w-full rounded-md border-2 bg-superficie px-md texto-corpo-g text-primario placeholder:text-secundario disabled:border-padrao disabled:bg-rebaixado disabled:text-secundario ${erro ? 'border-erro' : 'border-forte'} ${className}`}
        {...resto}
      />
      <DescricaoCampo campoId={campoId} erro={erro} ajuda={ajuda} />
    </div>
  );
}
