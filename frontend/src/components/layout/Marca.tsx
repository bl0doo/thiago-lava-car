import { Droplet } from 'lucide-react';

/** Logotipo do protótipo: gota em quadro amarelo-bandeirada, com THIAGO / LAVA CAR em Barlow Condensed. */
export function Marca({ compacta = false }: { compacta?: boolean }) {
  return (
    <div className="flex items-center gap-sm text-texto-inverso">
      <span className={`flex shrink-0 items-center justify-center rounded-md bg-acao-primaria text-sobre-destaque ${compacta ? 'size-10' : 'size-14'}`}>
        <Droplet aria-hidden="true" className={compacta ? 'size-6' : 'size-8'} strokeWidth={2} />
      </span>
      <span className="flex flex-col">
        <span className={`font-titulo font-semibold ${compacta ? 'text-base leading-4' : 'text-2xl leading-7'}`}>THIAGO</span>
        <span className={`font-titulo font-bold ${compacta ? 'text-2xl leading-6' : 'text-[40px] leading-9'}`}>LAVA CAR</span>
      </span>
    </div>
  );
}
