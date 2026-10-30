import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useState } from 'react';
import { interpretarErro } from '@/api/erros';
import { desfazerUltimoPasso, encerrarLavagem, iniciarLavagem, obterQuadro } from '@/api/ordensServico';
import type { Cartao, Quadro } from '@/api/tipos';
import { aplicarAcao, substituir, type Acao } from '@/lib/quadro';

export const INTERVALO_QUADRO_MS = 15_000;

const chaveQuadro = ['quadro'] as const;

const EXECUTAR: Record<Acao, (id: number) => Promise<Cartao>> = {
  iniciar: iniciarLavagem,
  encerrar: encerrarLavagem,
  desfazer: desfazerUltimoPasso,
};

export function useQuadro() {
  const cliente = useQueryClient();
  const [erroAcao, setErroAcao] = useState<string | null>(null);

  const consulta = useQuery({
    queryKey: chaveQuadro,
    queryFn: () => obterQuadro(),
    refetchInterval: INTERVALO_QUADRO_MS,
    refetchOnWindowFocus: true,
  });

  const mutacao = useMutation({
    mutationFn: ({ id, acao }: { id: number; acao: Acao }) => EXECUTAR[acao](id),
    // Atualização otimista: o cartão muda de coluna no toque e volta atrás se a API recusar.
    onMutate: async ({ id, acao }) => {
      setErroAcao(null);
      await cliente.cancelQueries({ queryKey: chaveQuadro });
      const anterior = cliente.getQueryData<Quadro>(chaveQuadro);
      if (anterior) cliente.setQueryData<Quadro>(chaveQuadro, aplicarAcao(anterior, id, acao, new Date()));
      return { anterior };
    },
    onError: (erro, _vars, contexto) => {
      if (contexto?.anterior) cliente.setQueryData(chaveQuadro, contexto.anterior);
      setErroAcao(interpretarErro(erro).mensagem);
    },
    onSuccess: (cartao) => {
      cliente.setQueryData<Quadro>(chaveQuadro, (atual) => (atual ? substituir(atual, cartao) : atual));
    },
    onSettled: () => cliente.invalidateQueries({ queryKey: chaveQuadro }),
  });

  return {
    quadro: consulta.data,
    carregando: consulta.isPending,
    erroConsulta: consulta.isError ? interpretarErro(consulta.error).mensagem : null,
    atualizadoEm: consulta.dataUpdatedAt,
    atualizando: consulta.isFetching,
    atualizar: () => void consulta.refetch(),
    erroAcao,
    executar: (id: number, acao: Acao) => mutacao.mutate({ id, acao }),
    cartaoPendente: mutacao.isPending ? mutacao.variables.id : null,
  };
}
