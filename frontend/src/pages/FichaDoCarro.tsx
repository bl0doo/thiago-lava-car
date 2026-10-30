import { useQuery } from '@tanstack/react-query';
import { CarFront, Flag } from 'lucide-react';
import { useNavigate, useParams } from 'react-router-dom';
import { interpretarErro } from '@/api/erros';
import { obterFicha, type Ficha } from '@/api/veiculos';
import { BarraTitulo } from '@/components/layout/BarraTitulo';
import { Alerta } from '@/components/ui/Alerta';
import { Button } from '@/components/ui/Button';
import { Carregando } from '@/components/ui/Carregando';
import { EstadoVazio } from '@/components/ui/EstadoVazio';
import { Placa } from '@/components/ui/Placa';
import { formatarDataCurta, formatarMinutos, formatarMoeda } from '@/lib/format';

/**
 * Ficha do carro (tela 08): o histórico do veículo ao longo do tempo, uma visita por linha na linha do tempo.
 * Serve ao dono (`/dono/veiculos/:id`) e ao cliente (`/cliente/veiculos/:id`); o valor aparece nos dois porque é o do próprio cliente.
 */
export function FichaDoCarro() {
  const { id } = useParams();
  const navegar = useNavigate();
  const consulta = useQuery({ queryKey: ['ficha', Number(id)], queryFn: () => obterFicha(Number(id)) });

  return (
    <section className="flex flex-col">
      <BarraTitulo sobretitulo="Ficha do carro" titulo={consulta.data?.veiculo.modelo ?? 'Veículo'} onVoltar={() => void navegar(-1)} />
      <div className="mx-auto flex w-full max-w-[40rem] flex-col gap-md px-md">
        {consulta.isPending ? <Carregando texto="Carregando a ficha…" cartoes={3} /> : null}
        {consulta.isError ? (
          <>
            <Alerta>{interpretarErro(consulta.error).mensagem}</Alerta>
            <Button variante="contorno" onClick={() => void consulta.refetch()}>
              Tentar de novo
            </Button>
          </>
        ) : null}
        {consulta.data ? <Conteudo ficha={consulta.data} /> : null}
      </div>
    </section>
  );
}

function Conteudo({ ficha }: { ficha: Ficha }) {
  const { veiculo, totalServicos, historico } = ficha;
  return (
    <>
      <div className="flex items-center gap-md rounded-lg border-2 border-forte bg-superficie p-md">
        <Placa placa={veiculo.placa} />
        <div className="flex flex-col">
          <p className="texto-rotulo-g">{veiculo.modelo}</p>
          <p className="texto-corpo-p text-secundario">
            {veiculo.tipo}
            {veiculo.cor ? ` · ${veiculo.cor}` : ''}
          </p>
          <p className="texto-corpo-p text-secundario">
            {totalServicos} {totalServicos === 1 ? 'visita' : 'visitas'} na Thiago Lava Car
          </p>
        </div>
      </div>
      {historico.length === 0 ? (
        <EstadoVazio Icone={CarFront} titulo="Sem visitas ainda">
          Depois da primeira lavagem, o histórico deste carro aparece aqui.
        </EstadoVazio>
      ) : (
        <ol className="flex flex-col" aria-label="Linha do tempo de visitas">
          {historico.map((v) => (
            <li key={v.id} className="relative flex gap-sm border-l-2 border-forte pb-md pl-md last:border-transparent">
              <span aria-hidden="true" className="absolute -left-[9px] top-1 flex size-4 items-center justify-center rounded-pill border-2 border-forte bg-acao-primaria" />
              <div className="flex flex-1 flex-col gap-2xs rounded-lg border-2 border-padrao bg-superficie p-md">
                <p className="texto-eyebrow text-secundario">{formatarDataCurta(v.data)}</p>
                <p className="texto-rotulo-m">{v.servicos.join(', ') || 'Serviço'}</p>
                <p className="flex items-center gap-xs texto-corpo-p text-secundario">
                  <Flag aria-hidden="true" className="size-4" />
                  {v.status === 'Concluido' ? 'Concluído' : v.status}
                  {v.duracaoRealMin !== null ? ` · ${formatarMinutos(v.duracaoRealMin)}` : ''} · {formatarMoeda(v.valorTotal)}
                </p>
              </div>
            </li>
          ))}
        </ol>
      )}
    </>
  );
}
