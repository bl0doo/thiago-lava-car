import { Delete } from 'lucide-react';
import { Button } from './Button';

export const PIN_TAMANHO = 4;

type Props = {
  valor: string;
  onChange: (valor: string) => void;
  onConfirmar: (valor: string) => void;
  desabilitado?: boolean;
};

/**
 * Teclado do PIN do lavador (tela 02 do protótipo): teclas de 72 px com rótulo de 28 px, para uso com luva.
 * O PIN tem 4 dígitos e é enviado sozinho ao completar, por isso a tecla OK do protótipo virou "apagar".
 */
export function TecladoPin({ valor, onChange, onConfirmar, desabilitado = false }: Props) {
  const digitar = (digito: string) => {
    if (desabilitado || valor.length >= PIN_TAMANHO) return;
    const novo = valor + digito;
    onChange(novo);
    if (novo.length === PIN_TAMANHO) onConfirmar(novo);
  };

  const tecla = 'w-full px-0';

  return (
    <div className="flex w-full flex-col gap-lg">
      <output aria-live="polite" aria-label={`${valor.length} de ${PIN_TAMANHO} dígitos`} className="flex h-11 items-center justify-center gap-md">
        {Array.from({ length: PIN_TAMANHO }, (_, i) => (
          <span
            key={i}
            aria-hidden="true"
            className={`size-5 rounded-full border-2 border-forte ${i < valor.length ? 'bg-inverso' : 'bg-superficie'}`}
          />
        ))}
      </output>
      <div className="grid grid-cols-3 gap-xs">
        {['1', '2', '3', '4', '5', '6', '7', '8', '9'].map((d) => (
          <Button key={d} variante="contorno" tamanho="lavador" className={tecla} disabled={desabilitado} onClick={() => digitar(d)}>
            {d}
          </Button>
        ))}
        <Button variante="contorno" tamanho="lavador" className={tecla} disabled={desabilitado || valor.length === 0} onClick={() => onChange('')} aria-label="Limpar o PIN">
          C
        </Button>
        <Button variante="contorno" tamanho="lavador" className={tecla} disabled={desabilitado} onClick={() => digitar('0')}>
          0
        </Button>
        <Button
          variante="contorno"
          tamanho="lavador"
          className={tecla}
          disabled={desabilitado || valor.length === 0}
          onClick={() => onChange(valor.slice(0, -1))}
          aria-label="Apagar último dígito"
        >
          <Delete aria-hidden="true" className="size-8" strokeWidth={2.5} />
        </Button>
      </div>
    </div>
  );
}
