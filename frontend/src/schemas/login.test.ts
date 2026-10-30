import { loginSchema, pinSchema } from './login';

describe('loginSchema', () => {
  it('aceita e-mail e senha', () => {
    expect(loginSchema.safeParse({ email: 'thiago@lavacar.com', senha: 'x' }).success).toBe(true);
  });

  it.each([
    [{ email: '', senha: 'x' }, 'Informe o e-mail.'],
    [{ email: 'nao-e-email', senha: 'x' }, 'E-mail inválido.'],
    [{ email: 'a@b.com', senha: '' }, 'Informe a senha.'],
  ])('recusa %j com a mensagem da API', (entrada, mensagem) => {
    const resultado = loginSchema.safeParse(entrada);
    expect(resultado.success).toBe(false);
    expect(resultado.error?.issues[0]?.message).toBe(mensagem);
  });
});

describe('pinSchema', () => {
  it.each(['1234', '4821'])('aceita %s', (pin) => {
    expect(pinSchema.safeParse(pin).success).toBe(true);
  });

  it.each(['123', '12ab', '12345', ''])('recusa %j', (pin) => {
    expect(pinSchema.safeParse(pin).success).toBe(false);
  });
});
