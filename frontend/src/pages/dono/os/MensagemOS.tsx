import { useMutation } from '@tanstack/react-query';
import { MessageCircle } from 'lucide-react';
import { useState } from 'react';
import { interpretarErro } from '@/api/erros';
import { gerarMensagem, registrarEnvio, type MensagemGerada, type TipoMensagem } from '@/api/mensagens';
import { Alerta } from '@/components/ui/Alerta';
import { Button, classesBotao } from '@/components/ui/Button';

const TEXTOS: Record<TipoMensagem, { botao: string; titulo: string }> = {
  'lavagem-iniciada': { botao: 'Avisar que começou', titulo: 'Mensagem de lavagem iniciada' },
  'lavagem-concluida': { botao: 'Avisar que ficou pronto', titulo: 'Mensagem de lavagem concluída' },
};

/** Gera a mensagem de acompanhamento, abre o WhatsApp com o texto pronto e registra o envio no histórico. */
export function MensagemOS({ osId, tipo }: { osId: number; tipo: TipoMensagem }) {
  const { botao, titulo } = TEXTOS[tipo];
  const [mensagem, setMensagem] = useState<MensagemGerada | null>(null);
  const [registrada, setRegistrada] = useState(false);
  const [erro, setErro] = useState<string | null>(null);

  const gerar = useMutation({
    mutationFn: () => gerarMensagem(osId, tipo),
    onMutate: () => {
      setErro(null);
      setRegistrada(false);
    },
    onSuccess: setMensagem,
    onError: (e) => setErro(interpretarErro(e).mensagem),
  });

  const registrar = useMutation({
    mutationFn: () => registrarEnvio(osId, tipo),
    onMutate: () => setErro(null),
    onSuccess: () => setRegistrada(true),
    onError: (e) => setErro(interpretarErro(e).mensagem),
  });

  return (
    <section aria-label={titulo} className="flex flex-col gap-sm">
      {erro ? <Alerta>{erro}</Alerta> : null}
      {mensagem ? (
        <div className="flex flex-col gap-sm rounded-lg border-2 border-forte bg-superficie p-md shadow-seca">
          <p className="texto-rotulo-m">{titulo}</p>
          <p className="whitespace-pre-line texto-corpo-m">{mensagem.texto}</p>
          {mensagem.linkWhatsApp ? (
            <a href={mensagem.linkWhatsApp} target="_blank" rel="noopener noreferrer" className={`${classesBotao('primario')} w-full`}>
              <MessageCircle aria-hidden="true" className="size-5" strokeWidth={2} />
              Abrir no WhatsApp
            </a>
          ) : (
            <Alerta tipo="atencao">Este cliente não tem WhatsApp cadastrado. Copie o texto e envie pelo canal que ele prefere.</Alerta>
          )}
          {registrada ? (
            <Alerta tipo="sucesso">Envio registrado no histórico.</Alerta>
          ) : (
            <Button variante="contorno" carregando={registrar.isPending} onClick={() => registrar.mutate()}>
              Já enviei
            </Button>
          )}
        </div>
      ) : (
        <Button variante="contorno" carregando={gerar.isPending} onClick={() => gerar.mutate()}>
          <MessageCircle aria-hidden="true" className="size-5" strokeWidth={2} />
          {botao}
        </Button>
      )}
    </section>
  );
}
