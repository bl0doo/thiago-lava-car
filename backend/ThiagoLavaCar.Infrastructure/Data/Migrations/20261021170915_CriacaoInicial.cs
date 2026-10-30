using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ThiagoLavaCar.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class CriacaoInicial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "servicos",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    nome = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    categoria = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    tempo_estimado_min = table.Column<int>(type: "integer", nullable: false),
                    custo_insumos = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    ativo = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_servicos", x => x.id);
                    table.CheckConstraint("ck_servicos_custo", "custo_insumos >= 0");
                    table.CheckConstraint("ck_servicos_tempo", "tempo_estimado_min > 0");
                });

            migrationBuilder.CreateTable(
                name: "taxas_pagamento",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    forma_pagamento = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    percentual = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    valor_fixo = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_taxas_pagamento", x => x.id);
                    table.CheckConstraint("ck_taxas_fixo", "valor_fixo >= 0");
                    table.CheckConstraint("ck_taxas_forma", "forma_pagamento IN ('Pix', 'Debito', 'Credito', 'Dinheiro')");
                    table.CheckConstraint("ck_taxas_percentual", "percentual >= 0 AND percentual <= 100");
                });

            migrationBuilder.CreateTable(
                name: "usuarios",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    nome = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    apelido = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    email = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: true),
                    perfil = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    senha_hash = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    pin_hash = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ativo = table.Column<bool>(type: "boolean", nullable: false),
                    falhas_login = table.Column<int>(type: "integer", nullable: false),
                    bloqueado_ate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    criado_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_usuarios", x => x.id);
                    table.CheckConstraint("ck_usuarios_credenciais", "(perfil = 'Lavador' AND pin_hash IS NOT NULL) OR (perfil IN ('Dono', 'Cliente') AND email IS NOT NULL AND senha_hash IS NOT NULL)");
                    table.CheckConstraint("ck_usuarios_perfil", "perfil IN ('Dono', 'Lavador', 'Cliente')");
                });

            migrationBuilder.CreateTable(
                name: "servicos_precos",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    servico_id = table.Column<int>(type: "integer", nullable: false),
                    tipo_veiculo = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: false),
                    preco = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    preco_com_cera = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_servicos_precos", x => x.id);
                    table.CheckConstraint("ck_servicos_precos_preco", "preco >= 0 AND (preco_com_cera IS NULL OR preco_com_cera >= 0)");
                    table.CheckConstraint("ck_servicos_precos_tipo", "tipo_veiculo IN ('Moto', 'Carro', 'Caminhonete')");
                    table.ForeignKey(
                        name: "fk_servicos_precos_servicos_servico_id",
                        column: x => x.servico_id,
                        principalTable: "servicos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "clientes",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    usuario_id = table.Column<int>(type: "integer", nullable: true),
                    nome = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    telefone = table.Column<string>(type: "character varying(11)", maxLength: 11, nullable: false),
                    email = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: true),
                    canal_preferido = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    consentimento_lgpd = table.Column<bool>(type: "boolean", nullable: false),
                    consentimento_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_clientes", x => x.id);
                    table.CheckConstraint("ck_clientes_canal", "canal_preferido IN ('WhatsApp', 'Telefone', 'Email')");
                    table.ForeignKey(
                        name: "fk_clientes_usuarios_usuario_id",
                        column: x => x.usuario_id,
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "refresh_tokens",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    usuario_id = table.Column<int>(type: "integer", nullable: false),
                    token_hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    criado_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    expira_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    revogado_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_refresh_tokens", x => x.id);
                    table.ForeignKey(
                        name: "fk_refresh_tokens_usuarios_usuario_id",
                        column: x => x.usuario_id,
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "veiculos",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    cliente_id = table.Column<int>(type: "integer", nullable: false),
                    placa = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    modelo_descricao = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    tipo = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: false),
                    cor = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    ativo = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_veiculos", x => x.id);
                    table.CheckConstraint("ck_veiculos_placa", "placa ~ '^[A-Z]{3}[0-9][A-Z0-9][0-9]{2}$'");
                    table.CheckConstraint("ck_veiculos_tipo", "tipo IN ('Moto', 'Carro', 'Caminhonete')");
                    table.ForeignKey(
                        name: "fk_veiculos_clientes_cliente_id",
                        column: x => x.cliente_id,
                        principalTable: "clientes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ordens_servico",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    veiculo_id = table.Column<int>(type: "integer", nullable: false),
                    cliente_id = table.Column<int>(type: "integer", nullable: false),
                    criado_por_id = table.Column<int>(type: "integer", nullable: false),
                    lavador_id = table.Column<int>(type: "integer", nullable: true),
                    status = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: false),
                    valor_total = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    duracao_estimada_min = table.Column<int>(type: "integer", nullable: false),
                    sem_avarias = table.Column<bool>(type: "boolean", nullable: false),
                    aceite_cliente = table.Column<bool>(type: "boolean", nullable: false),
                    pertences_entrada = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    pertences_encontrados = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    nome_quem_retira = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    criado_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    inicio_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    fim_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    duracao_real_min = table.Column<int>(type: "integer", nullable: true),
                    revisao_necessaria = table.Column<bool>(type: "boolean", nullable: false),
                    ultima_transicao_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ordens_servico", x => x.id);
                    table.CheckConstraint("ck_ordens_servico_aceite", "aceite_cliente = TRUE");
                    table.CheckConstraint("ck_ordens_servico_status", "status IN ('Aguardando', 'EmAndamento', 'Concluido', 'Entregue', 'Cancelada')");
                    table.CheckConstraint("ck_ordens_servico_temporizador", "fim_em IS NULL OR (inicio_em IS NOT NULL AND fim_em > inicio_em)");
                    table.CheckConstraint("ck_ordens_servico_valor", "valor_total >= 0");
                    table.ForeignKey(
                        name: "fk_ordens_servico_clientes_cliente_id",
                        column: x => x.cliente_id,
                        principalTable: "clientes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_ordens_servico_usuarios_criado_por_id",
                        column: x => x.criado_por_id,
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_ordens_servico_usuarios_lavador_id",
                        column: x => x.lavador_id,
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_ordens_servico_veiculos_veiculo_id",
                        column: x => x.veiculo_id,
                        principalTable: "veiculos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "avarias",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ordem_servico_id = table.Column<int>(type: "integer", nullable: false),
                    local = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    tipo = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    descricao = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_avarias", x => x.id);
                    table.ForeignKey(
                        name: "fk_avarias_ordens_servico_ordem_servico_id",
                        column: x => x.ordem_servico_id,
                        principalTable: "ordens_servico",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "itens_ordem_servico",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ordem_servico_id = table.Column<int>(type: "integer", nullable: false),
                    servico_id = table.Column<int>(type: "integer", nullable: false),
                    nome_servico = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    com_cera = table.Column<bool>(type: "boolean", nullable: false),
                    preco_final = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    preco_ajustado = table.Column<bool>(type: "boolean", nullable: false),
                    custo_insumos = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    tempo_estimado_min = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_itens_ordem_servico", x => x.id);
                    table.CheckConstraint("ck_itens_preco", "preco_final >= 0 AND custo_insumos >= 0");
                    table.CheckConstraint("ck_itens_tempo", "tempo_estimado_min > 0");
                    table.ForeignKey(
                        name: "fk_itens_ordem_servico_ordens_servico_ordem_servico_id",
                        column: x => x.ordem_servico_id,
                        principalTable: "ordens_servico",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_itens_ordem_servico_servicos_servico_id",
                        column: x => x.servico_id,
                        principalTable: "servicos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "pagamentos",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ordem_servico_id = table.Column<int>(type: "integer", nullable: false),
                    registrado_por_id = table.Column<int>(type: "integer", nullable: false),
                    forma_pagamento = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    valor_bruto = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    percentual_taxa = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    valor_taxa = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    valor_liquido = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    data_caixa = table.Column<DateOnly>(type: "date", nullable: false),
                    criado_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    estornado_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_pagamentos", x => x.id);
                    table.CheckConstraint("ck_pagamentos_forma", "forma_pagamento IN ('Pix', 'Debito', 'Credito', 'Dinheiro')");
                    table.CheckConstraint("ck_pagamentos_valor", "valor_bruto > 0 AND valor_taxa >= 0 AND valor_liquido >= 0");
                    table.ForeignKey(
                        name: "fk_pagamentos_ordens_servico_ordem_servico_id",
                        column: x => x.ordem_servico_id,
                        principalTable: "ordens_servico",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_pagamentos_usuarios_registrado_por_id",
                        column: x => x.registrado_por_id,
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_avarias_ordem_servico_id",
                table: "avarias",
                column: "ordem_servico_id");

            migrationBuilder.CreateIndex(
                name: "ix_clientes_telefone",
                table: "clientes",
                column: "telefone",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_clientes_usuario_id",
                table: "clientes",
                column: "usuario_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_itens_ordem_servico_ordem_servico_id",
                table: "itens_ordem_servico",
                column: "ordem_servico_id");

            migrationBuilder.CreateIndex(
                name: "ix_itens_ordem_servico_servico_id",
                table: "itens_ordem_servico",
                column: "servico_id");

            migrationBuilder.CreateIndex(
                name: "ix_ordens_servico_cliente_id",
                table: "ordens_servico",
                column: "cliente_id");

            migrationBuilder.CreateIndex(
                name: "ix_ordens_servico_criado_por_id",
                table: "ordens_servico",
                column: "criado_por_id");

            migrationBuilder.CreateIndex(
                name: "ix_ordens_servico_lavador_id",
                table: "ordens_servico",
                column: "lavador_id");

            migrationBuilder.CreateIndex(
                name: "ix_ordens_servico_status_criado_em",
                table: "ordens_servico",
                columns: new[] { "status", "criado_em" });

            migrationBuilder.CreateIndex(
                name: "ix_ordens_servico_veiculo_id",
                table: "ordens_servico",
                column: "veiculo_id");

            migrationBuilder.CreateIndex(
                name: "ix_pagamentos_data_caixa",
                table: "pagamentos",
                column: "data_caixa");

            migrationBuilder.CreateIndex(
                name: "ix_pagamentos_ordem_servico_id",
                table: "pagamentos",
                column: "ordem_servico_id");

            migrationBuilder.CreateIndex(
                name: "ix_pagamentos_registrado_por_id",
                table: "pagamentos",
                column: "registrado_por_id");

            migrationBuilder.CreateIndex(
                name: "ix_refresh_tokens_token_hash",
                table: "refresh_tokens",
                column: "token_hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_refresh_tokens_usuario_id",
                table: "refresh_tokens",
                column: "usuario_id");

            migrationBuilder.CreateIndex(
                name: "ix_servicos_nome",
                table: "servicos",
                column: "nome",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_servicos_precos_servico_id_tipo_veiculo",
                table: "servicos_precos",
                columns: new[] { "servico_id", "tipo_veiculo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_taxas_pagamento_forma_pagamento",
                table: "taxas_pagamento",
                column: "forma_pagamento",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_usuarios_email",
                table: "usuarios",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_veiculos_cliente_id",
                table: "veiculos",
                column: "cliente_id");

            migrationBuilder.CreateIndex(
                name: "ix_veiculos_placa",
                table: "veiculos",
                column: "placa",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "avarias");

            migrationBuilder.DropTable(
                name: "itens_ordem_servico");

            migrationBuilder.DropTable(
                name: "pagamentos");

            migrationBuilder.DropTable(
                name: "refresh_tokens");

            migrationBuilder.DropTable(
                name: "servicos_precos");

            migrationBuilder.DropTable(
                name: "taxas_pagamento");

            migrationBuilder.DropTable(
                name: "ordens_servico");

            migrationBuilder.DropTable(
                name: "servicos");

            migrationBuilder.DropTable(
                name: "veiculos");

            migrationBuilder.DropTable(
                name: "clientes");

            migrationBuilder.DropTable(
                name: "usuarios");
        }
    }
}
