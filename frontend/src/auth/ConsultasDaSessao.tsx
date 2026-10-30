import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { useEffect, useState, useSyncExternalStore, type ReactNode } from 'react';
import { sessao } from './sessao';

function CacheDeConsultas({ children }: { children: ReactNode }) {
  const [cliente] = useState(() => new QueryClient({
    defaultOptions: { queries: { retry: 1, staleTime: 5_000 } },
  }));

  useEffect(() => () => cliente.clear(), [cliente]);

  return <QueryClientProvider client={cliente}>{children}</QueryClientProvider>;
}

export function ConsultasDaSessao({ children }: { children: ReactNode }) {
  const { usuario } = useSyncExternalStore(sessao.assinar, sessao.obter);

  // Cada usuário tem seu próprio cliente: respostas de mutações antigas ficam no cache anterior.
  return <CacheDeConsultas key={usuario?.id ?? 'anonimo'}>{children}</CacheDeConsultas>;
}
