import { zodResolver } from '@hookform/resolvers/zod';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useEffect, useState } from 'react';
import { useForm } from 'react-hook-form';
import { useNavigate, useParams } from 'react-router-dom';
import { interpretarErro } from '@/api/erros';
import { atualizarServico, criarServico, listarServicos, type PrecoServico, type ServicoEntrada } from '@/api/servicos';
import { BarraTitulo } from '@/components/layout/BarraTitulo';
import { Alerta } from '@/components/ui/Alerta';
import { Button } from '@/components/ui/Button';
import { Campo } from '@/components/ui/Campo';
import { Carregando } from '@/components/ui/Carregando';
import { lerValor } from '@/lib/format';
import { TIPOS_VEICULO } from '@/schemas/comuns';
import { servicoSchema, type ServicoFormEntrada, type ServicoFormSaida } from '@/schemas/servico';

const texto = (n: number | null | undefined) => (n === null || n === undefined ? '' : n.toFixed(2).replace('.', ','));

const VAZIO: ServicoFormEntrada = {
  nome: '',
  categoria: '',
  tempoEstimadoMin: '',
  custoInsumos: '',
  precos: TIPOS_VEICULO.map((tipoVeiculo) => ({ tipoVeiculo, preco: '', precoComCera: '' })),
};

/** Novo serviço e edição (formulários "Novo serviço"): um preço por tipo de veículo, cera opcional. */
export function ServicoForm() {
  const { id } = useParams();
  const editando = id !== undefined;
  const navegar = useNavigate();
  const qc = useQueryClient();
  const [erroGeral, setErroGeral] = useState<string | null>(null);
  const catalogo = useQuery({ queryKey: ['servicos'], queryFn: listarServicos, enabled: editando });
  const atual = catalogo.data?.find((s) => s.id === Number(id));

  const {
    register,
    handleSubmit,
    reset,
    setError,
    formState: { errors },
  } = useForm<ServicoFormEntrada, unknown, ServicoFormSaida>({ resolver: zodResolver(servicoSchema), defaultValues: VAZIO });

  useEffect(() => {
    if (!atual) return;
    reset({
      nome: atual.nome,
      categoria: atual.categoria,
      tempoEstimadoMin: String(atual.tempoEstimadoMin),
      custoInsumos: texto(atual.custoInsumos ?? 0),
      precos: TIPOS_VEICULO.map((tipoVeiculo) => {
        const p = atual.precos.find((x) => x.tipoVeiculo === tipoVeiculo);
        return { tipoVeiculo, preco: texto(p?.preco), precoComCera: texto(p?.precoComCera) };
      }),
    });
  }, [atual, reset]);

  const salvar = useMutation({
    mutationFn: (d: ServicoEntrada) => (editando ? atualizarServico(Number(id), d) : criarServico(d)),
    onSuccess: async () => {
      await qc.invalidateQueries({ queryKey: ['servicos'] });
      void navegar('/dono/servicos');
    },
    onError: (e) => {
      const erro = interpretarErro(e);
      let apontou = false;
      for (const campo of ['nome', 'categoria', 'tempoEstimadoMin', 'custoInsumos'] as const) {
        if (erro.campos[campo]) {
          setError(campo, { message: erro.campos[campo] });
          apontou = true;
        }
      }
      if (!apontou) setErroGeral(erro.mensagem);
    },
  });

  if (editando && catalogo.isPending) return <Carregando tela texto="Carregando o serviço…" />;
  if (editando && !atual) {
    return (
      <section className="flex flex-col">
        <BarraTitulo titulo="Serviço" onVoltar={() => void navegar('/dono/servicos')} />
        <div className="px-md">
          <Alerta>{catalogo.isError ? interpretarErro(catalogo.error).mensagem : 'Serviço não encontrado.'}</Alerta>
        </div>
      </section>
    );
  }

  const enviar = handleSubmit((v) => {
    setErroGeral(null);
    const precos: PrecoServico[] = v.precos
      .filter((p) => p.preco.trim() !== '')
      .map((p) => ({ tipoVeiculo: p.tipoVeiculo, preco: lerValor(p.preco), precoComCera: p.precoComCera.trim() ? lerValor(p.precoComCera) : null }));
    if (precos.length === 0) return setErroGeral('Informe o preço de ao menos um tipo de veículo.');
    salvar.mutate({ nome: v.nome, categoria: v.categoria, tempoEstimadoMin: Number(v.tempoEstimadoMin), custoInsumos: lerValor(v.custoInsumos), precos });
  });

  return (
    <section className="flex flex-col">
      <BarraTitulo sobretitulo="Serviços e preços" titulo={editando ? 'Editar serviço' : 'Novo serviço'} onVoltar={() => void navegar('/dono/servicos')} />
      <form onSubmit={(e) => void enviar(e)} noValidate className="mx-auto flex w-full max-w-[40rem] flex-col gap-md px-md">
        {erroGeral ? <Alerta>{erroGeral}</Alerta> : null}
        <Campo rotulo="Nome" erro={errors.nome?.message} {...register('nome')} />
        <Campo rotulo="Categoria" ajuda="Ex.: Lavagem, Polimento, Higienização." erro={errors.categoria?.message} {...register('categoria')} />
        <Campo rotulo="Tempo estimado (minutos)" inputMode="numeric" erro={errors.tempoEstimadoMin?.message} {...register('tempoEstimadoMin')} />
        <Campo rotulo="Custo de insumos (R$)" inputMode="decimal" ajuda="Produtos gastos em uma lavagem. Entra no cálculo do lucro." erro={errors.custoInsumos?.message} {...register('custoInsumos')} />
        <fieldset className="flex flex-col gap-sm">
          <legend className="texto-rotulo-m">Preço por tipo de veículo</legend>
          <p className="texto-corpo-p text-secundario">Deixe em branco o tipo que o serviço não atende.</p>
          {TIPOS_VEICULO.map((tipo, i) => (
            <div key={tipo} className="grid grid-cols-2 gap-xs rounded-lg border-2 border-padrao bg-superficie p-md">
              <input type="hidden" {...register(`precos.${i}.tipoVeiculo`)} />
              <Campo rotulo={`${tipo} (R$)`} inputMode="decimal" erro={errors.precos?.[i]?.preco?.message} {...register(`precos.${i}.preco`)} />
              <Campo rotulo="Com cera (R$)" inputMode="decimal" erro={errors.precos?.[i]?.precoComCera?.message} {...register(`precos.${i}.precoComCera`)} />
            </div>
          ))}
        </fieldset>
        <Button type="submit" carregando={salvar.isPending}>
          {salvar.isPending ? 'Salvando…' : 'Salvar serviço'}
        </Button>
      </form>
    </section>
  );
}
