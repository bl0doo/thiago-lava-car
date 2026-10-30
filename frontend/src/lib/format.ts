const moeda = new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' });

export function formatarMoeda(valor: number): string {
  return moeda.format(valor);
}

const hora = new Intl.DateTimeFormat('pt-BR', { hour: '2-digit', minute: '2-digit', timeZone: 'America/Sao_Paulo' });

/** O banco guarda UTC; a tela mostra no horário de Brasília. */
export function formatarHora(iso: string | number | Date): string {
  return hora.format(new Date(iso));
}

const dataLonga = new Intl.DateTimeFormat('pt-BR', {
  weekday: 'long',
  day: '2-digit',
  month: 'long',
  timeZone: 'America/Sao_Paulo',
});

export function formatarData(iso: string | number | Date): string {
  return dataLonga.format(new Date(iso));
}

/** 65 000 ms vira "1:05"; a partir de uma hora, "1:02:03". */
export function formatarCronometro(ms: number): string {
  const total = Math.max(0, Math.floor(ms / 1000));
  const h = Math.floor(total / 3600);
  const m = Math.floor((total % 3600) / 60);
  const s = total % 60;
  const ss = String(s).padStart(2, '0');
  return h > 0 ? `${h}:${String(m).padStart(2, '0')}:${ss}` : `${m}:${ss}`;
}

/** 102 vira "1 h 42 min"; 45 vira "45 min". */
export function formatarMinutos(minutos: number): string {
  const h = Math.floor(minutos / 60);
  const m = minutos % 60;
  if (h === 0) return `${m} min`;
  return m === 0 ? `${h} h` : `${h} h ${m} min`;
}

/** "abc-1d23" vira "ABC1D23", a forma que a API grava. */
export function normalizarPlaca(texto: string): string {
  return texto.replace(/[^a-zA-Z0-9]/g, '').toUpperCase();
}

/** "44999998888" vira "(44) 99999-8888"; números incompletos são devolvidos como estão. */
export function formatarTelefone(digitos: string): string {
  const d = digitos.replace(/\D/g, '');
  if (d.length === 11) return `(${d.slice(0, 2)}) ${d.slice(2, 7)}-${d.slice(7)}`;
  if (d.length === 10) return `(${d.slice(0, 2)}) ${d.slice(2, 6)}-${d.slice(6)}`;
  return digitos;
}

const dataCurta = new Intl.DateTimeFormat('pt-BR', { day: '2-digit', month: '2-digit', year: 'numeric', timeZone: 'America/Sao_Paulo' });

export function formatarDataCurta(iso: string | number | Date): string {
  return dataCurta.format(new Date(iso));
}

const formatoIso = new Intl.DateTimeFormat('en-CA', { year: 'numeric', month: '2-digit', day: '2-digit', timeZone: 'America/Sao_Paulo' });

/** Data de hoje em Brasília no formato 2026-10-20 (a mesma regra do data_caixa da API). */
export function hojeIso(agora: Date = new Date()): string {
  return formatoIso.format(agora);
}

/** Converte "2026-10-20" em data local sem o deslocamento de fuso que `new Date('2026-10-20')` causaria. */
export function formatarDataIso(dia: string): string {
  const [a, m, d] = dia.split('-');
  return `${d}/${m}/${a}`;
}

/** Aceita "1.234,50", "12,5" ou "12.5" e devolve o número, ou NaN. */
export function lerValor(texto: string): number {
  const t = texto.trim().replace(/^R\$\s*/, '');
  if (!t) return NaN;
  const normal = t.includes(',') ? t.replace(/\./g, '').replace(',', '.') : t;
  return /^\d+(\.\d+)?$/.test(normal) ? Number(normal) : NaN;
}
