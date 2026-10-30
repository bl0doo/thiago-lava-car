import { CalendarDays, CarFront, Flag, LayoutDashboard, LogOut, Menu, User, Wallet, CalendarCheck, type LucideIcon } from 'lucide-react';
import { NavLink, Outlet } from 'react-router-dom';
import { useAuth } from '@/auth/AuthContext';
import { Marca } from './Marca';

type Item = { para: string; rotulo: string; Icone: LucideIcon; fim?: boolean };

function PularParaConteudo() {
  return (
    <a
      href="#conteudo"
      className="sr-only focus:not-sr-only focus:fixed focus:left-2 focus:top-2 focus:z-50 focus:rounded-md focus:border-2 focus:border-forte focus:bg-superficie focus:px-md focus:py-xs"
    >
      Pular para o conteúdo
    </a>
  );
}

/** Barra inferior do celular e do tablet em pé (componentes "Barra inferior (Dono)" e "(Cliente)"). */
function BarraInferior({ itens, rotulo }: { itens: Item[]; rotulo: string }) {
  return (
    <nav aria-label={rotulo} className="sticky bottom-0 z-10 border-t-2 border-forte bg-superficie pb-[env(safe-area-inset-bottom)] xl:hidden">
      <ul className="mx-auto flex max-w-3xl">
        {itens.map(({ para, rotulo: texto, Icone, fim }) => (
          <li key={para} className="flex-1">
            <NavLink to={para} end={fim} className="group flex min-h-[76px] flex-col items-center justify-center gap-2xs px-2xs texto-rotulo-p text-secundario aria-[current=page]:text-primario">
              <span className="flex h-8 w-14 items-center justify-center rounded-pill group-aria-[current=page]:border-2 group-aria-[current=page]:border-forte group-aria-[current=page]:bg-acao-primaria">
                <Icone aria-hidden="true" className="size-6" strokeWidth={2} />
              </span>
              {texto}
            </NavLink>
          </li>
        ))}
      </ul>
    </nav>
  );
}

/** Menu lateral do desktop, a partir de 1280 px (componentes "Menu lateral (Dono)" e "(Cliente)", 264 px). No tablet fica a barra inferior. */
function MenuLateral({ itens, rotulo }: { itens: Item[]; rotulo: string }) {
  const { usuario, sair } = useAuth();
  return (
    <aside className="sticky top-0 hidden h-dvh w-[264px] shrink-0 flex-col bg-inverso text-texto-inverso xl:flex">
      <div className="p-lg">
        <Marca compacta />
      </div>
      <div aria-hidden="true" className="faixa-quadriculada" />
      <nav aria-label={rotulo} className="flex-1 overflow-y-auto p-sm">
        <ul className="flex flex-col gap-2xs">
          {itens.map(({ para, rotulo: texto, Icone, fim }) => (
            <li key={para}>
              <NavLink
                to={para}
                end={fim}
                className="flex min-h-alvo items-center gap-sm rounded-md px-sm texto-rotulo-m hover:bg-[var(--asfalto-800)] aria-[current=page]:bg-acao-primaria aria-[current=page]:text-sobre-destaque"
              >
                <Icone aria-hidden="true" className="size-6" strokeWidth={2} />
                {texto}
              </NavLink>
            </li>
          ))}
        </ul>
      </nav>
      <div className="flex flex-col gap-xs border-t-2 border-[var(--asfalto-700)] p-md">
        <p className="texto-corpo-p text-[var(--asfalto-300)]">{usuario?.nome}</p>
        <button type="button" onClick={() => void sair()} className="flex min-h-alvo items-center gap-sm rounded-md px-sm texto-rotulo-m hover:bg-[var(--asfalto-800)]">
          <LogOut aria-hidden="true" className="size-6" strokeWidth={2} />
          Sair
        </button>
      </div>
    </aside>
  );
}

const ITENS_DONO: Item[] = [
  { para: '/dono', rotulo: 'Painel', Icone: LayoutDashboard, fim: true },
  { para: '/dono/pit-stop', rotulo: 'Pit Stop', Icone: Flag },
  { para: '/dono/agenda', rotulo: 'Agenda', Icone: CalendarDays },
  { para: '/dono/caixa', rotulo: 'Caixa', Icone: Wallet },
  { para: '/dono/mais', rotulo: 'Mais', Icone: Menu },
];

const ITENS_CLIENTE: Item[] = [
  { para: '/cliente/agendar', rotulo: 'Agendar', Icone: CalendarDays },
  { para: '/cliente/agendamentos', rotulo: 'Meus horários', Icone: CalendarCheck },
  { para: '/cliente/veiculos', rotulo: 'Veículos', Icone: CarFront },
  { para: '/cliente/perfil', rotulo: 'Perfil', Icone: User },
];

function Moldura({ itens, rotuloNav }: { itens?: Item[]; rotuloNav?: string }) {
  return (
    <div className="flex min-h-dvh bg-app">
      <PularParaConteudo />
      {itens ? <MenuLateral itens={itens} rotulo={rotuloNav ?? 'Navegação'} /> : null}
      <div className="flex min-w-0 flex-1 flex-col">
        <main id="conteudo" className="mx-auto w-full max-w-6xl flex-1 pb-lg">
          <Outlet />
        </main>
        {itens ? <BarraInferior itens={itens} rotulo={rotuloNav ?? 'Navegação'} /> : null}
      </div>
    </div>
  );
}

export const ShellDono = () => <Moldura itens={ITENS_DONO} rotuloNav="Navegação do dono" />;
export const ShellCliente = () => <Moldura itens={ITENS_CLIENTE} rotuloNav="Navegação do cliente" />;
export const ShellLavador = () => <Moldura />;
