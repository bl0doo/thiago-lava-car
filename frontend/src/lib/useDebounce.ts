import { useEffect, useState } from 'react';

/** Segura o valor por `ms` milissegundos; usado na busca para não chamar a API a cada tecla. */
export function useDebounce<T>(valor: T, ms = 300): T {
  const [atual, setAtual] = useState(valor);
  useEffect(() => {
    const t = setTimeout(() => setAtual(valor), ms);
    return () => clearTimeout(t);
  }, [valor, ms]);
  return atual;
}
