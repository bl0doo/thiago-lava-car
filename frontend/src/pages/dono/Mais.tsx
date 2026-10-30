import { ChartColumn, ChevronRight, LogOut, Plus, Receipt, Settings, Tags, Users, UsersRound, type LucideIcon } from 'lucide-react';
import { Link } from 'react-router-dom';
import { useAuth } from '@/auth/AuthContext';
import { BarraTitulo } from '@/components/layout/BarraTitulo';
import { Button } from '@/components/ui/Button';

const ITENS: { para: string; titulo: string; detalhe: string; Icone: LucideIcon }[] = [
  { para: '/dono/entrada', titulo: 'Nova entrada', detalhe: 'Registrar um carro que chegou', Icone: Plus },
  { para: '/dono/clientes', titulo: 'Clientes', detalhe: 'Busca por nome, telefone ou placa', Icone: Users },
  { para: '/dono/servicos', titulo: 'Serviços e preços', detalhe: 'Preço por tipo de veículo e tempo', Icone: Tags },
  { para: '/dono/despesas', titulo: 'Despesas e retiradas', detalhe: 'Contas da empresa e o que você tirou', Icone: Receipt },
  { para: '/dono/equipe', titulo: 'Equipe e diárias', detalhe: 'Lavadores, PIN e diárias do dia', Icone: UsersRound },
  { para: '/dono/relatorios', titulo: 'Relatórios', detalhe: 'Lucro por serviço e ponto de equilíbrio', Icone: ChartColumn },
  { para: '/dono/configuracoes', titulo: 'Configurações', detalhe: 'Horários, taxas e mensagens', Icone: Settings },
];

export function Mais() {
  const { usuario, sair } = useAuth();
  return (
    <section className="flex flex-col">
      <BarraTitulo sobretitulo={usuario?.nome ?? 'Gestão'} titulo="Mais" />
      <div className="flex flex-col gap-lg px-md">
        <ul className="flex flex-col divide-y divide-padrao rounded-lg border-2 border-forte bg-superficie">
          {ITENS.map(({ para, titulo, detalhe, Icone }) => (
            <li key={para}>
              <Link to={para} className="flex min-h-[66px] items-center gap-sm px-sm py-xs hover:bg-rebaixado">
                <span className="flex size-10 shrink-0 items-center justify-center rounded-md bg-rebaixado">
                  <Icone aria-hidden="true" className="size-6" strokeWidth={2} />
                </span>
                <span className="flex min-w-0 flex-1 flex-col">
                  <span className="texto-rotulo-g">{titulo}</span>
                  <span className="texto-corpo-p text-secundario">{detalhe}</span>
                </span>
                <ChevronRight aria-hidden="true" className="size-6 shrink-0 text-secundario" strokeWidth={2} />
              </Link>
            </li>
          ))}
        </ul>
        <Button variante="contorno" className="w-full xl:hidden" onClick={() => void sair()}>
          <LogOut aria-hidden="true" className="size-5" strokeWidth={2} />
          Sair
        </Button>
      </div>
    </section>
  );
}
