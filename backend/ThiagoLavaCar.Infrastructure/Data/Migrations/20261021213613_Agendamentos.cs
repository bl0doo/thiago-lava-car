using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ThiagoLavaCar.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class Agendamentos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "aceite_em",
                table: "ordens_servico",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "now()");

            migrationBuilder.AddColumn<int>(
                name: "agendamento_id",
                table: "ordens_servico",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<short>(
                name: "avaliacao",
                table: "ordens_servico",
                type: "smallint",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "entregue_em",
                table: "ordens_servico",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "agendamentos",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    cliente_id = table.Column<int>(type: "integer", nullable: false),
                    veiculo_id = table.Column<int>(type: "integer", nullable: false),
                    servico_id = table.Column<int>(type: "integer", nullable: false),
                    com_cera = table.Column<bool>(type: "boolean", nullable: false),
                    inicio = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    duracao_estimada_min = table.Column<int>(type: "integer", nullable: false),
                    preco_estimado = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    status = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: false),
                    criado_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_agendamentos", x => x.id);
                    table.CheckConstraint("ck_agendamentos_duracao", "duracao_estimada_min > 0");
                    table.CheckConstraint("ck_agendamentos_preco", "preco_estimado >= 0");
                    table.CheckConstraint("ck_agendamentos_status", "status IN ('Agendado', 'Atendido', 'Cancelado', 'NaoCompareceu')");
                    table.ForeignKey(
                        name: "fk_agendamentos_clientes_cliente_id",
                        column: x => x.cliente_id,
                        principalTable: "clientes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_agendamentos_servicos_servico_id",
                        column: x => x.servico_id,
                        principalTable: "servicos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_agendamentos_veiculos_veiculo_id",
                        column: x => x.veiculo_id,
                        principalTable: "veiculos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_ordens_servico_agendamento_id",
                table: "ordens_servico",
                column: "agendamento_id",
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_ordens_servico_avaliacao",
                table: "ordens_servico",
                sql: "avaliacao IS NULL OR avaliacao BETWEEN 1 AND 5");

            migrationBuilder.CreateIndex(
                name: "ix_agendamentos_cliente_id",
                table: "agendamentos",
                column: "cliente_id");

            migrationBuilder.CreateIndex(
                name: "ix_agendamentos_inicio",
                table: "agendamentos",
                column: "inicio");

            migrationBuilder.CreateIndex(
                name: "ix_agendamentos_servico_id",
                table: "agendamentos",
                column: "servico_id");

            migrationBuilder.CreateIndex(
                name: "ix_agendamentos_veiculo_id",
                table: "agendamentos",
                column: "veiculo_id");

            migrationBuilder.AddForeignKey(
                name: "fk_ordens_servico_agendamentos_agendamento_id",
                table: "ordens_servico",
                column: "agendamento_id",
                principalTable: "agendamentos",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
            migrationBuilder.Sql("ALTER TABLE agendamentos ENABLE ROW LEVEL SECURITY;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_ordens_servico_agendamentos_agendamento_id",
                table: "ordens_servico");

            migrationBuilder.DropTable(
                name: "agendamentos");

            migrationBuilder.DropIndex(
                name: "ix_ordens_servico_agendamento_id",
                table: "ordens_servico");

            migrationBuilder.DropCheckConstraint(
                name: "ck_ordens_servico_avaliacao",
                table: "ordens_servico");

            migrationBuilder.DropColumn(
                name: "aceite_em",
                table: "ordens_servico");

            migrationBuilder.DropColumn(
                name: "agendamento_id",
                table: "ordens_servico");

            migrationBuilder.DropColumn(
                name: "avaliacao",
                table: "ordens_servico");

            migrationBuilder.DropColumn(
                name: "entregue_em",
                table: "ordens_servico");
        }
    }
}
