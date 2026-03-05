using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CompraProgramada.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FixContaMasterTipo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "contas_graficas",
                keyColumn: "id",
                keyValue: 1L,
                column: "tipo",
                value: "MASTER");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "contas_graficas",
                keyColumn: "id",
                keyValue: 1L,
                column: "tipo",
                value: "Master");
        }
    }
}
