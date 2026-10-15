using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class UpdateAtencionMedicaAndAddRelatedTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AlertaDesnutricion",
                table: "AtencionesMedicas",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Anamnesis",
                table: "AtencionesMedicas",
                type: "TEXT",
                maxLength: 2000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "AptitudVacunacionResultado",
                table: "AtencionesMedicas",
                type: "TEXT",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "AutorizacionVeterinaria",
                table: "AtencionesMedicas",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "BuenEstadoGeneral",
                table: "AtencionesMedicas",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExamenFisico",
                table: "AtencionesMedicas",
                type: "TEXT",
                maxLength: 2000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "MotivoNoAptoVacunacion",
                table: "AtencionesMedicas",
                type: "TEXT",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PesoReferenciaUtilizado",
                table: "AtencionesMedicas",
                type: "TEXT",
                precision: 6,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PorcentajeDisminucionPeso",
                table: "AtencionesMedicas",
                type: "TEXT",
                precision: 6,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "SinEnfermedadInfecciosaAguda",
                table: "AtencionesMedicas",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "SinFiebre",
                table: "AtencionesMedicas",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VacunaDosis",
                table: "AtencionesMedicas",
                type: "TEXT",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VacunaLote",
                table: "AtencionesMedicas",
                type: "TEXT",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "VacunaProximaDosis",
                table: "AtencionesMedicas",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "VeterinarioId",
                table: "AtencionesMedicas",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "EstudiosComplementarios",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    AtencionMedicaId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Nombre = table.Column<string>(type: "TEXT", maxLength: 150, nullable: false),
                    Resultado = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EstudiosComplementarios", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EstudiosComplementarios_AtencionesMedicas_AtencionMedicaId",
                        column: x => x.AtencionMedicaId,
                        principalTable: "AtencionesMedicas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PrescripcionesMedicas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    AtencionMedicaId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Medicamento = table.Column<string>(type: "TEXT", maxLength: 150, nullable: false),
                    Dosis = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Frecuencia = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Duracion = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PrescripcionesMedicas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PrescripcionesMedicas_AtencionesMedicas_AtencionMedicaId",
                        column: x => x.AtencionMedicaId,
                        principalTable: "AtencionesMedicas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProcedimientosQuirurgicos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    AtencionMedicaId = table.Column<Guid>(type: "TEXT", nullable: false),
                    TipoProcedimiento = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Descripcion = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    Fecha = table.Column<DateTime>(type: "TEXT", nullable: false),
                    VeterinarioId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ObservacionesComplicaciones = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProcedimientosQuirurgicos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProcedimientosQuirurgicos_AtencionesMedicas_AtencionMedicaId",
                        column: x => x.AtencionMedicaId,
                        principalTable: "AtencionesMedicas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProcedimientosQuirurgicos_Usuarios_VeterinarioId",
                        column: x => x.VeterinarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AtencionesMedicas_VeterinarioId",
                table: "AtencionesMedicas",
                column: "VeterinarioId");

            migrationBuilder.CreateIndex(
                name: "IX_EstudiosComplementarios_AtencionMedicaId",
                table: "EstudiosComplementarios",
                column: "AtencionMedicaId");

            migrationBuilder.CreateIndex(
                name: "IX_PrescripcionesMedicas_AtencionMedicaId",
                table: "PrescripcionesMedicas",
                column: "AtencionMedicaId");

            migrationBuilder.CreateIndex(
                name: "IX_ProcedimientosQuirurgicos_AtencionMedicaId",
                table: "ProcedimientosQuirurgicos",
                column: "AtencionMedicaId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProcedimientosQuirurgicos_VeterinarioId",
                table: "ProcedimientosQuirurgicos",
                column: "VeterinarioId");

            migrationBuilder.AddForeignKey(
                name: "FK_AtencionesMedicas_Usuarios_VeterinarioId",
                table: "AtencionesMedicas",
                column: "VeterinarioId",
                principalTable: "Usuarios",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Turnos_AtencionesMedicas_AtencionMedicaId",
                table: "Turnos",
                column: "AtencionMedicaId",
                principalTable: "AtencionesMedicas",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AtencionesMedicas_Usuarios_VeterinarioId",
                table: "AtencionesMedicas");

            migrationBuilder.DropForeignKey(
                name: "FK_Turnos_AtencionesMedicas_AtencionMedicaId",
                table: "Turnos");

            migrationBuilder.DropTable(
                name: "EstudiosComplementarios");

            migrationBuilder.DropTable(
                name: "PrescripcionesMedicas");

            migrationBuilder.DropTable(
                name: "ProcedimientosQuirurgicos");

            migrationBuilder.DropIndex(
                name: "IX_AtencionesMedicas_VeterinarioId",
                table: "AtencionesMedicas");

            migrationBuilder.DropColumn(
                name: "AlertaDesnutricion",
                table: "AtencionesMedicas");

            migrationBuilder.DropColumn(
                name: "Anamnesis",
                table: "AtencionesMedicas");

            migrationBuilder.DropColumn(
                name: "AptitudVacunacionResultado",
                table: "AtencionesMedicas");

            migrationBuilder.DropColumn(
                name: "AutorizacionVeterinaria",
                table: "AtencionesMedicas");

            migrationBuilder.DropColumn(
                name: "BuenEstadoGeneral",
                table: "AtencionesMedicas");

            migrationBuilder.DropColumn(
                name: "ExamenFisico",
                table: "AtencionesMedicas");

            migrationBuilder.DropColumn(
                name: "MotivoNoAptoVacunacion",
                table: "AtencionesMedicas");

            migrationBuilder.DropColumn(
                name: "PesoReferenciaUtilizado",
                table: "AtencionesMedicas");

            migrationBuilder.DropColumn(
                name: "PorcentajeDisminucionPeso",
                table: "AtencionesMedicas");

            migrationBuilder.DropColumn(
                name: "SinEnfermedadInfecciosaAguda",
                table: "AtencionesMedicas");

            migrationBuilder.DropColumn(
                name: "SinFiebre",
                table: "AtencionesMedicas");

            migrationBuilder.DropColumn(
                name: "VacunaDosis",
                table: "AtencionesMedicas");

            migrationBuilder.DropColumn(
                name: "VacunaLote",
                table: "AtencionesMedicas");

            migrationBuilder.DropColumn(
                name: "VacunaProximaDosis",
                table: "AtencionesMedicas");

            migrationBuilder.DropColumn(
                name: "VeterinarioId",
                table: "AtencionesMedicas");
        }
    }
}
