import { zodResolver } from '@hookform/resolvers/zod';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { CarFront, ChevronRight, Plus } from 'lucide-react';
import { useState } from 'react';
import { useForm } from 'react-hook-form';
import { Link } from 'react-router-dom';
import { listarMeusVeiculos } from '@/api/clientes';
import { interpretarErro } from '@/api/erros';
import { criarVeiculo } from '@/api/veiculos';
import { BarraTitulo } from '@/components/layout/BarraTitulo';
import { Alerta } from '@/components/ui/Alerta';
import { Button } from '@/components/ui/Button';
import { Campo } from '@/components/ui/Campo';
import { Carregando } from '@/components/ui/Carregando';
import { EstadoVazio } from '@/components/ui/EstadoVazio';
import { Placa } from '@/components/ui/Placa';
import { Select } from '@/components/ui/Select';
import { TIPOS_VEICULO } from '@/schemas/comuns';
import { meuVeiculoSchema, type MeuVeiculoEntrada, type MeuVeiculoSaida } from '@/schemas/meusDados';

export function MeusVeiculos() {
  const qc = useQueryClient();
  const consulta = useQuery({ queryKey: ['meus-veiculos'], queryFn: listarMeusVeiculos });
  const [abrindo, setAbrindo] = useState(false);
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
    onSuccess: async () => {
      await qc.invalidateQueries({ queryKey: ['meus-veiculos'] });
      reset();
      setAbrindo(false);
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
    <section className="flex flex-col">
      <BarraTitulo sobretitulo="Minha garagem" titulo="Veículos" />
      <div className="mx-auto flex w-full max-w-[40rem] flex-col gap-md px-md">
        {consulta.isPending ? <Carregando texto="Carregando seus veículos…" cartoes={2} /> : null}
        {consulta.isError ? <Alerta>{interpretarErro(consulta.error).mensagem}</Alerta> : null}
        {consulta.data?.length === 0 ? (
          <EstadoVazio Icone={CarFront} titulo="Nenhum carro cadastrado">
            Cadastre seu carro para agendar e acompanhar o histórico dele.
          </EstadoVazio>
        ) : null}
        <ul className="flex flex-col gap-xs">
          {consulta.data?.map((v) => (
            <li key={v.id}>
              <Link to={`/cliente/veiculos/${v.id}`} className="flex min-h-[66px] items-center gap-sm rounded-lg border-2 border-forte bg-superficie px-md py-xs hover:bg-rebaixado">
                <Placa placa={v.placa} />
                <span className="min-w-0 flex-1 truncate texto-corpo-m">{v.modelo}</span>
                <ChevronRight aria-hidden="true" className="size-6 shrink-0 text-secundario" />
              </Link>
            </li>
          ))}
        </ul>
        {abrindo ? (
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
              <Button variante="contorno" className="flex-1" onClick={() => setAbrindo(false)}>
                Cancelar
              </Button>
            </div>
          </form>
        ) : (
          <Button onClick={() => setAbrindo(true)}>
            <Plus aria-hidden="true" className="size-5" strokeWidth={2.5} />
            Cadastrar carro
          </Button>
        )}
      </div>
    </section>
  );
}
