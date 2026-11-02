import { useQuery } from '@tanstack/react-query';
import { CarFront, ChevronRight, Plus } from 'lucide-react';
import { useState } from 'react';
import { Link } from 'react-router-dom';
import { listarMeusVeiculos } from '@/api/clientes';
import { interpretarErro } from '@/api/erros';
import { Alerta } from '@/components/ui/Alerta';
import { Button } from '@/components/ui/Button';
import { Carregando } from '@/components/ui/Carregando';
import { EstadoVazio } from '@/components/ui/EstadoVazio';
import { Placa } from '@/components/ui/Placa';
import { FormNovoVeiculo } from './FormNovoVeiculo';

export function MeusVeiculos() {
  const consulta = useQuery({ queryKey: ['meus-veiculos'], queryFn: listarMeusVeiculos });
  const [abrindo, setAbrindo] = useState(false);

  return (
    <section className="flex flex-col">
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
          <FormNovoVeiculo onCriado={() => setAbrindo(false)} onCancelar={() => setAbrindo(false)} />
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
