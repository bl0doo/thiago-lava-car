import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { SearchX, Star } from 'lucide-react';
import { useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { interpretarErro } from '@/api/erros';
import {
  avaliarOS,
  cancelarOS,
  desfazerUltimoPasso,
  encerrarLavagem,
  entregarOS,
  iniciarLavagem,
  obterOS,
  type DetalheOrdem,
} from '@/api/ordensServico';
import type { Cartao } from '@/api/tipos';
import { BarraTitulo } from '@/components/layout/BarraTitulo';
import { Alerta } from '@/components/ui/Alerta';
import { Button } from '@/components/ui/Button';
import { Campo } from '@/components/ui/Campo';
import { Carregando } from '@/components/ui/Carregando';
import { Dialogo } from '@/components/ui/Dialogo';
import { EstadoVazio } from '@/components/ui/EstadoVazio';
import { MensagemOS } from './os/MensagemOS';
import { PagamentosOS } from './os/PagamentosOS';
import { Placa } from '@/components/ui/Placa';
import { SeloStatus } from '@/components/ui/SeloStatus';
import { formatarHora, formatarMinutos, formatarMoeda } from '@/lib/format';
import { podeDesfazer } from '@/lib/quadro';
import { useAgora } from '@/lib/useAgora';

type Confirmacao = 'entregar' | 'cancelar' | null;

function Secao({ titulo, children }: { titulo: string; children: React.ReactNode }) {
  return (
    <section aria-label={titulo} className="flex flex-col gap-xs rounded-lg border-2 border-forte bg-superficie p-md shadow-seca">
      <h2 className="texto-h2">{titulo}</h2>
      {children}
    </section>
  );
}

/** Nota de 1 a 5: botões de 48 px com a nota escrita, para o estado não depender só da cor da estrela. */
function Avaliacao({ nota, pendente, onAvaliar }: { nota: number | null | undefined; pendente: boolean; onAvaliar: (n: number) => void }) {
  return (
    <div role="group" aria-label="Nota do cliente" className="flex gap-xs">
      {[1, 2, 3, 4, 5].map((n) => {
        const marcada = nota === n;
        return (
          <button
            key={n}
            type="button"
            disabled={pendente}
            aria-pressed={marcada}
            aria-label={`Nota ${n}`}
            onClick={() => onAvaliar(n)}
            className={`flex min-h-alvo flex-1 items-center justify-center gap-2xs rounded-md border-2 texto-rotulo-m ${marcada ? 'border-forte bg-inverso text-texto-inverso' : 'border-padrao bg-superficie text-primario'}`}
          >
            <Star aria-hidden="true" className="size-4" strokeWidth={2.5} fill={marcada ? 'currentColor' : 'none'} />
            {n}
          </button>
        );
      })}
    </div>
  );
}

export function DetalheOS() {
  const { id } = useParams();
  const osId = Number(id);
  const navegar = useNavigate();
  const qc = useQueryClient();
  const agora = useAgora();
  const [confirmacao, setConfirmacao] = useState<Confirmacao>(null);
  const [quemRetira, setQuemRetira] = useState('');
  const [erro, setErro] = useState<string | null>(null);

  const idValido = Number.isInteger(osId) && osId > 0;
  const consulta = useQuery({ queryKey: ['os', osId], queryFn: () => obterOS(osId), enabled: idValido, refetchOnWindowFocus: true });

  const mutacao = useMutation({
    mutationFn: (acao: () => Promise<Cartao>) => acao(),
    onMutate: () => setErro(null),
    onSuccess: (cartao) => {
      qc.setQueryData<DetalheOrdem>(['os', osId], (atual) => (atual ? { ...atual, ...cartao } : atual));
      setConfirmacao(null);
    },
    onError: (e) => {
      setErro(interpretarErro(e).mensagem);
      setConfirmacao(null);
    },
    onSettled: () => {
      void qc.invalidateQueries({ queryKey: ['os', osId] });
      void qc.invalidateQueries({ queryKey: ['quadro'] });
    },
  });
  const executar = (acao: () => Promise<Cartao>) => mutacao.mutate(acao);

  const voltar = () => void navegar('/dono');

  if (!idValido) {
    return (
      <>
        <BarraTitulo titulo="Cartão" onVoltar={voltar} />
        <div className="px-md">
          <EstadoVazio Icone={SearchX} titulo="Cartão não encontrado">
            Volte ao Pit Stop e escolha o carro de novo.
          </EstadoVazio>
        </div>
      </>
    );
  }

  if (consulta.isPending) {
    return (
      <>
        <BarraTitulo titulo="Cartão" onVoltar={voltar} />
        <div className="px-md">
          <Carregando cartoes={2} />
        </div>
      </>
    );
  }

  if (consulta.isError) {
    const { status, mensagem } = interpretarErro(consulta.error);
    return (
      <>
        <BarraTitulo titulo="Cartão" onVoltar={voltar} />
        <div className="flex flex-col gap-md px-md">
          <Alerta>{status === 404 ? 'Este cartão não existe mais.' : mensagem}</Alerta>
          {status === 404 ? null : <Button variante="contorno" onClick={() => void consulta.refetch()}>Tentar de novo</Button>}
        </div>
      </>
    );
  }

  const os = consulta.data;
  const ocupado = mutacao.isPending;
  const avaliavel = os.status === 'Concluido' || os.status === 'Entregue';
  const emAndamento = os.status === 'EmAndamento';
  const decorrido = emAndamento && os.inicioEm ? agora - new Date(os.inicioEm).getTime() : null;

  return (
    <div className="flex flex-col">
      <BarraTitulo sobretitulo={`Cartão #${os.id}`} titulo={os.modelo} onVoltar={voltar} />

      <div className="flex flex-col gap-md px-md pb-lg lg:max-w-[40rem]">
        {erro ? <Alerta>{erro}</Alerta> : null}

        <Secao titulo="Carro">
          <div className="flex flex-wrap items-center justify-between gap-sm">
            <Placa placa={os.placa} />
            <SeloStatus status={os.status} />
          </div>
          {os.cliente ? <p className="texto-corpo-g">{os.cliente}</p> : null}
          {os.telefone ? <p className="texto-corpo-p text-secundario">{os.telefone}</p> : null}
          <p className="texto-mono-dado text-secundario">
            Previsto: {formatarMinutos(os.duracaoEstimadaMin)}
            {decorrido !== null ? ` · no box há ${formatarMinutos(Math.floor(decorrido / 60_000))}` : ''}
            {os.fimEm ? ` · terminou às ${formatarHora(os.fimEm)}` : ''}
            {os.lavador ? ` · ${os.lavador}` : ''}
          </p>
        </Secao>

        <Secao titulo="Serviços">
          {os.itens?.length ? (
            <ul className="flex flex-col gap-2xs">
              {os.itens.map((item, i) => (
                <li key={`${item.servicoId}-${i}`} className="flex justify-between gap-sm texto-corpo-m">
                  <span>
                    {item.nome}
                    {item.comCera ? ' + cera' : ''}
                  </span>
                  <span className="texto-mono-dado">{formatarMoeda(item.precoFinal)}</span>
                </li>
              ))}
            </ul>
          ) : (
            <p className="texto-corpo-m">{os.servicos.join(' + ')}</p>
          )}
          {os.valorTotal != null ? (
            <p className="flex justify-between border-t-2 border-padrao pt-xs texto-rotulo-m">
              <span>Total</span>
              <span className="texto-mono-dado">{formatarMoeda(os.valorTotal)}</span>
            </p>
          ) : null}
          {os.valorPago != null && os.saldoPendente != null && os.pagamentos ? (
            <>
              <p className="flex justify-between texto-corpo-m">
                <span>Recebido</span>
                <span className="texto-mono-dado">{formatarMoeda(os.valorPago)}</span>
              </p>
              <p className="flex justify-between texto-rotulo-m">
                <span>Falta receber</span>
                <span className="texto-mono-dado">{formatarMoeda(os.saldoPendente)}</span>
              </p>
            </>
          ) : null}
        </Secao>

        {os.avarias?.length ? (
          <Secao titulo="Avarias na entrada">
            <ul className="flex list-disc flex-col gap-2xs pl-md texto-corpo-m">
              {os.avarias.map((a, i) => (
                <li key={i}>
                  {a.local}: {a.tipo}
                  {a.descricao ? ` (${a.descricao})` : ''}
                </li>
              ))}
            </ul>
          </Secao>
        ) : null}

        {os.pagamentos && os.saldoPendente != null && os.status !== 'Cancelada' ? (
          <Secao titulo="Pagamento">
            <PagamentosOS osId={osId} pagamentos={os.pagamentos} saldoPendente={os.saldoPendente} />
          </Secao>
        ) : null}

        {os.pertencesEntrada || os.nomeQuemRetira ? (
          <Secao titulo="Pertences e retirada">
            {os.pertencesEntrada ? <p className="texto-corpo-m">Na entrada: {os.pertencesEntrada}</p> : null}
            {os.nomeQuemRetira ? <p className="texto-corpo-m">Quem retira: {os.nomeQuemRetira}</p> : null}
          </Secao>
        ) : null}

        {emAndamento || os.status === 'Concluido' ? (
          <Secao titulo="Mensagem para o cliente">
            <MensagemOS key={os.status} osId={osId} tipo={emAndamento ? 'lavagem-iniciada' : 'lavagem-concluida'} />
          </Secao>
        ) : null}

        {avaliavel ? (
          <Secao titulo="Avaliação do cliente">
            <Avaliacao nota={os.avaliacao} pendente={ocupado} onAvaliar={(n) => executar(() => avaliarOS(osId, n))} />
          </Secao>
        ) : null}

        <div className="flex flex-col gap-xs sm:flex-row">
          {os.status === 'Aguardando' ? (
            <Button variante="secundario" disabled={ocupado} onClick={() => executar(() => iniciarLavagem(osId))} className="flex-1">
              Iniciar
            </Button>
          ) : null}
          {emAndamento ? (
            <Button disabled={ocupado} onClick={() => executar(() => encerrarLavagem(osId))} className="flex-1">
              Encerrar
            </Button>
          ) : null}
          {os.status === 'Concluido' ? (
            <Button disabled={ocupado} onClick={() => setConfirmacao('entregar')} className="flex-1">
              Entregar o carro
            </Button>
          ) : null}
          {podeDesfazer(os, agora) ? (
            <Button variante="contorno" disabled={ocupado} onClick={() => executar(() => desfazerUltimoPasso(osId))}>
              Desfazer
            </Button>
          ) : null}
          {os.status === 'Aguardando' ? (
            <Button variante="contorno" disabled={ocupado} onClick={() => setConfirmacao('cancelar')}>
              Cancelar OS
            </Button>
          ) : null}
        </div>
      </div>

      <Dialogo
        aberto={confirmacao === 'entregar'}
        titulo="Entregar o carro?"
        confirmar="Entregar"
        carregando={ocupado}
        onConfirmar={() => executar(() => entregarOS(osId, quemRetira.trim() || null))}
        onFechar={() => setConfirmacao(null)}
      >
        <div className="flex flex-col gap-sm">
          <p>
            {os.saldoPendente ? `Ainda faltam ${formatarMoeda(os.saldoPendente)} a receber; o saldo vira pendência no caixa.` : 'A OS passa para Entregue e sai do Pit Stop.'}
          </p>
          <Campo rotulo="Quem retirou (opcional)" maxLength={100} value={quemRetira} onChange={(e) => setQuemRetira(e.target.value)} />
        </div>
      </Dialogo>

      <Dialogo
        aberto={confirmacao === 'cancelar'}
        titulo="Cancelar esta OS?"
        confirmar="Cancelar OS"
        cancelar="Manter"
        tipo="perigo"
        carregando={ocupado}
        onConfirmar={() => executar(() => cancelarOS(osId))}
        onFechar={() => setConfirmacao(null)}
      >
        O carro sai da fila de largada. Isso não pode ser desfeito.
      </Dialogo>
    </div>
  );
}
