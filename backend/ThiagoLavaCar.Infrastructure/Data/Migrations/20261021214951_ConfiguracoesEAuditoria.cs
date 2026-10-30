using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ThiagoLavaCar.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class ConfiguracoesEAuditoria : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "configuracoes",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    nome_lava_jato = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    telefone = table.Column<string>(type: "character varying(11)", maxLength: 11, nullable: true),
                    abertura = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    fechamento = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    dias_funcionamento = table.Column<string>(type: "character varying(13)", maxLength: 13, nullable: false),
                    diaria_semana = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    diaria_sabado = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    intervalo_agenda_min = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_configuracoes", x => x.id);
                    table.CheckConstraint("ck_configuracoes_diarias", "diaria_semana > 0 AND diaria_sabado > 0");
                    table.CheckConstraint("ck_configuracoes_dias", "dias_funcionamento ~ '^[0-6](,[0-6])*$'");
                    table.CheckConstraint("ck_configuracoes_horario", "fechamento > abertura");
                    table.CheckConstraint("ck_configuracoes_intervalo", "intervalo_agenda_min BETWEEN 5 AND 240");
                    table.CheckConstraint("ck_configuracoes_linha_unica", "id = 1");
                });

            migrationBuilder.CreateTable(
                name: "logs_auditoria",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    usuario_id = table.Column<int>(type: "integer", nullable: false),
                    acao = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    entidade = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    entidade_id = table.Column<int>(type: "integer", nullable: false),
                    detalhes = table.Column<string>(type: "jsonb", nullable: true),
                    criado_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_logs_auditoria", x => x.id);
                    table.ForeignKey(
                        name: "fk_logs_auditoria_usuarios_usuario_id",
                        column: x => x.usuario_id,
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_logs_auditoria_criado_em",
                table: "logs_auditoria",
                column: "criado_em");

            migrationBuilder.CreateIndex(
                name: "ix_logs_auditoria_entidade_entidade_id",
                table: "logs_auditoria",
                columns: new[] { "entidade", "entidade_id" });

            migrationBuilder.CreateIndex(
                name: "ix_logs_auditoria_usuario_id",
                table: "logs_auditoria",
                column: "usuario_id");

            // Entrega 2, regra 13: RLS ligado, sem políticas públicas.
            migrationBuilder.Sql("ALTER TABLE configuracoes ENABLE ROW LEVEL SECURITY;");
            migrationBuilder.Sql("ALTER TABLE logs_auditoria ENABLE ROW LEVEL SECURITY;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "configuracoes");

            migrationBuilder.DropTable(
                name: "logs_auditoria");
        }
    }
}
