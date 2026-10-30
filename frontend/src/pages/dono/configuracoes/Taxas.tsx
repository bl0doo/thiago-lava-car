import { zodResolver } from '@hookform/resolvers/zod';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useState } from 'react';
import { useForm } from 'react-hook-form';
import { alterarTaxa, listarTaxas, type Taxa } from '@/api/configuracoes';
import { interpretarErro } from '@/api/erros';
import { Alerta } from '@/components/ui/Alerta';
import { Button } from '@/components/ui/Button';
import { Campo } from '@/components/ui/Campo';
import { Carregando } from '@/components/ui/Carregando';
import { ErroConsulta } from '@/components/ui/ErroConsulta';
import { ROTULO_FORMA } from '@/schemas/comuns';
import { taxaSchema, type TaxaEntrada } from '@/schemas/configuracao';

const paraTexto = (n: number) => String(n).replace('.', ',');

function LinhaTaxa({ taxa }: { taxa: Taxa }) {
  const qc = useQueryClient();
  const [erro, setErro] = useState<string | null>(null);
  const [salva, setSalva] = useState(false);
  const { register, handleSubmit, formState } = useForm<TaxaEntrada, unknown, { percentual: number; valorFixo: number }>({
    resolver: zodResolver(taxaSchema),
    defaultValues: { percentual: paraTexto(taxa.percentual), valorFixo: paraTexto(taxa.valorFixo) },
  });
  const nome = ROTULO_FORMA[taxa.formaPagamento as keyof typeof ROTULO_FORMA] ?? taxa.formaPagamento;

  const salvar = useMutation({
    mutationFn: (d: { percentual: number; valorFixo: number }) => alterarTaxa(taxa.formaPagamento, d.percentual, d.valorFixo),
    onMutate: () => {
      setErro(null);
      setSalva(false);
    },
    onSuccess: () => {
      setSalva(true);
      void qc.invalidateQueries({ queryKey: ['taxas'] });
    },
    onError: (e) => setErro(interpretarErro(e).mensagem),
  });

  return (
    <form
      aria-label={`Taxa de ${nome}`}
      onSubmit={handleSubmit((d) => salvar.mutate(d))}
      noValidate
      className="flex flex-col gap-sm rounded-lg border-2 border-forte bg-superficie p-md shadow-seca"
    >
      <h3 className="texto-h2">{nome}</h3>
      {erro ? <Alerta>{erro}</Alerta> : null}
      {salva ? <Alerta tipo="sucesso">Taxa salva. Vale para os próximos pagamentos.</Alerta> : null}
      <div className="grid grid-cols-2 gap-sm">
        <Campo rotulo="Percentual (%)" inputMode="decimal" erro={formState.errors.percentual?.message} {...register('percentual')} />
        <Campo rotulo="Valor fixo (R$)" inputMode="decimal" erro={formState.errors.valorFixo?.message} {...register('valorFixo')} />
      </div>
      <Button type="submit" variante="contorno" carregando={salvar.isPending}>
        Salvar taxa de {nome}
      </Button>
    </form>
  );
}

export function Taxas() {
  const query = useQuery({ queryKey: ['taxas'], queryFn: listarTaxas });
  if (query.isPending) return <Carregando />;
  if (query.isError) {
    return (
      <ErroConsulta erro={query.error} aoTentarNovamente={query.refetch} />
    );
  }
  return (
    <div className="flex flex-col gap-md">
      <p className="texto-corpo-p text-secundario">A taxa de cada forma de pagamento vale só para os pagamentos registrados daqui em diante.</p>
      {query.data.map((t) => (
        <LinhaTaxa key={t.formaPagamento} taxa={t} />
      ))}
    </div>
  );
}
