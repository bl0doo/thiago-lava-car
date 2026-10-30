import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useState } from 'react';
import { listarModelos, salvarModelo, type ModeloMensagem } from '@/api/configuracoes';
import { interpretarErro } from '@/api/erros';
import { Alerta } from '@/components/ui/Alerta';
import { AreaTexto } from '@/components/ui/AreaTexto';
import { Button } from '@/components/ui/Button';
import { Carregando } from '@/components/ui/Carregando';
import { ErroConsulta } from '@/components/ui/ErroConsulta';
import { modeloSchema } from '@/schemas/configuracao';

const TITULO: Record<string, string> = { 'lavagem-iniciada': 'Lavagem iniciada', 'lavagem-concluida': 'Lavagem concluída' };

function Modelo({ modelo }: { modelo: ModeloMensagem }) {
  const qc = useQueryClient();
  const titulo = TITULO[modelo.tipo] ?? modelo.tipo;
  const [texto, setTexto] = useState(modelo.texto);
  const [erro, setErro] = useState<string | null>(null);
  const [salvo, setSalvo] = useState(false);

  const salvar = useMutation({
    mutationFn: (t: string) => salvarModelo(modelo.tipo, t),
    onMutate: () => {
      setErro(null);
      setSalvo(false);
    },
    onSuccess: () => {
      setSalvo(true);
      void qc.invalidateQueries({ queryKey: ['modelos-mensagem'] });
    },
    onError: (e) => setErro(interpretarErro(e).mensagem),
  });

  function enviar() {
    const r = modeloSchema.safeParse({ texto });
    if (!r.success) {
      setErro(r.error.issues[0]?.message ?? 'Texto inválido.');
      setSalvo(false);
      return;
    }
    salvar.mutate(r.data.texto);
  }

  return (
    <section aria-label={titulo} className="flex flex-col gap-sm rounded-lg border-2 border-forte bg-superficie p-md shadow-seca">
      <h3 className="texto-h2">{titulo}</h3>
      {erro ? <Alerta>{erro}</Alerta> : null}
      {salvo ? <Alerta tipo="sucesso">Mensagem salva.</Alerta> : null}
      <AreaTexto rotulo={`Texto de ${titulo.toLowerCase()}`} maxLength={500} rows={5} value={texto} onChange={(e) => setTexto(e.target.value)} ajuda={`${texto.length}/500`} />
      <Button variante="contorno" carregando={salvar.isPending} onClick={enviar}>
        Salvar mensagem de {titulo.toLowerCase()}
      </Button>
    </section>
  );
}

export function Mensagens() {
  const query = useQuery({ queryKey: ['modelos-mensagem'], queryFn: listarModelos });
  if (query.isPending) return <Carregando />;
  if (query.isError) {
    return (
      <ErroConsulta erro={query.error} aoTentarNovamente={query.refetch} />
    );
  }
  return (
    <div className="flex flex-col gap-md">
      <p className="texto-corpo-p text-secundario">
        Variáveis: {'{nome}'}, {'{modelo}'}, {'{placa}'}, {'{serviço}'}, {'{duração estimada}'}, {'{valor}'} e {'{nome do lava-jato}'}.
      </p>
      {query.data.map((m) => (
        <Modelo key={m.tipo} modelo={m} />
      ))}
    </div>
  );
}
