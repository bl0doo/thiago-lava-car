import { useQuery } from '@tanstack/react-query';
import { ChartNoAxesColumn } from 'lucide-react';
import { useState } from 'react';
import { faturamento, lucroPorServico, pontoEquilibrio, produtividade, type Periodo } from '@/api/relatorios';
import { BarraTitulo } from '@/components/layout/BarraTitulo';
import { Abas } from '@/components/ui/Abas';
import { Alerta } from '@/components/ui/Alerta';
import { Campo } from '@/components/ui/Campo';
import { Carregando } from '@/components/ui/Carregando';
import { ErroConsulta } from '@/components/ui/ErroConsulta';
import { EstadoVazio } from '@/components/ui/EstadoVazio';
import { formatarDataIso, formatarMinutos, formatarMoeda, hojeIso } from '@/lib/format';

type Aba = 'lucro' | 'faturamento' | 'equilibrio' | 'tempos';

const ABAS: { id: Aba; rotulo: string }[] = [
  { id: 'lucro', rotulo: 'Lucro' },
  { id: 'faturamento', rotulo: 'Faturamento' },
  { id: 'equilibrio', rotulo: 'Equilíbrio' },
  { id: 'tempos', rotulo: 'Tempos' },
];

const primeiroDoMes = (hoje: string) => `${hoje.slice(0, 8)}01`;

function Consulta<T>({ query, vazio, children }: { query: ReturnType<typeof useQuery<T>>; vazio?: (dados: T) => boolean; children: (dados: T) => React.ReactNode }) {
  if (query.isPending) return <Carregando />;
  if (query.isError) {
    return (
      <ErroConsulta erro={query.error} aoTentarNovamente={query.refetch} />
    );
  }
  if (vazio?.(query.data)) {
    return (
      <EstadoVazio Icone={ChartNoAxesColumn} titulo="Sem dados neste período">
        Só entram OS totalmente pagas (ou concluídas, nos tempos) dentro das datas escolhidas.
      </EstadoVazio>
    );
  }
  return <>{children(query.data)}</>;
}

function Linha({ rotulo, valor, destaque = false }: { rotulo: string; valor: string; destaque?: boolean }) {
  return (
    <div className="flex items-baseline justify-between gap-sm">
      <dt className="texto-corpo-p text-secundario">{rotulo}</dt>
      <dd className={destaque ? 'texto-mono-dado font-bold' : 'texto-mono-dado'}>{valor}</dd>
    </div>
  );
}

function Lucro({ periodo }: { periodo: Periodo }) {
  const query = useQuery({ queryKey: ['relatorio', 'lucro', periodo], queryFn: () => lucroPorServico(periodo) });
  return (
    <Consulta query={query} vazio={(d) => d.length === 0}>
      {(linhas) => (
        <ol aria-label="Serviços do mais para o menos lucrativo" className="flex flex-col gap-sm">
          {linhas.map((l, i) => (
            <li key={l.servico} className="rounded-lg border-2 border-forte bg-superficie p-md shadow-seca">
              <h3 className="texto-h2">
                {i + 1}. {l.servico}
              </h3>
              <p className="texto-corpo-p text-secundario">
                {l.quantidade} {l.quantidade === 1 ? 'carro' : 'carros'} · {formatarMinutos(l.tempoMedioMin)} em média
              </p>
              <dl className="mt-sm flex flex-col gap-2xs">
                <Linha rotulo="Receita líquida" valor={formatarMoeda(l.receitaLiquida)} />
                <Linha rotulo="Insumos" valor={formatarMoeda(l.insumos)} />
                <Linha rotulo="Mão de obra" valor={formatarMoeda(l.maoDeObra)} />
                <Linha rotulo="Lucro" valor={formatarMoeda(l.lucro)} destaque />
                <Linha rotulo="Lucro por carro" valor={formatarMoeda(l.lucroMedio)} />
              </dl>
            </li>
          ))}
        </ol>
      )}
    </Consulta>
  );
}

function Faturamento({ periodo }: { periodo: Periodo }) {
  const query = useQuery({ queryKey: ['relatorio', 'faturamento', periodo], queryFn: () => faturamento(periodo) });
  return (
    <Consulta query={query} vazio={(d) => d.dias.length === 0}>
      {(f) => (
        <div className="flex flex-col gap-md">
          <section aria-label="Total do período" className="rounded-lg border-2 border-forte bg-superficie p-md shadow-seca">
            <dl className="flex flex-col gap-2xs">
              <Linha rotulo="Bruto" valor={formatarMoeda(f.totalBruto)} />
              <Linha rotulo="Líquido (após taxas)" valor={formatarMoeda(f.totalLiquido)} destaque />
              <Linha rotulo="OS pagas" valor={String(f.osPagas)} />
              <Linha rotulo="Ticket médio" valor={formatarMoeda(f.ticketMedio)} />
            </dl>
          </section>
          <ul aria-label="Por dia" className="flex flex-col gap-sm">
            {f.dias.map((d) => (
              <li key={d.data} className="rounded-md border-2 border-padrao bg-superficie p-sm">
                <h3 className="texto-rotulo-m">{formatarDataIso(d.data)}</h3>
                <dl className="flex flex-col gap-2xs">
                  <Linha rotulo="Bruto" valor={formatarMoeda(d.bruto)} />
                  <Linha rotulo="Líquido" valor={formatarMoeda(d.liquido)} />
                  <Linha rotulo="OS pagas" valor={String(d.osPagas)} />
                  <Linha rotulo="Ticket médio" valor={formatarMoeda(d.ticketMedio)} />
                </dl>
              </li>
            ))}
          </ul>
        </div>
      )}
    </Consulta>
  );
}

function Equilibrio() {
  const [mes, setMes] = useState(hojeIso().slice(0, 7));
  const mesValido = /^\d{4}-\d{2}$/.test(mes);
  const query = useQuery({ queryKey: ['relatorio', 'equilibrio', mes], queryFn: () => pontoEquilibrio(mes), enabled: mesValido });
  return (
    <div className="flex flex-col gap-md">
      <Campo rotulo="Mês" type="month" value={mes} onChange={(e) => setMes(e.target.value)} />
      {!mesValido ? (
        <Alerta tipo="atencao">Escolha um mês.</Alerta>
      ) : (
        <Consulta query={query}>
          {(p) => (
            <section aria-label="Ponto de equilíbrio" className="flex flex-col gap-sm rounded-lg border-2 border-forte bg-superficie p-md shadow-seca">
              <p className="texto-h2">
                {p.faltam === null
                  ? 'Ainda não dá para estimar as contas do mês.'
                  : p.faltam === 0
                    ? 'As contas do mês já estão pagas.'
                    : `Faltam ${p.faltam} ${p.faltam === 1 ? 'carro' : 'carros'} para pagar as contas.`}
              </p>
              <dl className="flex flex-col gap-2xs">
                <Linha rotulo="Despesas" valor={formatarMoeda(p.despesas)} />
                <Linha rotulo="Diárias" valor={formatarMoeda(p.diarias)} />
                <Linha rotulo="Contas do mês" valor={formatarMoeda(p.contas)} destaque />
                <Linha rotulo="Margem por carro" valor={p.margemPorCarro == null ? 'sem histórico' : formatarMoeda(p.margemPorCarro)} />
                <Linha rotulo="Carros necessários" valor={p.carrosNecessarios === null ? '—' : String(p.carrosNecessarios)} />
                <Linha rotulo="Carros pagos no mês" valor={String(p.carrosFeitos)} />
              </dl>
              {p.margemEstimada ? <p className="texto-corpo-p text-secundario">Margem estimada pelo catálogo, porque ainda não há histórico dos últimos 30 dias.</p> : null}
            </section>
          )}
        </Consulta>
      )}
    </div>
  );
}

function Tempos({ periodo }: { periodo: Periodo }) {
  const query = useQuery({ queryKey: ['relatorio', 'tempos', periodo], queryFn: () => produtividade(periodo) });
  return (
    <Consulta query={query} vazio={(d) => d.porServico.length === 0 && d.porLavador.length === 0}>
      {(p) => (
        <div className="flex flex-col gap-md">
          <section aria-labelledby="tempo-servico" className="flex flex-col gap-sm">
            <h3 id="tempo-servico" className="texto-eyebrow text-secundario">
              Por serviço
            </h3>
            <ul className="flex flex-col gap-sm">
              {p.porServico.map((s) => (
                <li key={s.servico} className="rounded-md border-2 border-padrao bg-superficie p-sm">
                  <p className="texto-rotulo-m">{s.servico}</p>
                  <dl className="flex flex-col gap-2xs">
                    <Linha rotulo="Tempo médio real" valor={formatarMinutos(s.tempoMedioMin)} destaque />
                    <Linha rotulo="Previsto" valor={formatarMinutos(s.estimadoMedioMin)} />
                    <Linha rotulo="Carros" valor={String(s.quantidade)} />
                  </dl>
                </li>
              ))}
            </ul>
          </section>
          <section aria-labelledby="tempo-lavador" className="flex flex-col gap-sm">
            <h3 id="tempo-lavador" className="texto-eyebrow text-secundario">
              Por lavador
            </h3>
            <ul className="flex flex-col gap-sm">
              {p.porLavador.map((l) => (
                <li key={l.lavador} className="rounded-md border-2 border-padrao bg-superficie p-sm">
                  <p className="texto-rotulo-m">{l.lavador}</p>
                  <dl className="flex flex-col gap-2xs">
                    <Linha rotulo="Tempo médio real" valor={formatarMinutos(l.tempoMedioMin)} destaque />
                    <Linha rotulo="Carros" valor={String(l.quantidade)} />
                  </dl>
                </li>
              ))}
            </ul>
          </section>
        </div>
      )}
    </Consulta>
  );
}

/** Relatórios gerenciais do dono: lucro por serviço, faturamento, ponto de equilíbrio e tempos. */
export function Relatorios() {
  const hoje = hojeIso();
  const [aba, setAba] = useState<Aba>('lucro');
  const [inicio, setInicio] = useState(primeiroDoMes(hoje));
  const [fim, setFim] = useState(hoje);
  const periodoOk = inicio !== '' && fim !== '' && inicio <= fim;
  const periodo = { inicio, fim };

  return (
    <div className="mx-auto flex max-w-[40rem] flex-col">
      <BarraTitulo sobretitulo="Dono" titulo="Relatórios" />
      <div className="flex flex-col gap-md px-md pb-lg">
        <Abas itens={ABAS} ativa={aba} onMudar={setAba} rotulo="Relatório" />
        {aba !== 'equilibrio' ? (
          <>
            <div className="grid grid-cols-2 gap-sm">
              <Campo rotulo="De" type="date" value={inicio} max={fim || undefined} onChange={(e) => setInicio(e.target.value)} />
              <Campo rotulo="Até" type="date" value={fim} min={inicio || undefined} onChange={(e) => setFim(e.target.value)} />
            </div>
            {!periodoOk ? <Alerta tipo="atencao">A data inicial precisa ser igual ou anterior à final.</Alerta> : null}
          </>
        ) : null}
        {aba === 'equilibrio' ? <Equilibrio /> : null}
        {periodoOk && aba === 'lucro' ? <Lucro periodo={periodo} /> : null}
        {periodoOk && aba === 'faturamento' ? <Faturamento periodo={periodo} /> : null}
        {periodoOk && aba === 'tempos' ? <Tempos periodo={periodo} /> : null}
      </div>
    </div>
  );
}
