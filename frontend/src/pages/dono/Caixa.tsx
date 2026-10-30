import { zodResolver } from '@hookform/resolvers/zod';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { Lock, LockOpen } from 'lucide-react';
import { useState } from 'react';
import { useForm, useWatch } from 'react-hook-form';
import { interpretarErro } from '@/api/erros';
import { fecharCaixa, listarFechamentos, obterResumoCaixa, reabrirCaixa, type CaixaResumo } from '@/api/caixa';
import { BarraTitulo } from '@/components/layout/BarraTitulo';
import { Alerta } from '@/components/ui/Alerta';
import { AreaTexto } from '@/components/ui/AreaTexto';
import { Button } from '@/components/ui/Button';
import { Campo } from '@/components/ui/Campo';
import { Carregando } from '@/components/ui/Carregando';
import { ErroConsulta } from '@/components/ui/ErroConsulta';
import { Dialogo } from '@/components/ui/Dialogo';
import { formatarDataIso, formatarMoeda, hojeIso, lerValor } from '@/lib/format';
import { fechamentoSchema, reaberturaSchema, type FechamentoEntrada } from '@/schemas/caixa';
import { FORMAS_PAGAMENTO, ROTULO_FORMA } from '@/schemas/comuns';

const calculadoDe = (resumo: CaixaResumo, forma: string) => resumo.porForma.find((f) => f.forma === forma)?.liquido ?? 0;
const paraTexto = (n: number) => n.toFixed(2).replace('.', ',');

function Total({ rotulo, valor, destaque = false }: { rotulo: string; valor: number; destaque?: boolean }) {
  return (
    <div className={`flex flex-col rounded-md border-2 p-sm ${destaque ? 'border-forte bg-rebaixado' : 'border-padrao'}`}>
      <span className="texto-corpo-p text-secundario">{rotulo}</span>
      <span className="texto-mono-dado text-[18px] font-bold">{formatarMoeda(valor)}</span>
    </div>
  );
}

function FormularioFechamento({ resumo, data, onErro }: { resumo: CaixaResumo; data: string; onErro: (m: string | null) => void }) {
  const qc = useQueryClient();
  const { register, handleSubmit, control, setError, formState } = useForm<FechamentoEntrada>({
    resolver: zodResolver(fechamentoSchema),
    defaultValues: {
      conferido: Object.fromEntries(FORMAS_PAGAMENTO.map((f) => [f, paraTexto(calculadoDe(resumo, f))])) as FechamentoEntrada['conferido'],
      justificativa: '',
    },
  });
  const digitado = useWatch({ control, name: 'conferido' });
  const diferencas = FORMAS_PAGAMENTO.map((f) => {
    const v = lerValor(digitado?.[f] ?? '');
    return Number.isNaN(v) ? 0 : v - calculadoDe(resumo, f);
  });
  const diverge = diferencas.some((d) => Math.abs(d) >= 0.005);

  const fechar = useMutation({
    mutationFn: (d: FechamentoEntrada) =>
      fecharCaixa(
        data,
        FORMAS_PAGAMENTO.map((f) => ({ forma: f, valor: lerValor(d.conferido[f]) })),
        d.justificativa.trim() || null,
      ),
    onMutate: () => onErro(null),
    onSuccess: () => {
      void qc.invalidateQueries({ queryKey: ['caixa'] });
    },
    onError: (e) => {
      const { campos, mensagem } = interpretarErro(e);
      if (campos.justificativa) setError('justificativa', { message: campos.justificativa });
      else onErro(mensagem);
    },
  });

  const enviar = handleSubmit((d) => {
    if (diverge && d.justificativa.trim().length < 10) {
      setError('justificativa', { message: 'Há diferença na conferência: explique em pelo menos 10 caracteres.' });
      return;
    }
    fechar.mutate(d);
  });

  return (
    <form noValidate onSubmit={enviar} className="flex flex-col gap-sm rounded-lg border-2 border-forte bg-superficie p-md shadow-seca">
      <h2 className="texto-h2">Conferência</h2>
      <p className="texto-corpo-p text-secundario">Digite o que você contou em cada forma (valor líquido, já sem taxas). Já vem preenchido com o calculado.</p>
      {FORMAS_PAGAMENTO.map((f, i) => (
        <div key={f} className="flex flex-col gap-2xs">
          <Campo
            rotulo={`${ROTULO_FORMA[f]} (calculado ${formatarMoeda(calculadoDe(resumo, f))})`}
            inputMode="decimal"
            erro={formState.errors.conferido?.[f]?.message}
            {...register(`conferido.${f}`)}
          />
          {Math.abs(diferencas[i] ?? 0) >= 0.005 ? (
            <p className="texto-rotulo-p text-aviso">
              Diferença de {(diferencas[i] ?? 0) > 0 ? '+' : '−'}
              {formatarMoeda(Math.abs(diferencas[i] ?? 0))}
            </p>
          ) : null}
        </div>
      ))}
      {diverge ? (
        <AreaTexto rotulo="Justificativa da diferença" maxLength={500} erro={formState.errors.justificativa?.message} {...register('justificativa')} />
      ) : null}
      <Button type="submit" carregando={fechar.isPending}>
        <Lock aria-hidden="true" className="size-5" />
        Fechar o caixa
      </Button>
    </form>
  );
}

export function Caixa() {
  const qc = useQueryClient();
  const hoje = hojeIso();
  const [data, setData] = useState(hoje);
  const [erro, setErro] = useState<string | null>(null);
  const [reabrindo, setReabrindo] = useState(false);
  const [justificativa, setJustificativa] = useState('');
  const [erroReabrir, setErroReabrir] = useState<string | null>(null);

  const resumo = useQuery({ queryKey: ['caixa', 'resumo', data], queryFn: () => obterResumoCaixa(data) });
  const fechado = resumo.data?.fechado ?? false;
  const fechamentos = useQuery({
    queryKey: ['caixa', 'fechamentos', data.slice(0, 7)],
    queryFn: () => listarFechamentos(data.slice(0, 7)),
    enabled: fechado,
  });
  const fechamento = fechamentos.data?.find((f) => f.data === data);

  const reabrir = useMutation({
    mutationFn: () => reabrirCaixa(data, justificativa),
    onSuccess: () => {
      setReabrindo(false);
      setJustificativa('');
      void qc.invalidateQueries({ queryKey: ['caixa'] });
    },
    onError: (e) => setErroReabrir(interpretarErro(e).mensagem),
  });

  const confirmarReabertura = () => {
    const r = reaberturaSchema.safeParse({ justificativa });
    if (!r.success) {
      setErroReabrir(r.error.issues[0]?.message ?? 'Informe a justificativa.');
      return;
    }
    setErroReabrir(null);
    reabrir.mutate();
  };

  return (
    <div className="flex flex-col">
      <BarraTitulo sobretitulo={formatarDataIso(data)} titulo="Caixa do dia" />
      <div className="flex flex-col gap-md px-md pb-lg lg:max-w-[40rem]">
        <Campo rotulo="Dia do caixa" type="date" max={hoje} value={data} onChange={(e) => {
            if (!e.target.value) return;
            setData(e.target.value);
            setErro(null);
          }} />

        {erro ? <Alerta>{erro}</Alerta> : null}
        {resumo.isPending ? <Carregando cartoes={2} /> : null}
        {resumo.isError ? (
          <ErroConsulta erro={resumo.error} aoTentarNovamente={resumo.refetch} />
        ) : null}

        {resumo.data ? (
          <>
            {resumo.data.osConcluidasSemPagamento > 0 ? (
              <Alerta tipo="atencao">
                {resumo.data.osConcluidasSemPagamento === 1
                  ? '1 OS concluída ainda não foi totalmente paga.'
                  : `${resumo.data.osConcluidasSemPagamento} OS concluídas ainda não foram totalmente pagas.`}
              </Alerta>
            ) : null}

            <section aria-label="Resumo do dia" className="grid grid-cols-2 gap-xs">
              <Total rotulo="Recebido (bruto)" valor={resumo.data.totalBruto} />
              <Total rotulo="Líquido (sem taxas)" valor={resumo.data.totalLiquido} />
              <Total rotulo="Despesas" valor={resumo.data.despesas} />
              <Total rotulo="Diárias" valor={resumo.data.diarias} />
              <div className="col-span-2">
                <Total rotulo="Sobra do dia" valor={resumo.data.totalLiquido - resumo.data.despesas - resumo.data.diarias} destaque />
              </div>
            </section>

            <section aria-label="Por forma de pagamento" className="flex flex-col gap-xs rounded-lg border-2 border-forte bg-superficie p-md shadow-seca">
              <h2 className="texto-h2">Por forma de pagamento</h2>
              {resumo.data.porForma.length === 0 ? (
                <p className="texto-corpo-m text-secundario">Nenhum pagamento neste dia.</p>
              ) : (
                <ul className="flex flex-col gap-2xs">
                  {resumo.data.porForma.map((f) => (
                    <li key={f.forma} className="flex flex-wrap justify-between gap-x-sm texto-corpo-m">
                      <span>
                        {ROTULO_FORMA[f.forma as keyof typeof ROTULO_FORMA] ?? f.forma} · {f.quantidade} {f.quantidade === 1 ? 'pagamento' : 'pagamentos'}
                      </span>
                      <span className="texto-mono-dado">
                        {formatarMoeda(f.liquido)} <span className="text-secundario">(taxas {formatarMoeda(f.taxas)})</span>
                      </span>
                    </li>
                  ))}
                </ul>
              )}
            </section>

            {fechado ? (
              <section aria-label="Caixa fechado" className="flex flex-col gap-sm rounded-lg border-2 border-forte bg-superficie p-md shadow-seca">
                <Alerta tipo="sucesso">Caixa fechado. Os pagamentos deste dia estão bloqueados.</Alerta>
                {fechamento ? (
                  <>
                    <p className="texto-corpo-m">
                      Conferido {formatarMoeda(fechamento.totalConferido)} · diferença {formatarMoeda(fechamento.diferenca)}
                    </p>
                    {fechamento.justificativa ? <p className="texto-corpo-m">Justificativa: {fechamento.justificativa}</p> : null}
                  </>
                ) : null}
                <Button variante="contorno" onClick={() => setReabrindo(true)}>
                  <LockOpen aria-hidden="true" className="size-5" />
                  Reabrir o caixa
                </Button>
              </section>
            ) : (
              <FormularioFechamento key={`${data}-${resumo.dataUpdatedAt}`} resumo={resumo.data} data={data} onErro={setErro} />
            )}
          </>
        ) : null}
      </div>

      <Dialogo
        aberto={reabrindo}
        titulo="Reabrir o caixa?"
        confirmar="Reabrir"
        tipo="perigo"
        carregando={reabrir.isPending}
        onConfirmar={confirmarReabertura}
        onFechar={() => {
          setReabrindo(false);
          setErroReabrir(null);
        }}
      >
        <div className="flex flex-col gap-sm">
          <p>Pagamentos e estornos deste dia voltam a ser permitidos. A reabertura fica na auditoria.</p>
          <AreaTexto rotulo="Justificativa" maxLength={500} value={justificativa} onChange={(e) => setJustificativa(e.target.value)} erro={erroReabrir ?? undefined} />
        </div>
      </Dialogo>
    </div>
  );
}
