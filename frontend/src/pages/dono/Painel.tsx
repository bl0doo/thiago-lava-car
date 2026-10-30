import { useQuery } from '@tanstack/react-query';
import { Flag, Plus, Wallet } from 'lucide-react';
import { Link } from 'react-router-dom';
import { interpretarErro } from '@/api/erros';
import { obterPainelHoje, type PainelHoje } from '@/api/painel';
import { BarraTitulo } from '@/components/layout/BarraTitulo';
import { Alerta } from '@/components/ui/Alerta';
import { Button, classesBotao } from '@/components/ui/Button';
import { Carregando } from '@/components/ui/Carregando';
import { formatarData, formatarMoeda } from '@/lib/format';

function NumeroChave({ rotulo, valor, destaque, detalhe }: { rotulo: string; valor: string; destaque?: boolean; detalhe?: string }) {
  return (
    <div className={`flex flex-col gap-2xs rounded-lg border-2 border-forte p-md shadow-seca ${destaque ? 'bg-acao-primaria text-sobre-destaque' : 'bg-superficie'}`}>
      <p className="texto-eyebrow">{rotulo}</p>
      <p className="texto-numero-m">{valor}</p>
      {detalhe ? <p className="texto-corpo-p">{detalhe}</p> : null}
    </div>
  );
}

/** Painel (tela 03): os dois números do dia e quantos carros faltam para pagar as contas, sem metáfora, de propósito. */
export function Painel() {
  const consulta = useQuery({ queryKey: ['painel', 'hoje'], queryFn: obterPainelHoje, refetchOnWindowFocus: true });

  return (
    <section className="flex flex-col">
      <BarraTitulo sobretitulo={formatarData(new Date())} titulo="Painel" />
      <div className="flex flex-col gap-md px-md">
        {consulta.isPending ? <Carregando texto="Carregando o painel…" cartoes={3} /> : null}
        {consulta.isError ? (
          <>
            <Alerta>{interpretarErro(consulta.error).mensagem}</Alerta>
            <Button variante="contorno" onClick={() => void consulta.refetch()}>
              Tentar de novo
            </Button>
          </>
        ) : null}
        {consulta.data ? <Conteudo p={consulta.data} /> : null}
        <div className="grid gap-xs sm:grid-cols-3">
          <Link to="/dono/entrada" className={`${classesBotao('primario')} w-full`}>
            <Plus aria-hidden="true" className="size-5" strokeWidth={2.5} />
            Nova entrada
          </Link>
          <Link to="/dono/pit-stop" className={`${classesBotao('contorno')} w-full`}>
            <Flag aria-hidden="true" className="size-5" strokeWidth={2} />
            Pit Stop
          </Link>
          <Link to="/dono/caixa" className={`${classesBotao('contorno')} w-full`}>
            <Wallet aria-hidden="true" className="size-5" strokeWidth={2} />
            Caixa do dia
          </Link>
        </div>
      </div>
    </section>
  );
}

function Conteudo({ p }: { p: PainelHoje }) {
  const faltam = p.carrosFaltamParaPagarContas;
  return (
    <>
      {p.pendencias.length > 0 ? (
        <ul className="flex flex-col gap-xs" aria-label="Pendências">
          {p.pendencias.map((t) => (
            <li key={t}>
              <Alerta tipo="atencao">{t}</Alerta>
            </li>
          ))}
        </ul>
      ) : null}
      <div className="grid gap-md sm:grid-cols-2">
        <NumeroChave rotulo="Entrou hoje" valor={formatarMoeda(p.entrouHoje)} detalhe={`${formatarMoeda(p.liquidoHoje)} depois das taxas`} />
        <NumeroChave rotulo="Sobrou de verdade" valor={formatarMoeda(p.sobrouDeVerdade)} destaque detalhe="Já descontadas despesas, diárias e insumos" />
      </div>
      <div className="rounded-lg border-2 border-forte bg-superficie p-md">
        <p className="texto-eyebrow text-secundario">Contas do mês</p>
        <p className="texto-h2">
          {faltam === null
            ? 'Ainda não dá para estimar as contas do mês.'
            : faltam === 0
              ? 'Contas do mês pagas.'
              : `Faltam ${faltam} ${faltam === 1 ? 'carro' : 'carros'} para pagar as contas`}
        </p>
        <p className="texto-corpo-p text-secundario">
          {p.carrosNoMes} {p.carrosNoMes === 1 ? 'carro lavado' : 'carros lavados'} neste mês
        </p>
      </div>
    </>
  );
}
