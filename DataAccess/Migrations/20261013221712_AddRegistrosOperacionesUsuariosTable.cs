using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddRegistrosOperacionesUsuariosTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RegistrosOperacionesUsuarios",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    UsuarioId = table.Column<Guid>(type: "TEXT", nullable: false),
                    UsernameAfectado = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    TipoOperacion = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    AdministradorId = table.Column<Guid>(type: "TEXT", nullable: false),
                    AdministradorUsername = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    FechaOperacion = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Detalles = table.Column<string>(type: "TEXT", maxLength: 250, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RegistrosOperacionesUsuarios", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RegistrosOperacionesUsuarios");
        }
    }
}
