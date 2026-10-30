import { useEffect, useState } from 'react';

/** Devolve o instante atual (ms), atualizado a cada `intervaloMs`. Alimenta os cronômetros dos cartões. */
export function useAgora(intervaloMs = 1000): number {
  const [agora, setAgora] = useState(() => Date.now());
  useEffect(() => {
    const id = window.setInterval(() => setAgora(Date.now()), intervaloMs);
    return () => window.clearInterval(id);
  }, [intervaloMs]);
  return agora;
}
