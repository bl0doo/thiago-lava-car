import { LogOut, RefreshCw, Undo2, Waves } from 'lucide-react';
import { Link } from 'react-router-dom';
import type { Cartao } from '@/api/tipos';
import { useAuth } from '@/auth/AuthContext';
import { CartaoOS } from '@/components/CartaoOS';
import { BarraTitulo } from '@/components/layout/BarraTitulo';
import { Alerta } from '@/components/ui/Alerta';
import { Button, classesBotao } from '@/components/ui/Button';
import { Carregando } from '@/components/ui/Carregando';
import { EstadoVazio } from '@/components/ui/EstadoVazio';
import { Placa } from '@/components/ui/Placa';
import { SeloStatus } from '@/components/ui/SeloStatus';
import { useQuadro } from '@/hooks/useQuadro';
import { formatarCronometro, formatarHora, formatarMinutos } from '@/lib/format';
import { podeDesfazer, type Acao } from '@/lib/quadro';
import { useAgora } from '@/lib/useAgora';

type AcaoProps = { onAcao: (id: number, acao: Acao) => void; pendente: number | null };

/** Faixa "Iniciada às 14:05 · Desfazer", visível só nos 2 minutos em que desfazer é permitido. */
function AvisoDesfazer({ cartao, onAcao, pendente }: { cartao: Cartao } & AcaoProps) {
  const quando = cartao.status === 'Concluido' ? `${cartao.placa} encerrada às ${formatarHora(cartao.fimEm!)}` : `Iniciada às ${formatarHora(cartao.inicioEm!)}`;
  return (
    <div className="flex items-center gap-sm rounded-md border-2 border-forte bg-rebaixado p-sm">
      <Undo2 aria-hidden="true" className="size-6 shrink-0" strokeWidth={2} />
      <p className="flex-1 texto-rotulo-m">{quando}</p>
      <Button variante="contorno" disabled={pendente === cartao.id} onClick={() => onAcao(cartao.id, 'desfazer')}>
        Desfazer
      </Button>
    </div>
  );
}

/** Tela 07 do protótipo: o carro no box com o cronômetro grande e o botão Encerrar de 72 px. */
function NoBox({ cartao, agora, onAcao, pendente }: { cartao: Cartao; agora: number } & AcaoProps) {
  const decorrido = cartao.inicioEm ? agora - new Date(cartao.inicioEm).getTime() : 0;
  return (
    <section className="flex flex-col gap-md">
      <article aria-label={`${cartao.modelo}, placa ${cartao.placa}`} className="flex flex-col gap-sm rounded-lg border-2 border-forte bg-superficie p-md shadow-seca">
        <header className="flex items-start justify-between gap-sm">
          <Placa placa={cartao.placa} />
          <SeloStatus status="EmAndamento" />
        </header>
        <p className="texto-corpo-g">
          {cartao.modelo} · {cartao.servicos.join(' + ')}
        </p>
        <p className="texto-eyebrow text-secundario">No box há</p>
        <p role="timer" aria-live="off" className="font-mono text-[56px] leading-[68px] font-bold tabular-nums">
          {formatarCronometro(decorrido)}
        </p>
        <p className="texto-corpo-p text-secundario">
          Previsão: {formatarMinutos(cartao.duracaoEstimadaMin)}. O cronômetro só informa: sem pressa, sem perder qualidade.
        </p>
      </article>
      <Button tamanho="lavador" className="w-full" carregando={pendente === cartao.id} onClick={() => onAcao(cartao.id, 'encerrar')}>
        Encerrar
      </Button>
      <Link to={`/lavador/os/${cartao.id}`} className={`${classesBotao('contorno')} w-full`}>
        Achou algo no carro?
      </Link>
      {podeDesfazer(cartao, agora) ? <AvisoDesfazer cartao={cartao} onAcao={onAcao} pendente={pendente} /> : null}
    </section>
  );
}

/** Fila do lavador (telas 06 e 07): o carro no box primeiro, depois o próximo com Iniciar e o resto da fila. */
export function MinhaFila() {
  const { usuario, sair } = useAuth();
  const { quadro, carregando, erroConsulta, erroAcao, executar, cartaoPendente, atualizar, atualizando } = useQuadro();
  const agora = useAgora();

  const sairBotao = (
    <Button variante="contorno" onClick={() => void sair()} className="px-sm">
      <LogOut aria-hidden="true" className="size-5" strokeWidth={2} />
      Sair
    </Button>
  );

  if (carregando || !quadro) {
    return (
      <div className="mx-auto flex max-w-[36rem] flex-col">
        <BarraTitulo sobretitulo={usuario?.nome} titulo="Minha fila" acao={sairBotao} />
        <div className="px-md">{carregando ? <Carregando /> : <Alerta>{erroConsulta ?? 'Não foi possível carregar a fila.'}</Alerta>}</div>
      </div>
    );
  }

  const [proximo, ...depois] = quadro.aguardando;
  // quem acabou de encerrar ainda precisa do "Desfazer" por 2 minutos
  const recemConcluidos = quadro.concluido.filter((c) => podeDesfazer(c, agora));
  const noBox = quadro.emAndamento;
  const naFila = quadro.aguardando.length;

  return (
    <div className="mx-auto flex max-w-[36rem] flex-col">
      <BarraTitulo
        sobretitulo={`${usuario?.nome ?? ''} · ${naFila} na fila`}
        titulo={noBox.length > 0 ? 'No box' : 'Minha fila'}
        acao={sairBotao}
      />
      <div className="flex flex-col gap-md px-md">
        {erroAcao ? <Alerta>{erroAcao}</Alerta> : null}
        {erroConsulta ? <Alerta tipo="atencao">Sem conexão agora. Mostrando os últimos dados.</Alerta> : null}

        {recemConcluidos.map((c) => (
          <AvisoDesfazer key={c.id} cartao={c} onAcao={executar} pendente={cartaoPendente} />
        ))}

        {noBox.map((c) => (
          <NoBox key={c.id} cartao={c} agora={agora} onAcao={executar} pendente={cartaoPendente} />
        ))}

        {proximo ? (
          <section aria-labelledby="proximo" className="flex flex-col gap-sm">
            <h2 id="proximo" className="texto-eyebrow text-secundario">
              Próximo carro
            </h2>
            <CartaoOS cartao={proximo} />
            <Button tamanho="lavador" className="w-full" carregando={cartaoPendente === proximo.id} onClick={() => executar(proximo.id, 'iniciar')}>
              Iniciar
            </Button>
          </section>
        ) : null}

        {depois.length > 0 ? (
          <section aria-labelledby="depois" className="flex flex-col gap-sm">
            <h2 id="depois" className="texto-eyebrow text-secundario">
              Depois
            </h2>
            {depois.map((c) => (
              <CartaoOS key={c.id} cartao={c} />
            ))}
          </section>
        ) : null}

        {noBox.length === 0 && !proximo ? (
          <>
            <EstadoVazio Icone={Waves} titulo="Nenhum carro na fila">
              Quando o dono registrar uma entrada, o carro aparece aqui.
            </EstadoVazio>
            <Button variante="contorno" tamanho="lavador" className="w-full" carregando={atualizando} onClick={atualizar}>
              <RefreshCw aria-hidden="true" className="size-7" strokeWidth={2.5} />
              Atualizar
            </Button>
          </>
        ) : (
          <p className="texto-corpo-p text-secundario">Iniciou sem querer? O botão Desfazer fica por 2 minutos.</p>
        )}
      </div>
    </div>
  );
}
