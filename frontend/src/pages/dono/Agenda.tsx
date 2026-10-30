import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { CalendarDays } from 'lucide-react';
import { useState } from 'react';
import { Link } from 'react-router-dom';
import { cancelarAgendamento, fazerCheckIn, marcarNaoCompareceu, obterAgenda, type Agendamento } from '@/api/agendamentos';
import { interpretarErro } from '@/api/erros';
import { BarraTitulo } from '@/components/layout/BarraTitulo';
import { Alerta } from '@/components/ui/Alerta';
import { Button, classesBotao } from '@/components/ui/Button';
import { Campo } from '@/components/ui/Campo';
import { Carregando } from '@/components/ui/Carregando';
import { ErroConsulta } from '@/components/ui/ErroConsulta';
import { Dialogo } from '@/components/ui/Dialogo';
import { EstadoVazio } from '@/components/ui/EstadoVazio';
import { Placa } from '@/components/ui/Placa';
import { SeloAgendamento } from '@/components/ui/SeloAgendamento';
import { formatarHora, formatarMinutos, formatarMoeda, hojeIso } from '@/lib/format';
import { PassoVistoria } from './entrada/PassoVistoria';

type Acao = { tipo: 'cancelar' | 'falta'; agendamento: Agendamento };

function CartaoAgendamento({ a, onCheckIn, onAcao }: { a: Agendamento; onCheckIn: () => void; onAcao: (tipo: Acao['tipo']) => void }) {
  return (
    <li className="flex flex-col gap-sm rounded-lg border-2 border-forte bg-superficie p-md shadow-seca">
      <div className="flex items-start justify-between gap-sm">
        <p className="texto-mono-dado font-bold">{formatarHora(a.inicio)}</p>
        <SeloAgendamento status={a.status} />
      </div>
      <Placa placa={a.placa} />
      <p className="texto-corpo-g">
        {a.servico}
        {a.comCera ? ' com cera' : ''}
      </p>
      <p className="texto-corpo-p text-secundario">
        {a.cliente ? `${a.cliente} · ` : ''}
        {formatarMinutos(a.duracaoEstimadaMin)} · {formatarMoeda(a.precoEstimado)}
      </p>
      {a.status === 'Agendado' ? (
        <div className="flex flex-wrap gap-sm">
          <Button onClick={onCheckIn}>Fazer check-in</Button>
          <Button variante="contorno" onClick={() => onAcao('falta')}>
            Não compareceu
          </Button>
          <Button variante="contorno" onClick={() => onAcao('cancelar')}>
            Cancelar horário
          </Button>
        </div>
      ) : null}
    </li>
  );
}

export function Agenda() {
  const qc = useQueryClient();
  const [data, setData] = useState(hojeIso());
  const [checkInId, setCheckInId] = useState<number | null>(null);
  const [acao, setAcao] = useState<Acao | null>(null);
  const [erro, setErro] = useState<string | null>(null);
  const [aviso, setAviso] = useState<string | null>(null);

  const query = useQuery({ queryKey: ['agenda', data], queryFn: () => obterAgenda(data), enabled: data !== '' });

  const aoErrar = (e: unknown) => setErro(interpretarErro(e).mensagem);
  const atualizar = () => qc.invalidateQueries({ queryKey: ['agenda'] });

  const checkIn = useMutation({
    mutationFn: ({ id, ...v }: { id: number } & Parameters<typeof fazerCheckIn>[1]) => fazerCheckIn(id, v),
    onSuccess: async () => {
      setCheckInId(null);
      setAviso('Check-in feito: o carro já está na fila do Pit Stop.');
      void qc.invalidateQueries({ queryKey: ['quadro'] });
      await atualizar();
    },
    onError: aoErrar,
  });

  const mudar = useMutation({
    mutationFn: ({ tipo, agendamento }: Acao) => (tipo === 'cancelar' ? cancelarAgendamento(agendamento.id) : marcarNaoCompareceu(agendamento.id)),
    onSuccess: async () => {
      setAcao(null);
      await atualizar();
    },
    onError: (e) => {
      setAcao(null);
      aoErrar(e);
    },
  });

  const agendamentos = query.data?.agendamentos ?? [];
  const emCheckIn = agendamentos.find((a) => a.id === checkInId);

  return (
    <div className="mx-auto flex max-w-[40rem] flex-col">
      <BarraTitulo sobretitulo="Dono" titulo="Agenda" />
      <div className="flex flex-col gap-md px-md pb-lg">
        <div className="flex items-end gap-sm">
          <div className="flex-1">
            <Campo
              rotulo="Dia"
              type="date"
              value={data}
              onChange={(e) => {
                setData(e.target.value);
                setCheckInId(null);
                setErro(null);
                setAviso(null);
              }}
            />
          </div>
          <Button variante="contorno" onClick={() => setData(hojeIso())}>
            Hoje
          </Button>
        </div>

        {erro ? <Alerta>{erro}</Alerta> : null}
        {aviso ? (
          <Alerta tipo="sucesso">
            {aviso}{' '}
            <Link to="/dono/pit-stop" className={classesBotao('contorno')}>
              Ver no Pit Stop
            </Link>
          </Alerta>
        ) : null}

        {data === '' ? <Alerta tipo="atencao">Escolha um dia.</Alerta> : null}
        {query.isPending && data !== '' ? <Carregando /> : null}
        {query.isError ? (
          <ErroConsulta erro={query.error} aoTentarNovamente={query.refetch} />
        ) : null}
        {query.isSuccess && agendamentos.length === 0 ? (
          <EstadoVazio Icone={CalendarDays} titulo="Nenhum horário neste dia">
            Quando um cliente agendar, o horário aparece aqui.
          </EstadoVazio>
        ) : null}

        {emCheckIn ? (
          <section aria-label={`Check-in de ${emCheckIn.placa}`} className="flex flex-col gap-md rounded-lg border-2 border-forte bg-rebaixado p-md">
            <h2 className="texto-h2">Check-in · {emCheckIn.placa}</h2>
            <PassoVistoria
              enviando={checkIn.isPending}
              erro={null}
              onVoltar={() => setCheckInId(null)}
              onConcluir={(v) => {
                setErro(null);
                checkIn.mutate({
                  id: emCheckIn.id,
                  semAvarias: v.semAvarias,
                  avarias: v.semAvarias ? [] : v.avarias,
                  pertencesEntrada: v.pertencesEntrada || null,
                  nomeQuemRetira: v.nomeQuemRetira || null,
                  aceiteCliente: v.aceiteCliente,
                });
              }}
            />
          </section>
        ) : null}

        {agendamentos.length > 0 ? (
          <ul aria-label="Horários do dia" className="flex flex-col gap-md">
            {agendamentos.map((a) => (
              <CartaoAgendamento
                key={a.id}
                a={a}
                onCheckIn={() => {
                  setErro(null);
                  setAviso(null);
                  setCheckInId(a.id);
                }}
                onAcao={(tipo) => setAcao({ tipo, agendamento: a })}
              />
            ))}
          </ul>
        ) : null}
      </div>

      <Dialogo
        aberto={acao?.tipo === 'falta'}
        titulo="Marcar falta?"
        confirmar="Não compareceu"
        carregando={mudar.isPending}
        onConfirmar={() => acao && mudar.mutate(acao)}
        onFechar={() => setAcao(null)}
      >
        O horário fica registrado como falta e libera a vaga.
      </Dialogo>
      <Dialogo
        aberto={acao?.tipo === 'cancelar'}
        titulo="Cancelar este horário?"
        confirmar="Cancelar horário"
        cancelar="Manter"
        tipo="perigo"
        carregando={mudar.isPending}
        onConfirmar={() => acao && mudar.mutate(acao)}
        onFechar={() => setAcao(null)}
      >
        O horário volta a ficar livre para outros clientes.
      </Dialogo>
    </div>
  );
}
