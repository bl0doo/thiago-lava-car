import { z } from 'zod';
import { CANAIS, nomeSchema, telefoneSchema } from './comuns';

/** Espelha o CadastroClienteRequestValidator da API (mesmas mensagens). */
export const cadastroSchema = z
  .object({
    nome: nomeSchema,
    telefone: telefoneSchema,
    email: z.string().trim().min(1, 'Informe o e-mail.').email('E-mail inválido.').max(254, 'E-mail muito longo.'),
    senha: z
      .string()
      .min(1, 'Informe a senha.')
      .min(8, 'A senha deve ter pelo menos 8 caracteres.')
      .max(128, 'A senha deve ter até 128 caracteres.')
      .refine((s) => /\p{L}/u.test(s) && /\d/.test(s), 'A senha deve ter letras e números.'),
    confirmacao: z.string().min(1, 'Repita a senha.'),
    canalPreferido: z.enum(CANAIS),
    consentimentoLgpd: z.boolean().refine((v) => v, 'Para criar a conta, aceite o uso dos seus dados (LGPD).'),
  })
  .refine((v) => v.senha === v.confirmacao, { path: ['confirmacao'], message: 'As senhas não são iguais.' });

/** O que o formulário digita (telefone ainda com máscara). */
export type CadastroEntrada = z.input<typeof cadastroSchema>;
export type CadastroSaida = z.output<typeof cadastroSchema>;
