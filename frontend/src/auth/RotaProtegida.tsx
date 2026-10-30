import { Navigate, Outlet, useLocation } from 'react-router-dom';
import type { Perfil } from '@/api/tipos';
import { Carregando } from '@/components/ui/Carregando';
import { useAuth } from './AuthContext';

/** Libera a rota só para os perfis informados. A API continua sendo a barreira real; isto evita telas que dariam 403. */
export function RotaProtegida({ perfis }: { perfis: Perfil[] }) {
  const { status, usuario } = useAuth();
  const local = useLocation();

  if (status === 'carregando') return <Carregando tela />;
  if (!usuario) {
    const destino = perfis.length === 1 && perfis[0] === 'Lavador' ? '/login/lavador' : '/login';
    return <Navigate to={destino} replace state={{ de: local.pathname }} />;
  }
  if (!perfis.includes(usuario.perfil)) return <Navigate to="/sem-permissao" replace />;
  return <Outlet />;
}
