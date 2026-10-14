using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddTurnosAndDisponibilidadesTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DisponibilidadesVeterinarios",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    VeterinarioId = table.Column<Guid>(type: "TEXT", nullable: false),
                    DiaSemana = table.Column<int>(type: "INTEGER", nullable: false),
                    Jornada = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    HoraInicio = table.Column<TimeSpan>(type: "TEXT", nullable: false),
                    HoraFin = table.Column<TimeSpan>(type: "TEXT", nullable: false),
                    Activo = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DisponibilidadesVeterinarios", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DisponibilidadesVeterinarios_Usuarios_VeterinarioId",
                        column: x => x.VeterinarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Turnos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    MascotaId = table.Column<Guid>(type: "TEXT", nullable: false),
                    VeterinarioId = table.Column<Guid>(type: "TEXT", nullable: false),
                    AtencionMedicaId = table.Column<Guid>(type: "TEXT", nullable: true),
                    FechaHora = table.Column<DateTime>(type: "TEXT", nullable: false),
                    MotivoConsulta = table.Column<string>(type: "TEXT", maxLength: 250, nullable: false),
                    Estado = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    EsAutomatico = table.Column<bool>(type: "INTEGER", nullable: false),
                    TipoOrigen = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    MotivoCancelacion = table.Column<string>(type: "TEXT", maxLength: 250, nullable: true),
                    FechaCancelacion = table.Column<DateTime>(type: "TEXT", nullable: true),
                    UsuarioCancelacionId = table.Column<Guid>(type: "TEXT", nullable: true),
                    UsuarioCancelacionUsername = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    FechaCreacion = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Turnos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Turnos_Mascotas_MascotaId",
                        column: x => x.MascotaId,
                        principalTable: "Mascotas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Turnos_Usuarios_VeterinarioId",
                        column: x => x.VeterinarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DisponibilidadesVeterinarios_VeterinarioId_DiaSemana_Jornada",
                table: "DisponibilidadesVeterinarios",
                columns: new[] { "VeterinarioId", "DiaSemana", "Jornada" });

            migrationBuilder.CreateIndex(
                name: "IX_Turnos_AtencionMedicaId",
                table: "Turnos",
                column: "AtencionMedicaId");

            migrationBuilder.CreateIndex(
                name: "IX_Turnos_MascotaId",
                table: "Turnos",
                column: "MascotaId");

            migrationBuilder.CreateIndex(
                name: "IX_Turnos_VeterinarioId_FechaHora",
                table: "Turnos",
                columns: new[] { "VeterinarioId", "FechaHora" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DisponibilidadesVeterinarios");

            migrationBuilder.DropTable(
                name: "Turnos");
        }
    }
}
