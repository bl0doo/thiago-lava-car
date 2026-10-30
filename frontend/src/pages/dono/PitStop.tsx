import { Clock, Droplet, Flag, Plus, type LucideIcon } from 'lucide-react';
import { useState } from 'react';
import { Link } from 'react-router-dom';
import type { Cartao } from '@/api/tipos';
import { CartaoOS } from '@/components/CartaoOS';
import { BarraTitulo } from '@/components/layout/BarraTitulo';
import { Alerta } from '@/components/ui/Alerta';
import { Button, classesBotao } from '@/components/ui/Button';
import { Carregando } from '@/components/ui/Carregando';
import { useQuadro } from '@/hooks/useQuadro';
import { formatarHora } from '@/lib/format';
import { podeDesfazer, type Acao } from '@/lib/quadro';
import { useAgora } from '@/lib/useAgora';

type Chave = 'aguardando' | 'emAndamento' | 'concluido';

/** As três colunas da metáfora Pit Stop. */
const COLUNAS: { chave: Chave; titulo: string; curto: string; ajuda: string; vazio: string; Icone: LucideIcon }[] = [
  { chave: 'aguardando', titulo: 'Fila de largada', curto: 'Fila', ajuda: 'carros esperando a vez', vazio: 'Nenhum carro esperando.', Icone: Clock },
  { chave: 'emAndamento', titulo: 'No box', curto: 'No box', ajuda: 'carros em lavagem agora', vazio: 'Nenhum carro no box.', Icone: Droplet },
  { chave: 'concluido', titulo: 'Bandeirada', curto: 'Bandeirada', ajuda: 'prontos para entregar', vazio: 'Nenhum carro pronto.', Icone: Flag },
];

function Acoes({ cartao, onAcao, pendente, agora }: { cartao: Cartao; onAcao: (id: number, acao: Acao) => void; pendente: boolean; agora: number }) {
  return (
    <>
      {cartao.status === 'Aguardando' ? (
        <Button variante="secundario" carregando={pendente} onClick={() => onAcao(cartao.id, 'iniciar')} className="flex-1">
          Iniciar
        </Button>
      ) : null}
      {cartao.status === 'EmAndamento' ? (
        <Button carregando={pendente} onClick={() => onAcao(cartao.id, 'encerrar')} className="flex-1">
          Encerrar
        </Button>
      ) : null}
      {podeDesfazer(cartao, agora) ? (
        <Button variante="contorno" disabled={pendente} onClick={() => onAcao(cartao.id, 'desfazer')}>
          Desfazer
        </Button>
      ) : null}
    </>
  );
}

/**
 * Quadro Pit Stop do dono (telas 04 e 11 do protótipo). No celular, uma coluna por vez escolhida nas abas
 * Fila / No box / Bandeirada; a partir do tablet deitado (1024 px), as três colunas lado a lado.
 */
export function PitStop() {
  const { quadro, carregando, erroConsulta, erroAcao, executar, cartaoPendente, atualizadoEm } = useQuadro();
  const agora = useAgora();
  const [ativa, setAtiva] = useState<Chave>('emAndamento');

  const novaEntrada = (
    <Link to="/dono/entrada" className={`${classesBotao('primario')} w-full lg:w-auto`}>
      <Plus aria-hidden="true" className="size-5" strokeWidth={2.5} />
      Nova entrada
    </Link>
  );

  if (carregando || !quadro) {
    return (
      <>
        <BarraTitulo sobretitulo="Hoje" titulo="Pit Stop" />
        <div className="px-md">{carregando ? <Carregando cartoes={3} /> : <Alerta>{erroConsulta ?? 'Não foi possível carregar o quadro.'}</Alerta>}</div>
      </>
    );
  }

  const total = quadro.aguardando.length + quadro.emAndamento.length + quadro.concluido.length;
  const colunaAtiva = COLUNAS.find((c) => c.chave === ativa)!;

  return (
    <div className="flex flex-col">
      <BarraTitulo
        sobretitulo={`${total} ${total === 1 ? 'carro' : 'carros'} hoje · atualiza a cada 15 s`}
        titulo="Pit Stop"
        acao={<div className="hidden lg:block">{novaEntrada}</div>}
      />

      <div className="flex flex-col gap-md px-md">
        {erroAcao ? <Alerta>{erroAcao}</Alerta> : null}
        {erroConsulta ? (
          <Alerta tipo="atencao">
            Sem conexão agora. Mostrando os dados das {atualizadoEm ? formatarHora(atualizadoEm) : 'última consulta'}.
          </Alerta>
        ) : null}

        {/* abas do celular: escolhem qual coluna aparece */}
        <div role="group" aria-label="Coluna do quadro" className="grid grid-cols-3 gap-xs lg:hidden">
          {COLUNAS.map(({ chave, curto }) => {
            const selecionada = chave === ativa;
            return (
              <button
                key={chave}
                type="button"
                aria-pressed={selecionada}
                onClick={() => setAtiva(chave)}
                className={`flex min-h-14 flex-col items-center justify-center rounded-md border-2 ${selecionada ? 'border-forte bg-inverso text-texto-inverso' : 'border-padrao bg-superficie text-primario'}`}
              >
                <span className="font-titulo text-[26px] leading-[30px] font-bold">{quadro[chave].length}</span>
                <span className="texto-rotulo-p">{curto}</span>
              </button>
            );
          })}
        </div>
        <p className="texto-corpo-p text-secundario lg:hidden">
          {colunaAtiva.titulo}: {colunaAtiva.ajuda}
        </p>

        <div className="grid gap-md lg:grid-cols-3">
          {COLUNAS.map(({ chave, titulo, vazio, Icone }) => (
            <section key={chave} aria-label={titulo} className={`flex-col gap-sm ${chave === ativa ? 'flex' : 'hidden'} lg:flex`}>
              <h2 className="hidden items-center gap-xs border-b-2 border-forte pb-xs lg:flex">
                <Icone aria-hidden="true" className="size-6" strokeWidth={2} />
                <span className="flex-1 texto-eyebrow text-[18px] leading-[22px]">{titulo}</span>
                <span className="font-titulo text-[22px] leading-[26px] font-bold" aria-label={`${quadro[chave].length} carros`}>
                  {quadro[chave].length}
                </span>
              </h2>
              {quadro[chave].length === 0 ? (
                <p className="rounded-lg border-2 border-dashed border-padrao px-md py-lg text-center texto-corpo-m text-secundario">{vazio}</p>
              ) : null}
              {quadro[chave].map((c) => (
                <CartaoOS key={c.id} cartao={c} href={`/dono/os/${c.id}`}>
                  <Acoes cartao={c} onAcao={executar} pendente={cartaoPendente === c.id} agora={agora} />
                </CartaoOS>
              ))}
            </section>
          ))}
        </div>

        <div className="lg:hidden">{novaEntrada}</div>
      </div>
    </div>
  );
}
