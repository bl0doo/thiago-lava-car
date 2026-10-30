import { CircleAlert, CircleCheck, Info, TriangleAlert, type LucideIcon } from 'lucide-react';
import type { ReactNode } from 'react';

/** Os quatro tipos do componente Aviso do Design System. */
export type TipoAviso = 'erro' | 'atencao' | 'sucesso' | 'informacao';

const TIPOS: Record<TipoAviso, { cores: string; Icone: LucideIcon }> = {
  erro: { cores: 'bg-erro-bg text-erro', Icone: CircleAlert },
  atencao: { cores: 'bg-aviso-bg text-aviso', Icone: TriangleAlert },
  sucesso: { cores: 'bg-concluido-bg text-concluido', Icone: CircleCheck },
  informacao: { cores: 'bg-andamento-bg text-andamento', Icone: Info },
};

type Props = { tipo?: TipoAviso; children: ReactNode; className?: string };

/**
 * Aviso com ícone e texto (o estado nunca é só cor). Erro e atenção usam role="alert", que o leitor de tela
 * anuncia na hora; sucesso e informação usam role="status", que espera a fala atual terminar.
 */
export function Alerta({ tipo = 'erro', children, className = '' }: Props) {
  const { cores, Icone } = TIPOS[tipo];
  const urgente = tipo === 'erro' || tipo === 'atencao';
  return (
    <div role={urgente ? 'alert' : 'status'} className={`flex items-start gap-sm rounded-md border-2 border-current px-md py-sm texto-rotulo-m ${cores} ${className}`}>
      <Icone aria-hidden="true" className="size-6 shrink-0" strokeWidth={2} />
      <div className="min-w-0 flex-1 self-center">{children}</div>
    </div>
  );
}
