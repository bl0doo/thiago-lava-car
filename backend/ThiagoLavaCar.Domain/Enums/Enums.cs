namespace ThiagoLavaCar.Domain.Enums;

public enum Perfil { Dono, Lavador, Cliente }

public enum StatusOrdemServico { Aguardando, EmAndamento, Concluido, Entregue, Cancelada }

public enum TipoVeiculo { Moto, Carro, Caminhonete }

public enum FormaPagamento { Pix, Debito, Credito, Dinheiro }

public enum CanalContato { WhatsApp, Telefone, Email }

public enum StatusAgendamento { Agendado, Atendido, Cancelado, NaoCompareceu }

public enum CategoriaDespesa { Insumos, Aluguel, Agua, Energia, Manutencao, Outros }

public enum TipoMensagem { LavagemIniciada, LavagemConcluida }
