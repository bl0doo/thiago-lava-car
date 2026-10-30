import type { components } from './schema';

type Schemas = components['schemas'];

export type Perfil = 'Dono' | 'Lavador' | 'Cliente';

export type Usuario = Omit<Schemas['UsuarioResponse'], 'perfil'> & { perfil: Perfil };
export type AuthResponse = Omit<Schemas['AuthResponse'], 'usuario'> & { usuario: Usuario };
export type LavadorResumo = Schemas['LavadorResumoResponse'];
export type Cartao = Schemas['CartaoResponse'];
export type Quadro = Schemas['QuadroResponse'];
export type ProblemDetails = Schemas['ProblemDetails'] & { errors?: Record<string, string[]>; traceId?: string };

export type StatusOS = 'Aguardando' | 'EmAndamento' | 'Concluido';
