using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ThiagoLavaCar.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class Mensagens : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "mensagens_enviadas",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ordem_servico_id = table.Column<int>(type: "integer", nullable: false),
                    enviada_por_id = table.Column<int>(type: "integer", nullable: false),
                    tipo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    texto = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    enviada_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_mensagens_enviadas", x => x.id);
                    table.CheckConstraint("ck_mensagens_enviadas_tipo", "tipo IN ('LavagemIniciada', 'LavagemConcluida')");
                    table.ForeignKey(
                        name: "fk_mensagens_enviadas_ordens_servico_ordem_servico_id",
                        column: x => x.ordem_servico_id,
                        principalTable: "ordens_servico",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_mensagens_enviadas_usuarios_enviada_por_id",
                        column: x => x.enviada_por_id,
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "modelos_mensagem",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    tipo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    texto = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    atualizado_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_modelos_mensagem", x => x.id);
                    table.CheckConstraint("ck_modelos_mensagem_tipo", "tipo IN ('LavagemIniciada', 'LavagemConcluida')");
                });

            migrationBuilder.CreateIndex(
                name: "ix_mensagens_enviadas_enviada_por_id",
                table: "mensagens_enviadas",
                column: "enviada_por_id");

            migrationBuilder.CreateIndex(
                name: "ix_mensagens_enviadas_ordem_servico_id",
                table: "mensagens_enviadas",
                column: "ordem_servico_id");

            migrationBuilder.CreateIndex(
                name: "ix_modelos_mensagem_tipo",
                table: "modelos_mensagem",
                column: "tipo",
                unique: true);

            // Entrega 2, regra 13: RLS ligado, sem políticas públicas.
            migrationBuilder.Sql("ALTER TABLE modelos_mensagem ENABLE ROW LEVEL SECURITY;");
            migrationBuilder.Sql("ALTER TABLE mensagens_enviadas ENABLE ROW LEVEL SECURITY;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "mensagens_enviadas");

            migrationBuilder.DropTable(
                name: "modelos_mensagem");
        }
    }
}
