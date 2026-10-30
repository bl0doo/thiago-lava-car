using ThiagoLavaCar.Domain.Entities;
using ThiagoLavaCar.Domain.Enums;
using ThiagoLavaCar.Domain.Exceptions;

namespace ThiagoLavaCar.UnitTests.Domain;

public class UsuarioTests
{
    private static readonly DateTimeOffset Agora = Fabrica.Agora;

    [Fact] // T02
    public void Primeira_falha_soma_uma_tentativa_sem_bloquear()
    {
        var usuario = Fabrica.Dono();

        usuario.RegistrarFalhaDeLogin(Agora);

        Assert.Equal(1, usuario.FalhasLogin);
        Assert.False(usuario.EstaBloqueado(Agora));
    }

    [Fact] // T03
    public void Quinta_falha_seguida_bloqueia_por_15_minutos()
    {
        var usuario = Fabrica.Dono();

        for (var i = 0; i < Usuario.MaximoFalhasSeguidas; i++)
        {
            usuario.RegistrarFalhaDeLogin(Agora);
        }

        Assert.True(usuario.EstaBloqueado(Agora));
        Assert.True(usuario.EstaBloqueado(Agora.AddMinutes(14)));
        Assert.False(usuario.EstaBloqueado(Agora.AddMinutes(15)));
    }

    [Fact]
    public void Login_com_sucesso_zera_as_falhas()
    {
        var usuario = Fabrica.Dono();
        usuario.RegistrarFalhaDeLogin(Agora);
        usuario.RegistrarFalhaDeLogin(Agora);

        usuario.RegistrarLoginComSucesso();

        Assert.Equal(0, usuario.FalhasLogin);
    }

    [Fact]
    public void Lavador_nao_pode_ser_criado_com_senha()
    {
        Assert.Throws<RegraInvalidaException>(() =>
            Usuario.CriarComSenha("Diego", "diego@lavacar.com", "hash", Perfil.Lavador, Agora));
    }

    [Fact]
    public void Email_e_gravado_em_minusculas_e_sem_espacos()
    {
        var usuario = Usuario.CriarComSenha("Thiago", "  Thiago@LavaCar.com ", "hash", Perfil.Dono, Agora);

        Assert.Equal("thiago@lavacar.com", usuario.Email);
    }
}
