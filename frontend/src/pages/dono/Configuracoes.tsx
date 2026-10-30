import { useState } from 'react';
import { BarraTitulo } from '@/components/layout/BarraTitulo';
import { Abas } from '@/components/ui/Abas';
import { DadosLavaJato } from './configuracoes/DadosLavaJato';
import { Mensagens } from './configuracoes/Mensagens';
import { Taxas } from './configuracoes/Taxas';

type Aba = 'lavajato' | 'taxas' | 'mensagens';

const ABAS: { id: Aba; rotulo: string }[] = [
  { id: 'lavajato', rotulo: 'Lava-jato' },
  { id: 'taxas', rotulo: 'Taxas' },
  { id: 'mensagens', rotulo: 'Mensagens' },
];

/** Configurações do dono: dados e horários, diárias padrão, taxas por forma de pagamento e modelos de mensagem. */
export function Configuracoes() {
  const [aba, setAba] = useState<Aba>('lavajato');
  return (
    <div className="mx-auto flex max-w-[40rem] flex-col">
      <BarraTitulo sobretitulo="Dono" titulo="Configurações" />
      <div className="flex flex-col gap-md px-md pb-lg">
        <Abas itens={ABAS} ativa={aba} onMudar={setAba} rotulo="Configuração" />
        {aba === 'lavajato' ? <DadosLavaJato /> : null}
        {aba === 'taxas' ? <Taxas /> : null}
        {aba === 'mensagens' ? <Mensagens /> : null}
      </div>
    </div>
  );
}
