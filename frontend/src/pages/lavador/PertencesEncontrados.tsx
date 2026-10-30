import { useMutation, useQuery } from '@tanstack/react-query';
import { useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { interpretarErro } from '@/api/erros';
import { obterCartao, registrarPertencesEncontrados } from '@/api/ordensServico';
import { BarraTitulo } from '@/components/layout/BarraTitulo';
import { Alerta } from '@/components/ui/Alerta';
import { AreaTexto } from '@/components/ui/AreaTexto';
import { Button } from '@/components/ui/Button';
import { Carregando } from '@/components/ui/Carregando';
import { Placa } from '@/components/ui/Placa';

const LIMITE = 500;

export function PertencesEncontrados() {
  const { id } = useParams();
  const osId = Number(id);
  const navegar = useNavigate();
  const [texto, setTexto] = useState('');
  const [erroCampo, setErroCampo] = useState<string | null>(null);
  const [erro, setErro] = useState<string | null>(null);
  const [salvo, setSalvo] = useState(false);
  const valido = Number.isInteger(osId) && osId > 0;

  const consulta = useQuery({ queryKey: ['cartao', osId], queryFn: () => obterCartao(osId), enabled: valido });

  const salvar = useMutation({
    mutationFn: () => registrarPertencesEncontrados(osId, texto.trim() || null),
    onMutate: () => {
      setErro(null);
      setSalvo(false);
    },
    onSuccess: () => setSalvo(true),
    onError: (e) => {
      const { campos, mensagem } = interpretarErro(e);
      if (campos.texto) setErroCampo(campos.texto);
      else setErro(mensagem);
    },
  });

  const voltar = () => void navegar('/lavador');

  const enviar = () => {
    if (texto.trim().length > LIMITE) {
      setErroCampo(`O texto deve ter até ${LIMITE} caracteres.`);
      return;
    }
    setErroCampo(null);
    salvar.mutate();
  };

  return (
    <div className="mx-auto flex max-w-[36rem] flex-col">
      <BarraTitulo sobretitulo="Achados no carro" titulo="Pertences encontrados" onVoltar={voltar} />
      <div className="flex flex-col gap-md px-md pb-lg">
        {!valido ? <Alerta>Cartão não encontrado. Volte à fila e escolha o carro de novo.</Alerta> : null}
        {valido && consulta.isPending ? <Carregando cartoes={1} /> : null}
        {consulta.isError ? (
          <>
            <Alerta>{interpretarErro(consulta.error).status === 404 ? 'Este cartão não existe mais.' : interpretarErro(consulta.error).mensagem}</Alerta>
            <Button variante="contorno" onClick={voltar}>
              Voltar à fila
            </Button>
          </>
        ) : null}

        {consulta.data ? (
          <>
            <div className="flex flex-wrap items-center justify-between gap-sm">
              <Placa placa={consulta.data.placa} />
              <p className="texto-corpo-g">{consulta.data.modelo}</p>
            </div>
            {erro ? <Alerta>{erro}</Alerta> : null}
            {salvo ? <Alerta tipo="sucesso">Salvo. O dono já pode ver.</Alerta> : null}
            <AreaTexto
              rotulo="O que você encontrou?"
              rows={5}
              maxLength={LIMITE}
              value={texto}
              onChange={(e) => {
                setTexto(e.target.value);
                setSalvo(false);
              }}
              erro={erroCampo ?? undefined}
              ajuda={`${texto.length} de ${LIMITE} caracteres. Ex.: óculos no porta-luvas, R$ 5 embaixo do banco.`}
            />
            <Button tamanho="lavador" className="w-full" carregando={salvar.isPending} onClick={enviar}>
              Salvar
            </Button>
          </>
        ) : null}
      </div>
    </div>
  );
}
