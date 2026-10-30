import type { Usuario } from '@/api/tipos';

/**
 * Sessão em memória: o access token nunca vai para localStorage (Entrega 2, seção 4).
 * Ao recarregar a página, a sessão volta pelo cookie HttpOnly de renovação (o do lavador vale até o fim do turno).
 */
type Estado = { token: string | null; usuario: Usuario | null };

let estado: Estado = { token: null, usuario: null };
const ouvintes = new Set<() => void>();

export const sessao = {
  obter: (): Estado => estado,
  token: (): string | null => estado.token,
  definir(token: string, usuario: Usuario) {
    estado = { token, usuario };
    ouvintes.forEach((o) => o());
  },
  limpar() {
    if (estado.token === null && estado.usuario === null) return;
    estado = { token: null, usuario: null };
    ouvintes.forEach((o) => o());
  },
  assinar(ouvinte: () => void) {
    ouvintes.add(ouvinte);
    return () => {
      ouvintes.delete(ouvinte);
    };
  },
};
