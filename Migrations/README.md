# Migraciones de Base de Datos (EF Core SQLite)

> Esta carpeta es solo documental. Las migraciones reales se generan y ejecutan desde `DataAccess/Migrations/` (proyecto `DataAccess`, `VeterinariaDbContext`). Ver también la sección 2.7 del [README](../README.md).

Ubicación principal en el proyecto:
- [DataAccess/Migrations/](../DataAccess/Migrations/)

## Migraciones registradas (11)

1. `20261009023939_InitialCreate`: tablas `Duenos`, `Mascotas` y `AtencionesMedicas`, índices únicos y restricciones relacionales `DeleteBehavior.Restrict`.
2. `20261013180000_AddUsuariosTable`: tabla `Usuarios` (roles `Administrador`, `Veterinario` y `Recepcionista`).
3. `20261013190000_AddTokensRevocadosTable`: tabla `TokensRevocados` para el cierre de sesión.
4. `20261013221712_AddRegistrosOperacionesUsuariosTable`: auditoría de desbloqueos y restablecimientos de contraseña.
5. `20261013233000_AddHistorialesPropietariosTable`: historial de reasignación de dueño de una mascota.
6. `20261014190338_AddTurnosAndDisponibilidadesTables`: tablas `Turnos` y `DisponibilidadesVeterinarios`.
7. `20261015211120_UpdateAtencionMedicaAndAddRelatedTables`: prescripciones, estudios complementarios y procedimientos quirúrgicos.
8. `20261016015755_AddReportesTable`: tabla `Reportes`.
9. `20261114165602_AddTiemposAtencionTurnoYRelacionAtencionMedica`: estado `EnAtencion`, `FechaHoraInicioAtencion` / `FechaHoraFinAtencion` y vínculo `AtencionMedica.TurnoId` (índice único filtrado).
10. `20261114202200_AddUniqueIndexesTurnosYDisponibilidad`: tres índices únicos de integridad de agenda (solo índices, sin reconstruir tablas):
    - `IX_Turnos_VeterinarioId_FechaHora` (filtrado: `Estado IN ('Confirmado','EnAtencion','Finalizado')`).
    - `IX_DisponibilidadesVeterinarios_VeterinarioId_DiaSemana_Jornada`.
    - `IX_Turnos_AtencionMedicaId_FechaHora` (filtrado: `AtencionMedicaId IS NOT NULL AND Estado <> 'Cancelado'`).
11. `20261115052125_AddFechaRegistroMascota`: agrega `Mascotas.FechaRegistro` (`TEXT NULL`) sin modificar filas existentes. Las mascotas históricas quedan con `NULL` y no se cuentan en reportes de altas por período.

## Comandos

```bash
dotnet ef migrations add <Nombre> --project DataAccess/DataAccess.csproj --startup-project API/API.csproj
dotnet ef database update --project DataAccess/DataAccess.csproj --startup-project API/API.csproj
```

La API también aplica las migraciones pendientes al iniciar (`DbInitializer` → `Database.MigrateAsync()`).

## Recomendaciones operativas

- La base local es `API/veterinaria.db` (excluida de Git).
- Antes de migrar una base con datos, crear una copia de respaldo y verificar `PRAGMA integrity_check` y `PRAGMA foreign_key_check`.
- No eliminar ni regenerar la base para resolver un error de migración sin respaldo previo.
