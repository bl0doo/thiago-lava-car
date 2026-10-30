import { Ban, CircleAlert, CircleCheck, Clock, Droplet, Flag, TriangleAlert, type LucideIcon } from 'lucide-react';
import type { StatusOS } from '@/api/tipos';

export type StatusSelo = StatusOS | 'Atencao' | 'Erro';

const SELOS: Record<StatusSelo, { texto: string; cores: string; Icone: LucideIcon }> = {
  Aguardando: { texto: 'Aguardando', cores: 'bg-aguardando-bg text-aguardando', Icone: Clock },
  EmAndamento: { texto: 'Em andamento', cores: 'bg-andamento-bg text-andamento', Icone: Droplet },
  Concluido: { texto: 'Concluído', cores: 'bg-concluido-bg text-concluido', Icone: Flag },
  Entregue: { texto: 'Entregue', cores: 'bg-concluido-bg text-concluido', Icone: CircleCheck },
  Cancelada: { texto: 'Cancelada', cores: 'bg-rebaixado text-secundario', Icone: Ban },
  Atencao: { texto: 'Atenção', cores: 'bg-aviso-bg text-aviso', Icone: TriangleAlert },
  Erro: { texto: 'Erro', cores: 'bg-erro-bg text-erro', Icone: CircleAlert },
};

/** Selo de status: cor, ícone e texto juntos (Aguardando = relógio, Em andamento = gota, Concluído = bandeira). */
export function SeloStatus({ status, texto }: { status: StatusSelo; texto?: string }) {
  const { texto: padrao, cores, Icone } = SELOS[status];
  return (
    <span className={`inline-flex shrink-0 items-center gap-2xs rounded-sm px-xs py-[5px] texto-rotulo-p ${cores}`}>
      <Icone aria-hidden="true" className="size-4" strokeWidth={2.5} />
      {texto ?? padrao}
    </span>
  );
}
