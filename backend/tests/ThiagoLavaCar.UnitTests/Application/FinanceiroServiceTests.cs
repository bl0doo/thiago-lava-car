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

public class FinanceiroServiceTests
{
    // 2026-10-20 é terça-feira; 2026-10-17 foi sábado.
    private static readonly DateOnly Hoje = new(2026, 10, 20);
    private static readonly DateOnly Sabado = new(2026, 10, 17);

    private readonly IDespesaRepository _despesas = Substitute.For<IDespesaRepository>();
    private readonly IRetiradaRepository _retiradas = Substitute.For<IRetiradaRepository>();
    private readonly IDiariaRepository _diarias = Substitute.For<IDiariaRepository>();
    private readonly IUsuarioRepository _usuarios = Substitute.For<IUsuarioRepository>();
    private readonly IConfiguracaoRepository _configuracao = Substitute.For<IConfiguracaoRepository>();
    private readonly IAuditoriaRepository _logs = Substitute.For<IAuditoriaRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IUsuarioLogado _logado = Substitute.For<IUsuarioLogado>();
    private readonly FakeTimeProvider _relogio = new(Fabrica.Agora);
    private readonly Auditoria _auditoria;

    public FinanceiroServiceTests()
    {
        _logado.Id.Returns(1);
        _logado.Perfil.Returns(Perfil.Dono);
        _auditoria = new Auditoria(_logs, _logado, _relogio);
        _usuarios.ObterPorIdAsync(2, Arg.Any<CancellationToken>()).Returns(Fabrica.Lavador(2));
        _usuarios.ObterPorIdAsync(3, Arg.Any<CancellationToken>()).Returns(Fabrica.Lavador(3, "Marcos"));
        _usuarios.ObterPorIdAsync(1, Arg.Any<CancellationToken>()).Returns(Fabrica.Dono());
        _configuracao.ObterAsync(Arg.Any<CancellationToken>()).Returns(Configuracao.Padrao("Thiago Lava Car"));
        _diarias.LavadoresComDiariaAsync(Arg.Any<DateOnly>(), Arg.Any<CancellationToken>()).Returns([]);
    }

    private DespesaService Despesas() => new(_despesas, _auditoria, _unitOfWork, _logado, _relogio);
    private RetiradaService Retiradas() => new(_retiradas, _auditoria, _unitOfWork, _logado, _relogio);
    private DiariaService Diarias() => new(_diarias, _usuarios, _configuracao, _auditoria, _unitOfWork, _relogio);

    [Fact]
    public async Task Despesa_e_criada_com_quem_registrou_e_valor_arredondado()
    {
        var resposta = await Despesas().CriarAsync(new SalvarDespesaRequest("Shampoo", "Insumos", 45.005m, Hoje), CancellationToken.None);

        Assert.Equal(("Shampoo", "Insumos", 45.01m), (resposta.Descricao, resposta.Categoria, resposta.Valor));
        await _despesas.Received(1).AdicionarAsync(Arg.Is<Despesa>(d => d.RegistradoPorId == 1), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SalvarAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Despesa_com_data_futura_e_recusada()
    {
        var erro = await Assert.ThrowsAsync<RegraInvalidaException>(() =>
            Despesas().CriarAsync(new SalvarDespesaRequest("Aluguel", "Aluguel", 900m, Hoje.AddDays(1)), CancellationToken.None));

        Assert.Contains("futura", erro.Message);
        await _unitOfWork.DidNotReceive().SalvarAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Despesa_inexistente_da_404_ao_editar_e_ao_excluir()
    {
        _despesas.ObterPorIdAsync(9, Arg.Any<CancellationToken>()).Returns((Despesa?)null);

        await Assert.ThrowsAsync<NaoEncontradoException>(() => Despesas().EditarAsync(9, new SalvarDespesaRequest("x", "Outros", 1m, Hoje), CancellationToken.None));
        await Assert.ThrowsAsync<NaoEncontradoException>(() => Despesas().ExcluirAsync(9, CancellationToken.None));
    }

    [Fact]
    public async Task Despesa_editada_muda_os_campos_e_excluida_vai_para_a_auditoria()
    {
        var despesa = Despesa.Criar(1, "Shampoo", CategoriaDespesa.Insumos, 45m, Hoje, Hoje, Fabrica.Agora).ComId(5);
        _despesas.ObterPorIdAsync(5, Arg.Any<CancellationToken>()).Returns(despesa);

        var editada = await Despesas().EditarAsync(5, new SalvarDespesaRequest("Cera", "Insumos", 60m, Hoje), CancellationToken.None);
        await Despesas().ExcluirAsync(5, CancellationToken.None);

        Assert.Equal(("Cera", 60m), (editada.Descricao, editada.Valor));
        _despesas.Received(1).Remover(despesa);
        await _logs.Received(1).AdicionarAsync(Arg.Is<LogAuditoria>(l => l.Acao == "ExclusaoDespesa" && l.EntidadeId == 5), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Listagem_soma_o_total_e_assume_o_mes_atual()
    {
        DateOnly? de = null, ate = null;
        _despesas.ListarAsync(Arg.Do<DateOnly>(d => de = d), Arg.Do<DateOnly>(a => ate = a), Arg.Any<CancellationToken>()).Returns(
        [
            Despesa.Criar(1, "A", CategoriaDespesa.Agua, 10.10m, Hoje, Hoje, Fabrica.Agora),
            Despesa.Criar(1, "B", CategoriaDespesa.Energia, 20.20m, Hoje, Hoje, Fabrica.Agora),
        ]);

        var resposta = await Despesas().ListarAsync(null, null, CancellationToken.None);

        Assert.Equal(30.30m, resposta.Total);
        Assert.Equal((new DateOnly(2026, 10, 1), Hoje), (de, ate));
    }

    [Fact]
    public async Task Periodo_invertido_e_recusado()
    {
        await Assert.ThrowsAsync<RegraInvalidaException>(() => Despesas().ListarAsync(Hoje, Hoje.AddDays(-1), CancellationToken.None));
    }

    [Fact]
    public async Task Retirada_e_registrada_e_a_exclusao_e_auditada()
    {
        var resposta = await Retiradas().RegistrarAsync(new RegistrarRetiradaRequest(500m, Hoje, "Pró-labore"), CancellationToken.None);
        var retirada = RetiradaDono.Criar(1, 500m, Hoje, Hoje, null).ComId(4);
        _retiradas.ObterPorIdAsync(4, Arg.Any<CancellationToken>()).Returns(retirada);
        await Retiradas().ExcluirAsync(4, CancellationToken.None);

        Assert.Equal(500m, resposta.Valor);
        _retiradas.Received(1).Remover(retirada);
        await _logs.Received(1).AdicionarAsync(Arg.Is<LogAuditoria>(l => l.Acao == "ExclusaoRetirada"), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Retirada_inexistente_da_404()
    {
        _retiradas.ObterPorIdAsync(9, Arg.Any<CancellationToken>()).Returns((RetiradaDono?)null);

        await Assert.ThrowsAsync<NaoEncontradoException>(() => Retiradas().ExcluirAsync(9, CancellationToken.None));
    }

    [Fact]
    public async Task Diaria_sem_valor_usa_o_padrao_da_semana_e_o_do_sabado()
    {
        var semana = await Diarias().LancarAsync(new LancarDiariaRequest(2, Hoje, null), CancellationToken.None);
        var sabado = await Diarias().LancarAsync(new LancarDiariaRequest(2, Sabado, null), CancellationToken.None);

        Assert.Equal((100m, 120m), (semana.Valor, sabado.Valor));
    }

    [Fact]
    public async Task Diaria_com_valor_informado_ignora_a_configuracao()
    {
        var resposta = await Diarias().LancarAsync(new LancarDiariaRequest(2, Hoje, 150m), CancellationToken.None);

        Assert.Equal(150m, resposta.Valor);
        await _configuracao.DidNotReceive().ObterAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Diaria_repetida_do_mesmo_lavador_no_dia_da_conflito()
    {
        _diarias.LavadoresComDiariaAsync(Hoje, Arg.Any<CancellationToken>()).Returns([2]);

        await Assert.ThrowsAsync<ConflitoException>(() => Diarias().LancarAsync(new LancarDiariaRequest(2, Hoje, null), CancellationToken.None));
    }

    [Fact]
    public async Task Diaria_so_para_lavador_ativo_e_data_nao_futura()
    {
        await Assert.ThrowsAsync<RegraInvalidaException>(() => Diarias().LancarAsync(new LancarDiariaRequest(1, Hoje, null), CancellationToken.None)); // dono
        await Assert.ThrowsAsync<RegraInvalidaException>(() => Diarias().LancarAsync(new LancarDiariaRequest(99, Hoje, null), CancellationToken.None)); // inexistente
        await Assert.ThrowsAsync<RegraInvalidaException>(() => Diarias().LancarAsync(new LancarDiariaRequest(2, Hoje.AddDays(1), null), CancellationToken.None));
    }

    [Fact]
    public async Task Diaria_sem_valor_e_sem_configuracao_pede_o_valor()
    {
        _configuracao.ObterAsync(Arg.Any<CancellationToken>()).Returns((Configuracao?)null);

        await Assert.ThrowsAsync<RegraInvalidaException>(() => Diarias().LancarAsync(new LancarDiariaRequest(2, Hoje, null), CancellationToken.None));
    }

    [Fact]
    public async Task Lancar_dia_cria_para_quem_falta_e_informa_quem_ja_tinha()
    {
        _diarias.LavadoresComDiariaAsync(Hoje, Arg.Any<CancellationToken>()).Returns([3]);

        var resposta = await Diarias().LancarDoDiaAsync(Hoje, new LancarDiariasDoDiaRequest([2, 3, 2], null), CancellationToken.None);

        Assert.Equal([2], resposta.Criadas.Select(d => d.LavadorId));
        Assert.Equal([3], resposta.JaLancadas);
        await _diarias.Received(1).AdicionarAsync(Arg.Any<Diaria>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Diaria_paga_nao_muda_de_valor_nem_e_excluida_mas_pode_ser_editada_sem_mudar_o_valor()
    {
        var paga = Diaria.Criar(2, Hoje, 100m).ComId(7);
        paga.MarcarPaga();
        _diarias.ObterPorIdAsync(7, Arg.Any<CancellationToken>()).Returns(paga);

        await Assert.ThrowsAsync<ConflitoException>(() => Diarias().EditarAsync(7, new EditarDiariaRequest(120m, null), CancellationToken.None));
        await Assert.ThrowsAsync<ConflitoException>(() => Diarias().ExcluirAsync(7, CancellationToken.None));
        var mesmoValor = await Diarias().EditarAsync(7, new EditarDiariaRequest(100m, true), CancellationToken.None);

        Assert.True(mesmoValor.Paga);
    }

    [Fact]
    public async Task Reajuste_de_diaria_e_auditado_e_marcar_paga_funciona()
    {
        var diaria = Diaria.Criar(2, Hoje, 100m).ComId(8);
        _diarias.ObterPorIdAsync(8, Arg.Any<CancellationToken>()).Returns(diaria);

        var resposta = await Diarias().EditarAsync(8, new EditarDiariaRequest(130m, true), CancellationToken.None);

        Assert.Equal((130m, true), (resposta.Valor, resposta.Paga));
        await _logs.Received(1).AdicionarAsync(Arg.Is<LogAuditoria>(l => l.Acao == "AjusteDiaria"), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Mes_de_diarias_invalido_e_recusado()
    {
        await Assert.ThrowsAsync<RegraInvalidaException>(() => Diarias().ListarAsync("10/2026", CancellationToken.None));
    }

    [Theory]
    [InlineData("", "Insumos", 10, false)]
    [InlineData("Shampoo", "Combustivel", 10, false)]
    [InlineData("Shampoo", "1", 10, false)]
    [InlineData("Shampoo", "Insumos", 0, false)]
    [InlineData("Shampoo", "Insumos", 10, true)]
    public void Validador_de_despesa(string descricao, string categoria, int valor, bool valido)
    {
        var resultado = new SalvarDespesaRequestValidator().Validate(new SalvarDespesaRequest(descricao, categoria, valor, Hoje));

        Assert.Equal(valido, resultado.IsValid);
    }

    [Fact]
    public void Validador_de_diarias_exige_lavador_e_valor_positivo()
    {
        Assert.False(new LancarDiariaRequestValidator().Validate(new LancarDiariaRequest(0, Hoje, null)).IsValid);
        Assert.False(new LancarDiariaRequestValidator().Validate(new LancarDiariaRequest(2, Hoje, 0m)).IsValid);
        Assert.False(new LancarDiariasDoDiaRequestValidator().Validate(new LancarDiariasDoDiaRequest([], null)).IsValid);
        Assert.True(new LancarDiariasDoDiaRequestValidator().Validate(new LancarDiariasDoDiaRequest([2], 100m)).IsValid);
    }
}
