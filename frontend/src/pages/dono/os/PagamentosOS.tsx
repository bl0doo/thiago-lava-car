import { zodResolver } from '@hookform/resolvers/zod';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { useState } from 'react';
import { useForm } from 'react-hook-form';
import { interpretarErro } from '@/api/erros';
import type { PagamentoDaOS } from '@/api/ordensServico';
import { estornarPagamento, registrarPagamento } from '@/api/pagamentos';
import { Alerta } from '@/components/ui/Alerta';
import { Button } from '@/components/ui/Button';
import { Campo } from '@/components/ui/Campo';
import { Dialogo } from '@/components/ui/Dialogo';
import { Select } from '@/components/ui/Select';
import { formatarDataIso, formatarMoeda } from '@/lib/format';
import { FORMAS_PAGAMENTO, ROTULO_FORMA } from '@/schemas/comuns';
import { pagamentoSchema, type PagamentoEntrada, type PagamentoSaida } from '@/schemas/pagamento';

type Props = { osId: number; pagamentos: PagamentoDaOS[]; saldoPendente: number };

const rotuloForma = (f: string) => ROTULO_FORMA[f as keyof typeof ROTULO_FORMA] ?? f;

/**
 * Recebimento da OS: lista os pagamentos (dividir em várias formas é registrar mais de um), mostra a taxa descontada
 * e deixa estornar. O valor já vem preenchido com o saldo pendente.
 */
export function PagamentosOS({ osId, pagamentos, saldoPendente }: Props) {
  const qc = useQueryClient();
  const [erro, setErro] = useState<string | null>(null);
  const [estornando, setEstornando] = useState<PagamentoDaOS | null>(null);
  const [motivo, setMotivo] = useState('');

  const { register, handleSubmit, reset, setError, formState } = useForm<PagamentoEntrada, unknown, PagamentoSaida>({
    resolver: zodResolver(pagamentoSchema),
    defaultValues: { formaPagamento: 'Pix', valor: saldoPendente > 0 ? String(saldoPendente).replace('.', ',') : '' },
  });

  const atualizar = () => {
    void qc.invalidateQueries({ queryKey: ['os', osId] });
    void qc.invalidateQueries({ queryKey: ['caixa'] });
  };

  const registrar = useMutation({
    mutationFn: (d: PagamentoSaida) => registrarPagamento(osId, d.formaPagamento, d.valor),
    onSuccess: (r) => {
      setErro(null);
      reset({ formaPagamento: 'Pix', valor: r.saldoPendente > 0 ? String(r.saldoPendente).replace('.', ',') : '' });
      atualizar();
    },
    onError: (e) => {
      const { campos, mensagem } = interpretarErro(e);
      if (campos.valorBruto) setError('valor', { message: campos.valorBruto });
      else if (campos.formaPagamento) setError('formaPagamento', { message: campos.formaPagamento });
      else setErro(mensagem);
    },
  });

  const estornar = useMutation({
    mutationFn: (p: PagamentoDaOS) => estornarPagamento(p.id, motivo.trim() || null),
    onSuccess: () => {
      setErro(null);
      setEstornando(null);
      setMotivo('');
      atualizar();
    },
    onError: (e) => {
      setErro(interpretarErro(e).mensagem);
      setEstornando(null);
    },
  });

  return (
    <div className="flex flex-col gap-sm">
      {erro ? <Alerta>{erro}</Alerta> : null}

      {pagamentos.length === 0 ? (
        <p className="texto-corpo-m text-secundario">Nenhum pagamento registrado.</p>
      ) : (
        <ul className="flex flex-col gap-xs">
          {pagamentos.map((p) => (
            <li key={p.id} className="flex flex-wrap items-center justify-between gap-xs rounded-md border-2 border-padrao p-sm">
              <div className="flex flex-col">
                <span className={`texto-rotulo-m ${p.estornado ? 'line-through' : ''}`}>
                  {rotuloForma(p.formaPagamento)} · {formatarMoeda(p.valorBruto)}
                </span>
                <span className="texto-corpo-p text-secundario">
                  {p.estornado ? 'Estornado · ' : ''}Líquido {formatarMoeda(p.valorLiquido)} (taxa {formatarMoeda(p.valorTaxa)}) · caixa de {formatarDataIso(p.dataCaixa)}
                </span>
              </div>
              {p.estornado ? null : (
                <Button variante="contorno" aria-label={`Estornar ${rotuloForma(p.formaPagamento)} ${formatarMoeda(p.valorBruto)}`} onClick={() => setEstornando(p)}>
                  Estornar
                </Button>
              )}
            </li>
          ))}
        </ul>
      )}

      {saldoPendente > 0 ? (
        <form noValidate onSubmit={handleSubmit((d) => registrar.mutate(d))} className="flex flex-col gap-sm border-t-2 border-padrao pt-sm">
          <p className="texto-rotulo-m">Falta receber {formatarMoeda(saldoPendente)}</p>
          <Select rotulo="Forma de pagamento" erro={formState.errors.formaPagamento?.message} {...register('formaPagamento')}>
            {FORMAS_PAGAMENTO.map((f) => (
              <option key={f} value={f}>
                {ROTULO_FORMA[f]}
              </option>
            ))}
          </Select>
          <Campo rotulo="Valor recebido" inputMode="decimal" erro={formState.errors.valor?.message} ajuda="Para dividir, receba uma parte aqui e o resto em outra forma." {...register('valor')} />
          <Button type="submit" carregando={registrar.isPending}>
            Registrar pagamento
          </Button>
        </form>
      ) : (
        <Alerta tipo="sucesso">Pago por completo.</Alerta>
      )}

      <Dialogo
        aberto={estornando !== null}
        titulo="Estornar este pagamento?"
        confirmar="Estornar"
        tipo="perigo"
        carregando={estornar.isPending}
        onConfirmar={() => estornando && estornar.mutate(estornando)}
        onFechar={() => setEstornando(null)}
      >
        <div className="flex flex-col gap-sm">
          <p>O valor volta para o saldo da OS e o estorno fica registrado na auditoria.</p>
          <Campo rotulo="Motivo (opcional)" maxLength={200} value={motivo} onChange={(e) => setMotivo(e.target.value)} />
        </div>
      </Dialogo>
    </div>
  );
}
