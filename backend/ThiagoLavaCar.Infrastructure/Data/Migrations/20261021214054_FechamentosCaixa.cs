using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ThiagoLavaCar.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class FechamentosCaixa : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "fechamentos_caixa",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    data = table.Column<DateOnly>(type: "date", nullable: false),
                    total_bruto = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    total_liquido = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    total_conferido = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    diferenca = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    justificativa = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    fechado_por_id = table.Column<int>(type: "integer", nullable: false),
                    fechado_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_fechamentos_caixa", x => x.id);
                    table.CheckConstraint("ck_fechamentos_caixa_diferenca", "diferenca = total_conferido - total_bruto");
                    table.CheckConstraint("ck_fechamentos_caixa_justificativa", "diferenca = 0 OR (justificativa IS NOT NULL AND char_length(justificativa) BETWEEN 10 AND 500)");
                    table.CheckConstraint("ck_fechamentos_caixa_totais", "total_bruto >= 0 AND total_liquido >= 0 AND total_conferido >= 0");
                    table.ForeignKey(
                        name: "fk_fechamentos_caixa_usuarios_fechado_por_id",
                        column: x => x.fechado_por_id,
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "fechamentos_forma",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    fechamento_id = table.Column<int>(type: "integer", nullable: false),
                    forma_pagamento = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    valor_calculado = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    valor_conferido = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_fechamentos_forma", x => x.id);
                    table.CheckConstraint("ck_fechamentos_forma_forma", "forma_pagamento IN ('Pix', 'Debito', 'Credito', 'Dinheiro')");
                    table.CheckConstraint("ck_fechamentos_forma_valores", "valor_calculado >= 0 AND valor_conferido >= 0");
                    table.ForeignKey(
                        name: "fk_fechamentos_forma_fechamentos_caixa_fechamento_id",
                        column: x => x.fechamento_id,
                        principalTable: "fechamentos_caixa",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_fechamentos_caixa_data",
                table: "fechamentos_caixa",
                column: "data",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_fechamentos_caixa_fechado_por_id",
                table: "fechamentos_caixa",
                column: "fechado_por_id");

            migrationBuilder.CreateIndex(
                name: "ix_fechamentos_forma_fechamento_id_forma_pagamento",
                table: "fechamentos_forma",
                columns: new[] { "fechamento_id", "forma_pagamento" },
                unique: true);

            // Entrega 2, regra 13: RLS ligado, sem políticas públicas.
            migrationBuilder.Sql("ALTER TABLE fechamentos_caixa ENABLE ROW LEVEL SECURITY;");
            migrationBuilder.Sql("ALTER TABLE fechamentos_forma ENABLE ROW LEVEL SECURITY;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "fechamentos_forma");

            migrationBuilder.DropTable(
                name: "fechamentos_caixa");
        }
    }
}
