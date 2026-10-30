using Microsoft.Extensions.Time.Testing;
using ThiagoLavaCar.Application.Abstractions;
using ThiagoLavaCar.Application.DTOs;
using ThiagoLavaCar.Application.Exceptions;
using ThiagoLavaCar.Application.Services;
using ThiagoLavaCar.Application.Validators;
using ThiagoLavaCar.Domain.Entities;
using ThiagoLavaCar.Domain.Enums;
using ThiagoLavaCar.Domain.Exceptions;

namespace ThiagoLavaCar.UnitTests.Application;

public class UsuarioServiceTests
{
    private readonly IUsuarioRepository _usuarios = Substitute.For<IUsuarioRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ISenhaHasher _hasher = Substitute.For<ISenhaHasher>();
    private readonly UsuarioService _servico;

    public UsuarioServiceTests()
    {
        _hasher.Gerar(Arg.Any<string>()).Returns(ci => "hash:" + ci.Arg<string>());
        _servico = new UsuarioService(_usuarios, Substitute.For<IRefreshTokenRepository>(), _unitOfWork, _hasher, Substitute.For<IUsuarioLogado>(), new FakeTimeProvider(Fabrica.Agora));
    }

    [Fact]
    public async Task Cria_lavador_com_pin_em_hash_e_apelido_sem_espacos()
    {
        var resposta = await _servico.CriarLavadorAsync(new CriarLavadorRequest(" Rafael Lima ", " Rafa ", "7391"), CancellationToken.None);

        Assert.Equal(("Rafael Lima", "Rafa", "Lavador", true), (resposta.Nome, resposta.Apelido, resposta.Perfil, resposta.Ativo));
        await _usuarios.Received(1).AdicionarAsync(Arg.Is<Usuario>(u => u.PinHash == "hash:7391" && u.Perfil == Perfil.Lavador), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SalvarAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Apelido_repetido_da_conflito_e_nao_grava()
    {
        _usuarios.ApelidoDeLavadorExisteAsync("Rafa", Arg.Any<CancellationToken>()).Returns(true);

        await Assert.ThrowsAsync<ConflitoException>(() => _servico.CriarLavadorAsync(new CriarLavadorRequest("Rafael", "Rafa", "7391"), CancellationToken.None));

        await _unitOfWork.DidNotReceive().SalvarAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Lista_filtra_por_perfil_sem_diferenciar_maiusculas_e_recusa_perfil_invalido()
    {
        _usuarios.ListarAsync(Perfil.Lavador, Arg.Any<CancellationToken>()).Returns([Fabrica.Lavador(2), Fabrica.Lavador(3, "Marcos")]);

        var lista = await _servico.ListarAsync("lavador", CancellationToken.None);

        Assert.Equal([2, 3], lista.Select(u => u.Id));
        await Assert.ThrowsAsync<RegraInvalidaException>(() => _servico.ListarAsync("Chefe", CancellationToken.None));
        await Assert.ThrowsAsync<RegraInvalidaException>(() => _servico.ListarAsync("1", CancellationToken.None));
    }

    [Theory]
    [InlineData("Rafael", "Rafa", "7391", true)]
    [InlineData("R", "Rafa", "7391", false)]
    [InlineData("Rafael", "", "7391", false)]
    [InlineData("Rafael", "Rafa", "1234", false)]
    [InlineData("Rafael", "Rafa", "73", false)]
    public void Validador_de_novo_lavador(string nome, string apelido, string pin, bool valido) =>
        Assert.Equal(valido, new CriarLavadorRequestValidator().Validate(new CriarLavadorRequest(nome, apelido, pin)).IsValid);
}
