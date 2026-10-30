import { z } from 'zod';
import { nomeSchema } from './comuns';

const apelido = z.string().trim().min(1, 'Informe o apelido.').max(30, 'O apelido deve ter até 30 caracteres.');

export const pinSchema = z.string().regex(/^\d{4}$/, 'O PIN deve ter 4 dígitos.');

export const novoLavadorSchema = z.object({ nome: nomeSchema, apelido, pin: pinSchema });
export type NovoLavadorEntrada = z.input<typeof novoLavadorSchema>;

export const editarMembroSchema = z.object({ nome: nomeSchema, apelido });
export type EditarMembroEntrada = z.input<typeof editarMembroSchema>;
