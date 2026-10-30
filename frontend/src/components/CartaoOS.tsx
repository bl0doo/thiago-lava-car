import type { ReactNode } from 'react';
import { Link } from 'react-router-dom';
import type { Cartao } from '@/api/tipos';
import { Placa } from '@/components/ui/Placa';
import { SeloStatus } from '@/components/ui/SeloStatus';
import { formatarCronometro, formatarHora, formatarMinutos, formatarMoeda } from '@/lib/format';
import { useAgora } from '@/lib/useAgora';

type Props = {
  cartao: Cartao;
  /** Ações do cartão (Iniciar, Encerrar, Desfazer), abaixo dos dados. */
  children?: ReactNode;
  /** Quando informado, o carro e o serviço viram um link para o detalhe do cartão. */
  href?: string;
};

/**
 * Cartão do Pit Stop (componente do Design System): placa, selo de status, carro e serviço, e o cronômetro.
 * O cronômetro só informa: passar do previsto troca a cor para atenção e diz isso em texto, sem alarme.
 */
export function CartaoOS({ cartao, children, href }: Props) {
  const agora = useAgora();
  const status = cartao.status;
  const emAndamento = status === 'EmAndamento';
  const decorrido = emAndamento && cartao.inicioEm ? agora - new Date(cartao.inicioEm).getTime() : null;
  const passou = decorrido !== null && decorrido > cartao.duracaoEstimadaMin * 60_000;

  return (
    <article
      aria-label={`${cartao.modelo}, placa ${cartao.placa}`}
      className="flex flex-col gap-sm rounded-lg border-2 border-forte bg-superficie p-md shadow-seca"
    >
      <header className="flex flex-wrap items-start justify-between gap-sm">
        <Placa placa={cartao.placa} />
        <SeloStatus status={cartao.revisaoNecessaria ? 'Atencao' : status} texto={cartao.revisaoNecessaria ? 'Conferir horário' : undefined} />
      </header>

      <div className="flex flex-col">
        {href ? (
          <Link to={href} className="texto-corpo-g text-primario underline decoration-2 underline-offset-2">
            {cartao.modelo} · {cartao.servicos.join(' + ')}
          </Link>
        ) : (
          <p className="texto-corpo-g text-primario">
            {cartao.modelo} · {cartao.servicos.join(' + ')}
          </p>
        )}
        {cartao.cliente ? <p className="texto-corpo-p text-secundario">{cartao.cliente}</p> : null}
      </div>

      <div className="flex flex-wrap items-baseline justify-between gap-x-sm gap-y-2xs texto-mono-dado text-secundario">
        <p>
          {emAndamento && decorrido !== null ? (
            <>
              <span className={`text-lg font-bold ${passou ? 'text-aviso' : 'text-primario'}`}>
                <span className="sr-only">No box há </span>
                {formatarCronometro(decorrido)}
              </span>{' '}
              de {formatarMinutos(cartao.duracaoEstimadaMin)}
              {passou ? ' · passou do previsto' : ''}
            </>
          ) : status === 'Concluido' ? (
            <>
              Levou {formatarMinutos(cartao.duracaoRealMin ?? 0)}
              {cartao.fimEm ? ` · ${formatarHora(cartao.fimEm)}` : ''}
            </>
          ) : (
            <>Previsto: {formatarMinutos(cartao.duracaoEstimadaMin)}</>
          )}
          {cartao.lavador ? ` · ${cartao.lavador}` : ''}
        </p>
        {cartao.valorTotal != null ? <p className="texto-rotulo-m text-primario">{formatarMoeda(cartao.valorTotal)}</p> : null}
      </div>

      {children ? <div className="flex flex-wrap gap-xs">{children}</div> : null}
    </article>
  );
}
