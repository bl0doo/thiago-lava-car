import { useSearchParams } from 'react-router-dom';
import { BarraTitulo } from '@/components/layout/BarraTitulo';
import { ListaAgendamentos } from './ListaAgendamentos';
import { MeusDados } from './MeusDados';
import { MeusVeiculos } from './MeusVeiculos';

const ABAS = [
  { id: 'veiculos', rotulo: 'Veículos' },
  { id: 'historico', rotulo: 'Histórico' },
  { id: 'dados', rotulo: 'Meus dados' },
] as const;

type AbaId = (typeof ABAS)[number]['id'];

/** Tudo o que não é agendar: garagem, histórico de horários e dados pessoais. A aba vive na URL (?aba=). */
export function MinhaConta() {
  const [params, setParams] = useSearchParams();
  const pedida = params.get('aba');
  const aba: AbaId = ABAS.find((a) => a.id === pedida)?.id ?? 'veiculos';

  return (
    <div className="mx-auto flex max-w-[40rem] flex-col">
      <BarraTitulo sobretitulo="Cliente" titulo="Minha conta" />
      <div role="tablist" aria-label="Seções da conta" className="mx-md mb-md flex gap-2xs rounded-lg border-2 border-forte bg-superficie p-2xs">
        {ABAS.map(({ id, rotulo }) => (
          <button
            key={id}
            type="button"
            role="tab"
            id={`aba-${id}`}
            aria-selected={aba === id}
            aria-controls="painel-conta"
            onClick={() => setParams({ aba: id }, { replace: true })}
            className="min-h-alvo flex-1 rounded-md px-xs texto-rotulo-m text-secundario aria-selected:bg-acao-primaria aria-selected:text-primario"
          >
            {rotulo}
          </button>
        ))}
      </div>
      <div role="tabpanel" id="painel-conta" aria-labelledby={`aba-${aba}`} className="pb-lg">
        {aba === 'veiculos' ? <MeusVeiculos /> : null}
        {aba === 'historico' ? (
          <div className="px-md">
            <ListaAgendamentos mostrar="anteriores" />
          </div>
        ) : null}
        {aba === 'dados' ? <MeusDados /> : null}
      </div>
    </div>
  );
}
