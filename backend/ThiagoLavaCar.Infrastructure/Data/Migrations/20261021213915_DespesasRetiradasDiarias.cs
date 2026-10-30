using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ThiagoLavaCar.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class DespesasRetiradasDiarias : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "despesas",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    registrado_por_id = table.Column<int>(type: "integer", nullable: false),
                    descricao = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    categoria = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: false),
                    valor = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    data = table.Column<DateOnly>(type: "date", nullable: false),
                    criado_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_despesas", x => x.id);
                    table.CheckConstraint("ck_despesas_categoria", "categoria IN ('Insumos', 'Aluguel', 'Agua', 'Energia', 'Manutencao', 'Outros')");
                    table.CheckConstraint("ck_despesas_valor", "valor > 0");
                    table.ForeignKey(
                        name: "fk_despesas_usuarios_registrado_por_id",
                        column: x => x.registrado_por_id,
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "diarias",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    lavador_id = table.Column<int>(type: "integer", nullable: false),
                    data = table.Column<DateOnly>(type: "date", nullable: false),
                    valor = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    paga = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_diarias", x => x.id);
                    table.CheckConstraint("ck_diarias_valor", "valor > 0");
                    table.ForeignKey(
                        name: "fk_diarias_usuarios_lavador_id",
                        column: x => x.lavador_id,
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "retiradas_dono",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    registrado_por_id = table.Column<int>(type: "integer", nullable: false),
                    valor = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    data = table.Column<DateOnly>(type: "date", nullable: false),
                    observacao = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_retiradas_dono", x => x.id);
                    table.CheckConstraint("ck_retiradas_dono_valor", "valor > 0");
                    table.ForeignKey(
                        name: "fk_retiradas_dono_usuarios_registrado_por_id",
                        column: x => x.registrado_por_id,
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_despesas_data",
                table: "despesas",
                column: "data");

            migrationBuilder.CreateIndex(
                name: "ix_despesas_registrado_por_id",
                table: "despesas",
                column: "registrado_por_id");

            migrationBuilder.CreateIndex(
                name: "ix_diarias_lavador_id_data",
                table: "diarias",
                columns: new[] { "lavador_id", "data" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_retiradas_dono_data",
                table: "retiradas_dono",
                column: "data");

            migrationBuilder.CreateIndex(
                name: "ix_retiradas_dono_registrado_por_id",
                table: "retiradas_dono",
                column: "registrado_por_id");

            // Entrega 2, regra 13: RLS ligado, sem políticas públicas.
            migrationBuilder.Sql("ALTER TABLE despesas ENABLE ROW LEVEL SECURITY;");
            migrationBuilder.Sql("ALTER TABLE retiradas_dono ENABLE ROW LEVEL SECURITY;");
            migrationBuilder.Sql("ALTER TABLE diarias ENABLE ROW LEVEL SECURITY;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "despesas");

            migrationBuilder.DropTable(
                name: "diarias");

            migrationBuilder.DropTable(
                name: "retiradas_dono");
        }
    }
}
