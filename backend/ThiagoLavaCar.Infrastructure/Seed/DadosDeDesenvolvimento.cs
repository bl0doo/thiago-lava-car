using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ThiagoLavaCar.Application.Abstractions;
using ThiagoLavaCar.Domain.Entities;
using ThiagoLavaCar.Domain.Enums;
using ThiagoLavaCar.Infrastructure.Data;

namespace ThiagoLavaCar.Infrastructure.Seed;

/// <summary>
/// Dados para desenvolvimento local, ligados por <c>Seed:Habilitar=true</c>. Preços, tempos e diárias são os
/// informados pelo dono na Entrega 1 (seção "Dados do negócio"). Taxas de pagamento e custo de insumos o dono
/// ainda não sabe (hipótese H3 da Entrega 1): entram zerados e são cadastrados por ele em Configurações.
/// As senhas e PINs vêm da configuração (User Secrets ou variáveis); sem elas, os usuários não são criados.
/// </summary>
public sealed class DadosDeDesenvolvimento(
    AppDbContext db, ISenhaHasher hasher, TimeProvider relogio, IConfiguration configuracao, ILogger<DadosDeDesenvolvimento> log)
{
    public async Task ExecutarAsync(CancellationToken ct)
    {
        if (await db.Usuarios.AnyAsync(ct))
        {
            return;
        }

        var agora = relogio.GetUtcNow();
        var senhaDono = configuracao["Seed:DonoSenha"];
        var pinLavadores = configuracao["Seed:LavadorPin"];
        if (string.IsNullOrWhiteSpace(senhaDono) || string.IsNullOrWhiteSpace(pinLavadores))
        {
            log.LogWarning("Seed ignorado: defina Seed:DonoSenha e Seed:LavadorPin (User Secrets ou variáveis de ambiente).");
            return;
        }

        var dono = Usuario.CriarComSenha("Thiago", "thiago@lavacar.local", hasher.Gerar(senhaDono), Perfil.Dono, agora);
        var diego = Usuario.CriarLavador("Diego Alves", "Diego", hasher.Gerar(pinLavadores), agora);
        var marcos = Usuario.CriarLavador("Marcos Lima", "Marcos", hasher.Gerar(pinLavadores), agora);
        db.Usuarios.AddRange(dono, diego, marcos);

        // Tabela de preços do dono (Entrega 1). Cada serviço leva de 1h30 a 2h; motor e chassi não têm opção com cera.
        // Tempos de motor e chassi não foram informados: 30 min, como no exemplo da Entrega 2, até o dono confirmar.
        var lavagem = Servico.Criar("Lavagem e limpeza interna", "Lavagem", 90, 0m);
        lavagem.DefinirPreco(TipoVeiculo.Moto, 50m, 65m);
        lavagem.DefinirPreco(TipoVeiculo.Carro, 80m, 100m);
        lavagem.DefinirPreco(TipoVeiculo.Caminhonete, 120m, 150m);
        var motor = Servico.Criar("Motor", "Extra", 30, 0m);
        motor.DefinirPreco(TipoVeiculo.Carro, 120m, null);
        motor.DefinirPreco(TipoVeiculo.Caminhonete, 120m, null);
        var chassi = Servico.Criar("Chassi (parte de baixo)", "Extra", 30, 0m);
        chassi.DefinirPreco(TipoVeiculo.Carro, 140m, null);
        chassi.DefinirPreco(TipoVeiculo.Caminhonete, 140m, null);
        db.Servicos.AddRange(lavagem, motor, chassi);

        // Taxas de pagamento, configuração e modelos de mensagem vêm da migration DadosDeReferencia.

        db.ModelosVeiculo.AddRange(
            ModeloVeiculo.Criar("Chevrolet", "Onix", TipoVeiculo.Carro),
            ModeloVeiculo.Criar("Volkswagen", "Gol", TipoVeiculo.Carro),
            ModeloVeiculo.Criar("Fiat", "Strada", TipoVeiculo.Caminhonete),
            ModeloVeiculo.Criar("Toyota", "Hilux", TipoVeiculo.Caminhonete),
            ModeloVeiculo.Criar("Honda", "CG 160", TipoVeiculo.Moto));

        var renata =Cliente.Criar("Renata Souza", "44999998888", null, CanalContato.WhatsApp, true, agora);
        db.Clientes.Add(renata);
        await db.SaveChangesAsync(ct);

        // Diária do dia de cada lavador, pelo valor padrão da configuração (R$ 100 na semana, R$ 120 aos sábados).
        var hoje = DateOnly.FromDateTime(agora.UtcDateTime);
        var padrao = Configuracao.Padrao("Thiago Lava Car");
        db.Diarias.AddRange(
            Diaria.Criar(diego.Id, hoje, padrao.DiariaSugerida(hoje)),
            Diaria.Criar(marcos.Id, hoje, padrao.DiariaSugerida(hoje)));
        db.Despesas.Add(Despesa.Criar(dono.Id, "Shampoo automotivo (galão 5 L)", CategoriaDespesa.Insumos, 45m, hoje, hoje, agora));

        var onix = Veiculo.Criar(renata.Id, "ABC1D23", "Chevrolet Onix", TipoVeiculo.Carro, "Prata");
        var hilux = Veiculo.Criar(renata.Id, "XYZ9876", "Toyota Hilux", TipoVeiculo.Caminhonete, "Branca");
        db.Veiculos.AddRange(onix, hilux);
        await db.SaveChangesAsync(ct);

        OrdemServico Entrada(Veiculo veiculo, Servico servico, bool cera) => OrdemServico.Registrar(
            veiculo, dono.Id,
            [ItemOrdemServico.Criar(servico, cera, servico.ObterPreco(veiculo.Tipo, cera), false)],
            semAvarias: true, avarias: [], aceiteCliente: true, pertencesEntrada: null, nomeQuemRetira: null, agora);

        db.OrdensServico.AddRange(Entrada(onix, lavagem, true), Entrada(hilux, lavagem, false));
        await db.SaveChangesAsync(ct);
        log.LogInformation("Dados de desenvolvimento criados.");
    }
}
