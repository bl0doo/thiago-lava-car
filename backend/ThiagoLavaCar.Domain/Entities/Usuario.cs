using ThiagoLavaCar.Domain.Common;
using ThiagoLavaCar.Domain.Enums;
using ThiagoLavaCar.Domain.Exceptions;

namespace ThiagoLavaCar.Domain.Entities;

public sealed class Usuario : Entidade
{
    public const int MaximoFalhasSeguidas = 5;
    public static readonly TimeSpan DuracaoBloqueio = TimeSpan.FromMinutes(15);

    private Usuario() { }

    public string Nome { get; private set; } = null!;
    public string? Apelido { get; private set; }
    public string? Email { get; private set; }
    public Perfil Perfil { get; private set; }
    public string? SenhaHash { get; private set; }
    public string? PinHash { get; private set; }
    public bool Ativo { get; private set; } = true;
    public int FalhasLogin { get; private set; }
    public DateTimeOffset? BloqueadoAte { get; private set; }
    public DateTimeOffset CriadoEm { get; private set; }

    /// <summary>Dono e Cliente entram com e-mail e senha.</summary>
    public static Usuario CriarComSenha(string nome, string email, string senhaHash, Perfil perfil, DateTimeOffset agora)
    {
        if (perfil == Perfil.Lavador)
        {
            throw new RegraInvalidaException("Lavador entra com PIN, não com senha.");
        }

        return new Usuario
        {
            Nome = nome,
            Email = email.Trim().ToLowerInvariant(),
            SenhaHash = senhaHash,
            Perfil = perfil,
            CriadoEm = agora,
        };
    }

    /// <summary>Lavador entra com apelido e PIN.</summary>
    public static Usuario CriarLavador(string nome, string apelido, string pinHash, DateTimeOffset agora) =>
        new()
        {
            Nome = nome,
            Apelido = apelido,
            PinHash = pinHash,
            Perfil = Perfil.Lavador,
            CriadoEm = agora,
        };

    public bool EstaBloqueado(DateTimeOffset agora) => BloqueadoAte is { } ate && ate > agora;

    /// <summary>Soma uma falha; na 5ª seguida bloqueia por 15 minutos e reinicia o contador.</summary>
    public void RegistrarFalhaDeLogin(DateTimeOffset agora)
    {
        FalhasLogin++;
        if (FalhasLogin >= MaximoFalhasSeguidas)
        {
            BloqueadoAte = agora + DuracaoBloqueio;
            FalhasLogin = 0;
        }
    }

    public void RegistrarLoginComSucesso()
    {
        FalhasLogin = 0;
        BloqueadoAte = null;
    }

    public void AlterarNomes(string nome, string? apelido)
    {
        Nome = nome;
        Apelido = Perfil == Perfil.Lavador ? apelido : null;
    }

    /// <summary>Troca o PIN do lavador e zera o bloqueio: quem esqueceu o PIN volta a entrar com o novo.</summary>
    public void RedefinirPin(string pinHash)
    {
        if (Perfil != Perfil.Lavador)
        {
            throw new RegraInvalidaException("Só o lavador tem PIN.");
        }

        PinHash = pinHash;
        RegistrarLoginComSucesso();
    }

    public void Desativar() => Ativo = false;

    public void Ativar() => Ativo = true;
}
