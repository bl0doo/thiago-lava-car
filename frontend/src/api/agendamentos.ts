import { api } from './client';
import type { TipoVeiculo } from './veiculos';

export type StatusAgendamento = 'Agendado' | 'Atendido' | 'Cancelado' | 'NaoCompareceu';

export type Agendamento = {
  id: number;
  clienteId: number;
  veiculoId: number;
  placa: string;
  servicoId: number;
  servico: string;
  comCera: boolean;
  inicio: string;
  duracaoEstimadaMin: number;
  precoEstimado: number;
  status: StatusAgendamento;
  /** Só vem para o dono. */
  cliente?: string | null;
};

export type AgendaDoDia = { data: string; agendamentos: Agendamento[] };
export type Horarios = { preco: number; duracaoEstimadaMin: number; horarios: string[] };
export type NovoAgendamento = { veiculoId: number; servicoId: number; comCera: boolean; inicio: string };
export type AvariaCheckIn = { local: string; tipo: string; descricao: string };
export type CheckIn = {
  semAvarias: boolean;
  avarias: AvariaCheckIn[];
  pertencesEntrada: string | null;
  nomeQuemRetira: string | null;
  aceiteCliente: boolean;
};

/** GET /agendamentos?data= (dono): agenda do dia com todos os status. */
export async function obterAgenda(data: string): Promise<AgendaDoDia> {
  return (await api.get<AgendaDoDia>('/agendamentos', { params: { data } })).data;
}

/** GET /agendamentos/meus (cliente): próximos e passados, do mais recente para o mais antigo. */
export async function listarMeusAgendamentos(): Promise<Agendamento[]> {
  return (await api.get<Agendamento[]>('/agendamentos/meus')).data;
}

/** GET /agendamentos/horarios: horários livres do dia (hoje até 30 dias à frente) com preço e duração. */
export async function listarHorarios(data: string, servicoId: number, tipoVeiculo: TipoVeiculo, comCera: boolean): Promise<Horarios> {
  return (await api.get<Horarios>('/agendamentos/horarios', { params: { data, servicoId, tipoVeiculo, comCera } })).data;
}

/** POST /agendamentos: 409 se o horário acabou de ser ocupado ou o cliente já tem 2 futuros. */
export async function criarAgendamento(dados: NovoAgendamento): Promise<Agendamento> {
  return (await api.post<Agendamento>('/agendamentos', dados)).data;
}

/** PATCH /agendamentos/{id}/cancelar: só enquanto está Agendado. */
export async function cancelarAgendamento(id: number): Promise<Agendamento> {
  return (await api.patch<Agendamento>(`/agendamentos/${id}/cancelar`)).data;
}

/** PATCH /agendamentos/{id}/nao-compareceu (dono). */
export async function marcarNaoCompareceu(id: number): Promise<Agendamento> {
  return (await api.patch<Agendamento>(`/agendamentos/${id}/nao-compareceu`)).data;
}

/** POST /agendamentos/{id}/check-in (dono): vira uma OS em Aguardando com a vistoria. */
export async function fazerCheckIn(id: number, dados: CheckIn): Promise<{ id: number }> {
  return (await api.post<{ id: number }>(`/agendamentos/${id}/check-in`, dados)).data;
}
