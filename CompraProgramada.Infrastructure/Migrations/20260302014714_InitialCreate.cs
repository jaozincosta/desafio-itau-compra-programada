using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CompraProgramada.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "cestas_recomendacao",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    nome = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ativa = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    data_criacao = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    data_desativacao = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cestas_recomendacao", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "clientes",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    nome = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    cpf = table.Column<string>(type: "varchar(11)", maxLength: 11, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    email = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    valor_mensal = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ativo = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    data_adesao = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    data_saida = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_clientes", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "cotacoes",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    data_pregao = table.Column<DateTime>(type: "date", nullable: false),
                    ticker = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    preco_abertura = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    preco_fechamento = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    preco_maximo = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    preco_minimo = table.Column<decimal>(type: "decimal(18,4)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cotacoes", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "itens_cesta",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    cesta_id = table.Column<long>(type: "bigint", nullable: false),
                    ticker = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    percentual = table.Column<decimal>(type: "decimal(5,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_itens_cesta", x => x.id);
                    table.ForeignKey(
                        name: "FK_itens_cesta_cestas_recomendacao_cesta_id",
                        column: x => x.cesta_id,
                        principalTable: "cestas_recomendacao",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "contas_graficas",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    cliente_id = table.Column<long>(type: "bigint", nullable: true),
                    numero_conta = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    tipo = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    data_criacao = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_contas_graficas", x => x.id);
                    table.ForeignKey(
                        name: "FK_contas_graficas_clientes_cliente_id",
                        column: x => x.cliente_id,
                        principalTable: "clientes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "eventos_ir",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    cliente_id = table.Column<long>(type: "bigint", nullable: false),
                    tipo = table.Column<string>(type: "varchar(15)", maxLength: 15, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    valor_base = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    valor_ir = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    publicado_kafka = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    data_evento = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_eventos_ir", x => x.id);
                    table.ForeignKey(
                        name: "FK_eventos_ir_clientes_cliente_id",
                        column: x => x.cliente_id,
                        principalTable: "clientes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "rebalanceamentos",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    cliente_id = table.Column<long>(type: "bigint", nullable: false),
                    tipo = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ticker_vendido = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ticker_comprado = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    valor_venda = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    quantidade_vendida = table.Column<int>(type: "int", nullable: false),
                    quantidade_comprada = table.Column<int>(type: "int", nullable: false),
                    data_rebalanceamento = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_rebalanceamentos", x => x.id);
                    table.ForeignKey(
                        name: "FK_rebalanceamentos_clientes_cliente_id",
                        column: x => x.cliente_id,
                        principalTable: "clientes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "custodias",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    conta_grafica_id = table.Column<long>(type: "bigint", nullable: false),
                    ticker = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    quantidade = table.Column<int>(type: "int", nullable: false),
                    preco_medio = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    data_ultima_atualizacao = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_custodias", x => x.id);
                    table.ForeignKey(
                        name: "FK_custodias_contas_graficas_conta_grafica_id",
                        column: x => x.conta_grafica_id,
                        principalTable: "contas_graficas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ordens_compra",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    conta_master_id = table.Column<long>(type: "bigint", nullable: false),
                    ticker = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    quantidade = table.Column<int>(type: "int", nullable: false),
                    preco_unitario = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    tipo_mercado = table.Column<string>(type: "varchar(15)", maxLength: 15, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    data_execucao = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ordens_compra", x => x.id);
                    table.ForeignKey(
                        name: "FK_ordens_compra_contas_graficas_conta_master_id",
                        column: x => x.conta_master_id,
                        principalTable: "contas_graficas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "distribuicoes",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    ordem_compra_id = table.Column<long>(type: "bigint", nullable: false),
                    custodia_filhote_id = table.Column<long>(type: "bigint", nullable: false),
                    ticker = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    quantidade = table.Column<int>(type: "int", nullable: false),
                    preco_unitario = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    data_distribuicao = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_distribuicoes", x => x.id);
                    table.ForeignKey(
                        name: "FK_distribuicoes_ordens_compra_ordem_compra_id",
                        column: x => x.ordem_compra_id,
                        principalTable: "ordens_compra",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.InsertData(
                table: "contas_graficas",
                columns: new[] { "id", "cliente_id", "data_criacao", "numero_conta", "tipo" },
                values: new object[] { 1L, null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "MST-000001", "Master" });

            migrationBuilder.CreateIndex(
                name: "IX_clientes_cpf",
                table: "clientes",
                column: "cpf",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_contas_graficas_cliente_id",
                table: "contas_graficas",
                column: "cliente_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_cotacoes_ticker_data_pregao",
                table: "cotacoes",
                columns: new[] { "ticker", "data_pregao" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_custodias_conta_grafica_id_ticker",
                table: "custodias",
                columns: new[] { "conta_grafica_id", "ticker" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_distribuicoes_ordem_compra_id",
                table: "distribuicoes",
                column: "ordem_compra_id");

            migrationBuilder.CreateIndex(
                name: "IX_eventos_ir_cliente_id",
                table: "eventos_ir",
                column: "cliente_id");

            migrationBuilder.CreateIndex(
                name: "IX_itens_cesta_cesta_id",
                table: "itens_cesta",
                column: "cesta_id");

            migrationBuilder.CreateIndex(
                name: "IX_ordens_compra_conta_master_id",
                table: "ordens_compra",
                column: "conta_master_id");

            migrationBuilder.CreateIndex(
                name: "IX_rebalanceamentos_cliente_id",
                table: "rebalanceamentos",
                column: "cliente_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "cotacoes");

            migrationBuilder.DropTable(
                name: "custodias");

            migrationBuilder.DropTable(
                name: "distribuicoes");

            migrationBuilder.DropTable(
                name: "eventos_ir");

            migrationBuilder.DropTable(
                name: "itens_cesta");

            migrationBuilder.DropTable(
                name: "rebalanceamentos");

            migrationBuilder.DropTable(
                name: "ordens_compra");

            migrationBuilder.DropTable(
                name: "cestas_recomendacao");

            migrationBuilder.DropTable(
                name: "contas_graficas");

            migrationBuilder.DropTable(
                name: "clientes");
        }
    }
}
