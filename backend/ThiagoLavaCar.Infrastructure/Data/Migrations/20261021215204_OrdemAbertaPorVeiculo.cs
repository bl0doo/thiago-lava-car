using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ThiagoLavaCar.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class OrdemAbertaPorVeiculo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_ordens_servico_veiculo_id",
                table: "ordens_servico");

            migrationBuilder.CreateIndex(
                name: "ux_ordens_servico_veiculo_aberta",
                table: "ordens_servico",
                column: "veiculo_id",
                unique: true,
                filter: "status IN ('Aguardando', 'EmAndamento')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ux_ordens_servico_veiculo_aberta",
                table: "ordens_servico");

            migrationBuilder.CreateIndex(
                name: "ix_ordens_servico_veiculo_id",
                table: "ordens_servico",
                column: "veiculo_id");
        }
    }
}
