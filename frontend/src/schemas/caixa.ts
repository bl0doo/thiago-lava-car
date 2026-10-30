import { z } from 'zod';
import { FORMAS_PAGAMENTO } from './comuns';
import { lerValor } from '@/lib/format';

const conferido = z
  .string()
  .refine((v) => !Number.isNaN(lerValor(v)), 'Informe o valor conferido em reais, como 12,50.');

/** Uma linha por forma de pagamento; a justificativa só é cobrada quando algum valor difere do calculado. */
export const fechamentoSchema = z.object({
  conferido: z.object(Object.fromEntries(FORMAS_PAGAMENTO.map((f) => [f, conferido])) as Record<(typeof FORMAS_PAGAMENTO)[number], typeof conferido>),
  justificativa: z.string().trim().max(500, 'A justificativa deve ter até 500 caracteres.'),
});
export type FechamentoEntrada = z.input<typeof fechamentoSchema>;

export const reaberturaSchema = z.object({
  justificativa: z.string().trim().min(10, 'A justificativa deve ter de 10 a 500 caracteres.').max(500, 'A justificativa deve ter de 10 a 500 caracteres.'),
});
