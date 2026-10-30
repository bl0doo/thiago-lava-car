/** Lista paginada padrão da API: { itens, total, pagina, tamanho } (Entrega 2, seção 3). */
export type Pagina<T> = { itens: T[]; total: number; pagina: number; tamanho: number };
