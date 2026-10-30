import { formatarCronometro, formatarHora, formatarMinutos, formatarMoeda } from './format';

describe('formatarMoeda', () => {
  it('usa reais com vírgula e duas casas', () => {
    expect(formatarMoeda(96.51).replace(/\s/g, ' ')).toBe('R$ 96,51');
    expect(formatarMoeda(1234.5).replace(/\s/g, ' ')).toBe('R$ 1.234,50');
  });
});

describe('formatarHora', () => {
  it('converte de UTC para o horário de Brasília', () => {
    expect(formatarHora('2026-10-20T16:05:00Z')).toBe('13:05');
    expect(formatarHora('2026-10-21T01:30:00Z')).toBe('22:30');
  });
});

describe('formatarCronometro', () => {
  it.each([
    [0, '0:00'],
    [65_000, '1:05'],
    [3_599_000, '59:59'],
    [3_723_000, '1:02:03'],
    [-5_000, '0:00'],
  ])('%i ms vira %s', (ms, esperado) => {
    expect(formatarCronometro(ms)).toBe(esperado);
  });
});

describe('formatarMinutos', () => {
  it.each([
    [45, '45 min'],
    [60, '1 h'],
    [102, '1 h 42 min'],
  ])('%i min vira %s', (min, esperado) => {
    expect(formatarMinutos(min)).toBe(esperado);
  });
});
