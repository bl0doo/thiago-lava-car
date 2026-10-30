import { useQuery } from '@tanstack/react-query';
import { ChevronRight, Plus, SearchX, Users } from 'lucide-react';
import { useState } from 'react';
import { Link } from 'react-router-dom';
import { listarClientes } from '@/api/clientes';
import { BarraTitulo } from '@/components/layout/BarraTitulo';
import { Button, classesBotao } from '@/components/ui/Button';
import { Campo } from '@/components/ui/Campo';
import { Carregando } from '@/components/ui/Carregando';
import { ErroConsulta } from '@/components/ui/ErroConsulta';
import { EstadoVazio } from '@/components/ui/EstadoVazio';
import { formatarTelefone } from '@/lib/format';
import { useDebounce } from '@/lib/useDebounce';

export function Clientes() {
  const [texto, setTexto] = useState('');
  const [pagina, setPagina] = useState(1);
  const busca = useDebounce(texto.trim());
  const consulta = useQuery({ queryKey: ['clientes', busca, pagina], queryFn: () => listarClientes(busca, pagina), placeholderData: (anterior) => anterior });

  const novo = (
    <Link to="/dono/clientes/novo" className={classesBotao('primario')}>
      <Plus aria-hidden="true" className="size-5" strokeWidth={2.5} />
      Novo cliente
    </Link>
  );
  const dados = consulta.data;

  return (
    <section className="flex flex-col">
      <BarraTitulo sobretitulo="Gestão" titulo="Clientes" acao={<div className="hidden sm:block">{novo}</div>} />
      <div className="flex flex-col gap-md px-md">
        <Campo
          rotulo="Buscar"
          type="search"
          placeholder="Nome, telefone ou placa"
          autoComplete="off"
          value={texto}
          onChange={(e) => {
            setTexto(e.target.value);
            setPagina(1);
          }}
        />
        {consulta.isPending ? <Carregando texto="Carregando clientes…" cartoes={3} /> : null}
        {consulta.isError ? (
          <ErroConsulta erro={consulta.error} aoTentarNovamente={consulta.refetch} />
        ) : null}
        {dados && dados.itens.length === 0 ? (
          busca ? (
            <EstadoVazio Icone={SearchX} titulo="Ninguém encontrado">
              Confira o nome, o telefone ou a placa. Se for um cliente novo, cadastre abaixo.
            </EstadoVazio>
          ) : (
            <EstadoVazio Icone={Users} titulo="Nenhum cliente ainda">
              Os clientes aparecem aqui quando fazem a primeira entrada ou o cadastro.
            </EstadoVazio>
          )
        ) : null}
        {dados && dados.itens.length > 0 ? (
          <>
            <p className="texto-corpo-p text-secundario" aria-live="polite">
              {dados.total} {dados.total === 1 ? 'cliente' : 'clientes'}
            </p>
            <ul className="flex flex-col divide-y divide-padrao rounded-lg border-2 border-forte bg-superficie">
              {dados.itens.map((c) => (
                <li key={c.id}>
                  <Link to={`/dono/clientes/${c.id}`} className="flex min-h-[66px] items-center gap-sm px-md py-xs hover:bg-rebaixado">
                    <span className="flex min-w-0 flex-1 flex-col">
                      <span className="texto-rotulo-g">{c.nome}</span>
                      <span className="texto-mono-dado text-secundario">{formatarTelefone(c.telefone)}</span>
                    </span>
                    <ChevronRight aria-hidden="true" className="size-6 shrink-0 text-secundario" strokeWidth={2} />
                  </Link>
                </li>
              ))}
            </ul>
            {dados.total > dados.tamanho ? (
              <div className="flex items-center justify-between gap-sm">
                <Button variante="contorno" disabled={pagina <= 1} onClick={() => setPagina(pagina - 1)}>
                  Anterior
                </Button>
                <span className="texto-corpo-p text-secundario">
                  Página {dados.pagina} de {Math.ceil(dados.total / dados.tamanho)}
                </span>
                <Button variante="contorno" disabled={pagina * dados.tamanho >= dados.total} onClick={() => setPagina(pagina + 1)}>
                  Próxima
                </Button>
              </div>
            ) : null}
          </>
        ) : null}
        <div className="sm:hidden">{novo}</div>
      </div>
    </section>
  );
}
