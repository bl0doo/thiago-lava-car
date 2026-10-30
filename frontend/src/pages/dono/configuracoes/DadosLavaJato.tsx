import { zodResolver } from '@hookform/resolvers/zod';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useState } from 'react';
import { useForm } from 'react-hook-form';
import { obterConfiguracao, salvarConfiguracao, type Configuracao } from '@/api/configuracoes';
import { interpretarErro } from '@/api/erros';
import { Alerta } from '@/components/ui/Alerta';
import { Button } from '@/components/ui/Button';
import { Campo } from '@/components/ui/Campo';
import { Carregando } from '@/components/ui/Carregando';
import { configuracaoSchema, DIAS_SEMANA, type ConfiguracaoEntrada, type ConfiguracaoSaida } from '@/schemas/configuracao';

const paraTexto = (n: number) => String(n).replace('.', ',');

function Formulario({ atual }: { atual: Configuracao }) {
  const qc = useQueryClient();
  const [erro, setErro] = useState<string | null>(null);
  const [salvo, setSalvo] = useState(false);
  const { register, handleSubmit, setError, formState } = useForm<ConfiguracaoEntrada, unknown, ConfiguracaoSaida>({
    resolver: zodResolver(configuracaoSchema),
    defaultValues: {
      nomeLavaJato: atual.nomeLavaJato,
      telefone: atual.telefone ?? '',
      abertura: atual.abertura,
      fechamento: atual.fechamento,
      dias: atual.diasFuncionamento.map(String),
      diariaSemana: paraTexto(atual.diariaSemana),
      diariaSabado: paraTexto(atual.diariaSabado),
      intervaloAgendaMin: String(atual.intervaloAgendaMin),
    },
  });

  const salvar = useMutation({
    mutationFn: (d: ConfiguracaoSaida) =>
      salvarConfiguracao({
        nomeLavaJato: d.nomeLavaJato,
        telefone: d.telefone === '' ? null : d.telefone,
        abertura: d.abertura,
        fechamento: d.fechamento,
        diasFuncionamento: d.dias.map(Number).sort((a, b) => a - b),
        diariaSemana: d.diariaSemana,
        diariaSabado: d.diariaSabado,
        intervaloAgendaMin: d.intervaloAgendaMin,
      }),
    onMutate: () => {
      setErro(null);
      setSalvo(false);
    },
    onSuccess: () => {
      setSalvo(true);
      void qc.invalidateQueries({ queryKey: ['configuracao'] });
    },
    onError: (e) => {
      const { mensagem, campos } = interpretarErro(e);
      const nomes = ['nomeLavaJato', 'telefone', 'abertura', 'fechamento', 'diariaSemana', 'diariaSabado', 'intervaloAgendaMin'] as const;
      let achou = false;
      for (const [campo, msg] of Object.entries(campos)) {
        const alvo = nomes.find((n) => n.toLowerCase() === campo.toLowerCase());
        if (alvo) {
          setError(alvo, { message: msg });
          achou = true;
        }
      }
      if (!achou) setErro(mensagem);
    },
  });

  const e = formState.errors;
  return (
    <form onSubmit={handleSubmit((d) => salvar.mutate(d))} noValidate className="flex flex-col gap-md">
      {erro ? <Alerta>{erro}</Alerta> : null}
      {salvo ? <Alerta tipo="sucesso">Configurações salvas.</Alerta> : null}
      <Campo rotulo="Nome do lava-jato" erro={e.nomeLavaJato?.message} {...register('nomeLavaJato')} />
      <Campo rotulo="Telefone" type="tel" erro={e.telefone?.message} {...register('telefone')} />
      <div className="grid grid-cols-2 gap-sm">
        <Campo rotulo="Abre às" type="time" erro={e.abertura?.message} {...register('abertura')} />
        <Campo rotulo="Fecha às" type="time" erro={e.fechamento?.message} {...register('fechamento')} />
      </div>
      <fieldset className="flex flex-col gap-xs">
        <legend className="texto-rotulo-m">Dias de funcionamento</legend>
        <div className="grid grid-cols-2 gap-xs">
          {DIAS_SEMANA.map((nome, i) => (
            <label key={nome} className="flex min-h-alvo items-center gap-sm rounded-md border-2 border-padrao bg-superficie px-sm texto-corpo-g">
              <input type="checkbox" value={String(i)} className="size-5" {...register('dias')} />
              {nome}
            </label>
          ))}
        </div>
        {e.dias?.message ? <p className="texto-rotulo-p text-erro">{e.dias.message}</p> : null}
      </fieldset>
      <div className="grid grid-cols-2 gap-sm">
        <Campo rotulo="Diária de segunda a sexta (R$)" inputMode="decimal" erro={e.diariaSemana?.message} {...register('diariaSemana')} />
        <Campo rotulo="Diária de sábado (R$)" inputMode="decimal" erro={e.diariaSabado?.message} {...register('diariaSabado')} />
      </div>
      <Campo
        rotulo="Intervalo da agenda (minutos)"
        inputMode="numeric"
        ajuda="De quanto em quanto tempo o cliente pode escolher um horário."
        erro={e.intervaloAgendaMin?.message}
        {...register('intervaloAgendaMin')}
      />
      <Button type="submit" carregando={salvar.isPending}>
        Salvar
      </Button>
    </form>
  );
}

export function DadosLavaJato() {
  const query = useQuery({ queryKey: ['configuracao'], queryFn: obterConfiguracao });
  if (query.isPending) return <Carregando />;
  if (query.isError) {
    return (
      <div className="flex flex-col gap-sm">
        <Alerta>{interpretarErro(query.error).mensagem}</Alerta>
        <Button variante="contorno" onClick={() => void query.refetch()}>
          Tentar de novo
        </Button>
      </div>
    );
  }
  return <Formulario atual={query.data} />;
}
