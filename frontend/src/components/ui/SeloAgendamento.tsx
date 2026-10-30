import type { StatusAgendamento } from '@/api/agendamentos';
import { SeloStatus, type StatusSelo } from './SeloStatus';

const MAPA: Record<StatusAgendamento, { status: StatusSelo; texto: string }> = {
  Agendado: { status: 'Aguardando', texto: 'Agendado' },
  Atendido: { status: 'Entregue', texto: 'Atendido' },
  Cancelado: { status: 'Cancelada', texto: 'Cancelado' },
  NaoCompareceu: { status: 'Atencao', texto: 'Não compareceu' },
};

/** Selo do agendamento: reaproveita as cores e ícones do SeloStatus, sempre com texto. */
export function SeloAgendamento({ status }: { status: StatusAgendamento }) {
  const { status: selo, texto } = MAPA[status];
  return <SeloStatus status={selo} texto={texto} />;
}
