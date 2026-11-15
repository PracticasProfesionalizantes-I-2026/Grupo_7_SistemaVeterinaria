using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddTiemposAtencionTurnoYRelacionAtencionMedica : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "FechaHoraFinAtencion",
                table: "Turnos",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "FechaHoraInicioAtencion",
                table: "Turnos",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "TurnoId",
                table: "AtencionesMedicas",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AtencionesMedicas_TurnoId",
                table: "AtencionesMedicas",
                column: "TurnoId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_AtencionesMedicas_Turnos_TurnoId",
                table: "AtencionesMedicas",
                column: "TurnoId",
                principalTable: "Turnos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AtencionesMedicas_Turnos_TurnoId",
                table: "AtencionesMedicas");

            migrationBuilder.DropIndex(
                name: "IX_AtencionesMedicas_TurnoId",
                table: "AtencionesMedicas");

            migrationBuilder.DropColumn(
                name: "FechaHoraFinAtencion",
                table: "Turnos");

            migrationBuilder.DropColumn(
                name: "FechaHoraInicioAtencion",
                table: "Turnos");

            migrationBuilder.DropColumn(
                name: "TurnoId",
                table: "AtencionesMedicas");
        }
    }
}
