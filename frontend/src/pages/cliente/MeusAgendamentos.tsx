import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { CalendarDays } from 'lucide-react';
import { useState } from 'react';
import { Link } from 'react-router-dom';
import { cancelarAgendamento, listarMeusAgendamentos, type Agendamento } from '@/api/agendamentos';
import { interpretarErro } from '@/api/erros';
import { BarraTitulo } from '@/components/layout/BarraTitulo';
import { Alerta } from '@/components/ui/Alerta';
import { Button, classesBotao } from '@/components/ui/Button';
import { Carregando } from '@/components/ui/Carregando';
import { Dialogo } from '@/components/ui/Dialogo';
import { EstadoVazio } from '@/components/ui/EstadoVazio';
import { Placa } from '@/components/ui/Placa';
import { SeloAgendamento } from '@/components/ui/SeloAgendamento';
import { formatarDataCurta, formatarHora, formatarMinutos, formatarMoeda } from '@/lib/format';

function Cartao({ a, onCancelar }: { a: Agendamento; onCancelar: () => void }) {
  return (
    <li className="flex flex-col gap-sm rounded-lg border-2 border-forte bg-superficie p-md shadow-seca">
      <div className="flex items-start justify-between gap-sm">
        <p className="texto-h2">
          {formatarDataCurta(a.inicio)} às {formatarHora(a.inicio)}
        </p>
        <SeloAgendamento status={a.status} />
      </div>
      <Placa placa={a.placa} />
      <p className="texto-corpo-g">
        {a.servico}
        {a.comCera ? ' com cera' : ''}
      </p>
      <p className="texto-corpo-p text-secundario">
        {formatarMinutos(a.duracaoEstimadaMin)} · {formatarMoeda(a.precoEstimado)}
      </p>
      {a.status === 'Agendado' ? (
        <Button variante="contorno" onClick={onCancelar}>
          Cancelar horário
        </Button>
      ) : null}
    </li>
  );
}

/** Meus agendamentos (cliente): próximos primeiro, depois os passados, com cancelamento enquanto está agendado. */
export function MeusAgendamentos() {
  const qc = useQueryClient();
  const query = useQuery({ queryKey: ['meus-agendamentos'], queryFn: listarMeusAgendamentos });
  const [cancelando, setCancelando] = useState<Agendamento | null>(null);
  const [erro, setErro] = useState<string | null>(null);

  const cancelar = useMutation({
    mutationFn: (a: Agendamento) => cancelarAgendamento(a.id),
    onSuccess: async () => {
      setCancelando(null);
      await qc.invalidateQueries({ queryKey: ['meus-agendamentos'] });
    },
    onError: (e) => {
      setCancelando(null);
      setErro(interpretarErro(e).mensagem);
    },
  });

  const itens = query.data ?? [];
  const proximos = itens.filter((a) => a.status === 'Agendado').sort((x, y) => x.inicio.localeCompare(y.inicio));
  const anteriores = itens.filter((a) => a.status !== 'Agendado');

  return (
    <div className="mx-auto flex max-w-[40rem] flex-col">
      <BarraTitulo sobretitulo="Cliente" titulo="Meus horários" />
      <div className="flex flex-col gap-md px-md pb-lg">
        {erro ? <Alerta>{erro}</Alerta> : null}
        {query.isPending ? <Carregando /> : null}
        {query.isError ? (
          <div className="flex flex-col gap-sm">
            <Alerta>{interpretarErro(query.error).mensagem}</Alerta>
            <Button variante="contorno" onClick={() => void query.refetch()}>
              Tentar de novo
            </Button>
          </div>
        ) : null}
        {query.isSuccess && itens.length === 0 ? (
          <EstadoVazio Icone={CalendarDays} titulo="Você ainda não tem horários">
            Escolha o dia e o serviço e reserve sem fila.
          </EstadoVazio>
        ) : null}

        {proximos.length > 0 ? (
          <section aria-labelledby="proximos" className="flex flex-col gap-sm">
            <h2 id="proximos" className="texto-eyebrow text-secundario">
              Próximos
            </h2>
            <ul className="flex flex-col gap-md">
              {proximos.map((a) => (
                <Cartao key={a.id} a={a} onCancelar={() => setCancelando(a)} />
              ))}
            </ul>
          </section>
        ) : null}

        {anteriores.length > 0 ? (
          <section aria-labelledby="anteriores" className="flex flex-col gap-sm">
            <h2 id="anteriores" className="texto-eyebrow text-secundario">
              Anteriores
            </h2>
            <ul className="flex flex-col gap-md">
              {anteriores.map((a) => (
                <Cartao key={a.id} a={a} onCancelar={() => setCancelando(a)} />
              ))}
            </ul>
          </section>
        ) : null}

        {query.isSuccess ? (
          <Link to="/cliente/agendar" className={`${classesBotao('primario')} w-full`}>
            Agendar um horário
          </Link>
        ) : null}
      </div>

      <Dialogo
        aberto={cancelando !== null}
        titulo="Cancelar este horário?"
        confirmar="Cancelar horário"
        cancelar="Manter"
        tipo="perigo"
        carregando={cancelar.isPending}
        onConfirmar={() => cancelando && cancelar.mutate(cancelando)}
        onFechar={() => setCancelando(null)}
      >
        O horário volta a ficar livre para outros clientes.
      </Dialogo>
    </div>
  );
}
