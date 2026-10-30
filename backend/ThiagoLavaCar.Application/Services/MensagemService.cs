using System.Globalization;
using ThiagoLavaCar.Application.Abstractions;
using ThiagoLavaCar.Application.Common;
using ThiagoLavaCar.Application.DTOs;
using ThiagoLavaCar.Application.Exceptions;
using ThiagoLavaCar.Domain.Common;
using ThiagoLavaCar.Domain.Entities;
using ThiagoLavaCar.Domain.Enums;
using ThiagoLavaCar.Domain.Exceptions;

namespace ThiagoLavaCar.Application.Services;

public interface IMensagemService
{
    Task<MensagemGeradaResponse> GerarAsync(int ordemId, string tipo, CancellationToken ct);
    Task<MensagemEnviadaResponse> RegistrarEnvioAsync(int ordemId, string tipo, CancellationToken ct);
}

public sealed class MensagemService(
    IOrdemServicoRepository ordens,
    IPagamentoRepository pagamentos,
    IModeloMensagemRepository modelos,
    IConfiguracaoRepository configuracoes,
    IMensagemEnviadaRepository enviadas,
    IUnitOfWork unitOfWork,
    IUsuarioLogado usuarioLogado,
    TimeProvider relogio) : IMensagemService
{
    public async Task<MensagemGeradaResponse> GerarAsync(int ordemId, string tipo, CancellationToken ct)
    {
        var (texto, cliente) = await MontarAsync(ordemId, TiposMensagem.Parse(tipo), ct);
        return new MensagemGeradaResponse(texto, cliente.CanalPreferido.ToString(), LinkWhatsApp(cliente.Telefone, texto));
    }

    public async Task<MensagemEnviadaResponse> RegistrarEnvioAsync(int ordemId, string tipo, CancellationToken ct)
    {
        var tipoMensagem = TiposMensagem.Parse(tipo);
        var (texto, _) = await MontarAsync(ordemId, tipoMensagem, ct);
        var envio = MensagemEnviada.Registrar(ordemId, usuarioLogado.Id, tipoMensagem, texto, relogio.GetUtcNow());
        await enviadas.AdicionarAsync(envio, ct);
        await unitOfWork.SalvarAsync(ct);
        return new MensagemEnviadaResponse(envio.Id, TiposMensagem.Slug(tipoMensagem), envio.Texto, envio.EnviadaEm);
    }

    private async Task<(string Texto, Cliente Cliente)> MontarAsync(int ordemId, TipoMensagem tipo, CancellationToken ct)
    {
        var os = await ordens.ObterPorIdAsync(ordemId, ct) ?? throw new NaoEncontradoException("Ordem de serviço não encontrada.");
        var esperado = tipo == TipoMensagem.LavagemIniciada ? StatusOrdemServico.EmAndamento : StatusOrdemServico.Concluido;
        if (os.Status != esperado)
        {
            throw new ConflitoException(tipo == TipoMensagem.LavagemIniciada
                ? "A mensagem de lavagem iniciada só vale para uma OS em andamento."
                : "A mensagem de lavagem concluída só vale para uma OS concluída.");
        }

        var modelo = await modelos.ObterPorTipoAsync(tipo, ct) ?? throw new NaoEncontradoException("Modelo de mensagem não encontrado.");
        var configuracao = await configuracoes.ObterAsync(ct) ?? throw new NaoEncontradoException("Configuração do lava-jato não encontrada.");
        var pago = await pagamentos.SomarPagoAsync(ordemId, ct);
        var cliente = os.Cliente!;

        var dados = new DadosDaMensagem(
            cliente.Nome,
            os.Veiculo!.ModeloDescricao,
            os.Veiculo.Placa,
            string.Join(", ", os.Itens.Select(i => i.ComCera ? $"{i.NomeServico} com cera" : i.NomeServico)),
            os.DuracaoEstimadaMin,
            Math.Max(0m, Dinheiro.Arredondar(os.ValorTotal - pago)),
            configuracao.NomeLavaJato);
        return (MensagemRenderizador.Renderizar(modelo.Texto, dados), cliente);
    }

    private static string LinkWhatsApp(string telefone, string texto)
    {
        var numero = new string(telefone.Where(char.IsDigit).ToArray());
        if (!numero.StartsWith("55", StringComparison.Ordinal) || numero.Length < 12)
        {
            numero = "55" + numero;
        }

        return string.Create(CultureInfo.InvariantCulture, $"https://wa.me/{numero}?text={Uri.EscapeDataString(texto)}");
    }
}
