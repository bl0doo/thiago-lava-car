import { useQueryClient } from '@tanstack/react-query';
import { createContext, useContext, useEffect, useMemo, useState, useSyncExternalStore, type ReactNode } from 'react';
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
  const queryClient = useQueryClient();
  const { usuario } = useSyncExternalStore(sessao.assinar, sessao.obter);
  const [restaurando, setRestaurando] = useState(true);

  useEffect(() => {
    let usuarioId = sessao.obter().usuario?.id;
    return sessao.assinar(() => {
      const proximoId = sessao.obter().usuario?.id;
      if (usuarioId !== proximoId) {
        usuarioId = proximoId;
        queryClient.clear();
      }
    });
  }, [queryClient]);

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

  const valor = useMemo<AuthContextoValor>(
    () => ({
      status: restaurando && !usuario ? 'carregando' : usuario ? 'autenticado' : 'anonimo',
      usuario,
      entrar: authApi.entrar,
      entrarComPin: authApi.entrarComPin,
      sair: authApi.sair,
    }),
    [restaurando, usuario],
  );

  return <AuthContexto value={valor}>{children}</AuthContexto>;
}

export function useAuth(): AuthContextoValor {
  const contexto = useContext(AuthContexto);
  if (!contexto) throw new Error('useAuth precisa estar dentro de <AuthProvider>.');
  return contexto;
}

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
