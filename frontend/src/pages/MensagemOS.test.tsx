import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { erroHttp } from '@/test/fabrica';
import { MensagemOS } from './dono/os/MensagemOS';

const api = vi.hoisted(() => ({ gerarMensagem: vi.fn(), registrarEnvio: vi.fn() }));
vi.mock('@/api/mensagens', () => api);

function renderizar(tipo: 'lavagem-iniciada' | 'lavagem-concluida' = 'lavagem-iniciada') {
  render(
    <QueryClientProvider client={new QueryClient({ defaultOptions: { queries: { retry: false } } })}>
      <MensagemOS osId={7} tipo={tipo} />
    </QueryClientProvider>,
  );
}

const gerada = { texto: 'Oi Renata, começamos a lavar seu Onix.', canal: 'WhatsApp', linkWhatsApp: 'https://wa.me/5511999990000?text=Oi' };

describe('Mensagem para o cliente (WhatsApp)', () => {
  beforeEach(() => {
    vi.resetAllMocks();
    api.gerarMensagem.mockResolvedValue(gerada);
    api.registrarEnvio.mockResolvedValue({ id: 1, tipo: 'lavagem-iniciada', texto: gerada.texto, enviadaEm: '2026-10-23T13:00:00Z' });
  });

  it('gera o texto e abre o WhatsApp em outra aba', async () => {
    renderizar();

    await userEvent.click(screen.getByRole('button', { name: 'Avisar que começou' }));

    expect(await screen.findByText(gerada.texto)).toBeInTheDocument();
    expect(api.gerarMensagem).toHaveBeenCalledWith(7, 'lavagem-iniciada');
    const link = screen.getByRole('link', { name: 'Abrir no WhatsApp' });
    expect(link).toHaveAttribute('href', gerada.linkWhatsApp);
    expect(link).toHaveAttribute('target', '_blank');
    expect(link).toHaveAttribute('rel', expect.stringContaining('noopener'));
  });

  it('registra o envio no histórico e some com o botão', async () => {
    renderizar('lavagem-concluida');

    await userEvent.click(screen.getByRole('button', { name: 'Avisar que ficou pronto' }));
    await userEvent.click(await screen.findByRole('button', { name: 'Já enviei' }));

    expect(await screen.findByText('Envio registrado no histórico.')).toBeInTheDocument();
    expect(api.registrarEnvio).toHaveBeenCalledWith(7, 'lavagem-concluida');
    expect(screen.queryByRole('button', { name: 'Já enviei' })).not.toBeInTheDocument();
  });

  it('sem link do WhatsApp, avisa em vez de mostrar um botão quebrado', async () => {
    api.gerarMensagem.mockResolvedValue({ ...gerada, canal: 'Telefone', linkWhatsApp: '' });

    renderizar();
    await userEvent.click(screen.getByRole('button', { name: 'Avisar que começou' }));

    expect(await screen.findByText(/não tem WhatsApp cadastrado/)).toBeInTheDocument();
    expect(screen.queryByRole('link', { name: 'Abrir no WhatsApp' })).not.toBeInTheDocument();
  });

  it('mostra o 409 da API quando a OS não está na etapa certa', async () => {
    api.gerarMensagem.mockRejectedValue(erroHttp(409, { detail: 'A OS precisa estar em andamento.' }));

    renderizar();
    await userEvent.click(screen.getByRole('button', { name: 'Avisar que começou' }));

    expect(await screen.findByText('A OS precisa estar em andamento.')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Avisar que começou' })).toBeInTheDocument();
  });
});
