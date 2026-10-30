import { zodResolver } from '@hookform/resolvers/zod';
import { Plus, Trash2 } from 'lucide-react';
import { useFieldArray, useForm, useWatch } from 'react-hook-form';
import { Alerta } from '@/components/ui/Alerta';
import { Button } from '@/components/ui/Button';
import { Caixinha } from '@/components/ui/Caixinha';
import { Campo } from '@/components/ui/Campo';
import { AreaTexto } from '@/components/ui/AreaTexto';
import { Select } from '@/components/ui/Select';
import { LOCAIS_AVARIA, TIPOS_AVARIA, vistoriaSchema, type VistoriaEntrada, type VistoriaSaida } from '@/schemas/entrada';

type Props = { enviando: boolean; erro: string | null; onVoltar: () => void; onConcluir: (v: VistoriaSaida) => void };

/** Passo 3: vistoria de entrada (avarias, pertences, quem retira) e aceite do cliente. */
export function PassoVistoria({ enviando, erro, onVoltar, onConcluir }: Props) {
  const {
    register,
    handleSubmit,
    control,
    formState: { errors },
  } = useForm<VistoriaEntrada, unknown, VistoriaSaida>({
    resolver: zodResolver(vistoriaSchema),
    defaultValues: { semAvarias: false, avarias: [], pertencesEntrada: '', nomeQuemRetira: '', aceiteCliente: false },
  });
  const { fields, append, remove } = useFieldArray({ control, name: 'avarias' });
  const semAvarias = useWatch({ control, name: 'semAvarias' });

  return (
    <form onSubmit={(e) => void handleSubmit(onConcluir)(e)} noValidate className="flex flex-col gap-md">
      <Caixinha {...register('semAvarias')}>Sem avarias (o carro está sem riscos ou amassados)</Caixinha>
      {!semAvarias ? (
        <fieldset className="flex flex-col gap-sm">
          <legend className="texto-rotulo-m">Avarias anotadas</legend>
          {fields.map((f, i) => (
            <div key={f.id} className="flex flex-col gap-xs rounded-lg border-2 border-padrao bg-superficie p-md">
              <Select rotulo="Onde" erro={errors.avarias?.[i]?.local?.message} {...register(`avarias.${i}.local`)}>
                <option value="">Escolha…</option>
                {LOCAIS_AVARIA.map((l) => (
                  <option key={l}>{l}</option>
                ))}
              </Select>
              <Select rotulo="Tipo" erro={errors.avarias?.[i]?.tipo?.message} {...register(`avarias.${i}.tipo`)}>
                <option value="">Escolha…</option>
                {TIPOS_AVARIA.map((t) => (
                  <option key={t}>{t}</option>
                ))}
              </Select>
              <Campo rotulo="Detalhe (opcional)" maxLength={500} erro={errors.avarias?.[i]?.descricao?.message} {...register(`avarias.${i}.descricao`)} />
              <Button variante="contorno" onClick={() => remove(i)}>
                <Trash2 aria-hidden="true" className="size-5" />
                Remover avaria
              </Button>
            </div>
          ))}
          {errors.avarias?.message ? <Alerta>{errors.avarias.message}</Alerta> : null}
          <Button variante="contorno" onClick={() => append({ local: '', tipo: '', descricao: '' })}>
            <Plus aria-hidden="true" className="size-5" />
            Outra avaria
          </Button>
        </fieldset>
      ) : null}
      <AreaTexto rotulo="Pertences deixados no carro (opcional)" maxLength={500} erro={errors.pertencesEntrada?.message} {...register('pertencesEntrada')} />
      <Campo rotulo="Quem vai retirar o carro (opcional)" maxLength={100} erro={errors.nomeQuemRetira?.message} {...register('nomeQuemRetira')} />
      <Caixinha erro={errors.aceiteCliente?.message} {...register('aceiteCliente')}>
        O cliente viu a vistoria e concorda com o estado do carro na entrada.
      </Caixinha>
      {erro ? <Alerta>{erro}</Alerta> : null}
      <div className="flex flex-col gap-xs sm:flex-row-reverse">
        <Button type="submit" carregando={enviando} className="flex-1">
          {enviando ? 'Registrando…' : 'Registrar entrada'}
        </Button>
        <Button variante="contorno" onClick={onVoltar} className="flex-1">
          Voltar
        </Button>
      </div>
    </form>
  );
}
