import { z } from 'zod';
import { nomeSchema, pinSchema } from './comuns';

const apelido = z.string().trim().min(1, 'Informe o apelido.').max(30, 'O apelido deve ter até 30 caracteres.');

export const novoLavadorSchema = z.object({ nome: nomeSchema, apelido, pin: pinSchema });

export const editarMembroSchema = z.object({ nome: nomeSchema, apelido });
