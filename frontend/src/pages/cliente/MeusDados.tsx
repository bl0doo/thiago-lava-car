import { zodResolver } from '@hookform/resolvers/zod';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useEffect, useState } from 'react';
import { useForm } from 'react-hook-form';
import { atualizarMeusDados, obterMeusDados } from '@/api/clientes';
import { interpretarErro } from '@/api/erros';
import { useAuth } from '@/auth/AuthContext';
import { BarraTitulo } from '@/components/layout/BarraTitulo';
import { Alerta } from '@/components/ui/Alerta';
import { Button } from '@/components/ui/Button';
import { Campo } from '@/components/ui/Campo';
import { Carregando } from '@/components/ui/Carregando';
import { Select } from '@/components/ui/Select';
import { meusDadosSchema, type MeusDadosEntrada, type MeusDadosSaida } from '@/schemas/meusDados';

export function MeusDados() {
  const { sair } = useAuth();
  const qc = useQueryClient();
  const consulta = useQuery({ queryKey: ['meus-dados'], queryFn: obterMeusDados });
  const [salvo, setSalvo] = useState(false);
  const [erroGeral, setErroGeral] = useState<string | null>(null);
  const {
    register,
    handleSubmit,
    reset,
    setError,
    formState: { errors },
  } = useForm<MeusDadosEntrada, unknown, MeusDadosSaida>({ resolver: zodResolver(meusDadosSchema), defaultValues: { canalPreferido: 'WhatsApp', email: '' } });

  useEffect(() => {
    const c = consulta.data;
    if (c) reset({ nome: c.nome, telefone: c.telefone, email: c.email ?? '', canalPreferido: c.canalPreferido });
  }, [consulta.data, reset]);

  const salvar = useMutation({
    mutationFn: atualizarMeusDados,
    onSuccess: (c) => {
      qc.setQueryData(['meus-dados'], c);
      setSalvo(true);
    },
    onError: (e) => {
      const erro = interpretarErro(e);
      let apontou = false;
      for (const campo of ['nome', 'telefone', 'email', 'canalPreferido'] as const) {
        if (erro.campos[campo]) {
          setError(campo, { message: erro.campos[campo] });
          apontou = true;
        }
      }
      if (!apontou) setErroGeral(erro.mensagem);
    },
  });

  return (
    <section className="flex flex-col">
      <BarraTitulo sobretitulo="Perfil" titulo="Meus dados" />
      <div className="mx-auto flex w-full max-w-[40rem] flex-col gap-md px-md">
        {consulta.isPending ? <Carregando texto="Carregando seus dados…" cartoes={2} /> : null}
        {consulta.isError ? <Alerta>{interpretarErro(consulta.error).mensagem}</Alerta> : null}
        {consulta.data ? (
          <form
            noValidate
            className="flex flex-col gap-md"
            onSubmit={(e) =>
              void handleSubmit((v) => {
                setSalvo(false);
                setErroGeral(null);
                salvar.mutate({ nome: v.nome, telefone: v.telefone, email: v.email ? v.email : null, canalPreferido: v.canalPreferido });
              })(e)
            }
          >
            {salvo ? <Alerta tipo="sucesso">Dados salvos.</Alerta> : null}
            {erroGeral ? <Alerta>{erroGeral}</Alerta> : null}
            <Campo rotulo="Nome" erro={errors.nome?.message} {...register('nome')} />
            <Campo rotulo="Celular com DDD" type="tel" inputMode="tel" erro={errors.telefone?.message} {...register('telefone')} />
            <Campo rotulo="E-mail" type="email" inputMode="email" erro={errors.email?.message} {...register('email')} />
            <Select rotulo="Como prefere ser avisado?" erro={errors.canalPreferido?.message} {...register('canalPreferido')}>
              <option value="WhatsApp">WhatsApp</option>
              <option value="Telefone">Telefone</option>
              <option value="Email">E-mail</option>
            </Select>
            <Button type="submit" carregando={salvar.isPending}>
              {salvar.isPending ? 'Salvando…' : 'Salvar'}
            </Button>
          </form>
        ) : null}
        <Button variante="contorno" onClick={() => void sair()}>
          Sair
        </Button>
      </div>
    </section>
  );
}
