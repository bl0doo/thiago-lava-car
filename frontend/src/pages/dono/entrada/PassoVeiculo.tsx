import { useMutation, useQuery } from '@tanstack/react-query';
import { zodResolver } from '@hookform/resolvers/zod';
import { CircleCheck } from 'lucide-react';
import { useState } from 'react';
import { useForm, useWatch } from 'react-hook-form';
import { Link } from 'react-router-dom';
import { listarClientes, obterCliente } from '@/api/clientes';
import { interpretarErro } from '@/api/erros';
import { buscarVeiculoPorPlaca, criarVeiculo, type Veiculo } from '@/api/veiculos';
import { Alerta } from '@/components/ui/Alerta';
import { Button } from '@/components/ui/Button';
import { Campo } from '@/components/ui/Campo';
import { Placa } from '@/components/ui/Placa';
import { Select } from '@/components/ui/Select';
import { formatarTelefone } from '@/lib/format';
import { useDebounce } from '@/lib/useDebounce';
import { TIPOS_VEICULO } from '@/schemas/comuns';
import { placaSchema, veiculoNovoSchema } from '@/schemas/entrada';

type Props = { onPronto: (v: Veiculo, nomeDono: string) => void };

type Etapa = { tipo: 'placa' } | { tipo: 'novo'; placa: string };

export function PassoVeiculo({ onPronto }: Props) {
  const [etapa, setEtapa] = useState<Etapa>({ tipo: 'placa' });
  const [texto, setTexto] = useState('');
  const [erroPlaca, setErroPlaca] = useState<string | undefined>();
  const [achado, setAchado] = useState<Veiculo | null>(null);
  const dono = useQuery({ queryKey: ['cliente', achado?.clienteId], queryFn: () => obterCliente(achado!.clienteId), enabled: achado !== null });

  const busca = useMutation({
    mutationFn: buscarVeiculoPorPlaca,
    onSuccess: (v, placa) => {
      if (v) setAchado(v);
      else setEtapa({ tipo: 'novo', placa });
    },
    onError: (e) => setErroPlaca(interpretarErro(e).mensagem),
  });

  function procurar() {
    setErroPlaca(undefined);
    setAchado(null);
    const r = placaSchema.safeParse(texto);
    if (!r.success) {
      setErroPlaca(r.error.issues[0]?.message);
      return;
    }
    busca.mutate(r.data);
  }

  if (etapa.tipo === 'novo') return <VeiculoNovo placa={etapa.placa} onVoltar={() => setEtapa({ tipo: 'placa' })} onPronto={onPronto} />;

  return (
    <form
      noValidate
      onSubmit={(e) => {
        e.preventDefault();
        procurar();
      }}
      className="flex flex-col gap-md"
    >
      <Campo rotulo="Placa do carro" value={texto} onChange={(e) => setTexto(e.target.value)} autoCapitalize="characters" autoComplete="off" maxLength={8} placeholder="ABC1D23" erro={erroPlaca} className="texto-mono-placa uppercase" />
      {achado ? (
        <div className="flex flex-col gap-sm rounded-lg border-2 border-forte bg-superficie p-md">
          <p className="flex items-center gap-xs texto-rotulo-m text-concluido">
            <CircleCheck aria-hidden="true" className="size-5" /> Veículo encontrado
          </p>
          <div className="flex items-center gap-sm">
            <Placa placa={achado.placa} />
            <p className="texto-corpo-m">
              {achado.modelo}
              <br />
              <span className="text-secundario">{dono.data ? `de ${dono.data.nome}` : 'carregando o dono…'}</span>
            </p>
          </div>
          <Button disabled={!dono.data} onClick={() => dono.data && onPronto(achado, dono.data.nome)}>Continuar com este veículo</Button>
        </div>
      ) : (
        <Button type="submit" carregando={busca.isPending}>
          {busca.isPending ? 'Procurando…' : 'Procurar placa'}
        </Button>
      )}
    </form>
  );
}

function VeiculoNovo({ placa, onVoltar, onPronto }: { placa: string; onVoltar: () => void; onPronto: (v: Veiculo, nomeDono: string) => void }) {
  const [buscaCliente, setBuscaCliente] = useState('');
  const termo = useDebounce(buscaCliente.trim());
  const clientes = useQuery({ queryKey: ['clientes', termo, 1], queryFn: () => listarClientes(termo, 1), enabled: termo.length >= 2 });
  const [erroGeral, setErroGeral] = useState<string | null>(null);
  const [nomeDono, setNomeDono] = useState('');

  const {
    register,
    handleSubmit,
    setValue,
    setError,
    control,
    formState: { errors },
  } = useForm({
    resolver: zodResolver(veiculoNovoSchema),
    defaultValues: { modeloDescricao: '', cor: '', tipoVeiculo: 'Carro' as const },
  });
  const clienteId = useWatch({ control, name: 'clienteId' });

  const criar = useMutation({
    mutationFn: criarVeiculo,
    onSuccess: (v) => onPronto(v, nomeDono),
    onError: (e) => {
      const erro = interpretarErro(e);
      for (const campo of ['clienteId', 'placa', 'modeloDescricao', 'tipoVeiculo', 'cor'] as const) {
        const m = erro.campos[campo];
        if (m && campo !== 'placa') setError(campo, { message: m });
      }
      setErroGeral(erro.campos.placa ?? (Object.keys(erro.campos).length ? null : erro.mensagem));
    },
  });

  const enviar = handleSubmit((v) => {
    setErroGeral(null);
    criar.mutate({
      clienteId: v.clienteId,
      placa,
      modeloDescricao: v.modeloDescricao,
      tipoVeiculo: v.tipoVeiculo,
      cor: v.cor || null,
    });
  });

  return (
    <form onSubmit={(e) => void enviar(e)} noValidate className="flex flex-col gap-md">
      <Alerta tipo="informacao">
        A placa <strong className="font-mono">{placa}</strong> ainda não está cadastrada. Preencha os dados do veículo.
      </Alerta>
      <fieldset className="flex flex-col gap-xs">
        <legend className="texto-rotulo-m">Dono do veículo</legend>
        <Campo rotulo="Buscar cliente" value={buscaCliente} onChange={(e) => setBuscaCliente(e.target.value)} placeholder="Nome ou telefone" autoComplete="off" erro={errors.clienteId?.message} />
        {clientes.data?.itens.length === 0 ? (
          <p className="texto-corpo-p text-secundario">
            Ninguém encontrado.{' '}
            <Link to="/dono/clientes/novo" className="underline underline-offset-4">
              Cadastrar cliente
            </Link>
          </p>
        ) : null}
        <ul className="flex flex-col gap-xs">
          {clientes.data?.itens.map((c) => (
            <li key={c.id}>
              <button
                type="button"
                aria-pressed={clienteId === c.id}
                onClick={() => {
                  setValue('clienteId', c.id, { shouldValidate: true });
                  setNomeDono(c.nome);
                }}
                className={`flex min-h-alvo w-full items-center justify-between rounded-md border-2 px-md text-left ${clienteId === c.id ? 'border-forte bg-acao-primaria text-sobre-destaque' : 'border-padrao bg-superficie'}`}
              >
                <span className="texto-rotulo-m">{c.nome}</span>
                <span className="texto-mono-dado">{formatarTelefone(c.telefone)}</span>
              </button>
            </li>
          ))}
        </ul>
      </fieldset>
      <Campo
        rotulo="Modelo"
        autoComplete="off"
        erro={errors.modeloDescricao?.message}
        {...register('modeloDescricao')}
      />
      <Select rotulo="Tipo" ajuda="Define o preço dos serviços." erro={errors.tipoVeiculo?.message} {...register('tipoVeiculo')}>
        {TIPOS_VEICULO.map((t) => (
          <option key={t} value={t}>
            {t === 'Caminhonete' ? 'Caminhonete / SUV' : t}
          </option>
        ))}
      </Select>
      <Campo rotulo="Cor (opcional)" erro={errors.cor?.message} {...register('cor')} />
      {erroGeral ? <Alerta>{erroGeral}</Alerta> : null}
      <div className="flex flex-col gap-xs sm:flex-row-reverse">
        <Button type="submit" carregando={criar.isPending} className="flex-1">
          Cadastrar veículo
        </Button>
        <Button variante="contorno" onClick={onVoltar} className="flex-1">
          Outra placa
        </Button>
      </div>
    </form>
  );
}
