import { z } from 'zod';
import { canalSchema, emailOpcionalSchema, nomeSchema, placaSchema, telefoneSchema, tipoVeiculoSchema } from './comuns';

/** PUT /clientes/me: nome, telefone, e-mail opcional e canal. O consentimento LGPD não se edita aqui. */
export const meusDadosSchema = z.object({ nome: nomeSchema, telefone: telefoneSchema, email: emailOpcionalSchema, canalPreferido: canalSchema });

export type MeusDadosEntrada = z.input<typeof meusDadosSchema>;
export type MeusDadosSaida = z.output<typeof meusDadosSchema>;

/** POST /veiculos feito pelo cliente: o clienteId vem do token. */
export const meuVeiculoSchema = z.object({
  placa: placaSchema,
  modeloDescricao: z.string().trim().min(1, 'Informe o modelo do carro.').max(100, 'O modelo pode ter até 100 caracteres.'),
  tipoVeiculo: tipoVeiculoSchema,
  cor: z.string().trim().max(30, 'A cor pode ter até 30 caracteres.'),
});
export type MeuVeiculoEntrada = z.input<typeof meuVeiculoSchema>;
export type MeuVeiculoSaida = z.output<typeof meuVeiculoSchema>;
