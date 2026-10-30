import { useQuery } from '@tanstack/react-query';
import { useState } from 'react';
import { interpretarErro } from '@/api/erros';
import { listarServicos, type Servico } from '@/api/servicos';
import type { TipoVeiculo } from '@/api/veiculos';
import { Alerta } from '@/components/ui/Alerta';
import { Button } from '@/components/ui/Button';
import { Caixinha } from '@/components/ui/Caixinha';
import { Campo } from '@/components/ui/Campo';
import { Carregando } from '@/components/ui/Carregando';
import { formatarMinutos, formatarMoeda, lerValor } from '@/lib/format';

export type ItemEscolhido = { servicoId: number; comCera: boolean; precoManual: string };

/** Preço de tabela do serviço para o tipo do veículo (com ou sem cera); null quando não há preço para o tipo. */
export function precoDoItem(s: Servico, tipo: TipoVeiculo, comCera: boolean) {
  const p = s.precos.find((x) => x.tipoVeiculo === tipo);
  if (!p) return null;
  const preco = comCera ? p.precoComCera : p.preco;
  return preco == null ? null : { preco, minutos: s.tempoEstimadoMin };
}

export function resumo(servicos: Servico[], itens: ItemEscolhido[], tipo: TipoVeiculo) {
  let total = 0;
  let minutos = 0;
  for (const it of itens) {
    const s = servicos.find((x) => x.id === it.servicoId);
    const p = s ? precoDoItem(s, tipo, it.comCera) : null;
    if (!p) continue;
    const manual = lerValor(it.precoManual);
    total += !Number.isNaN(manual) && manual > 0 ? manual : p.preco;
    minutos += p.minutos;
  }
  return { total, minutos };
}

type Props = { tipo: TipoVeiculo; itens: ItemEscolhido[]; onMudar: (itens: ItemEscolhido[]) => void; onContinuar: () => void; onVoltar: () => void };

export function PassoServicos({ tipo, itens, onMudar, onContinuar, onVoltar }: Props) {
  const consulta = useQuery({ queryKey: ['servicos'], queryFn: listarServicos });
  const [erro, setErro] = useState<string | null>(null);

  if (consulta.isPending) return <Carregando texto="Carregando os serviços…" cartoes={3} />;
  if (consulta.isError) return <Alerta>{interpretarErro(consulta.error).mensagem}</Alerta>;

  const servicos = consulta.data.filter((s) => s.ativo);
  const { total, minutos } = resumo(servicos, itens, tipo);

  function alternar(s: Servico, marcado: boolean) {
    onMudar(marcado ? [...itens, { servicoId: s.id, comCera: false, precoManual: '' }] : itens.filter((i) => i.servicoId !== s.id));
  }
  const mudar = (id: number, parte: Partial<ItemEscolhido>) => onMudar(itens.map((i) => (i.servicoId === id ? { ...i, ...parte } : i)));

  function seguir() {
    if (itens.length === 0) return setErro('Escolha ao menos um serviço.');
    const invalido = itens.find((i) => i.precoManual.trim() !== '' && !(lerValor(i.precoManual) > 0));
    if (invalido) return setErro('O preço corrigido deve ser um valor em reais maior que zero.');
    setErro(null);
    onContinuar();
  }

  return (
    <div className="flex flex-col gap-md">
      <ul className="flex flex-col gap-sm">
        {servicos.map((s) => {
          const item = itens.find((i) => i.servicoId === s.id);
          const base = precoDoItem(s, tipo, false);
          const semPreco = base === null;
          return (
            <li key={s.id} className="flex flex-col rounded-lg border-2 border-forte bg-superficie px-md">
              <Caixinha checked={Boolean(item)} disabled={semPreco} onChange={(e) => alternar(s, e.target.checked)}>
                <span className="texto-rotulo-g">{s.nome}</span>{' '}
                <span className="texto-corpo-p text-secundario">
                  {semPreco ? `sem preço para ${tipo.toLowerCase()}` : `${formatarMoeda(base.preco)} · ${formatarMinutos(base.minutos)}`}
                </span>
              </Caixinha>
              {item ? (
                <div className="flex flex-col gap-xs pb-md pl-9">
                  {s.precos.find((p) => p.tipoVeiculo === tipo)?.precoComCera != null ? (
                    <Caixinha checked={item.comCera} onChange={(e) => mudar(s.id, { comCera: e.target.checked })}>
                      Com cera ({formatarMoeda(precoDoItem(s, tipo, true)?.preco ?? 0)})
                    </Caixinha>
                  ) : null}
                  <Campo rotulo="Corrigir preço (opcional)" inputMode="decimal" placeholder="Ex.: 90,00" value={item.precoManual} onChange={(e) => mudar(s.id, { precoManual: e.target.value })} />
                </div>
              ) : null}
            </li>
          );
        })}
      </ul>
      <div className="flex items-baseline justify-between rounded-lg bg-rebaixado p-md">
        <p className="texto-rotulo-m">Total · {formatarMinutos(minutos)}</p>
        <p className="texto-numero-m">{formatarMoeda(total)}</p>
      </div>
      {erro ? <Alerta>{erro}</Alerta> : null}
      <div className="flex flex-col gap-xs sm:flex-row-reverse">
        <Button onClick={seguir} className="flex-1">
          Continuar
        </Button>
        <Button variante="contorno" onClick={onVoltar} className="flex-1">
          Voltar
        </Button>
      </div>
    </div>
  );
}
