import { Lock, MapPinOff } from 'lucide-react';
import { Link } from 'react-router-dom';
import { BarraTitulo } from '@/components/layout/BarraTitulo';
import { Alerta } from '@/components/ui/Alerta';
import { classesBotao } from '@/components/ui/Button';
import { EstadoVazio } from '@/components/ui/EstadoVazio';

/** Telas D10 e D11 do protótipo: o que aconteceu, em uma frase, e o caminho de volta. */
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

/** Tela ainda não implementada: guarda a rota prevista na Entrega 2 e diz de onde virão os dados. */
export function EmConstrucao({ titulo, descricao }: { titulo: string; descricao: string }) {
  return (
    <section className="flex flex-col">
      <BarraTitulo titulo={titulo} />
      <div className="flex flex-col gap-md px-md">
        <p className="texto-corpo-m text-secundario">{descricao}</p>
        <Alerta tipo="informacao">Em construção</Alerta>
      </div>
    </section>
  );
}
