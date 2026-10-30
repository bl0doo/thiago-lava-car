import type { components } from './schema';

export type Schemas = components['schemas'];

export type Perfil = 'Dono' | 'Lavador' | 'Cliente';

export type Usuario = Omit<Schemas['UsuarioResponse'], 'perfil'> & { perfil: Perfil };
export type AuthResponse = Omit<Schemas['AuthResponse'], 'usuario'> & { usuario: Usuario };
export type LavadorResumo = Schemas['LavadorResumoResponse'];
export type Cartao = Omit<Schemas['CartaoResponse'], 'status'> & { status: StatusOS };
export type Quadro = Omit<Schemas['QuadroResponse'], 'aguardando' | 'emAndamento' | 'concluido'> & {
  aguardando: Cartao[];
  emAndamento: Cartao[];
  concluido: Cartao[];
};
export type ProblemDetails = Schemas['ProblemDetails'] & { errors?: Record<string, string[]>; traceId?: string };

export type StatusOS = 'Aguardando' | 'EmAndamento' | 'Concluido' | 'Entregue' | 'Cancelada';
