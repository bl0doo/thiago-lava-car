import { z } from 'zod';
import { FORMAS_PAGAMENTO, textoLivre, valorSchema } from './comuns';

export const pagamentoSchema = z.object({
  formaPagamento: z.enum(FORMAS_PAGAMENTO, { error: 'Escolha a forma de pagamento.' }),
  valor: valorSchema('valor recebido'),
});
export type PagamentoEntrada = z.input<typeof pagamentoSchema>;
export type PagamentoSaida = z.output<typeof pagamentoSchema>;

export const estornoSchema = z.object({ motivo: textoLivre(200) });
