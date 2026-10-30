import { z } from 'zod';
import { placaSchema, textoLivre, tipoVeiculoSchema } from './comuns';

export { placaSchema };

export const veiculoNovoSchema = z.object({
  clienteId: z.number({ error: 'Escolha o dono do veículo.' }),
  modeloDescricao: textoLivre(100).min(1, 'Informe o modelo do veículo.'),
  tipoVeiculo: tipoVeiculoSchema,
  cor: textoLivre(30),
});

export const LOCAIS_AVARIA = ['Para-choque dianteiro', 'Para-choque traseiro', 'Porta', 'Capô', 'Teto', 'Vidro', 'Roda', 'Interior', 'Outro'] as const;
export const TIPOS_AVARIA = ['Risco', 'Amassado', 'Trinca', 'Mancha', 'Falta de peça', 'Outro'] as const;

export const avariaSchema = z.object({
  local: z.string().min(1, 'Escolha onde está a avaria.'),
  tipo: z.string().min(1, 'Escolha o tipo da avaria.'),
  descricao: textoLivre(500),
});

/** Mesmas regras de POST /ordens-servico: sem avarias exige lista vazia, com avarias exige ao menos uma, aceite obrigatório. */
export const vistoriaSchema = z
  .object({
    semAvarias: z.boolean(),
    // as linhas só são validadas quando há avarias a registrar (com "Sem avarias" marcado elas são descartadas)
    avarias: z.array(z.object({ local: z.string(), tipo: z.string(), descricao: z.string() })),
    pertencesEntrada: textoLivre(500),
    nomeQuemRetira: textoLivre(100),
    aceiteCliente: z.boolean().refine((v) => v, 'O cliente precisa concordar com a vistoria.'),
  })
  .superRefine((v, ctx) => {
    if (v.semAvarias) return;
    if (v.avarias.length === 0) {
      ctx.addIssue({ code: 'custom', path: ['avarias'], message: 'Anote ao menos uma avaria ou marque "Sem avarias".' });
    }
    v.avarias.forEach((a, i) => {
      const r = avariaSchema.safeParse(a);
      if (!r.success) for (const issue of r.error.issues) ctx.addIssue({ code: 'custom', path: ['avarias', i, ...issue.path], message: issue.message });
    });
  });

export type VistoriaEntrada = z.input<typeof vistoriaSchema>;
export type VistoriaSaida = z.output<typeof vistoriaSchema>;
