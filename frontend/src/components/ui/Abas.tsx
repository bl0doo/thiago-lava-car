type Aba<T extends string> = { id: T; rotulo: string };

/** Abas simples (ex.: Despesas | Retiradas). O conteúdo é do chamador; aqui só o seletor com `aria-pressed`. */
export function Abas<T extends string>({ itens, ativa, onMudar, rotulo }: { itens: Aba<T>[]; ativa: T; onMudar: (id: T) => void; rotulo: string }) {
  return (
    <div role="group" aria-label={rotulo} className="flex gap-xs">
      {itens.map(({ id, rotulo: texto }) => (
        <button
          key={id}
          type="button"
          aria-pressed={id === ativa}
          onClick={() => onMudar(id)}
          className={`min-h-alvo flex-1 rounded-md border-2 px-md texto-rotulo-m ${id === ativa ? 'border-forte bg-inverso text-texto-inverso' : 'border-padrao bg-superficie text-primario'}`}
        >
          {texto}
        </button>
      ))}
    </div>
  );
}
