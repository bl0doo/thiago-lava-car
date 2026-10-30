import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { Dialogo } from './Dialogo';

describe('Dialogo', () => {
  it('mostra título e chama confirmar e fechar', async () => {
    const onConfirmar = vi.fn();
    const onFechar = vi.fn();
    render(
      <Dialogo aberto titulo="Excluir despesa?" confirmar="Excluir" tipo="perigo" onConfirmar={onConfirmar} onFechar={onFechar}>
        Não dá para desfazer.
      </Dialogo>,
    );
    expect(screen.getByRole('dialog', { name: 'Excluir despesa?' })).toBeInTheDocument();
    await userEvent.click(screen.getByRole('button', { name: 'Excluir' }));
    await userEvent.click(screen.getByRole('button', { name: 'Voltar' }));
    expect(onConfirmar).toHaveBeenCalledOnce();
    expect(onFechar).toHaveBeenCalledOnce();
  });
});
