import { useMutation, useQueryClient } from '@tanstack/react-query';
import { CircleCheck } from 'lucide-react';
import { useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { interpretarErro } from '@/api/erros';
import { registrarEntrada, type OSCriada } from '@/api/ordensServico';
import type { Veiculo } from '@/api/veiculos';
import { BarraTitulo } from '@/components/layout/BarraTitulo';
import { classesBotao } from '@/components/ui/Button';
import { Placa } from '@/components/ui/Placa';
import { formatarHora, formatarMinutos, formatarMoeda, lerValor } from '@/lib/format';
import { PassoServicos, type ItemEscolhido } from './entrada/PassoServicos';
import { PassoVeiculo } from './entrada/PassoVeiculo';
import { PassoVistoria } from './entrada/PassoVistoria';

const TITULOS = ['Veículo', 'Serviços', 'Vistoria'] as const;

/** Nova entrada (telas 05 e cenários): placa → serviços → vistoria e aceite. Termina com o cartão em "Fila de largada". */
export function NovaEntrada() {
  const navegar = useNavigate();
  const qc = useQueryClient();
  const [passo, setPasso] = useState(0);
  const [veiculo, setVeiculo] = useState<Veiculo | null>(null);
  const [dono, setDono] = useState('');
  const [itens, setItens] = useState<ItemEscolhido[]>([]);
  const [criada, setCriada] = useState<OSCriada | null>(null);
  const [erro, setErro] = useState<string | null>(null);

  const registrar = useMutation({
    mutationFn: registrarEntrada,
    onSuccess: async (os) => {
      await qc.invalidateQueries({ queryKey: ['quadro'] });
      setCriada(os);
    },
    onError: (e) => {
      const r = interpretarErro(e);
      setErro(r.status === 409 ? `${r.mensagem} Veja o cartão no Pit Stop.` : r.mensagem);
    },
  });

  if (criada && veiculo) {
    return (
      <section className="flex flex-col">
        <BarraTitulo sobretitulo="Nova entrada" titulo="Carro na fila" />
        <div className="mx-auto flex w-full max-w-[40rem] flex-col gap-md px-md">
          <div className="flex flex-col items-center gap-sm rounded-lg border-2 border-forte bg-concluido-bg p-lg text-center text-concluido" role="status">
            <CircleCheck aria-hidden="true" className="size-10" />
            <p className="texto-h2">Entrada registrada</p>
            <Placa placa={veiculo.placa} />
            <p className="texto-corpo-m text-primario">
              {formatarMoeda(criada.valorTotal)} · {formatarMinutos(criada.duracaoEstimadaMin)} · previsão {formatarHora(criada.previsaoConclusao)}
            </p>
          </div>
          <Link to="/dono/pit-stop" className={`${classesBotao('primario')} w-full`}>
            Ver no Pit Stop
          </Link>
          <button type="button" className={`${classesBotao('contorno')} w-full`} onClick={() => window.location.assign('/dono/entrada')}>
            Registrar outro carro
          </button>
        </div>
      </section>
    );
  }

  return (
    <section className="flex flex-col">
      <BarraTitulo sobretitulo={`Nova entrada · passo ${passo + 1} de ${TITULOS.length}`} titulo={TITULOS[passo]!} onVoltar={() => (passo === 0 ? void navegar('/dono/pit-stop') : setPasso(passo - 1))} />
      <div className="mx-auto flex w-full max-w-[40rem] flex-col gap-md px-md">
        <ol className="flex gap-2xs" aria-label="Progresso">
          {TITULOS.map((t, i) => (
            <li key={t} aria-current={i === passo ? 'step' : undefined} className={`h-2 flex-1 rounded-pill ${i <= passo ? 'bg-acao-primaria' : 'bg-rebaixado'}`}>
              <span className="sr-only">{t}</span>
            </li>
          ))}
        </ol>
        {veiculo && passo > 0 ? (
          <div className="flex items-center gap-sm">
            <Placa placa={veiculo.placa} />
            <span className="texto-corpo-m">
              {veiculo.modelo} · {dono}
            </span>
          </div>
        ) : null}
        {passo === 0 ? (
          <PassoVeiculo
            onPronto={(v, nome) => {
              setVeiculo(v);
              setDono(nome);
              setItens([]);
              setPasso(1);
            }}
          />
        ) : null}
        {passo === 1 && veiculo ? <PassoServicos tipo={veiculo.tipo} itens={itens} onMudar={setItens} onContinuar={() => setPasso(2)} onVoltar={() => setPasso(0)} /> : null}
        {passo === 2 && veiculo ? (
          <PassoVistoria
            enviando={registrar.isPending}
            erro={erro}
            onVoltar={() => setPasso(1)}
            onConcluir={(v) => {
              setErro(null);
              registrar.mutate({
                veiculoId: veiculo.id,
                itens: itens.map((i) => ({ servicoId: i.servicoId, comCera: i.comCera, precoManual: i.precoManual.trim() ? lerValor(i.precoManual) : null })),
                semAvarias: v.semAvarias,
                avarias: v.semAvarias ? [] : v.avarias,
                pertencesEntrada: v.pertencesEntrada || null,
                nomeQuemRetira: v.nomeQuemRetira || null,
                aceiteCliente: v.aceiteCliente,
              });
            }}
          />
        ) : null}
      </div>
    </section>
  );
}
