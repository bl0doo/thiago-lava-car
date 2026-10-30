import { UserRound, Users } from 'lucide-react';
import { useQuery } from '@tanstack/react-query';
import { useState } from 'react';
import { Link, Navigate, useNavigate } from 'react-router-dom';
import { listarLavadores } from '@/api/auth';
import { interpretarErro } from '@/api/erros';
import type { LavadorResumo } from '@/api/tipos';
import { rotaInicial, useAuth } from '@/auth/AuthContext';
import { BarraTitulo } from '@/components/layout/BarraTitulo';
import { Alerta } from '@/components/ui/Alerta';
import { Button } from '@/components/ui/Button';
import { Carregando } from '@/components/ui/Carregando';
import { EstadoVazio } from '@/components/ui/EstadoVazio';
import { TecladoPin } from '@/components/ui/TecladoPin';
import { pinSchema } from '@/schemas/comuns';

export function LoginLavador() {
  const { usuario, entrarComPin } = useAuth();
  const navegar = useNavigate();
  const [escolhido, setEscolhido] = useState<LavadorResumo | null>(null);
  const [pin, setPin] = useState('');
  const [erro, setErro] = useState<string | null>(null);
  const [bloqueado, setBloqueado] = useState<string | null>(null);
  const [enviando, setEnviando] = useState(false);
  const lavadores = useQuery({ queryKey: ['lavadores'], queryFn: listarLavadores });

  if (usuario) return <Navigate to={rotaInicial(usuario.perfil)} replace />;

  const confirmar = async (valor: string) => {
    if (!escolhido) return;
    const valido = pinSchema.safeParse(valor);
    if (!valido.success) {
      setErro(valido.error.issues[0]?.message ?? 'PIN inválido.');
      return;
    }
    setEnviando(true);
    setErro(null);
    try {
      const logado = await entrarComPin(escolhido.id, valor);
      void navegar(rotaInicial(logado.perfil), { replace: true });
    } catch (e) {
      const falha = interpretarErro(e);
      // 423: errou o PIN 5 vezes e o acesso travou por 15 minutos (tela A4 do protótipo)
      if (falha.status === 423) setBloqueado(falha.mensagem);
      else setErro(falha.mensagem);
      setPin('');
    } finally {
      setEnviando(false);
    }
  };

  const trocarLavador = () => {
    setEscolhido(null);
    setPin('');
    setErro(null);
    setBloqueado(null);
  };

  if (bloqueado) {
    return (
      <main className="mx-auto flex min-h-dvh max-w-[28rem] flex-col bg-app">
        <BarraTitulo sobretitulo="PIN" titulo="Bloqueado" onVoltar={trocarLavador} />
        <div className="flex flex-1 flex-col gap-lg px-md pb-lg">
          <Alerta tipo="atencao">{bloqueado}</Alerta>
          <p className="texto-corpo-m text-secundario">Precisa entrar agora? Peça ao dono para liberar o seu acesso.</p>
          <Button variante="contorno" tamanho="lavador" className="mt-auto w-full" onClick={trocarLavador}>
            Voltar
          </Button>
        </div>
      </main>
    );
  }

  return (
    <main className="mx-auto flex min-h-dvh max-w-[28rem] flex-col bg-app">
      <BarraTitulo
        sobretitulo="Sou lavador"
        titulo={escolhido ? `Olá, ${escolhido.apelido}` : 'Quem é você?'}
        onVoltar={escolhido ? trocarLavador : () => void navegar('/login')}
      />
      <div className="flex flex-1 flex-col gap-lg px-md pb-lg">
        {erro ? <Alerta>{erro}</Alerta> : null}

        {escolhido ? (
          <>
            <p className="texto-corpo-g text-center">Digite seu PIN de 4 números</p>
            <TecladoPin valor={pin} onChange={setPin} onConfirmar={(v) => void confirmar(v)} desabilitado={enviando} />
            <p className="mt-auto text-center texto-corpo-p text-secundario">Errou 5 vezes? O acesso trava por 15 minutos.</p>
          </>
        ) : lavadores.isPending ? (
          <Carregando cartoes={3} />
        ) : lavadores.isError ? (
          <Alerta>{interpretarErro(lavadores.error).mensagem}</Alerta>
        ) : lavadores.data.length === 0 ? (
          <EstadoVazio Icone={Users} titulo="Nenhum lavador cadastrado">
            Peça ao dono para cadastrar a equipe.
          </EstadoVazio>
        ) : (
          <ul className="flex flex-col gap-sm">
            {lavadores.data.map((l) => (
              <li key={l.id}>
                <Button variante="contorno" tamanho="lavador" className="w-full justify-start" onClick={() => setEscolhido(l)}>
                  <UserRound aria-hidden="true" className="size-8" strokeWidth={2} />
                  {l.apelido}
                </Button>
              </li>
            ))}
          </ul>
        )}

        {!escolhido ? (
          <Link to="/login" className="mt-auto text-center texto-rotulo-m text-marca underline underline-offset-4">
            Entrar como dono ou cliente
          </Link>
        ) : null}
      </div>
    </main>
  );
}
