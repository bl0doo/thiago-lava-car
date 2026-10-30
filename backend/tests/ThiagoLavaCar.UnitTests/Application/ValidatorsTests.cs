using ThiagoLavaCar.Application.DTOs;
using ThiagoLavaCar.Application.Validators;

namespace ThiagoLavaCar.UnitTests.Application;

public class ValidatorsTests
{
    private readonly LoginPinRequestValidator _pin = new();
    private readonly LoginRequestValidator _login = new();

    [Theory] // T06
    [InlineData("123")]
    [InlineData("12ab")]
    [InlineData("12345")]
    [InlineData("482100")]
    [InlineData("")]
    public void Pin_invalido_e_recusado_no_campo_pin(string pin)
    {
        var resultado = _pin.Validate(new LoginPinRequest(2, pin));

        Assert.False(resultado.IsValid);
        Assert.All(resultado.Errors, e => Assert.Equal(nameof(LoginPinRequest.Pin), e.PropertyName));
    }

    [Theory]
    [InlineData("1234")]
    [InlineData("4821")]
    public void Pin_de_4_digitos_e_aceito(string pin) => Assert.True(_pin.Validate(new LoginPinRequest(2, pin)).IsValid);

    [Fact]
    public void Lavador_e_obrigatorio_no_login_por_pin() => Assert.False(_pin.Validate(new LoginPinRequest(0, "1234")).IsValid);

    [Theory]
    [InlineData("", "senha")]
    [InlineData("nao-e-email", "senha")]
    [InlineData("a@b.com", "")]
    public void Login_com_email_ou_senha_invalidos_e_recusado(string email, string senha) =>
        Assert.False(_login.Validate(new LoginRequest(email, senha)).IsValid);

    [Fact]
    public void Login_com_email_e_senha_e_aceito() => Assert.True(_login.Validate(new LoginRequest("a@b.com", "x")).IsValid);
}
