using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddUniqueIndexesTurnosYDisponibilidad : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Turnos_VeterinarioId_FechaHora",
                table: "Turnos");

            migrationBuilder.DropIndex(
                name: "IX_DisponibilidadesVeterinarios_VeterinarioId_DiaSemana_Jornada",
                table: "DisponibilidadesVeterinarios");

            migrationBuilder.CreateIndex(
                name: "IX_Turnos_VeterinarioId_FechaHora",
                table: "Turnos",
                columns: new[] { "VeterinarioId", "FechaHora" },
                unique: true,
                filter: "Estado IN ('Confirmado', 'EnAtencion', 'Finalizado')");

            migrationBuilder.CreateIndex(
                name: "IX_Turnos_AtencionMedicaId_FechaHora",
                table: "Turnos",
                columns: new[] { "AtencionMedicaId", "FechaHora" },
                unique: true,
                filter: "AtencionMedicaId IS NOT NULL AND Estado <> 'Cancelado'");

            migrationBuilder.CreateIndex(
                name: "IX_DisponibilidadesVeterinarios_VeterinarioId_DiaSemana_Jornada",
                table: "DisponibilidadesVeterinarios",
                columns: new[] { "VeterinarioId", "DiaSemana", "Jornada" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Turnos_VeterinarioId_FechaHora",
                table: "Turnos");

            migrationBuilder.DropIndex(
                name: "IX_Turnos_AtencionMedicaId_FechaHora",
                table: "Turnos");

            migrationBuilder.DropIndex(
                name: "IX_DisponibilidadesVeterinarios_VeterinarioId_DiaSemana_Jornada",
                table: "DisponibilidadesVeterinarios");

            migrationBuilder.CreateIndex(
                name: "IX_Turnos_VeterinarioId_FechaHora",
                table: "Turnos",
                columns: new[] { "VeterinarioId", "FechaHora" });

            migrationBuilder.CreateIndex(
                name: "IX_DisponibilidadesVeterinarios_VeterinarioId_DiaSemana_Jornada",
                table: "DisponibilidadesVeterinarios",
                columns: new[] { "VeterinarioId", "DiaSemana", "Jornada" });
        }
    }
}

