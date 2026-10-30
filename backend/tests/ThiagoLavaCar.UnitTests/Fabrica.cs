using System.Reflection;
using ThiagoLavaCar.Domain.Common;
using ThiagoLavaCar.Domain.Entities;
using ThiagoLavaCar.Domain.Enums;

namespace ThiagoLavaCar.UnitTests;

/// <summary>Monta entidades válidas para os testes. O EF atribui o Id no banco; aqui ele é definido por reflexão.</summary>
internal static class Fabrica
{
    public static readonly DateTimeOffset Agora = new(2026, 10, 20, 13, 5, 0, TimeSpan.Zero);

    public static T ComId<T>(this T entidade, int id) where T : Entidade
    {
        typeof(Entidade).GetProperty(nameof(Entidade.Id), BindingFlags.Instance | BindingFlags.Public)!.SetValue(entidade, id);
        return entidade;
    }

    public static Usuario Dono(string hash = "hash-senha") =>
        Usuario.CriarComSenha("Thiago", "thiago@lavacar.com", hash, Perfil.Dono, Agora).ComId(1);

    public static Usuario Lavador(int id = 2, string apelido = "Diego", string hash = "hash-pin") =>
        Usuario.CriarLavador("Diego Alves", apelido, hash, Agora).ComId(id);

    public static Servico Lavagem()
    {
        var servico = Servico.Criar("Lavagem completa", "Lavagem", 90, 8m).ComId(1);
        servico.DefinirPreco(TipoVeiculo.Carro, 80m, 100m);
        servico.DefinirPreco(TipoVeiculo.Moto, 40m, null);
        return servico;
    }

    public static Veiculo Onix() => Veiculo.Criar(10, "abc-1d23", "Chevrolet Onix", TipoVeiculo.Carro, "Prata").ComId(30);

    public static OrdemServico OrdemAguardando(int id = 88)
    {
        var veiculo = Onix();
        var servico = Lavagem();
        return OrdemServico.Registrar(
            veiculo, criadoPorId: 1,
            [ItemOrdemServico.Criar(servico, true, servico.ObterPreco(TipoVeiculo.Carro, true), false)],
            semAvarias: true, avarias: [], aceiteCliente: true, pertencesEntrada: null, nomeQuemRetira: null, Agora).ComId(id);
    }
}
