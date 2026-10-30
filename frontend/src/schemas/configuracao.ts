import { z } from 'zod';
import { lerValor } from '@/lib/format';
import { valorSchema } from './comuns';

const hora = z.string().regex(/^([01]\d|2[0-3]):[0-5]\d$/, 'Use o horário no formato HH:mm.');

export const DIAS_SEMANA = ['Domingo', 'Segunda', 'Terça', 'Quarta', 'Quinta', 'Sexta', 'Sábado'] as const;

export const configuracaoSchema = z.object({
  nomeLavaJato: z.string().trim().min(1, 'Informe o nome do lava-jato.').max(100, 'O nome deve ter até 100 caracteres.'),
  telefone: z.string().trim().max(20, 'O telefone deve ter até 20 caracteres.'),
  abertura: hora,
  fechamento: hora,
  dias: z.array(z.string()).min(1, 'Informe ao menos um dia de funcionamento.'),
  diariaSemana: valorSchema('valor'),
  diariaSabado: valorSchema('valor'),
  intervaloAgendaMin: z
    .string()
    .regex(/^\d+$/, 'O intervalo da agenda deve ficar entre 5 e 240 minutos.')
    .transform(Number)
    .refine((n) => n >= 5 && n <= 240, 'O intervalo da agenda deve ficar entre 5 e 240 minutos.'),
});
export type ConfiguracaoEntrada = z.input<typeof configuracaoSchema>;
export type ConfiguracaoSaida = z.output<typeof configuracaoSchema>;

const numeroOuNaN = (v: string) => (v.trim() === '' ? 0 : lerValor(v));

export const taxaSchema = z.object({
  percentual: z
    .string()
    .refine((v) => !Number.isNaN(numeroOuNaN(v)), 'Informe o percentual, como 3,5.')
    .transform(numeroOuNaN)
    .refine((n) => n >= 0 && n <= 100, 'O percentual deve estar entre 0 e 100.'),
  valorFixo: z
    .string()
    .refine((v) => !Number.isNaN(numeroOuNaN(v)), 'Informe o valor fixo em reais, como 0,50.')
    .transform(numeroOuNaN),
});
export type TaxaEntrada = z.input<typeof taxaSchema>;

export const modeloSchema = z.object({
  texto: z.string().trim().min(1, 'Informe o texto da mensagem.').max(500, 'O texto deve ter até 500 caracteres.'),
});
