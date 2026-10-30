import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { useState } from 'react';
import { TecladoPin } from './TecladoPin';

function Exemplo({ onConfirmar }: { onConfirmar: (v: string) => void }) {
  const [valor, setValor] = useState('');
  return <TecladoPin valor={valor} onChange={setValor} onConfirmar={onConfirmar} />;
}

describe('TecladoPin', () => {
  it('envia sozinho ao completar 4 dígitos e ignora toques a mais', async () => {
    const usuario = userEvent.setup();
    const aoConfirmar = vi.fn();
    render(<Exemplo onConfirmar={aoConfirmar} />);

    for (const d of '482') await usuario.click(screen.getByRole('button', { name: d }));
    expect(aoConfirmar).not.toHaveBeenCalled();
    await usuario.click(screen.getByRole('button', { name: '1' }));
    await usuario.click(screen.getByRole('button', { name: '9' }));

    expect(aoConfirmar).toHaveBeenCalledExactlyOnceWith('4821');
  });

  it('apaga o último dígito', async () => {
    const usuario = userEvent.setup();
    render(<Exemplo onConfirmar={vi.fn()} />);
    await usuario.click(screen.getByRole('button', { name: '1' }));
    await usuario.click(screen.getByRole('button', { name: '2' }));
    expect(screen.getByLabelText('2 de 4 dígitos')).toBeInTheDocument();

    await usuario.click(screen.getByRole('button', { name: 'Apagar último dígito' }));

    expect(screen.getByLabelText('1 de 4 dígitos')).toBeInTheDocument();
  });

  it('os botões têm a altura mínima de toque', () => {
    render(<Exemplo onConfirmar={vi.fn()} />);
    expect(screen.getByRole('button', { name: '5' })).toHaveClass('min-h-alvo-lavador-principal');
  });
});
