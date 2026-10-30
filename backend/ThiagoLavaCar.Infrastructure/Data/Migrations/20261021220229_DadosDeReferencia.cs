using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ThiagoLavaCar.Infrastructure.Data.Migrations
{
    /// <summary>
    /// Dados que a API precisa para funcionar em qualquer ambiente, com ou sem seed: as 4 taxas de pagamento
    /// (zeradas, o dono ajusta depois), a linha única de configurações e os 2 modelos de mensagem.
    /// Usa ON CONFLICT DO NOTHING, então não sobrescreve valores que o dono já tenha cadastrado.
    /// </summary>
    public partial class DadosDeReferencia : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "INSERT INTO taxas_pagamento (forma_pagamento, percentual, valor_fixo) VALUES " +
                "('Pix', 0, 0), ('Debito', 0, 0), ('Credito', 0, 0), ('Dinheiro', 0, 0) " +
                "ON CONFLICT (forma_pagamento) DO NOTHING;");

            migrationBuilder.Sql(
                "INSERT INTO configuracoes (id, nome_lava_jato, abertura, fechamento, dias_funcionamento, diaria_semana, diaria_sabado, intervalo_agenda_min) " +
                "VALUES (1, 'Thiago Lava Car', '08:00', '18:00', '1,2,3,4,5,6', 100.00, 120.00, 30) " +
                "ON CONFLICT (id) DO NOTHING;");

            migrationBuilder.Sql(
                "INSERT INTO modelos_mensagem (tipo, texto, atualizado_em) VALUES " +
                "('LavagemIniciada', 'Olá, {nome}! A lavagem do seu {modelo} (placa {placa}) foi iniciada. Serviço: {serviço}. " +
                "Previsão: {duração estimada}. Qualquer coisa, é só chamar. {nome do lava-jato}', now()), " +
                "('LavagemConcluida', 'Olá, {nome}! A lavagem do seu {modelo} (placa {placa}) ficou pronta e já pode ser retirada. " +
                "Valor: {valor}. Obrigado pela preferência! {nome do lava-jato}', now()) " +
                "ON CONFLICT (tipo) DO NOTHING;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DELETE FROM modelos_mensagem;");
            migrationBuilder.Sql("DELETE FROM configuracoes;");
            migrationBuilder.Sql("DELETE FROM taxas_pagamento;");
        }
    }
}
