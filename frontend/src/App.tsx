import { createBrowserRouter, RouterProvider } from 'react-router-dom';
import { AuthProvider } from '@/auth/AuthContext';
import { ConsultasDaSessao } from '@/auth/ConsultasDaSessao';
import { rotas } from '@/routes';

const router = createBrowserRouter(rotas);

export function App() {
  return (
    <AuthProvider>
      <ConsultasDaSessao>
        <RouterProvider router={router} />
      </ConsultasDaSessao>
    </AuthProvider>
  );
}
