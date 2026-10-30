import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { CarFront, CircleCheck } from 'lucide-react';
import { useState } from 'react';
import { Link } from 'react-router-dom';
import { criarAgendamento, listarHorarios } from '@/api/agendamentos';
import { listarMeusVeiculos } from '@/api/clientes';
import { interpretarErro } from '@/api/erros';
import { listarServicos } from '@/api/servicos';
import { BarraTitulo } from '@/components/layout/BarraTitulo';
import { Alerta } from '@/components/ui/Alerta';
import { Button, classesBotao } from '@/components/ui/Button';
import { Caixinha } from '@/components/ui/Caixinha';
import { Campo } from '@/components/ui/Campo';
import { Carregando } from '@/components/ui/Carregando';
import { EstadoVazio } from '@/components/ui/EstadoVazio';
import { Select } from '@/components/ui/Select';
import { formatarDataIso, formatarMinutos, formatarMoeda, hojeIso } from '@/lib/format';

const DIAS_A_FRENTE = 30;

function somarDias(dia: string, dias: number): string {
  const d = new Date(`${dia}T12:00:00Z`);
  d.setUTCDate(d.getUTCDate() + dias);
  return d.toISOString().slice(0, 10);
}

export function Agendar() {
  const qc = useQueryClient();
  const hoje = hojeIso();
  const [veiculoId, setVeiculoId] = useState('');
  const [servicoId, setServicoId] = useState('');
  const [comCera, setComCera] = useState(false);
  const [data, setData] = useState(hoje);
  const [hora, setHora] = useState<string | null>(null);
  const [erro, setErro] = useState<string | null>(null);
  const [feito, setFeito] = useState<{ data: string; hora: string } | null>(null);

  const veiculos = useQuery({ queryKey: ['meus-veiculos'], queryFn: listarMeusVeiculos });
  const servicos = useQuery({ queryKey: ['servicos'], queryFn: listarServicos });

  const veiculo = veiculos.data?.find((v) => String(v.id) === veiculoId);
  const servico = servicos.data?.find((s) => String(s.id) === servicoId);
  const precoDoTipo = servico && veiculo ? servico.precos.find((p) => p.tipoVeiculo === veiculo.tipo) : undefined;
  const temCera = precoDoTipo?.precoComCera != null;
  const cera = temCera && comCera;
  const dataValida = data >= hoje && data <= somarDias(hoje, DIAS_A_FRENTE);

  const horarios = useQuery({
    queryKey: ['horarios', data, servicoId, veiculo?.tipo, cera],
    queryFn: () => listarHorarios(data, Number(servicoId), veiculo!.tipo, cera),
    enabled: Boolean(veiculo && servico && dataValida),
  });

  const agendar = useMutation({
    mutationFn: () => criarAgendamento({ veiculoId: Number(veiculoId), servicoId: Number(servicoId), comCera: cera, inicio: `${data}T${hora}:00-03:00` }),
    onMutate: () => setErro(null),
    onSuccess: () => {
      setFeito({ data, hora: hora! });
      void qc.invalidateQueries({ queryKey: ['meus-agendamentos'] });
      void qc.invalidateQueries({ queryKey: ['horarios'] });
    },
    onError: (e) => {
      setErro(interpretarErro(e).mensagem);
      // o horário pode ter sido ocupado por outra pessoa: recarrega a grade
      setHora(null);
      void qc.invalidateQueries({ queryKey: ['horarios'] });
    },
  });

  if (feito) {
    return (
      <div className="mx-auto flex max-w-[40rem] flex-col">
        <BarraTitulo sobretitulo="Cliente" titulo="Agendar" />
        <div className="flex flex-col gap-md px-md">
          <div role="status" className="flex flex-col items-center gap-sm rounded-lg border-2 border-forte bg-concluido-bg p-lg text-center text-concluido">
            <CircleCheck aria-hidden="true" className="size-10" />
            <p className="texto-h2">Horário reservado</p>
            <p className="texto-corpo-m text-primario">
              {formatarDataIso(feito.data)} às {feito.hora} · {veiculo?.placa}
            </p>
          </div>
          <Link to="/cliente/agendamentos" className={`${classesBotao('primario')} w-full`}>
            Ver meus horários
          </Link>
        </div>
      </div>
    );
  }

  if (veiculos.isPending || servicos.isPending) {
    return (
      <div className="mx-auto flex max-w-[40rem] flex-col">
        <BarraTitulo sobretitulo="Cliente" titulo="Agendar" />
        <div className="px-md">
          <Carregando />
        </div>
      </div>
    );
  }

  if (veiculos.isError || servicos.isError) {
    return (
      <div className="mx-auto flex max-w-[40rem] flex-col">
        <BarraTitulo sobretitulo="Cliente" titulo="Agendar" />
        <div className="flex flex-col gap-sm px-md">
          <Alerta>{interpretarErro(veiculos.error ?? servicos.error).mensagem}</Alerta>
          <Button
            variante="contorno"
            onClick={() => {
              void veiculos.refetch();
              void servicos.refetch();
            }}
          >
            Tentar de novo
          </Button>
        </div>
      </div>
    );
  }

  if (veiculos.data.length === 0) {
    return (
      <div className="mx-auto flex max-w-[40rem] flex-col">
        <BarraTitulo sobretitulo="Cliente" titulo="Agendar" />
        <div className="flex flex-col gap-md px-md">
          <EstadoVazio Icone={CarFront} titulo="Cadastre um veículo primeiro">
            O horário é reservado para um carro seu.
          </EstadoVazio>
          <Link to="/cliente/veiculos" className={`${classesBotao('primario')} w-full`}>
            Meus veículos
          </Link>
        </div>
      </div>
    );
  }

  const ativos = servicos.data.filter((s) => s.ativo);

  return (
    <div className="mx-auto flex max-w-[40rem] flex-col">
      <BarraTitulo sobretitulo="Cliente" titulo="Agendar" />
      <div className="flex flex-col gap-md px-md pb-lg">
        {erro ? <Alerta>{erro}</Alerta> : null}

        <Select
          rotulo="Veículo"
          value={veiculoId}
          onChange={(e) => {
            setVeiculoId(e.target.value);
            setHora(null);
          }}
        >
          <option value="">Escolha…</option>
          {veiculos.data.map((v) => (
            <option key={v.id} value={v.id}>
              {v.placa} · {v.modelo}
            </option>
          ))}
        </Select>

        <Select
          rotulo="Serviço"
          value={servicoId}
          onChange={(e) => {
            setServicoId(e.target.value);
            setHora(null);
          }}
        >
          <option value="">Escolha…</option>
          {ativos.map((s) => (
            <option key={s.id} value={s.id}>
              {s.nome}
            </option>
          ))}
        </Select>

        {temCera ? (
          <Caixinha
            checked={comCera}
            onChange={(e) => {
              setComCera(e.target.checked);
              setHora(null);
            }}
          >
            Com cera
          </Caixinha>
        ) : null}

        <Campo
          rotulo="Dia"
          type="date"
          min={hoje}
          max={somarDias(hoje, DIAS_A_FRENTE)}
          value={data}
          ajuda="Dá para agendar de hoje até 30 dias à frente."
          erro={dataValida ? undefined : 'Escolha um dia entre hoje e os próximos 30 dias.'}
          onChange={(e) => {
            setData(e.target.value);
            setHora(null);
          }}
        />

        {veiculo && servico && dataValida ? (
          <section aria-labelledby="horarios" className="flex flex-col gap-sm">
            <h2 id="horarios" className="texto-eyebrow text-secundario">
              Horários livres
            </h2>
            {horarios.isPending ? <Carregando /> : null}
            {horarios.isError ? <Alerta>{interpretarErro(horarios.error).mensagem}</Alerta> : null}
            {horarios.isSuccess && horarios.data.horarios.length === 0 ? (
              <Alerta tipo="informacao">Não há horários livres neste dia. Tente outro dia.</Alerta>
            ) : null}
            {horarios.isSuccess && horarios.data.horarios.length > 0 ? (
              <div role="group" aria-label="Horários livres" className="grid grid-cols-3 gap-xs">
                {horarios.data.horarios.map((h) => (
                  <button
                    key={h}
                    type="button"
                    aria-pressed={h === hora}
                    onClick={() => setHora(h)}
                    className={`min-h-alvo rounded-md border-2 texto-mono-dado ${h === hora ? 'border-forte bg-inverso text-texto-inverso' : 'border-padrao bg-superficie text-primario'}`}
                  >
                    {h}
                  </button>
                ))}
              </div>
            ) : null}
          </section>
        ) : null}

        {hora && horarios.data ? (
          <section aria-label="Resumo" className="flex flex-col gap-2xs rounded-lg border-2 border-forte bg-superficie p-md shadow-seca">
            <p className="texto-h2">
              {formatarDataIso(data)} às {hora}
            </p>
            <p className="texto-corpo-g">
              {servico?.nome}
              {cera ? ' com cera' : ''} · {veiculo?.placa}
            </p>
            <p className="texto-corpo-p text-secundario">
              {formatarMinutos(horarios.data.duracaoEstimadaMin)} · {formatarMoeda(horarios.data.preco)}
            </p>
          </section>
        ) : null}

        <Button disabled={!hora} carregando={agendar.isPending} onClick={() => agendar.mutate()}>
          Confirmar horário
        </Button>
      </div>
    </div>
  );
}
