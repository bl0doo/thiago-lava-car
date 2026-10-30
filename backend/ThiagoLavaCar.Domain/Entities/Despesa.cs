using ThiagoLavaCar.Domain.Common;
using ThiagoLavaCar.Domain.Enums;
using ThiagoLavaCar.Domain.Exceptions;

namespace ThiagoLavaCar.Domain.Entities;

/// <summary>Gasto da empresa por categoria; as retiradas do dono ficam em <see cref="RetiradaDono"/>.</summary>
public sealed class Despesa : Entidade
{
    private Despesa() { }

    public int RegistradoPorId { get; private set; }
    public string Descricao { get; private set; } = null!;
    public CategoriaDespesa Categoria { get; private set; }
    public decimal Valor { get; private set; }
    public DateOnly Data { get; private set; }
    public DateTimeOffset CriadoEm { get; private set; }

    public static Despesa Criar(
        int registradoPorId, string descricao, CategoriaDespesa categoria, decimal valor, DateOnly data, DateOnly hoje, DateTimeOffset agora)
    {
        Validacao.Lancamento(descricao, valor, data, hoje);
        return new Despesa
        {
            RegistradoPorId = registradoPorId,
            Descricao = descricao.Trim(),
            Categoria = categoria,
            Valor = Dinheiro.Arredondar(valor),
            Data = data,
            CriadoEm = agora,
        };
    }

    public void Atualizar(string descricao, CategoriaDespesa categoria, decimal valor, DateOnly data, DateOnly hoje)
    {
        Validacao.Lancamento(descricao, valor, data, hoje);
        Descricao = descricao.Trim();
        Categoria = categoria;
        Valor = Dinheiro.Arredondar(valor);
        Data = data;
    }
}

internal static class Validacao
{
    /// <summary>Regras comuns de despesa e retirada: descrição obrigatória, valor &gt; 0, data não futura.</summary>
    public static void Lancamento(string? descricao, decimal valor, DateOnly data, DateOnly hoje)
    {
        if (string.IsNullOrWhiteSpace(descricao))
        {
            throw new RegraInvalidaException("A descrição é obrigatória.");
        }

        if (valor <= 0)
        {
            throw new RegraInvalidaException("O valor deve ser maior que zero.");
        }

        if (data > hoje)
        {
            throw new RegraInvalidaException("A data não pode ser futura.");
        }
    }
}
