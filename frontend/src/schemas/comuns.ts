import { z } from 'zod';
import { lerValor, normalizarPlaca } from '@/lib/format';

/** Espelham os validators da API (mesmas mensagens), para o erro aparecer antes de ir ao servidor. */
export const nomeSchema = z.string().trim().min(2, 'O nome deve ter ao menos 2 caracteres.').max(100, 'O nome pode ter até 100 caracteres.');

export const telefoneSchema = z
  .string()
  .transform((v) => v.replace(/\D/g, ''))
  .pipe(z.string().regex(/^\d{10,11}$/, 'Informe o telefone com DDD (10 ou 11 dígitos).'));

export const emailOpcionalSchema = z
  .string()
  .trim()
  .max(254, 'E-mail muito longo.')
  .refine((v) => v === '' || z.email().safeParse(v).success, 'E-mail inválido.');

const PLACA = /^([A-Z]{3}\d{4}|[A-Z]{3}\d[A-Z]\d{2})$/;
export const placaSchema = z
  .string()
  .transform(normalizarPlaca)
  .pipe(z.string().regex(PLACA, 'Placa inválida. Use o formato ABC1234 ou ABC1D23.'));

export const CANAIS = ['WhatsApp', 'Telefone', 'Email'] as const;
export const canalSchema = z.enum(CANAIS, { error: 'Escolha o canal.' });

export const TIPOS_VEICULO = ['Moto', 'Carro', 'Caminhonete'] as const;
export const tipoVeiculoSchema = z.enum(TIPOS_VEICULO, { error: 'Escolha o tipo do veículo.' });

export const pinSchema = z.string().regex(/^\d{4}$/, 'O PIN deve ter 4 dígitos.');

export const FORMAS_PAGAMENTO = ['Pix', 'Debito', 'Credito', 'Dinheiro'] as const;
export const ROTULO_FORMA: Record<(typeof FORMAS_PAGAMENTO)[number], string> = { Pix: 'Pix', Debito: 'Débito', Credito: 'Crédito', Dinheiro: 'Dinheiro' };

/** Campo de dinheiro digitado como texto ("12,50"); vira número depois de validado. */
export const valorSchema = (rotulo = 'valor') =>
  z
    .string()
    .refine((v) => !Number.isNaN(lerValor(v)), `Informe o ${rotulo} em reais, como 12,50.`)
    .refine((v) => Number.isNaN(lerValor(v)) || lerValor(v) > 0, 'O valor deve ser maior que zero.')
    .transform(lerValor);

export const textoLivre = (max = 500) => z.string().trim().max(max, `Use até ${max} caracteres.`);
