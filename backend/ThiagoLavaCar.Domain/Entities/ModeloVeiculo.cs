using ThiagoLavaCar.Domain.Common;
using ThiagoLavaCar.Domain.Enums;
using ThiagoLavaCar.Domain.Exceptions;

namespace ThiagoLavaCar.Domain.Entities;

/// <summary>Modelo comum de veículo, usado no autocompletar do cadastro; define o tipo (moto, carro, caminhonete).</summary>
public sealed class ModeloVeiculo : Entidade
{
    private ModeloVeiculo() { }

    public string Marca { get; private set; } = null!;
    public string Nome { get; private set; } = null!;
    public TipoVeiculo TipoVeiculo { get; private set; }

    public string Descricao => $"{Marca} {Nome}";

    public static ModeloVeiculo Criar(string marca, string nome, TipoVeiculo tipoVeiculo)
    {
        if (string.IsNullOrWhiteSpace(marca) || string.IsNullOrWhiteSpace(nome))
        {
            throw new RegraInvalidaException("Informe a marca e o nome do modelo.");
        }

        return new ModeloVeiculo { Marca = marca.Trim(), Nome = nome.Trim(), TipoVeiculo = tipoVeiculo };
    }
}
