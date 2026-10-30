import { z } from 'zod';

/** Espelha o LoginRequestValidator da API (as mensagens também). */
export const loginSchema = z.object({
  email: z.string().min(1, 'Informe o e-mail.').email('E-mail inválido.').max(254, 'E-mail muito longo.'),
  senha: z.string().min(1, 'Informe a senha.'),
});

export type LoginForm = z.infer<typeof loginSchema>;

/** Espelha o LoginPinRequestValidator: PIN de 4 dígitos. */
export const pinSchema = z.string().regex(/^\d{4}$/, 'O PIN deve ter 4 dígitos.');
