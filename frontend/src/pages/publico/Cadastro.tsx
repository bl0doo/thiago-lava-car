import { zodResolver } from '@hookform/resolvers/zod';
import { useState } from 'react';
import { useForm } from 'react-hook-form';
import { Link, Navigate, useNavigate } from 'react-router-dom';
import { cadastrarCliente } from '@/api/auth';
import { interpretarErro } from '@/api/erros';
import { rotaInicial, useAuth } from '@/auth/AuthContext';
import { Marca } from '@/components/layout/Marca';
import { Alerta } from '@/components/ui/Alerta';
import { Button } from '@/components/ui/Button';
import { Campo } from '@/components/ui/Campo';
import { Caixinha } from '@/components/ui/Caixinha';
import { Select } from '@/components/ui/Select';
import { cadastroSchema, type CadastroEntrada, type CadastroSaida } from '@/schemas/cadastro';

const CAMPOS_DA_API = ['nome', 'telefone', 'email', 'senha', 'canalPreferido', 'consentimentoLgpd'] as const;

/** Cadastro do cliente (cenário "cadastro do cliente: padrão, erros, LGPD"): cria a conta e já entra na área do cliente. */
export function Cadastro() {
  const { usuario } = useAuth();
  const navegar = useNavigate();
  const [erroGeral, setErroGeral] = useState<string | null>(null);
  const {
    register,
    handleSubmit,
    setError,
    formState: { errors, isSubmitting },
  } = useForm<CadastroEntrada, unknown, CadastroSaida>({
    resolver: zodResolver(cadastroSchema),
    defaultValues: { canalPreferido: 'WhatsApp', consentimentoLgpd: false },
  });

  if (usuario) return <Navigate to={rotaInicial(usuario.perfil)} replace />;

  const enviar = handleSubmit(async ({ nome, telefone, email, senha, canalPreferido, consentimentoLgpd }) => {
    setErroGeral(null);
    try {
      const logado = await cadastrarCliente({ nome, telefone, email, senha, canalPreferido, consentimentoLgpd });
      void navegar(rotaInicial(logado.perfil), { replace: true });
    } catch (e) {
      const erro = interpretarErro(e);
      let apontou = false;
      for (const campo of CAMPOS_DA_API) {
        const mensagem = erro.campos[campo];
        if (mensagem) {
          setError(campo, { message: mensagem });
          apontou = true;
        }
      }
      // 409 (e-mail ou telefone já usado) e falhas de rede vão numa mensagem no topo
      if (!apontou) setErroGeral(erro.mensagem);
    }
  });

  return (
    <main className="flex min-h-dvh flex-col bg-app">
      <header className="flex flex-col bg-inverso">
        <div className="mx-auto w-full max-w-[28rem] px-lg pt-xl pb-lg">
          <Marca />
        </div>
        <div aria-hidden="true" className="faixa-quadriculada" />
      </header>
      <div className="mx-auto flex w-full max-w-[28rem] flex-col gap-md p-lg">
        <h1 className="texto-h1">Criar conta</h1>
        <p className="texto-corpo-m text-secundario">Agende horários e acompanhe o histórico do seu carro.</p>
        <form onSubmit={(e) => void enviar(e)} noValidate className="flex flex-col gap-md">
          {erroGeral ? (
            <Alerta>
              {erroGeral}{' '}
              <Link to="/login" className="underline underline-offset-4">
                Ir para o login
              </Link>
            </Alerta>
          ) : null}
          <Campo rotulo="Nome" autoComplete="name" erro={errors.nome?.message} {...register('nome')} />
          <Campo rotulo="Celular com DDD" type="tel" inputMode="tel" autoComplete="tel" placeholder="(44) 99999-8888" erro={errors.telefone?.message} {...register('telefone')} />
          <Campo rotulo="E-mail" type="email" inputMode="email" autoComplete="email" erro={errors.email?.message} {...register('email')} />
          <Campo rotulo="Senha" type="password" autoComplete="new-password" ajuda="Pelo menos 8 caracteres, com letras e números." erro={errors.senha?.message} {...register('senha')} />
          <Campo rotulo="Repita a senha" type="password" autoComplete="new-password" erro={errors.confirmacao?.message} {...register('confirmacao')} />
          <Select rotulo="Como prefere ser avisado?" erro={errors.canalPreferido?.message} {...register('canalPreferido')}>
            <option value="WhatsApp">WhatsApp</option>
            <option value="Telefone">Telefone</option>
            <option value="Email">E-mail</option>
          </Select>
          <Caixinha erro={errors.consentimentoLgpd?.message} {...register('consentimentoLgpd')}>
            Concordo que o lava-jato guarde meu nome, telefone e a placa do meu carro para atendimento e avisos. Posso ver e corrigir esses dados quando quiser (LGPD).
          </Caixinha>
          <Button type="submit" carregando={isSubmitting} className="w-full">
            {isSubmitting ? 'Criando…' : 'Criar conta'}
          </Button>
        </form>
        <p className="texto-corpo-m text-secundario">
          Já tem conta?{' '}
          <Link to="/login" className="texto-rotulo-m text-marca underline underline-offset-4">
            Entrar
          </Link>
        </p>
      </div>
    </main>
  );
}
