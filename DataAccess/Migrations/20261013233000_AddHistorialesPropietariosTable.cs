using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddHistorialesPropietariosTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "HistorialesPropietarios",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    MascotaId = table.Column<Guid>(type: "TEXT", nullable: false),
                    DuenoAnteriorId = table.Column<Guid>(type: "TEXT", nullable: false),
                    NuevoDuenoId = table.Column<Guid>(type: "TEXT", nullable: false),
                    FechaReasignacion = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UsuarioId = table.Column<Guid>(type: "TEXT", nullable: false),
                    UsuarioUsername = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HistorialesPropietarios", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HistorialesPropietarios_Duenos_DuenoAnteriorId",
                        column: x => x.DuenoAnteriorId,
                        principalTable: "Duenos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_HistorialesPropietarios_Duenos_NuevoDuenoId",
                        column: x => x.NuevoDuenoId,
                        principalTable: "Duenos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_HistorialesPropietarios_Mascotas_MascotaId",
                        column: x => x.MascotaId,
                        principalTable: "Mascotas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_HistorialesPropietarios_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_HistorialesPropietarios_DuenoAnteriorId",
                table: "HistorialesPropietarios",
                column: "DuenoAnteriorId");

            migrationBuilder.CreateIndex(
                name: "IX_HistorialesPropietarios_MascotaId",
                table: "HistorialesPropietarios",
                column: "MascotaId");

            migrationBuilder.CreateIndex(
                name: "IX_HistorialesPropietarios_NuevoDuenoId",
                table: "HistorialesPropietarios",
                column: "NuevoDuenoId");

            migrationBuilder.CreateIndex(
                name: "IX_HistorialesPropietarios_UsuarioId",
                table: "HistorialesPropietarios",
                column: "UsuarioId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HistorialesPropietarios");
        }
    }
}
