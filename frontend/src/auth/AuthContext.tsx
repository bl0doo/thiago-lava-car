import { createContext, useCallback, useContext, useEffect, useMemo, useState, useSyncExternalStore, type ReactNode } from 'react';
import * as authApi from '@/api/auth';
import { renovarSessao } from '@/api/client';
import type { Perfil, Usuario } from '@/api/tipos';
import { sessao } from './sessao';

type Status = 'carregando' | 'anonimo' | 'autenticado';

type AuthContextoValor = {
  status: Status;
  usuario: Usuario | null;
  entrar: (email: string, senha: string) => Promise<Usuario>;
  entrarComPin: (usuarioId: number, pin: string) => Promise<Usuario>;
  sair: () => Promise<void>;
};

const AuthContexto = createContext<AuthContextoValor | null>(null);

export function AuthProvider({ children }: { children: ReactNode }) {
  const { usuario } = useSyncExternalStore(sessao.assinar, sessao.obter);
  const [restaurando, setRestaurando] = useState(true);

  // Ao abrir ou recarregar a página, tenta recuperar a sessão pelo cookie de renovação.
  useEffect(() => {
    let ativo = true;
    renovarSessao()
      .catch(() => undefined)
      .finally(() => {
        if (ativo) setRestaurando(false);
      });
    return () => {
      ativo = false;
    };
  }, []);

  const entrar = useCallback((email: string, senha: string) => authApi.entrar(email, senha), []);
  const entrarComPin = useCallback((id: number, pin: string) => authApi.entrarComPin(id, pin), []);
  const sair = useCallback(() => authApi.sair(), []);

  const valor = useMemo<AuthContextoValor>(
    () => ({
      status: restaurando && !usuario ? 'carregando' : usuario ? 'autenticado' : 'anonimo',
      usuario,
      entrar,
      entrarComPin,
      sair,
    }),
    [restaurando, usuario, entrar, entrarComPin, sair],
  );

  return <AuthContexto value={valor}>{children}</AuthContexto>;
}

export function useAuth(): AuthContextoValor {
  const contexto = useContext(AuthContexto);
  if (!contexto) throw new Error('useAuth precisa estar dentro de <AuthProvider>.');
  return contexto;
}

/** Rota inicial de cada perfil, logo após o login (Entrega 2, seção 5). */
export function rotaInicial(perfil: Perfil): string {
  switch (perfil) {
    case 'Dono':
      return '/dono';
    case 'Lavador':
      return '/lavador';
    case 'Cliente':
      return '/cliente/agendar';
  }
}
