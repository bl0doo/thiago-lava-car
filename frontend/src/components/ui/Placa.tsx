/** Placa no padrão Mercosul: faixa azul com "BRASIL" e os caracteres em IBM Plex Mono (não confunde 0/O e 1/I). */
export function Placa({ placa, className = '' }: { placa: string; className?: string }) {
  return (
    <span className={`inline-flex w-[126px] shrink-0 flex-col overflow-hidden rounded-sm border-2 border-forte bg-branco ${className}`}>
      <span aria-hidden="true" className="bg-[var(--pista-700)] text-center text-[8px] font-semibold leading-[12px] tracking-[0.2em] text-texto-inverso">
        BRASIL
      </span>
      <span className="py-[5px] text-center texto-mono-placa text-primario">{placa}</span>
    </span>
  );
}
