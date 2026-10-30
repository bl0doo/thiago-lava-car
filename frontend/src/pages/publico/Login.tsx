import { User } from 'lucide-react';
import { zodResolver } from '@hookform/resolvers/zod';
import { useState } from 'react';
import { useForm } from 'react-hook-form';
import { Link, Navigate, useLocation, useNavigate } from 'react-router-dom';
import { interpretarErro } from '@/api/erros';
import { rotaInicial, useAuth } from '@/auth/AuthContext';
import { Marca } from '@/components/layout/Marca';
import { Alerta } from '@/components/ui/Alerta';
import { Button, classesBotao } from '@/components/ui/Button';
import { Campo } from '@/components/ui/Campo';
import { loginSchema, type LoginForm } from '@/schemas/login';

export function Login() {
  const { usuario, entrar } = useAuth();
  const navegar = useNavigate();
  const local = useLocation() as { state?: { de?: string } };
  const [erroGeral, setErroGeral] = useState<string | null>(null);
  const {
    register,
    handleSubmit,
    setError,
    formState: { errors, isSubmitting },
  } = useForm<LoginForm>({ resolver: zodResolver(loginSchema) });

  if (usuario) return <Navigate to={rotaInicial(usuario.perfil)} replace />;

  const enviar = handleSubmit(async ({ email, senha }) => {
    setErroGeral(null);
    try {
      const logado = await entrar(email, senha);
      void navegar(local.state?.de ?? rotaInicial(logado.perfil), { replace: true });
    } catch (e) {
      const erro = interpretarErro(e);
      // 422: a API aponta o campo; 401, 423 e 429: mensagem geral no topo do formulário
      for (const [campo, mensagem] of Object.entries(erro.campos)) {
        if (campo === 'email' || campo === 'senha') setError(campo, { message: mensagem });
      }
      if (Object.keys(erro.campos).length === 0) setErroGeral(erro.mensagem);
    }
  });

  return (
    <main className="flex min-h-dvh flex-col bg-app lg:flex-row">
      <section className="flex flex-col bg-inverso lg:w-1/2 lg:justify-center">
        <div className="flex flex-col gap-md px-lg pt-16 pb-xl lg:mx-auto lg:w-full lg:max-w-[32rem]">
          <Marca />
          <p className="texto-corpo-g text-texto-inverso">Quanto entrou, quanto sobrou e por quê. Todo dia.</p>
        </div>
        <div aria-hidden="true" className="faixa-quadriculada lg:hidden" />
      </section>
      <div
        aria-hidden="true"
        className="hidden w-3 shrink-0 lg:block"
        style={{ background: 'repeating-linear-gradient(180deg, var(--acao-primaria-bg) 0 12px, var(--bg-inverso) 12px 24px)' }}
      />

      <div className="flex flex-1 flex-col lg:justify-center">
        <div className="mx-auto flex w-full max-w-[28rem] flex-col gap-md p-lg">
          <h1 className="texto-h1">Entrar</h1>
          <form onSubmit={(e) => void enviar(e)} noValidate className="flex flex-col gap-md">
            {erroGeral ? <Alerta>{erroGeral}</Alerta> : null}
            <Campo rotulo="E-mail" type="email" autoComplete="username" inputMode="email" erro={errors.email?.message} {...register('email')} />
            <Campo rotulo="Senha" type="password" autoComplete="current-password" erro={errors.senha?.message} {...register('senha')} />
            <Button type="submit" carregando={isSubmitting} className="w-full">
              {isSubmitting ? 'Entrando…' : 'Entrar'}
            </Button>
          </form>
          <p className="texto-corpo-m text-secundario">Trabalha na lavagem?</p>
          <Link to="/login/lavador" className={`${classesBotao('contorno')} w-full`}>
            <User aria-hidden="true" className="size-6" strokeWidth={2} />
            Sou lavador
          </Link>
          <p className="texto-corpo-m text-secundario">
            Cliente?{' '}
            <Link to="/cadastro" className="texto-rotulo-m text-marca underline underline-offset-4">
              Crie sua conta
            </Link>
          </p>
        </div>
      </div>
    </main>
  );
}
