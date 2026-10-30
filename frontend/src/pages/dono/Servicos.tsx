import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { Plus, Tags } from 'lucide-react';
import { Link } from 'react-router-dom';
import { interpretarErro } from '@/api/erros';
import { alterarAtivoServico, listarServicos } from '@/api/servicos';
import { BarraTitulo } from '@/components/layout/BarraTitulo';
import { Alerta } from '@/components/ui/Alerta';
import { Button, classesBotao } from '@/components/ui/Button';
import { Carregando } from '@/components/ui/Carregando';
import { EstadoVazio } from '@/components/ui/EstadoVazio';
import { formatarMinutos, formatarMoeda } from '@/lib/format';

/** Serviços e preços (tela 15): catálogo com preço por tipo de veículo, tempo e custo de insumos; ativar e desativar. */
export function Servicos() {
  const qc = useQueryClient();
  const consulta = useQuery({ queryKey: ['servicos'], queryFn: listarServicos });
  const alternar = useMutation({
    mutationFn: ({ id, ativo }: { id: number; ativo: boolean }) => alterarAtivoServico(id, ativo),
    onSettled: () => qc.invalidateQueries({ queryKey: ['servicos'] }),
  });

  const novo = (
    <Link to="/dono/servicos/novo" className={classesBotao('primario')}>
      <Plus aria-hidden="true" className="size-5" strokeWidth={2.5} />
      Novo serviço
    </Link>
  );

  return (
    <section className="flex flex-col">
      <BarraTitulo sobretitulo="Gestão" titulo="Serviços e preços" acao={<div className="hidden sm:block">{novo}</div>} />
      <div className="flex flex-col gap-md px-md">
        {consulta.isPending ? <Carregando texto="Carregando serviços…" cartoes={3} /> : null}
        {consulta.isError ? <Alerta>{interpretarErro(consulta.error).mensagem}</Alerta> : null}
        {alternar.isError ? <Alerta>{interpretarErro(alternar.error).mensagem}</Alerta> : null}
        {consulta.data?.length === 0 ? (
          <EstadoVazio Icone={Tags} titulo="Nenhum serviço cadastrado">
            Cadastre o primeiro serviço para poder registrar entradas.
          </EstadoVazio>
        ) : null}
        <ul className="flex flex-col gap-sm">
          {consulta.data?.map((s) => (
            <li key={s.id} className={`flex flex-col gap-xs rounded-lg border-2 p-md ${s.ativo ? 'border-forte bg-superficie' : 'border-padrao bg-rebaixado'}`}>
              <div className="flex items-start justify-between gap-sm">
                <div>
                  <p className="texto-rotulo-g">{s.nome}</p>
                  <p className="texto-corpo-p text-secundario">
                    {s.categoria} · {formatarMinutos(s.tempoEstimadoMin)}
                    {s.custoInsumos !== undefined ? ` · insumos ${formatarMoeda(s.custoInsumos)}` : ''}
                  </p>
                </div>
                <span className="texto-rotulo-p text-secundario">{s.ativo ? 'Ativo' : 'Desativado'}</span>
              </div>
              <ul className="flex flex-wrap gap-xs">
                {s.precos.map((p) => (
                  <li key={p.tipoVeiculo} className="rounded-sm bg-rebaixado px-xs py-2xs texto-mono-dado">
                    {p.tipoVeiculo}: {formatarMoeda(p.preco)}
                    {p.precoComCera !== null ? ` · cera ${formatarMoeda(p.precoComCera)}` : ''}
                  </li>
                ))}
              </ul>
              <div className="flex gap-xs">
                <Link to={`/dono/servicos/${s.id}`} className={`${classesBotao('contorno')} flex-1`}>
                  Editar
                </Link>
                <Button variante="contorno" className="flex-1" disabled={alternar.isPending} onClick={() => alternar.mutate({ id: s.id, ativo: !s.ativo })}>
                  {s.ativo ? 'Desativar' : 'Ativar'}
                </Button>
              </div>
            </li>
          ))}
        </ul>
        <div className="sm:hidden">{novo}</div>
      </div>
    </section>
  );
}
