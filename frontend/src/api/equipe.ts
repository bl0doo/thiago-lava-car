import { api } from './client';

export type Diaria = { id: number; lavadorId: number; data: string; valor: number; paga: boolean };
export type Diarias = { itens: Diaria[]; total: number };
export type DiariasDoDia = { criadas: Diaria[]; jaLancadas: number[] };

/** GET /diarias?mes=AAAA-MM */
export async function listarDiarias(mes: string): Promise<Diarias> {
  return (await api.get<Diarias>('/diarias', { params: { mes } })).data;
}

/** POST /diarias/lancar-dia?data=: sem `valor`, vale a diária padrão (R$ 100 na semana, R$ 120 aos sábados); quem já tem diária volta em `jaLancadas`. */
export async function lancarDiariasDoDia(data: string, lavadorIds: number[], valor: number | null): Promise<DiariasDoDia> {
  return (await api.post<DiariasDoDia>('/diarias/lancar-dia', { lavadorIds, valor }, { params: { data } })).data;
}

/** PUT /diarias/{id}: depois de paga, o valor não muda mais (409). */
export async function editarDiaria(id: number, valor: number, paga: boolean): Promise<Diaria> {
  return (await api.put<Diaria>(`/diarias/${id}`, { valor, paga })).data;
}

export async function excluirDiaria(id: number): Promise<void> {
  await api.delete(`/diarias/${id}`);
}

/** PUT /usuarios/{id}/pin: PIN de 4 dígitos; zera o bloqueio e encerra as sessões abertas do lavador. */
export async function redefinirPin(id: number, pin: string): Promise<void> {
  await api.put(`/usuarios/${id}/pin`, { pin });
}

/** PATCH /usuarios/{id}/ativo: desativar encerra as sessões do usuário. */
export async function alterarAtivo(id: number, ativo: boolean): Promise<void> {
  await api.patch(`/usuarios/${id}/ativo`, { ativo });
}

export type Membro = { id: number; nome: string; apelido: string | null; perfil: string; ativo: boolean };
export type NovoLavador = { nome: string; apelido: string; pin: string };

/** GET /usuarios?perfil=Lavador: inclui os desativados, ordenado por nome. */
export async function listarEquipe(): Promise<Membro[]> {
  return (await api.get<Membro[]>('/usuarios', { params: { perfil: 'Lavador' } })).data;
}

/** POST /usuarios: PIN de 4 dígitos e não óbvio (1234, 0000); apelido até 30 caracteres e único entre lavadores (409). */
export async function criarLavador(dados: NovoLavador): Promise<Membro> {
  return (await api.post<Membro>('/usuarios', dados)).data;
}

/** PUT /usuarios/{id}: nome e apelido. */
export async function editarMembro(id: number, nome: string, apelido: string | null): Promise<Membro> {
  return (await api.put<Membro>(`/usuarios/${id}`, { nome, apelido })).data;
}
