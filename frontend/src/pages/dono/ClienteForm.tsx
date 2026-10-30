import { zodResolver } from '@hookform/resolvers/zod';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { CarFront, ChevronRight } from 'lucide-react';
import axios from 'axios';
import { useEffect, useState } from 'react';
import { useForm } from 'react-hook-form';
import { Link, useNavigate, useParams } from 'react-router-dom';
import { atualizarCliente, criarCliente, obterCliente, type ClienteEntrada } from '@/api/clientes';
import { interpretarErro } from '@/api/erros';
import { BarraTitulo } from '@/components/layout/BarraTitulo';
import { Alerta } from '@/components/ui/Alerta';
import { Button } from '@/components/ui/Button';
import { Caixinha } from '@/components/ui/Caixinha';
import { Campo } from '@/components/ui/Campo';
import { Carregando } from '@/components/ui/Carregando';
import { Placa } from '@/components/ui/Placa';
import { Select } from '@/components/ui/Select';
import { clienteSchema, type ClienteFormEntrada, type ClienteFormSaida } from '@/schemas/cliente';

const CAMPOS = ['nome', 'telefone', 'email', 'canalPreferido', 'consentimentoLgpd'] as const;

/** Id do cadastro já existente que a API devolve no 409 de telefone repetido. */
function idExistente(e: unknown): number | null {
  if (!axios.isAxiosError(e)) return null;
  const d = e.response?.data as { clienteId?: number; idExistente?: number } | undefined;
  return d?.clienteId ?? d?.idExistente ?? null;
}

export function ClienteForm() {
  const { id } = useParams();
  const editando = id !== undefined;
  const clienteId = Number(id);
  const navegar = useNavigate();
  const qc = useQueryClient();
  const [erroGeral, setErroGeral] = useState<string | null>(null);
  const [existente, setExistente] = useState<number | null>(null);
  const [salvo, setSalvo] = useState(false);

  const consulta = useQuery({ queryKey: ['cliente', clienteId], queryFn: () => obterCliente(clienteId), enabled: editando });
  const {
    register,
    handleSubmit,
    setError,
    reset,
    formState: { errors },
  } = useForm<ClienteFormEntrada, unknown, ClienteFormSaida>({
    resolver: zodResolver(clienteSchema),
    defaultValues: { canalPreferido: 'WhatsApp', consentimentoLgpd: false, email: '' },
  });

  useEffect(() => {
    const c = consulta.data;
    if (c) reset({ nome: c.nome, telefone: c.telefone, email: c.email ?? '', canalPreferido: c.canalPreferido, consentimentoLgpd: c.consentimentoLgpd });
  }, [consulta.data, reset]);

  const salvar = useMutation({
    mutationFn: (d: ClienteEntrada) => (editando ? atualizarCliente(clienteId, d) : criarCliente(d)),
    onSuccess: async (c) => {
      await qc.invalidateQueries({ queryKey: ['clientes'] });
      qc.setQueryData(['cliente', c.id], c);
      if (editando) setSalvo(true);
      else void navegar(`/dono/clientes/${c.id}`, { replace: true });
    },
    onError: (e) => {
      const erro = interpretarErro(e);
      let apontou = false;
      for (const campo of CAMPOS) {
        if (erro.campos[campo]) {
          setError(campo, { message: erro.campos[campo] });
          apontou = true;
        }
      }
      setExistente(erro.status === 409 ? idExistente(e) : null);
      if (!apontou) setErroGeral(erro.mensagem);
    },
  });

  const enviar = handleSubmit((v) => {
    setErroGeral(null);
    setExistente(null);
    setSalvo(false);
    salvar.mutate({ nome: v.nome, telefone: v.telefone, email: v.email ? v.email : null, canalPreferido: v.canalPreferido, consentimentoLgpd: v.consentimentoLgpd });
  });

  if (editando && consulta.isPending) return <Carregando tela texto="Carregando o cliente…" />;
  if (editando && consulta.isError) {
    return (
      <section className="flex flex-col">
        <BarraTitulo titulo="Cliente" onVoltar={() => void navegar('/dono/clientes')} />
        <div className="px-md">
          <Alerta>{interpretarErro(consulta.error).mensagem}</Alerta>
        </div>
      </section>
    );
  }

  return (
    <section className="flex flex-col">
      <BarraTitulo sobretitulo="Clientes" titulo={editando ? 'Editar cliente' : 'Novo cliente'} onVoltar={() => void navegar('/dono/clientes')} />
      <div className="mx-auto flex w-full max-w-[40rem] flex-col gap-lg px-md">
        <form onSubmit={(e) => void enviar(e)} noValidate className="flex flex-col gap-md">
          {erroGeral ? <Alerta>{erroGeral}</Alerta> : null}
          {existente ? (
            <Alerta tipo="atencao">
              Este telefone já tem cadastro.{' '}
              <Link to={`/dono/clientes/${existente}`} className="underline underline-offset-4">
                Abrir o cliente encontrado
              </Link>
            </Alerta>
          ) : null}
          {salvo ? <Alerta tipo="sucesso">Dados salvos.</Alerta> : null}
          <Campo rotulo="Nome" autoComplete="off" erro={errors.nome?.message} {...register('nome')} />
          <Campo rotulo="Celular com DDD" type="tel" inputMode="tel" placeholder="(44) 99999-8888" erro={errors.telefone?.message} {...register('telefone')} />
          <Campo rotulo="E-mail (opcional)" type="email" inputMode="email" erro={errors.email?.message} {...register('email')} />
          <Select rotulo="Canal preferido" erro={errors.canalPreferido?.message} {...register('canalPreferido')}>
            <option value="WhatsApp">WhatsApp</option>
            <option value="Telefone">Telefone</option>
            <option value="Email">E-mail</option>
          </Select>
          <Caixinha erro={errors.consentimentoLgpd?.message} {...register('consentimentoLgpd')}>
            O cliente concordou com o uso dos dados dele para atendimento e avisos (LGPD).
          </Caixinha>
          <Button type="submit" carregando={salvar.isPending}>
            {salvar.isPending ? 'Salvando…' : 'Salvar'}
          </Button>
        </form>

        {editando && consulta.data ? (
          <section aria-labelledby="veiculos" className="flex flex-col gap-sm">
            <h2 id="veiculos" className="texto-h2">
              Veículos
            </h2>
            {consulta.data.veiculos.length === 0 ? <p className="texto-corpo-m text-secundario">Nenhum veículo cadastrado. O veículo é cadastrado na Nova entrada.</p> : null}
            <ul className="flex flex-col gap-xs">
              {consulta.data.veiculos.map((v) => (
                <li key={v.id}>
                  <Link to={`/dono/veiculos/${v.id}`} className="flex min-h-[66px] items-center gap-sm rounded-lg border-2 border-forte bg-superficie px-md py-xs hover:bg-rebaixado">
                    <Placa placa={v.placa} />
                    <span className="flex min-w-0 flex-1 items-center gap-xs texto-corpo-m">
                      <CarFront aria-hidden="true" className="size-5 shrink-0" />
                      <span className="truncate">{v.modelo}</span>
                    </span>
                    <ChevronRight aria-hidden="true" className="size-6 shrink-0 text-secundario" />
                  </Link>
                </li>
              ))}
            </ul>
          </section>
        ) : null}
      </div>
    </section>
  );
}
