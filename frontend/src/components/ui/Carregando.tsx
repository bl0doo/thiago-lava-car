/**
 * Esqueleto de carregamento do Design System. O texto fica só para o leitor de tela; quem vê, vê a forma
 * da tela chegando (cartões), sem animação quando o sistema pede movimento reduzido.
 */
export function Carregando({ tela = false, texto = 'Carregando…', cartoes = 2 }: { tela?: boolean; texto?: string; cartoes?: number }) {
  return (
    <div role="status" className={`flex flex-col gap-sm ${tela ? 'min-h-dvh justify-center p-lg' : 'py-md'}`}>
      <span className="sr-only">{texto}</span>
      <div aria-hidden="true" className="flex flex-col gap-sm motion-safe:animate-pulse">
        <div className="h-4 w-2/3 rounded-sm bg-rebaixado" />
        {Array.from({ length: cartoes }, (_, i) => (
          <div key={i} className="h-[110px] rounded-lg bg-rebaixado" />
        ))}
      </div>
    </div>
  );
}
