using ThiagoLavaCar.Domain.Common;
using ThiagoLavaCar.Domain.Enums;
using ThiagoLavaCar.Domain.Exceptions;

namespace ThiagoLavaCar.Domain.Entities;

public sealed class Veiculo : Entidade
{
    private Veiculo() { }

    public int ClienteId { get; private set; }
    public int? ModeloVeiculoId { get; private set; }
    public string Placa { get; private set; } = null!;
    public string ModeloDescricao { get; private set; } = null!;
    public TipoVeiculo Tipo { get; private set; }
    public string? Cor { get; private set; }
    public bool Ativo { get; private set; } = true;
    public DateTimeOffset CriadoEm { get; private set; }

    public Cliente? Cliente { get; private set; }

    public static Veiculo Criar(int clienteId, string placa, string modeloDescricao, TipoVeiculo tipo, string? cor, int? modeloVeiculoId = null)
    {
        var veiculo = new Veiculo
        {
            ClienteId = clienteId,
            ModeloVeiculoId = modeloVeiculoId,
        };
        veiculo.Atualizar(placa, modeloDescricao, tipo, cor);
        return veiculo;
    }

    public void Atualizar(string placa, string modeloDescricao, TipoVeiculo tipo, string? cor)
    {
        if (!PlacaBrasileira.EhValida(placa))
        {
            throw new RegraInvalidaException("Placa inválida. Use o formato ABC1234 ou ABC1D23.");
        }

        Placa = PlacaBrasileira.Normalizar(placa);
        ModeloDescricao = modeloDescricao;
        Tipo = tipo;
        Cor = cor;
    }
}
