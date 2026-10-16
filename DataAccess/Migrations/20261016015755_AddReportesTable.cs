using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddReportesTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Reportes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Motivo = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    FechaDesde = table.Column<DateTime>(type: "TEXT", nullable: false),
                    FechaHasta = table.Column<DateTime>(type: "TEXT", nullable: false),
                    TiposReporteJson = table.Column<string>(type: "TEXT", nullable: false),
                    FechaGeneracion = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UsuarioGeneradorId = table.Column<Guid>(type: "TEXT", nullable: false),
                    UsuarioGeneradorUsername = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    TotalAtenciones = table.Column<int>(type: "INTEGER", nullable: false),
                    TotalVacunaciones = table.Column<int>(type: "INTEGER", nullable: false),
                    TotalMascotas = table.Column<int>(type: "INTEGER", nullable: false),
                    TotalDuenos = table.Column<int>(type: "INTEGER", nullable: false),
                    TotalTurnos = table.Column<int>(type: "INTEGER", nullable: false),
                    DatosConsolidadosJson = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Reportes", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Reportes_FechaGeneracion",
                table: "Reportes",
                column: "FechaGeneracion");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Reportes");
        }
    }
}
