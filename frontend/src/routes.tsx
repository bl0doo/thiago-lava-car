import { Navigate, type RouteObject } from 'react-router-dom';
import { rotaInicial, useAuth } from '@/auth/AuthContext';
import { RotaProtegida } from '@/auth/RotaProtegida';
import { Carregando } from '@/components/ui/Carregando';
import { ShellCliente, ShellDono, ShellLavador } from '@/components/layout/Shells';
import { NaoEncontrada, SemPermissao } from '@/pages/Mensagens';

function Inicio() {
  const { status, usuario } = useAuth();
  if (status === 'carregando') return <Carregando tela />;
  return <Navigate to={usuario ? rotaInicial(usuario.perfil) : '/login'} replace />;
}

export const rotas: RouteObject[] = [
  { path: '/', element: <Inicio /> },
  { path: '/login', lazy: async () => ({ Component: (await import('@/pages/publico/Login')).Login }) },
  { path: '/login/lavador', lazy: async () => ({ Component: (await import('@/pages/publico/LoginLavador')).LoginLavador }) },
  { path: '/cadastro', lazy: async () => ({ Component: (await import('@/pages/publico/Cadastro')).Cadastro }) },
  { path: '/sem-permissao', element: <SemPermissao /> },
  {
    element: <RotaProtegida perfis={['Dono']} />,
    children: [
      {
        path: '/dono',
        element: <ShellDono />,
        children: [
          { index: true, lazy: async () => ({ Component: (await import('@/pages/dono/Painel')).Painel }) },
          { path: 'pit-stop', lazy: async () => ({ Component: (await import('@/pages/dono/PitStop')).PitStop }) },
          { path: 'entrada', lazy: async () => ({ Component: (await import('@/pages/dono/NovaEntrada')).NovaEntrada }) },
          { path: 'os/:id', lazy: async () => ({ Component: (await import('@/pages/dono/DetalheOS')).DetalheOS }) },
          { path: 'agenda', lazy: async () => ({ Component: (await import('@/pages/dono/Agenda')).Agenda }) },
          { path: 'clientes', lazy: async () => ({ Component: (await import('@/pages/dono/Clientes')).Clientes }) },
          { path: 'clientes/novo', lazy: async () => ({ Component: (await import('@/pages/dono/ClienteForm')).ClienteForm }) },
          { path: 'clientes/:id', lazy: async () => ({ Component: (await import('@/pages/dono/ClienteForm')).ClienteForm }) },
          { path: 'veiculos/:id', lazy: async () => ({ Component: (await import('@/pages/FichaDoCarro')).FichaDoCarro }) },
          { path: 'caixa', lazy: async () => ({ Component: (await import('@/pages/dono/Caixa')).Caixa }) },
          { path: 'despesas', lazy: async () => ({ Component: (await import('@/pages/dono/Despesas')).Despesas }) },
          { path: 'equipe', lazy: async () => ({ Component: (await import('@/pages/dono/Equipe')).Equipe }) },
          { path: 'servicos', lazy: async () => ({ Component: (await import('@/pages/dono/Servicos')).Servicos }) },
          { path: 'servicos/novo', lazy: async () => ({ Component: (await import('@/pages/dono/ServicoForm')).ServicoForm }) },
          { path: 'servicos/:id', lazy: async () => ({ Component: (await import('@/pages/dono/ServicoForm')).ServicoForm }) },
          { path: 'relatorios', lazy: async () => ({ Component: (await import('@/pages/dono/Relatorios')).Relatorios }) },
          { path: 'configuracoes', lazy: async () => ({ Component: (await import('@/pages/dono/Configuracoes')).Configuracoes }) },
          { path: 'mais', lazy: async () => ({ Component: (await import('@/pages/dono/Mais')).Mais }) },
        ],
      },
    ],
  },
  {
    element: <RotaProtegida perfis={['Lavador']} />,
    children: [
      {
        path: '/lavador',
        element: <ShellLavador />,
        children: [
          { index: true, lazy: async () => ({ Component: (await import('@/pages/lavador/MinhaFila')).MinhaFila }) },
          { path: 'os/:id', lazy: async () => ({ Component: (await import('@/pages/lavador/PertencesEncontrados')).PertencesEncontrados }) },
        ],
      },
    ],
  },
  {
    element: <RotaProtegida perfis={['Cliente']} />,
    children: [
      {
        path: '/cliente',
        element: <ShellCliente />,
        children: [
          { path: 'agendar', lazy: async () => ({ Component: (await import('@/pages/cliente/Agendar')).Agendar }) },
          { path: 'agendamentos', lazy: async () => ({ Component: (await import('@/pages/cliente/MeusAgendamentos')).MeusAgendamentos }) },
          { path: 'veiculos', lazy: async () => ({ Component: (await import('@/pages/cliente/MeusVeiculos')).MeusVeiculos }) },
          { path: 'veiculos/:id', lazy: async () => ({ Component: (await import('@/pages/FichaDoCarro')).FichaDoCarro }) },
          { path: 'perfil', lazy: async () => ({ Component: (await import('@/pages/cliente/MeusDados')).MeusDados }) },
        ],
      },
    ],
  },
  { path: '*', element: <NaoEncontrada /> },
];
