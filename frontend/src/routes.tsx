import { Navigate, type RouteObject } from 'react-router-dom';
import { rotaInicial, useAuth } from '@/auth/AuthContext';
import { RotaProtegida } from '@/auth/RotaProtegida';
import { Carregando } from '@/components/ui/Carregando';
import { ShellCliente, ShellDono, ShellLavador } from '@/components/layout/Shells';
import { ClienteForm } from '@/pages/dono/ClienteForm';
import { Agenda } from '@/pages/dono/Agenda';
import { Caixa } from '@/pages/dono/Caixa';
import { Configuracoes } from '@/pages/dono/Configuracoes';
import { Clientes } from '@/pages/dono/Clientes';
import { DetalheOS } from '@/pages/dono/DetalheOS';
import { Despesas } from '@/pages/dono/Despesas';
import { Equipe } from '@/pages/dono/Equipe';
import { Mais } from '@/pages/dono/Mais';
import { ServicoForm } from '@/pages/dono/ServicoForm';
import { Servicos } from '@/pages/dono/Servicos';
import { NovaEntrada } from '@/pages/dono/NovaEntrada';
import { Agendar } from '@/pages/cliente/Agendar';
import { MeusAgendamentos } from '@/pages/cliente/MeusAgendamentos';
import { MeusDados } from '@/pages/cliente/MeusDados';
import { MeusVeiculos } from '@/pages/cliente/MeusVeiculos';
import { Painel } from '@/pages/dono/Painel';
import { Relatorios } from '@/pages/dono/Relatorios';
import { NaoEncontrada, SemPermissao } from '@/pages/Mensagens';
import { FichaDoCarro } from '@/pages/FichaDoCarro';
import { Cadastro } from '@/pages/publico/Cadastro';
import { PertencesEncontrados } from '@/pages/lavador/PertencesEncontrados';
import { Login } from '@/pages/publico/Login';
import { LoginLavador } from '@/pages/publico/LoginLavador';

function Inicio() {
  const { status, usuario } = useAuth();
  if (status === 'carregando') return <Carregando tela />;
  return <Navigate to={usuario ? rotaInicial(usuario.perfil) : '/login'} replace />;
}

/** Mapa de rotas da Entrega 2, seção 5. */
export const rotas: RouteObject[] = [
  { path: '/', element: <Inicio /> },
  { path: '/login', element: <Login /> },
  { path: '/login/lavador', element: <LoginLavador /> },
  { path: '/cadastro', element: <Cadastro /> },
  { path: '/sem-permissao', element: <SemPermissao /> },
  {
    element: <RotaProtegida perfis={['Dono']} />,
    children: [
      {
        path: '/dono',
        element: <ShellDono />,
        children: [
          { index: true, element: <Painel /> },
          { path: 'pit-stop', lazy: async () => ({ Component: (await import('@/pages/dono/PitStop')).PitStop }) },
          { path: 'entrada', element: <NovaEntrada /> },
          { path: 'os/:id', element: <DetalheOS /> },
          { path: 'agenda', element: <Agenda /> },
          { path: 'clientes', element: <Clientes /> },
          { path: 'clientes/novo', element: <ClienteForm /> },
          { path: 'clientes/:id', element: <ClienteForm /> },
          { path: 'veiculos/:id', element: <FichaDoCarro /> },
          { path: 'caixa', element: <Caixa /> },
          { path: 'despesas', element: <Despesas /> },
          { path: 'equipe', element: <Equipe /> },
          { path: 'servicos', element: <Servicos /> },
          { path: 'servicos/novo', element: <ServicoForm /> },
          { path: 'servicos/:id', element: <ServicoForm /> },
          { path: 'relatorios', element: <Relatorios /> },
          { path: 'configuracoes', element: <Configuracoes /> },
          { path: 'mais', element: <Mais /> },
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
          { path: 'os/:id', element: <PertencesEncontrados /> },
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
          { path: 'agendar', element: <Agendar /> },
          { path: 'agendamentos', element: <MeusAgendamentos /> },
          { path: 'veiculos', element: <MeusVeiculos /> },
          { path: 'veiculos/:id', element: <FichaDoCarro /> },
          { path: 'perfil', element: <MeusDados /> },
        ],
      },
    ],
  },
  { path: '*', element: <NaoEncontrada /> },
];
