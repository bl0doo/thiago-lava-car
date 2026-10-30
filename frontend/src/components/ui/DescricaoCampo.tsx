import { CircleAlert } from 'lucide-react';

type Props = { campoId: string; erro?: string; ajuda?: string };

export function descricaoCampo({ campoId, erro, ajuda }: Props): string | undefined {
  return [erro ? `${campoId}-erro` : null, ajuda ? `${campoId}-ajuda` : null].filter(Boolean).join(' ') || undefined;
}

export function DescricaoCampo({ campoId, erro, ajuda }: Props) {
  return (
    <>
      {erro ? (
        <p id={`${campoId}-erro`} className="flex items-start gap-2xs texto-rotulo-p leading-5 text-erro">
          <CircleAlert aria-hidden="true" className="mt-0.5 size-4 shrink-0" strokeWidth={2.5} />
          {erro}
        </p>
      ) : null}
      {ajuda ? (
        <p id={`${campoId}-ajuda`} className="texto-corpo-p text-secundario">
          {ajuda}
        </p>
      ) : null}
    </>
  );
}
