using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ThiagoLavaCar.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class ModeloVeiculo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "criado_em",
                table: "veiculos",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "now()");

            migrationBuilder.AddColumn<int>(
                name: "modelo_veiculo_id",
                table: "veiculos",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "modelos_veiculo",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    marca = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    nome = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    tipo_veiculo = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_modelos_veiculo", x => x.id);
                    table.CheckConstraint("ck_modelos_veiculo_tipo", "tipo_veiculo IN ('Moto', 'Carro', 'Caminhonete')");
                });

            migrationBuilder.CreateIndex(
                name: "ix_veiculos_modelo_veiculo_id",
                table: "veiculos",
                column: "modelo_veiculo_id");

            migrationBuilder.CreateIndex(
                name: "ix_modelos_veiculo_marca_nome",
                table: "modelos_veiculo",
                columns: new[] { "marca", "nome" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "fk_veiculos_modelos_veiculo_modelo_veiculo_id",
                table: "veiculos",
                column: "modelo_veiculo_id",
                principalTable: "modelos_veiculo",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            // Entrega 2, regra 13: RLS ligado, sem políticas públicas.
            migrationBuilder.Sql("ALTER TABLE modelos_veiculo ENABLE ROW LEVEL SECURITY;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_veiculos_modelos_veiculo_modelo_veiculo_id",
                table: "veiculos");

            migrationBuilder.DropTable(
                name: "modelos_veiculo");

            migrationBuilder.DropIndex(
                name: "ix_veiculos_modelo_veiculo_id",
                table: "veiculos");

            migrationBuilder.DropColumn(
                name: "criado_em",
                table: "veiculos");

            migrationBuilder.DropColumn(
                name: "modelo_veiculo_id",
                table: "veiculos");
        }
    }
}
