import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { CalendarDays } from 'lucide-react';
import { useState } from 'react';
import { cancelarAgendamento, listarMeusAgendamentos, type Agendamento } from '@/api/agendamentos';
import { interpretarErro } from '@/api/erros';
import { Alerta } from '@/components/ui/Alerta';
import { Button } from '@/components/ui/Button';
import { Carregando } from '@/components/ui/Carregando';
import { ErroConsulta } from '@/components/ui/ErroConsulta';
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

/**
 * Horários do cliente. `proximos` fica discreto (some se não houver nenhum) para caber no topo de Agendar;
 * `anteriores` é o histórico da aba Minha conta.
 */
export function ListaAgendamentos({ mostrar }: { mostrar: 'proximos' | 'anteriores' }) {
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
  const lista =
    mostrar === 'proximos'
      ? itens.filter((a) => a.status === 'Agendado').sort((x, y) => x.inicio.localeCompare(y.inicio))
      : itens.filter((a) => a.status !== 'Agendado');
  const historico = mostrar === 'anteriores';
  const titulo = historico ? 'Anteriores' : 'Próximos';

  return (
    <div className="flex flex-col gap-md">
      {erro ? <Alerta>{erro}</Alerta> : null}
      {historico && query.isPending ? <Carregando /> : null}
      {historico && query.isError ? <ErroConsulta erro={query.error} aoTentarNovamente={query.refetch} /> : null}
      {historico && query.isSuccess && lista.length === 0 ? (
        <EstadoVazio Icone={CalendarDays} titulo="Nenhum atendimento ainda">
          Os horários concluídos ou cancelados aparecem aqui.
        </EstadoVazio>
      ) : null}

      {lista.length > 0 ? (
        <section aria-labelledby={`lista-${mostrar}`} className="flex flex-col gap-sm">
          <h2 id={`lista-${mostrar}`} className="texto-eyebrow text-secundario">
            {titulo}
          </h2>
          <ul className="flex flex-col gap-md">
            {lista.map((a) => (
              <Cartao key={a.id} a={a} onCancelar={() => setCancelando(a)} />
            ))}
          </ul>
        </section>
      ) : null}

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
