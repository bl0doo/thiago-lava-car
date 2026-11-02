import { zodResolver } from '@hookform/resolvers/zod';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { useState } from 'react';
import { useForm } from 'react-hook-form';
import { interpretarErro } from '@/api/erros';
import { criarVeiculo, type Veiculo } from '@/api/veiculos';
import { Alerta } from '@/components/ui/Alerta';
import { Button } from '@/components/ui/Button';
import { Campo } from '@/components/ui/Campo';
import { Select } from '@/components/ui/Select';
import { TIPOS_VEICULO } from '@/schemas/comuns';
import { meuVeiculoSchema, type MeuVeiculoEntrada, type MeuVeiculoSaida } from '@/schemas/meusDados';

/** Cadastro de carro do próprio cliente; usado em Agendar (sem sair do fluxo) e em Minha conta. */
export function FormNovoVeiculo({ onCriado, onCancelar }: { onCriado?: (veiculo: Veiculo) => void; onCancelar?: () => void }) {
  const qc = useQueryClient();
  const [erroGeral, setErroGeral] = useState<string | null>(null);
  const {
    register,
    handleSubmit,
    reset,
    setError,
    formState: { errors },
  } = useForm<MeuVeiculoEntrada, unknown, MeuVeiculoSaida>({ resolver: zodResolver(meuVeiculoSchema), defaultValues: { tipoVeiculo: 'Carro', cor: '' } });

  const criar = useMutation({
    mutationFn: (v: MeuVeiculoSaida) => criarVeiculo({ placa: v.placa, modeloDescricao: v.modeloDescricao, tipoVeiculo: v.tipoVeiculo, cor: v.cor || null }),
    onSuccess: async (veiculo) => {
      await qc.invalidateQueries({ queryKey: ['meus-veiculos'] });
      reset();
      onCriado?.(veiculo);
    },
    onError: (e) => {
      const erro = interpretarErro(e);
      let apontou = false;
      for (const campo of ['placa', 'modeloDescricao', 'tipoVeiculo', 'cor'] as const) {
        if (erro.campos[campo]) {
          setError(campo, { message: erro.campos[campo] });
          apontou = true;
        }
      }
      if (!apontou) setErroGeral(erro.mensagem);
    },
  });

  return (
    <form
      noValidate
      className="flex flex-col gap-md rounded-lg border-2 border-forte bg-superficie p-md"
      onSubmit={(e) =>
        void handleSubmit((v) => {
          setErroGeral(null);
          criar.mutate(v);
        })(e)
      }
    >
      <h2 className="texto-h2">Cadastrar carro</h2>
      {erroGeral ? <Alerta>{erroGeral}</Alerta> : null}
      <Campo rotulo="Placa" autoCapitalize="characters" autoComplete="off" maxLength={8} placeholder="ABC1D23" erro={errors.placa?.message} {...register('placa')} />
      <Campo rotulo="Modelo" erro={errors.modeloDescricao?.message} {...register('modeloDescricao')} />
      <Select rotulo="Tipo" erro={errors.tipoVeiculo?.message} {...register('tipoVeiculo')}>
        {TIPOS_VEICULO.map((t) => (
          <option key={t} value={t}>
            {t === 'Caminhonete' ? 'Caminhonete / SUV' : t}
          </option>
        ))}
      </Select>
      <Campo rotulo="Cor (opcional)" erro={errors.cor?.message} {...register('cor')} />
      <div className="flex flex-col gap-xs sm:flex-row-reverse">
        <Button type="submit" carregando={criar.isPending} className="flex-1">
          Cadastrar
        </Button>
        {onCancelar ? (
          <Button variante="contorno" className="flex-1" onClick={onCancelar}>
            Cancelar
          </Button>
        ) : null}
      </div>
    </form>
  );
}
