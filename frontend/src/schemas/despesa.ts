import { z } from 'zod';
import { CATEGORIAS_DESPESA } from '@/api/financeiro';
import { textoLivre, valorSchema } from './comuns';

const dataSchema = z.string().min(1, 'Informe a data.');

export const ROTULO_CATEGORIA: Record<(typeof CATEGORIAS_DESPESA)[number], string> = {
  Insumos: 'Insumos',
  Aluguel: 'Aluguel',
  Agua: 'Água',
  Energia: 'Energia',
  Manutencao: 'Manutenção',
  Outros: 'Outros',
};

export const despesaSchema = z.object({
  descricao: z.string().trim().min(1, 'Informe a descrição.').max(200, 'A descrição deve ter até 200 caracteres.'),
  categoria: z.enum(CATEGORIAS_DESPESA, { error: 'Escolha a categoria.' }),
  valor: valorSchema('valor'),
  data: dataSchema,
});
export type DespesaFormEntrada = z.input<typeof despesaSchema>;
export type DespesaFormSaida = z.output<typeof despesaSchema>;

export const retiradaSchema = z.object({
  valor: valorSchema('valor'),
  data: dataSchema,
  observacao: textoLivre(200),
});
export type RetiradaFormEntrada = z.input<typeof retiradaSchema>;
export type RetiradaFormSaida = z.output<typeof retiradaSchema>;
