using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ThiagoLavaCar.Infrastructure.Data.Migrations
{
    /// <summary>
    /// Entrega 2, regra 13: Row Level Security ligado em todas as tabelas, sem políticas para anon e authenticated,
    /// para que a API REST automática do Supabase não exponha dados. A API .NET conecta como dono das tabelas
    /// e continua com acesso total. Em PostgreSQL comum (Docker local, testes) o comando é inofensivo.
    /// </summary>
    public partial class AtivarRowLevelSecurity : Migration
    {
        private static readonly string[] Tabelas =
        [
            "usuarios", "refresh_tokens", "clientes", "veiculos", "servicos", "servicos_precos",
            "ordens_servico", "itens_ordem_servico", "avarias", "taxas_pagamento", "pagamentos",
        ];

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var tabela in Tabelas)
            {
                migrationBuilder.Sql($"ALTER TABLE {tabela} ENABLE ROW LEVEL SECURITY;");
            }
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var tabela in Tabelas)
            {
                migrationBuilder.Sql($"ALTER TABLE {tabela} DISABLE ROW LEVEL SECURITY;");
            }
        }
    }
}
