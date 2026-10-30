import { interpretarErro } from '@/api/erros';
import { Alerta } from './Alerta';
import { Button } from './Button';

type Props = { erro: unknown; aoTentarNovamente: () => unknown };

export function ErroConsulta({ erro, aoTentarNovamente }: Props) {
  return (
    <div className="flex flex-col gap-sm">
      <Alerta>{interpretarErro(erro).mensagem}</Alerta>
      <Button variante="contorno" onClick={() => void aoTentarNovamente()}>
        Tentar de novo
      </Button>
    </div>
  );
}
