# Migraciones de Base de Datos (EF Core SQLite)

Las migraciones de Entity Framework Core se gestionan mediante el DbContext de la capa `DataAccess`.

Ubicación principal en el proyecto:
- [DataAccess/Migrations/](file:///c:/Users/jazvi/OneDrive/Documentos/GitHub/Grupo_7_SistemaVeterinaria/DataAccess/Migrations/)

Migraciones registradas:
1. `20261009023939_InitialCreate.cs`: Creación de tablas `Duenos`, `Mascotas`, `AtencionesMedicas`, índices únicos y restricciones relacionales `DeleteBehavior.Restrict`.
