using System.Globalization;
using ThiagoLavaCar.Application.Abstractions;
using ThiagoLavaCar.Application.Common;
using ThiagoLavaCar.Application.DTOs;
using ThiagoLavaCar.Application.Exceptions;
using ThiagoLavaCar.Domain.Entities;

namespace ThiagoLavaCar.Application.Services;

public interface IConfiguracaoService
{
    Task<ConfiguracaoResponse> ObterAsync(CancellationToken ct);
    Task<ConfiguracaoResponse> AtualizarAsync(AtualizarConfiguracaoRequest request, CancellationToken ct);
    Task<IReadOnlyList<ModeloMensagemResponse>> ListarModelosAsync(CancellationToken ct);
    Task<ModeloMensagemResponse> AtualizarModeloAsync(string tipo, AtualizarModeloMensagemRequest request, CancellationToken ct);
}

public sealed class ConfiguracaoService(
    IConfiguracaoRepository configuracoes,
    IModeloMensagemRepository modelos,
    Auditoria auditoria,
    IUnitOfWork unitOfWork,
    TimeProvider relogio) : IConfiguracaoService
{
    public async Task<ConfiguracaoResponse> ObterAsync(CancellationToken ct) =>
        Mapear(await configuracoes.ObterAsync(ct) ?? throw new NaoEncontradoException("Configuração do lava-jato não encontrada."));

    public async Task<ConfiguracaoResponse> AtualizarAsync(AtualizarConfiguracaoRequest request, CancellationToken ct)
    {
        var configuracao = await configuracoes.ObterParaEdicaoAsync(ct) ?? throw new NaoEncontradoException("Configuração do lava-jato não encontrada.");
        var antes = Mapear(configuracao);

        configuracao.Atualizar(
            request.NomeLavaJato,
            string.IsNullOrWhiteSpace(request.Telefone) ? null : request.Telefone.Trim(),
            TimeOnly.ParseExact(request.Abertura, "HH:mm", CultureInfo.InvariantCulture),
            TimeOnly.ParseExact(request.Fechamento, "HH:mm", CultureInfo.InvariantCulture),
            string.Join(',', request.DiasFuncionamento.Order()),
            request.DiariaSemana,
            request.DiariaSabado,
            request.IntervaloAgendaMin);

        await auditoria.RegistrarAsync("AjusteConfiguracao", nameof(Configuracao), Configuracao.IdUnico, new { antes, depois = Mapear(configuracao) }, ct);
        await unitOfWork.SalvarAsync(ct);
        return Mapear(configuracao);
    }

    public async Task<IReadOnlyList<ModeloMensagemResponse>> ListarModelosAsync(CancellationToken ct) =>
        (await modelos.ListarAsync(ct)).Select(Mapear).ToList();

    public async Task<ModeloMensagemResponse> AtualizarModeloAsync(string tipo, AtualizarModeloMensagemRequest request, CancellationToken ct)
    {
        var tipoMensagem = TiposMensagem.Parse(tipo);
        var modelo = await modelos.ObterPorTipoAsync(tipoMensagem, ct) ?? throw new NaoEncontradoException("Modelo de mensagem não encontrado.");
        modelo.Editar(request.Texto, relogio.GetUtcNow());
        await unitOfWork.SalvarAsync(ct);
        return Mapear(modelo);
    }

    private static ConfiguracaoResponse Mapear(Configuracao c) => new(
        c.NomeLavaJato,
        c.Telefone,
        c.Abertura.ToString("HH:mm", CultureInfo.InvariantCulture),
        c.Fechamento.ToString("HH:mm", CultureInfo.InvariantCulture),
        c.DiasFuncionamento.Split(',').Select(d => int.Parse(d, CultureInfo.InvariantCulture)).ToList(),
        c.DiariaSemana,
        c.DiariaSabado,
        c.IntervaloAgendaMin);

    private static ModeloMensagemResponse Mapear(ModeloMensagem m) => new(TiposMensagem.Slug(m.Tipo), m.Texto, m.AtualizadoEm);
}
