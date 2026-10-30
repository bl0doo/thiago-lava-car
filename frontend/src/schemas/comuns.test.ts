import { lerValor, normalizarPlaca, formatarTelefone, hojeIso } from '@/lib/format';
import { placaSchema, telefoneSchema, valorSchema, emailOpcionalSchema } from './comuns';

describe('validações comuns', () => {
  it('normaliza e valida placa antiga e Mercosul', () => {
    expect(normalizarPlaca('abc-1d23')).toBe('ABC1D23');
    expect(placaSchema.safeParse('abc-1d23').data).toBe('ABC1D23');
    expect(placaSchema.safeParse('ABC1234').success).toBe(true);
    expect(placaSchema.safeParse('AB12345').error?.issues[0]?.message).toMatch(/Placa inválida/);
  });
  it('telefone aceita máscara e exige DDD', () => {
    expect(telefoneSchema.safeParse('(44) 99999-8888').data).toBe('44999998888');
    expect(telefoneSchema.safeParse('99999').success).toBe(false);
    expect(formatarTelefone('44999998888')).toBe('(44) 99999-8888');
  });
  it('valor em reais', () => {
    expect(lerValor('1.234,50')).toBe(1234.5);
    expect(lerValor('12.5')).toBe(12.5);
    expect(lerValor('abc')).toBeNaN();
    expect(valorSchema().safeParse('0').success).toBe(false);
    expect(valorSchema().safeParse('12,50').data).toBe(12.5);
  });
  it('e-mail é opcional', () => {
    expect(emailOpcionalSchema.safeParse('').success).toBe(true);
    expect(emailOpcionalSchema.safeParse('x@').success).toBe(false);
  });
  it('data de hoje usa Brasília', () => {
    expect(hojeIso(new Date('2026-10-21T01:00:00Z'))).toBe('2026-10-20');
  });
});
