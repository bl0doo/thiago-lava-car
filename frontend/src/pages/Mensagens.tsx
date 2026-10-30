import { Lock, MapPinOff } from 'lucide-react';
import { Link } from 'react-router-dom';
import { classesBotao } from '@/components/ui/Button';
import { EstadoVazio } from '@/components/ui/EstadoVazio';

function TelaDeErro({ titulo, codigo, children, Icone }: { titulo: string; codigo: string; children: string; Icone: typeof Lock }) {
  return (
    <main className="mx-auto flex min-h-dvh max-w-[28rem] flex-col justify-center gap-lg bg-app p-lg">
      <p className="texto-eyebrow text-secundario">Erro {codigo}</p>
      <EstadoVazio Icone={Icone} titulo={titulo} nivel="h1">
        {children}
      </EstadoVazio>
      <Link to="/" className={`${classesBotao('primario')} w-full`}>
        Voltar para o início
      </Link>
    </main>
  );
}

export function SemPermissao() {
  return (
    <TelaDeErro titulo="Sem permissão" codigo="403" Icone={Lock}>
      Seu perfil não tem acesso a esta tela.
    </TelaDeErro>
  );
}

export function NaoEncontrada() {
  return (
    <TelaDeErro titulo="Página não encontrada" codigo="404" Icone={MapPinOff}>
      O endereço pode estar errado ou a página mudou de lugar.
    </TelaDeErro>
  );
}

