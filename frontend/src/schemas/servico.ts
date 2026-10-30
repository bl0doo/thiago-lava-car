import { z } from 'zod';
import { lerValor } from '@/lib/format';
import { TIPOS_VEICULO } from './comuns';

const dinheiro = (rotulo: string, obrigatorio: boolean) =>
  z
    .string()
    .refine((v) => (v.trim() === '' ? !obrigatorio : lerValor(v) >= 0), `Informe ${rotulo} em reais, como 12,50.`)
    .refine((v) => v.trim() === '' || !(lerValor(v) > 99999), 'Valor alto demais.');

// preço em branco = o serviço não atende esse tipo de veículo
const precoSchema = z
  .object({
    tipoVeiculo: z.enum(TIPOS_VEICULO),
    preco: dinheiro('o preço', false),
    precoComCera: dinheiro('o preço com cera', false),
  })
  .superRefine((p, ctx) => {
    if (p.preco.trim() !== '' && !(lerValor(p.preco) > 0)) ctx.addIssue({ code: 'custom', path: ['preco'], message: 'O preço deve ser maior que zero.' });
    if (p.preco.trim() === '' && p.precoComCera.trim() !== '') ctx.addIssue({ code: 'custom', path: ['preco'], message: 'Informe o preço sem cera.' });
  });

/** Catálogo: um preço por tipo de veículo, cera opcional, tempo em minutos e custo de insumos. */
export const servicoSchema = z.object({
  nome: z.string().trim().min(2, 'O nome deve ter ao menos 2 caracteres.').max(100, 'O nome pode ter até 100 caracteres.'),
  categoria: z.string().trim().min(1, 'Informe a categoria.').max(50, 'A categoria pode ter até 50 caracteres.'),
  tempoEstimadoMin: z
    .string()
    .refine((v) => /^\d+$/.test(v.trim()) && Number(v) >= 5 && Number(v) <= 600, 'Informe o tempo em minutos, entre 5 e 600.'),
  custoInsumos: dinheiro('o custo de insumos', true),
  precos: z.array(precoSchema),
});

export type ServicoFormEntrada = z.input<typeof servicoSchema>;
export type ServicoFormSaida = z.output<typeof servicoSchema>;
