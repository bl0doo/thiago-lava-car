using System.Text.Json;
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

public class OrdemServicoServiceTests
{
    private static readonly JsonSerializerOptions OpcoesJson = new(JsonSerializerDefaults.Web);

    private readonly IOrdemServicoRepository _ordens = Substitute.For<IOrdemServicoRepository>();
    private readonly IUsuarioRepository _usuarios = Substitute.For<IUsuarioRepository>();
    private readonly IVeiculoRepository _veiculos = Substitute.For<IVeiculoRepository>();
    private readonly IServicoRepository _servicos = Substitute.For<IServicoRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IUsuarioLogado _logado = Substitute.For<IUsuarioLogado>();
    private readonly FakeTimeProvider _relogio = new(Fabrica.Agora);
    private readonly OrdemServicoService _servico;

    public OrdemServicoServiceTests()
    {
        _servico = new OrdemServicoService(_ordens, _usuarios, _veiculos, _servicos, _unitOfWork, _logado, new Auditoria(Substitute.For<IAuditoriaRepository>(), _logado, _relogio), _relogio);
    }

    private void LogarComo(Usuario usuario)
    {
        _logado.Id.Returns(usuario.Id);
        _logado.Perfil.Returns(usuario.Perfil);
    }

    /// <summary>OS completa o bastante para o mapeamento (veículo, cliente, lavador carregados).</summary>
    private static OrdemServico OsComRelacionamentos(int id = 88)
    {
        var os = Fabrica.OrdemAguardando(id);
        var cliente = Cliente.Criar("Renata Souza", "44999998888", null, CanalContato.WhatsApp, true, Fabrica.Agora).ComId(10);
        Set(os, nameof(OrdemServico.Veiculo), Fabrica.Onix());
        Set(os, nameof(OrdemServico.Cliente), cliente);
        return os;
    }

    private static void Set(object alvo, string propriedade, object? valor) =>
        alvo.GetType().GetProperty(propriedade)!.SetValue(alvo, valor);

    [Fact] // T11
    public async Task Quadro_para_o_lavador_nao_traz_valor_total_nem_telefone_nem_nome_do_cliente()
    {
        LogarComo(Fabrica.Lavador());
        _ordens.ObterQuadroAsync(Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns([OsComRelacionamentos()]);

        var quadro = await _servico.ObterQuadroAsync(null, default);

        var json = JsonSerializer.Serialize(quadro, OpcoesJson);
        Assert.DoesNotContain("valorTotal", json, StringComparison.Ordinal);
        Assert.DoesNotContain("telefone", json, StringComparison.Ordinal);
        Assert.DoesNotContain("cliente", json, StringComparison.Ordinal);
        Assert.DoesNotContain("Renata", json, StringComparison.Ordinal);
        Assert.Single(quadro.Aguardando);
    }

    [Fact]
    public async Task Quadro_para_o_dono_traz_valor_e_contato()
    {
        LogarComo(Fabrica.Dono());
        _ordens.ObterQuadroAsync(Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns([OsComRelacionamentos()]);

        var cartao = (await _servico.ObterQuadroAsync(null, default)).Aguardando.Single();

        Assert.Equal(100m, cartao.ValorTotal);
        Assert.Equal("44999998888", cartao.Telefone);
        Assert.Equal("Renata Souza", cartao.Cliente);
    }

    [Fact]
    public async Task Quadro_usa_o_dia_de_Brasilia_e_nao_o_dia_UTC()
    {
        LogarComo(Fabrica.Dono());
        _relogio.SetUtcNow(new DateTimeOffset(2026, 10, 21, 1, 30, 0, TimeSpan.Zero)); // 22h30 do dia 20 em Brasília
        _ordens.ObterQuadroAsync(Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>()).Returns([]);

        var quadro = await _servico.ObterQuadroAsync(null, default);

        Assert.Equal(new DateOnly(2026, 10, 20), quadro.Data);
        await _ordens.Received(1).ObterQuadroAsync(
            new DateTimeOffset(2026, 10, 20, 3, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 10, 21, 3, 0, 0, TimeSpan.Zero),
            Arg.Any<CancellationToken>());
    }

    [Fact] // T26
    public async Task Lavador_inicia_e_fica_gravado_como_responsavel()
    {
        var diego = Fabrica.Lavador();
        LogarComo(diego);
        var os = OsComRelacionamentos();
        _ordens.ObterPorIdAsync(88, Arg.Any<CancellationToken>()).Returns(os);

        var cartao = await _servico.IniciarAsync(88, null, default);

        Assert.Equal("EmAndamento", cartao.Status);
        Assert.Equal(2, os.LavadorId);
        Assert.Equal(Fabrica.Agora, os.InicioEm);
        await _unitOfWork.Received(1).SalvarAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Dono_pode_iniciar_em_nome_de_um_lavador_ativo()
    {
        LogarComo(Fabrica.Dono());
        var os = OsComRelacionamentos();
        _ordens.ObterPorIdAsync(88, Arg.Any<CancellationToken>()).Returns(os);
        _usuarios.ObterPorIdAsync(3, Arg.Any<CancellationToken>()).Returns(Fabrica.Lavador(3, "Marcos"));

        await _servico.IniciarAsync(88, 3, default);

        Assert.Equal(3, os.LavadorId);
    }

    [Fact]
    public async Task Dono_nao_pode_indicar_um_usuario_que_nao_e_lavador()
    {
        LogarComo(Fabrica.Dono());
        _ordens.ObterPorIdAsync(88, Arg.Any<CancellationToken>()).Returns(OsComRelacionamentos());
        _usuarios.ObterPorIdAsync(9, Arg.Any<CancellationToken>()).Returns(Fabrica.Dono());

        await Assert.ThrowsAsync<RegraInvalidaException>(() => _servico.IniciarAsync(88, 9, default));
    }

    [Fact]
    public async Task Lavador_nao_pode_iniciar_em_nome_de_outro()
    {
        LogarComo(Fabrica.Lavador());
        _ordens.ObterPorIdAsync(88, Arg.Any<CancellationToken>()).Returns(OsComRelacionamentos());

        await Assert.ThrowsAsync<RegraInvalidaException>(() => _servico.IniciarAsync(88, 3, default));
    }

    [Fact] // T27
    public async Task Encerrar_cartao_em_Aguardando_devolve_conflito_e_nao_grava()
    {
        LogarComo(Fabrica.Lavador());
        _ordens.ObterPorIdAsync(88, Arg.Any<CancellationToken>()).Returns(OsComRelacionamentos());

        await Assert.ThrowsAsync<ConflitoException>(() => _servico.EncerrarAsync(88, default));
        await _unitOfWork.DidNotReceive().SalvarAsync(Arg.Any<CancellationToken>());
    }

    [Fact] // T28
    public async Task Iniciar_e_encerrar_calcula_a_duracao_real_com_o_relogio_do_servidor()
    {
        LogarComo(Fabrica.Lavador());
        var os = OsComRelacionamentos();
        _ordens.ObterPorIdAsync(88, Arg.Any<CancellationToken>()).Returns(os);

        await _servico.IniciarAsync(88, null, default);
        _relogio.Advance(TimeSpan.FromMinutes(102));
        var cartao = await _servico.EncerrarAsync(88, default);

        Assert.Equal("Concluido", cartao.Status);
        Assert.Equal(102, cartao.DuracaoRealMin);
    }

    [Fact]
    public async Task Cartao_inexistente_devolve_nao_encontrado()
    {
        LogarComo(Fabrica.Dono());

        await Assert.ThrowsAsync<NaoEncontradoException>(() => _servico.ObterAsync(999, default));
    }
}

public class OrdemServicoEntradaTests
{
    private readonly IOrdemServicoRepository _ordens = Substitute.For<IOrdemServicoRepository>();
    private readonly IVeiculoRepository _veiculos = Substitute.For<IVeiculoRepository>();
    private readonly IServicoRepository _servicos = Substitute.For<IServicoRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IUsuarioLogado _logado = Substitute.For<IUsuarioLogado>();
    private readonly OrdemServicoService _servico;

    public OrdemServicoEntradaTests()
    {
        _logado.Id.Returns(1);
        _logado.Perfil.Returns(Perfil.Dono);
        _veiculos.ObterPorIdAsync(30, Arg.Any<CancellationToken>()).Returns(Fabrica.Onix());
        _servicos.ObterPorIdAsync(1, Arg.Any<CancellationToken>()).Returns(Fabrica.Lavagem());
        _servico = new OrdemServicoService(_ordens, Substitute.For<IUsuarioRepository>(), _veiculos, _servicos, _unitOfWork, _logado, new Auditoria(Substitute.For<IAuditoriaRepository>(), _logado, new Microsoft.Extensions.Time.Testing.FakeTimeProvider(Fabrica.Agora)), new Microsoft.Extensions.Time.Testing.FakeTimeProvider(Fabrica.Agora));
    }

    private static RegistrarEntradaRequest Entrada(bool cera = true, bool semAvarias = true, bool aceite = true) => new(
        30, [new ItemEntradaRequest(1, cera, null)], semAvarias, null, " Cadeirinha ", null, aceite);

    [Fact]
    public async Task Entrada_calcula_valor_duracao_e_previsao_com_o_preco_da_tabela_do_tipo()
    {
        var criada = await _servico.RegistrarEntradaAsync(Entrada(cera: true), default);

        Assert.Equal("Aguardando", criada.Status);
        Assert.Equal(100m, criada.ValorTotal);
        Assert.Equal(90, criada.DuracaoEstimadaMin);
        Assert.Equal(Fabrica.Agora.AddMinutes(90), criada.PrevisaoConclusao);
        await _ordens.Received(1).AdicionarAsync(Arg.Is<OrdemServico>(o => o.PertencesEntrada == "Cadeirinha" && o.CriadoPorId == 1), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Veiculo_ja_no_patio_devolve_409()
    {
        _ordens.ExisteAbertaDoVeiculoAsync(30, Arg.Any<CancellationToken>()).Returns(true);

        var erro = await Assert.ThrowsAsync<ConflitoException>(() => _servico.RegistrarEntradaAsync(Entrada(), default));

        Assert.Equal("Este veículo já está no pátio.", erro.Message);
    }

    [Fact]
    public async Task Servico_sem_preco_para_o_tipo_ou_desativado_devolve_422()
    {
        var moto = Fabrica.Onix();
        _veiculos.ObterPorIdAsync(31, Arg.Any<CancellationToken>()).Returns(Veiculo.Criar(10, "MOT1A23", "Honda CG", TipoVeiculo.Moto, null).ComId(31));

        await Assert.ThrowsAsync<RegraInvalidaException>(() =>
            _servico.RegistrarEntradaAsync(Entrada() with { VeiculoId = 31 }, default)); // moto não tem opção com cera

        var inativo = Fabrica.Lavagem();
        inativo.DefinirAtivo(false);
        _servicos.ObterPorIdAsync(1, Arg.Any<CancellationToken>()).Returns(inativo);
        await Assert.ThrowsAsync<RegraInvalidaException>(() => _servico.RegistrarEntradaAsync(Entrada(cera: false), default));
        Assert.NotNull(moto);
    }

    [Fact]
    public async Task Veiculo_ou_servico_inexistente_devolve_404()
    {
        await Assert.ThrowsAsync<NaoEncontradoException>(() => _servico.RegistrarEntradaAsync(Entrada() with { VeiculoId = 99 }, default));
        await Assert.ThrowsAsync<NaoEncontradoException>(() =>
            _servico.RegistrarEntradaAsync(Entrada() with { Itens = [new ItemEntradaRequest(77, false, null)] }, default));
    }

    [Fact]
    public void Validador_rejeita_vistoria_inconsistente_e_entrada_sem_aceite()
    {
        var v = new RegistrarEntradaRequestValidator();
        AvariaRequest[] uma = [new("Para-choque", "Risco", null)];

        var semAvariasMasListadas = v.Validate(Entrada(semAvarias: true) with { Avarias = uma });
        var comAvariasMasVazia = v.Validate(Entrada(semAvarias: false) with { Avarias = [] });
        var semAceite = v.Validate(Entrada(aceite: false));

        Assert.Contains(semAvariasMasListadas.Errors, e => e.PropertyName == "Avarias");
        Assert.Contains(comAvariasMasVazia.Errors, e => e.PropertyName == "Avarias");
        Assert.Contains(semAceite.Errors, e => e.PropertyName == "AceiteCliente");
    }

    [Fact]
    public void Validador_rejeita_lista_vazia_de_itens_e_preco_manual_nao_positivo()
    {
        var v = new RegistrarEntradaRequestValidator();

        Assert.False(v.Validate(Entrada() with { Itens = [] }).IsValid);
        Assert.False(v.Validate(Entrada() with { Itens = [new ItemEntradaRequest(1, false, 0m)] }).IsValid);
        Assert.True(v.Validate(Entrada() with { Itens = [new ItemEntradaRequest(1, false, 50m)] }).IsValid);
        Assert.True(v.Validate(Entrada()).IsValid);
        Assert.True(v.Validate(Entrada(semAvarias: false) with { Avarias = [new AvariaRequest("Para-choque", "Risco", "5 cm")] }).IsValid);
    }
}
