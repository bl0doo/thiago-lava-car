using ThiagoLavaCar.Application.Abstractions;
using ThiagoLavaCar.Application.Common;
using ThiagoLavaCar.Application.DTOs;
using ThiagoLavaCar.Application.Exceptions;
using ThiagoLavaCar.Application.Mapping;
using ThiagoLavaCar.Domain.Common;
using ThiagoLavaCar.Domain.Entities;
using ThiagoLavaCar.Domain.Enums;
using ThiagoLavaCar.Domain.Exceptions;

namespace ThiagoLavaCar.Application.Services;

public sealed class OrdemServicoService(
    IOrdemServicoRepository ordens,
    IUsuarioRepository usuarios,
    IVeiculoRepository veiculos,
    IServicoRepository servicos,
    IUnitOfWork unitOfWork,
    IUsuarioLogado usuarioLogado,
    Auditoria auditoria,
    TimeProvider relogio) : IOrdemServicoService
{
    public async Task<QuadroResponse> ObterQuadroAsync(DateOnly? data, CancellationToken ct)
    {
        var dia = data ?? FusoBrasilia.DataDe(relogio.GetUtcNow());
        var (inicio, fim) = FusoBrasilia.IntervaloDoDia(dia);
        var cartoes = await ordens.ObterQuadroAsync(inicio, fim, ct);

        CartaoResponse[] Coluna(StatusOrdemServico status) => cartoes
            .Where(os => os.Status == status)
            .Select(os => CartaoMapper.Mapear(os, usuarioLogado.Perfil))
            .ToArray();

        return new QuadroResponse(dia, Coluna(StatusOrdemServico.Aguardando), Coluna(StatusOrdemServico.EmAndamento), Coluna(StatusOrdemServico.Concluido));
    }

    public async Task<CartaoResponse> ObterAsync(int id, CancellationToken ct) =>
        CartaoMapper.Mapear(await BuscarAsync(id, ct), usuarioLogado.Perfil);

    public async Task<OrdemCriadaResponse> RegistrarEntradaAsync(RegistrarEntradaRequest request, CancellationToken ct)
    {
        var veiculo = await veiculos.ObterPorIdAsync(request.VeiculoId, ct)
            ?? throw new NaoEncontradoException("Veículo não encontrado.");
        if (await ordens.ExisteAbertaDoVeiculoAsync(veiculo.Id, ct))
        {
            throw new ConflitoException("Este veículo já está no pátio.");
        }

        var (itens, ajustes) = await MontarItensAsync(veiculo, request.Itens, ct);

        var agora = relogio.GetUtcNow();
        var avarias = (request.Avarias ?? []).Select(a => Avaria.Criar(a.Local.Trim(), a.Tipo.Trim(), a.Descricao?.Trim())).ToList();
        var ordem = OrdemServico.Registrar(
            veiculo, usuarioLogado.Id, itens, request.SemAvarias, avarias, request.AceiteCliente,
            LimparTexto(request.PertencesEntrada), LimparTexto(request.NomeQuemRetira), agora);

        await ordens.AdicionarAsync(ordem, ct);
        await unitOfWork.SalvarAsync(ct);
        if (ajustes.Count > 0)
        {
            // A OS só ganha número depois de gravada, por isso a auditoria do preço manual vem logo em seguida.
            await RegistrarAjustesAsync(ordem.Id, ajustes, ct);
            await unitOfWork.SalvarAsync(ct);
        }

        return new OrdemCriadaResponse(ordem.Id, ordem.Status.ToString(), ordem.ValorTotal, ordem.DuracaoEstimadaMin, agora.AddMinutes(ordem.DuracaoEstimadaMin));
    }

    public async Task<CartaoResponse> EditarAsync(int id, EditarOrdemRequest request, CancellationToken ct)
    {
        var os = await BuscarAsync(id, ct);
        var (itens, ajustes) = await MontarItensAsync(os.Veiculo!, request.Itens, ct);
        os.Editar(itens, LimparTexto(request.PertencesEntrada), LimparTexto(request.NomeQuemRetira));
        await RegistrarAjustesAsync(os.Id, ajustes, ct);
        await unitOfWork.SalvarAsync(ct);
        return CartaoMapper.Mapear(os, usuarioLogado.Perfil);
    }

    public async Task<CartaoResponse> EntregarAsync(int id, string? nomeQuemRetira, CancellationToken ct)
    {
        var os = await BuscarAsync(id, ct);
        os.Entregar(relogio.GetUtcNow(), LimparTexto(nomeQuemRetira));
        await unitOfWork.SalvarAsync(ct);
        return CartaoMapper.Mapear(os, usuarioLogado.Perfil);
    }

    public async Task<CartaoResponse> CancelarAsync(int id, CancellationToken ct)
    {
        var os = await BuscarAsync(id, ct);
        os.Cancelar();
        await unitOfWork.SalvarAsync(ct);
        return CartaoMapper.Mapear(os, usuarioLogado.Perfil);
    }

    public async Task<CartaoResponse> AvaliarAsync(int id, int nota, CancellationToken ct)
    {
        var os = await BuscarAsync(id, ct);
        os.Avaliar(nota);
        await unitOfWork.SalvarAsync(ct);
        return CartaoMapper.Mapear(os, usuarioLogado.Perfil);
    }

    /// <summary>Preço de cada item pela tabela do tipo do veículo, copiado para o item (histórico imutável).</summary>
    private async Task<(List<ItemOrdemServico> Itens, List<AjusteDePreco> Ajustes)> MontarItensAsync(
        Veiculo veiculo, IReadOnlyList<ItemEntradaRequest> pedidos, CancellationToken ct)
    {
        var itens = new List<ItemOrdemServico>();
        var ajustes = new List<AjusteDePreco>();
        foreach (var pedido in pedidos)
        {
            var servico = await servicos.ObterPorIdAsync(pedido.ServicoId, ct)
                ?? throw new NaoEncontradoException("Serviço não encontrado.");
            if (!servico.Ativo)
            {
                throw new RegraInvalidaException($"O serviço \"{servico.Nome}\" está desativado.");
            }

            var precoTabela = servico.ObterPreco(veiculo.Tipo, pedido.ComCera);
            if (pedido.PrecoManual is { } manual)
            {
                var preco = Dinheiro.Arredondar(manual);
                ajustes.Add(new AjusteDePreco(servico.Id, servico.Nome, pedido.ComCera, precoTabela, preco));
                itens.Add(ItemOrdemServico.Criar(servico, pedido.ComCera, preco, precoAjustado: true));
            }
            else
            {
                itens.Add(ItemOrdemServico.Criar(servico, pedido.ComCera, precoTabela, precoAjustado: false));
            }
        }

        return (itens, ajustes);
    }

    private sealed record AjusteDePreco(int ServicoId, string Servico, bool ComCera, decimal PrecoTabela, decimal PrecoManual);

    private async Task RegistrarAjustesAsync(int ordemId, IReadOnlyList<AjusteDePreco> ajustes, CancellationToken ct)
    {
        foreach (var ajuste in ajustes)
        {
            await auditoria.RegistrarAsync("PrecoManual", nameof(OrdemServico), ordemId, ajuste, ct);
        }
    }

    public async Task<CartaoResponse> RegistrarPertencesEncontradosAsync(int id, string? texto, CancellationToken ct)
    {
        var os = await BuscarAsync(id, ct);
        os.RegistrarPertencesEncontrados(LimparTexto(texto));
        await unitOfWork.SalvarAsync(ct);
        return CartaoMapper.Mapear(os, usuarioLogado.Perfil);
    }

    private static string? LimparTexto(string? texto) => string.IsNullOrWhiteSpace(texto) ? null : texto.Trim();

    public async Task<CartaoResponse> IniciarAsync(int id, int? lavadorId, CancellationToken ct)
    {
        var os = await BuscarAsync(id, ct);
        var responsavel = await ResolverResponsavelAsync(lavadorId, ct);

        os.Iniciar(responsavel, relogio.GetUtcNow());
        await unitOfWork.SalvarAsync(ct);
        return CartaoMapper.Mapear(await BuscarAsync(id, ct), usuarioLogado.Perfil);
    }

    public async Task<CartaoResponse> EncerrarAsync(int id, CancellationToken ct)
    {
        var os = await BuscarAsync(id, ct);
        os.Encerrar(relogio.GetUtcNow());
        await unitOfWork.SalvarAsync(ct);
        return CartaoMapper.Mapear(os, usuarioLogado.Perfil);
    }

    public async Task<CartaoResponse> DesfazerAsync(int id, CancellationToken ct)
    {
        var os = await BuscarAsync(id, ct);
        os.Desfazer(relogio.GetUtcNow());
        await unitOfWork.SalvarAsync(ct);
        return CartaoMapper.Mapear(os, usuarioLogado.Perfil);
    }

    private async Task<Domain.Entities.OrdemServico> BuscarAsync(int id, CancellationToken ct) =>
        await ordens.ObterPorIdAsync(id, ct) ?? throw new NaoEncontradoException("Ordem de serviço não encontrada.");

    /// <summary>O lavador logado é o responsável; só o Dono pode indicar outro lavador.</summary>
    private async Task<int> ResolverResponsavelAsync(int? lavadorId, CancellationToken ct)
    {
        if (lavadorId is null || lavadorId == usuarioLogado.Id)
        {
            return usuarioLogado.Id;
        }

        if (usuarioLogado.Perfil != Perfil.Dono)
        {
            throw new RegraInvalidaException("Só o dono pode iniciar a lavagem em nome de outro lavador.");
        }

        var lavador = await usuarios.ObterPorIdAsync(lavadorId.Value, ct);
        if (lavador is not { Ativo: true, Perfil: Perfil.Lavador })
        {
            throw new RegraInvalidaException("Lavador não encontrado ou inativo.");
        }

        return lavador.Id;
    }
}
