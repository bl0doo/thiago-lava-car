import { useEffect, useId, useRef, type ReactNode } from 'react';
import { Button } from './Button';

type Props = {
  aberto: boolean;
  titulo: string;
  children?: ReactNode;
  confirmar: string;
  cancelar?: string;
  /** `perigo` pinta o botão de confirmar de vermelho (excluir, estornar, cancelar). */
  tipo?: 'padrao' | 'perigo';
  carregando?: boolean;
  onConfirmar: () => void;
  onFechar: () => void;
};

/** Diálogo de confirmação do Design System (padrão e perigo), sobre o elemento nativo <dialog>, que já prende o foco e fecha com Esc. */
export function Dialogo({ aberto, titulo, children, confirmar, cancelar = 'Voltar', tipo = 'padrao', carregando, onConfirmar, onFechar }: Props) {
  const ref = useRef<HTMLDialogElement>(null);
  const tituloId = useId();

  useEffect(() => {
    const d = ref.current;
    if (!d) return;
    if (aberto && !d.open) {
      if (d.showModal) d.showModal();
      else d.setAttribute('open', '');
    }
    if (!aberto && d.open) {
      if (d.close) d.close();
      else d.removeAttribute('open');
    }
  }, [aberto]);

  return (
    <dialog
      ref={ref}
      aria-labelledby={tituloId}
      onCancel={(e) => {
        e.preventDefault();
        onFechar();
      }}
      className="m-auto w-[calc(100%-2rem)] max-w-[26rem] rounded-lg border-2 border-forte bg-superficie p-lg text-primario shadow-seca backdrop:bg-black/50"
    >
      <div className="flex flex-col gap-md">
        <h2 id={tituloId} className="texto-h2">
          {titulo}
        </h2>
        {children ? <div className="texto-corpo-m text-secundario">{children}</div> : null}
        <div className="flex flex-col gap-xs sm:flex-row-reverse">
          <Button variante={tipo === 'perigo' ? 'perigo' : 'primario'} carregando={carregando} onClick={onConfirmar} className="flex-1">
            {confirmar}
          </Button>
          <Button variante="contorno" onClick={onFechar} className="flex-1">
            {cancelar}
          </Button>
        </div>
      </div>
    </dialog>
  );
}
