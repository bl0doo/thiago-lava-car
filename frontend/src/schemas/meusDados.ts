import { z } from 'zod';
import { canalSchema, emailOpcionalSchema, nomeSchema, telefoneSchema } from './comuns';

/** PUT /clientes/me: nome, telefone, e-mail opcional e canal. O consentimento LGPD não se edita aqui. */
export const meusDadosSchema = z.object({ nome: nomeSchema, telefone: telefoneSchema, email: emailOpcionalSchema, canalPreferido: canalSchema });

export type MeusDadosEntrada = z.input<typeof meusDadosSchema>;
export type MeusDadosSaida = z.output<typeof meusDadosSchema>;

/** POST /veiculos feito pelo cliente: o clienteId vem do token. */
export const meuVeiculoSchema = z.object({
  placa: z.string().transform((v) => v.replace(/[^a-zA-Z0-9]/g, '').toUpperCase()).pipe(z.string().regex(/^([A-Z]{3}\d{4}|[A-Z]{3}\d[A-Z]\d{2})$/, 'Placa inválida. Use o formato ABC1234 ou ABC1D23.')),
  modeloDescricao: z.string().trim().min(1, 'Informe o modelo do carro.').max(100, 'O modelo pode ter até 100 caracteres.'),
  tipoVeiculo: z.enum(['Moto', 'Carro', 'Caminhonete'], { error: 'Escolha o tipo do veículo.' }),
  cor: z.string().trim().max(30, 'A cor pode ter até 30 caracteres.'),
});
export type MeuVeiculoEntrada = z.input<typeof meuVeiculoSchema>;
export type MeuVeiculoSaida = z.output<typeof meuVeiculoSchema>;
