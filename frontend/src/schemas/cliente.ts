import { z } from 'zod';
import { canalSchema, emailOpcionalSchema, nomeSchema, telefoneSchema } from './comuns';

/** Espelha o validator de POST /clientes: telefone com DDD e LGPD obrigatório. */
export const clienteSchema = z.object({
  nome: nomeSchema,
  telefone: telefoneSchema,
  email: emailOpcionalSchema,
  canalPreferido: canalSchema,
  consentimentoLgpd: z.boolean().refine((v) => v, 'Sem o consentimento LGPD o cadastro não é gravado.'),
});

export type ClienteFormEntrada = z.input<typeof clienteSchema>;
export type ClienteFormSaida = z.output<typeof clienteSchema>;
