import { zodResolver } from '@hookform/resolvers/zod';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { Receipt } from 'lucide-react';
import { useState } from 'react';
import { useForm } from 'react-hook-form';
import { interpretarErro } from '@/api/erros';
import {
  CATEGORIAS_DESPESA,
  criarDespesa,
  editarDespesa,
  excluirDespesa,
  excluirRetirada,
  listarDespesas,
  listarRetiradas,
  registrarRetirada,
  type Despesa,
  type Retirada,
} from '@/api/financeiro';
import { BarraTitulo } from '@/components/layout/BarraTitulo';
import { Abas } from '@/components/ui/Abas';
import { Alerta } from '@/components/ui/Alerta';
import { AreaTexto } from '@/components/ui/AreaTexto';
import { Button } from '@/components/ui/Button';
import { Campo } from '@/components/ui/Campo';
import { Carregando } from '@/components/ui/Carregando';
import { Dialogo } from '@/components/ui/Dialogo';
import { EstadoVazio } from '@/components/ui/EstadoVazio';
import { Select } from '@/components/ui/Select';
import { formatarDataIso, formatarMoeda, hojeIso } from '@/lib/format';
import {
  despesaSchema,
  retiradaSchema,
  ROTULO_CATEGORIA,
  type DespesaFormEntrada,
  type DespesaFormSaida,
  type RetiradaFormEntrada,
  type RetiradaFormSaida,
} from '@/schemas/despesa';

type Aba = 'despesas' | 'retiradas';

const paraTexto = (n: number) => String(n).replace('.', ',');
const rotuloCategoria = (c: string) => ROTULO_CATEGORIA[c as keyof typeof ROTULO_CATEGORIA] ?? c;

function FormDespesa({ editando, onPronto, onErro }: { editando: Despesa | null; onPronto: () => void; onErro: (m: string | null) => void }) {
  const qc = useQueryClient();
  const { register, handleSubmit, reset, setError, formState } = useForm<DespesaFormEntrada, unknown, DespesaFormSaida>({
    resolver: zodResolver(despesaSchema),
    defaultValues: editando
      ? { descricao: editando.descricao, categoria: editando.categoria as DespesaFormEntrada['categoria'], valor: paraTexto(editando.valor), data: editando.data }
      : { descricao: '', categoria: 'Insumos', valor: '', data: hojeIso() },
  });

  const salvar = useMutation({
    mutationFn: (d: DespesaFormSaida) => (editando ? editarDespesa(editando.id, d) : criarDespesa(d)),
    onMutate: () => onErro(null),
    onSuccess: () => {
      void qc.invalidateQueries({ queryKey: ['despesas'] });
      void qc.invalidateQueries({ queryKey: ['caixa'] });
      reset({ descricao: '', categoria: 'Insumos', valor: '', data: hojeIso() });
      onPronto();
    },
    onError: (e) => {
      const { campos, mensagem } = interpretarErro(e);
      const conhecidos = ['descricao', 'categoria', 'valor', 'data'] as const;
      const achou = conhecidos.filter((c) => campos[c]);
      if (achou.length === 0) onErro(mensagem);
      for (const c of achou) setError(c, { message: campos[c] });
    },
  });

  return (
    <form noValidate onSubmit={handleSubmit((d) => salvar.mutate(d))} className="flex flex-col gap-sm rounded-lg border-2 border-forte bg-superficie p-md shadow-seca">
      <h2 className="texto-h2">{editando ? 'Editar despesa' : 'Nova despesa'}</h2>
      <Campo rotulo="Descrição" maxLength={200} erro={formState.errors.descricao?.message} {...register('descricao')} />
      <Select rotulo="Categoria" erro={formState.errors.categoria?.message} {...register('categoria')}>
        {CATEGORIAS_DESPESA.map((c) => (
          <option key={c} value={c}>
            {ROTULO_CATEGORIA[c]}
          </option>
        ))}
      </Select>
      <Campo rotulo="Valor" inputMode="decimal" erro={formState.errors.valor?.message} {...register('valor')} />
      <Campo rotulo="Data" type="date" max={hojeIso()} erro={formState.errors.data?.message} {...register('data')} />
      <div className="flex flex-col gap-xs sm:flex-row">
        <Button type="submit" carregando={salvar.isPending} className="flex-1">
          {editando ? 'Salvar alterações' : 'Adicionar despesa'}
        </Button>
        {editando ? (
          <Button variante="contorno" onClick={onPronto}>
            Cancelar edição
          </Button>
        ) : null}
      </div>
    </form>
  );
}

function FormRetirada({ onErro }: { onErro: (m: string | null) => void }) {
  const qc = useQueryClient();
  const { register, handleSubmit, reset, setError, formState } = useForm<RetiradaFormEntrada, unknown, RetiradaFormSaida>({
    resolver: zodResolver(retiradaSchema),
    defaultValues: { valor: '', data: hojeIso(), observacao: '' },
  });

  const salvar = useMutation({
    mutationFn: (d: RetiradaFormSaida) => registrarRetirada({ valor: d.valor, data: d.data, observacao: d.observacao || null }),
    onMutate: () => onErro(null),
    onSuccess: () => {
      void qc.invalidateQueries({ queryKey: ['retiradas'] });
      reset({ valor: '', data: hojeIso(), observacao: '' });
    },
    onError: (e) => {
      const { campos, mensagem } = interpretarErro(e);
      const achou = (['valor', 'data', 'observacao'] as const).filter((c) => campos[c]);
      if (achou.length === 0) onErro(mensagem);
      for (const c of achou) setError(c, { message: campos[c] });
    },
  });

  return (
    <form noValidate onSubmit={handleSubmit((d) => salvar.mutate(d))} className="flex flex-col gap-sm rounded-lg border-2 border-forte bg-superficie p-md shadow-seca">
      <h2 className="texto-h2">Nova retirada</h2>
      <p className="texto-corpo-p text-secundario">Dinheiro que o dono tira do caixa. Não entra no custo do negócio nem no lucro por serviço.</p>
      <Campo rotulo="Valor" inputMode="decimal" erro={formState.errors.valor?.message} {...register('valor')} />
      <Campo rotulo="Data" type="date" max={hojeIso()} erro={formState.errors.data?.message} {...register('data')} />
      <AreaTexto rotulo="Observação (opcional)" maxLength={200} erro={formState.errors.observacao?.message} {...register('observacao')} />
      <Button type="submit" carregando={salvar.isPending}>
        Registrar retirada
      </Button>
    </form>
  );
}

/** Despesas da empresa e retiradas do dono (`/dono/despesas`): duas abas separadas, com total do mês. */
export function Despesas() {
  const qc = useQueryClient();
  const [aba, setAba] = useState<Aba>('despesas');
  const [mes, setMes] = useState(hojeIso().slice(0, 7));
  const [erro, setErro] = useState<string | null>(null);
  const [editando, setEditando] = useState<Despesa | null>(null);
  const [excluindoDespesa, setExcluindoDespesa] = useState<Despesa | null>(null);
  const [excluindoRetirada, setExcluindoRetirada] = useState<Retirada | null>(null);
  const de = `${mes}-01`;

  const despesas = useQuery({ queryKey: ['despesas', de], queryFn: () => listarDespesas(de), enabled: aba === 'despesas' });
  const retiradas = useQuery({ queryKey: ['retiradas', de], queryFn: () => listarRetiradas(de), enabled: aba === 'retiradas' });

  const excluir = useMutation({
    mutationFn: async () => {
      if (excluindoDespesa) await excluirDespesa(excluindoDespesa.id);
      if (excluindoRetirada) await excluirRetirada(excluindoRetirada.id);
    },
    onSuccess: () => {
      void qc.invalidateQueries({ queryKey: ['despesas'] });
      void qc.invalidateQueries({ queryKey: ['retiradas'] });
      void qc.invalidateQueries({ queryKey: ['caixa'] });
    },
    onError: (e) => setErro(interpretarErro(e).mensagem),
    onSettled: () => {
      setExcluindoDespesa(null);
      setExcluindoRetirada(null);
    },
  });

  const consulta = aba === 'despesas' ? despesas : retiradas;
  const total = aba === 'despesas' ? despesas.data?.total : retiradas.data?.total;

  return (
    <div className="flex flex-col">
      <BarraTitulo sobretitulo="Financeiro" titulo="Despesas e retiradas" />
      <div className="flex flex-col gap-md px-md pb-lg lg:max-w-[40rem]">
        <Abas
          rotulo="Tipo de lançamento"
          ativa={aba}
          onMudar={(a) => {
            setAba(a);
            setErro(null);
          }}
          itens={[
            { id: 'despesas', rotulo: 'Despesas' },
            { id: 'retiradas', rotulo: 'Retiradas' },
          ]}
        />

        {erro ? <Alerta>{erro}</Alerta> : null}

        {aba === 'despesas' ? (
          <FormDespesa key={editando?.id ?? 'nova'} editando={editando} onPronto={() => setEditando(null)} onErro={setErro} />
        ) : (
          <FormRetirada onErro={setErro} />
        )}

        <Campo rotulo="Mês" type="month" max={hojeIso().slice(0, 7)} value={mes} onChange={(e) => e.target.value && setMes(e.target.value)} />

        {consulta.isPending ? <Carregando cartoes={2} /> : null}
        {consulta.isError ? (
          <>
            <Alerta>{interpretarErro(consulta.error).mensagem}</Alerta>
            <Button variante="contorno" onClick={() => void consulta.refetch()}>
              Tentar de novo
            </Button>
          </>
        ) : null}

        {total !== undefined ? (
          <p className="flex justify-between rounded-md border-2 border-forte bg-rebaixado px-md py-sm texto-rotulo-m">
            <span>{aba === 'despesas' ? 'Total de despesas' : 'Total de retiradas'}</span>
            <span className="texto-mono-dado">{formatarMoeda(total)}</span>
          </p>
        ) : null}

        {aba === 'despesas' && despesas.data ? (
          despesas.data.itens.length === 0 ? (
            <EstadoVazio Icone={Receipt} titulo="Nenhuma despesa neste mês">
              Use o formulário acima para lançar a primeira.
            </EstadoVazio>
          ) : (
            <ul aria-label="Despesas do mês" className="flex flex-col gap-xs">
              {despesas.data.itens.map((d) => (
                <li key={d.id} className="flex flex-wrap items-center justify-between gap-xs rounded-md border-2 border-padrao bg-superficie p-sm">
                  <div className="flex min-w-0 flex-col">
                    <span className="texto-rotulo-m">{d.descricao}</span>
                    <span className="texto-corpo-p text-secundario">
                      {rotuloCategoria(d.categoria)} · {formatarDataIso(d.data)}
                    </span>
                  </div>
                  <span className="texto-mono-dado">{formatarMoeda(d.valor)}</span>
                  <div className="flex gap-xs">
                    <Button variante="contorno" aria-label={`Editar ${d.descricao}`} onClick={() => setEditando(d)}>
                      Editar
                    </Button>
                    <Button variante="contorno" aria-label={`Excluir ${d.descricao}`} onClick={() => setExcluindoDespesa(d)}>
                      Excluir
                    </Button>
                  </div>
                </li>
              ))}
            </ul>
          )
        ) : null}

        {aba === 'retiradas' && retiradas.data ? (
          retiradas.data.itens.length === 0 ? (
            <EstadoVazio Icone={Receipt} titulo="Nenhuma retirada neste mês">
              Registre aqui o dinheiro que você tirar do caixa.
            </EstadoVazio>
          ) : (
            <ul aria-label="Retiradas do mês" className="flex flex-col gap-xs">
              {retiradas.data.itens.map((r) => (
                <li key={r.id} className="flex flex-wrap items-center justify-between gap-xs rounded-md border-2 border-padrao bg-superficie p-sm">
                  <div className="flex min-w-0 flex-col">
                    <span className="texto-rotulo-m">{formatarDataIso(r.data)}</span>
                    {r.observacao ? <span className="texto-corpo-p text-secundario">{r.observacao}</span> : null}
                  </div>
                  <span className="texto-mono-dado">{formatarMoeda(r.valor)}</span>
                  <Button variante="contorno" aria-label={`Excluir retirada de ${formatarMoeda(r.valor)}`} onClick={() => setExcluindoRetirada(r)}>
                    Excluir
                  </Button>
                </li>
              ))}
            </ul>
          )
        ) : null}
      </div>

      <Dialogo
        aberto={excluindoDespesa !== null || excluindoRetirada !== null}
        titulo={excluindoDespesa ? 'Excluir esta despesa?' : 'Excluir esta retirada?'}
        confirmar="Excluir"
        tipo="perigo"
        carregando={excluir.isPending}
        onConfirmar={() => excluir.mutate()}
        onFechar={() => {
          setExcluindoDespesa(null);
          setExcluindoRetirada(null);
        }}
      >
        O lançamento sai da lista e dos totais.
      </Dialogo>
    </div>
  );
}
