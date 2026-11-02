import { destinoAposLogin } from './AuthContext';

describe('destinoAposLogin', () => {
  it('sem rota de origem, vai para a inicial do perfil', () => {
    expect(destinoAposLogin('Cliente')).toBe('/cliente/agendar');
    expect(destinoAposLogin('Dono')).toBe('/dono');
  });

  it('volta para a rota de origem quando ela é da área do perfil', () => {
    expect(destinoAposLogin('Cliente', '/cliente/conta')).toBe('/cliente/conta');
    expect(destinoAposLogin('Dono', '/dono/caixa')).toBe('/dono/caixa');
  });

  it('ignora a origem de outra área (evita cair em /sem-permissao)', () => {
    expect(destinoAposLogin('Cliente', '/dono')).toBe('/cliente/agendar');
    expect(destinoAposLogin('Cliente', '/lavador/os/3')).toBe('/cliente/agendar');
    expect(destinoAposLogin('Dono', '/clientela')).toBe('/dono');
  });
});
