# Contexto Operativo para Agentes de IA — Sistema Veterinaria

Este documento define el contexto operativo, directivas de arquitectura, convenciones técnicas y comandos CLI para cualquier agente de Inteligencia Artificial que opere sobre el repositorio **Grupo_7_SistemaVeterinaria**.

> Estado de referencia: suite de **687 pruebas superadas** (400 unitarias + 287 de integración) y compilación sin errores, verificadas tras las correcciones de auditoría 1 a 7 y 9. La recomendación 8 (refactorización de servicios) sigue **pendiente**.

---

## 1. Propósito del Proyecto

El proyecto implementa una **API RESTful N-Tier en .NET 10** con **SQLite y EF Core 10** para la gestión clínica y administrativa de una veterinaria ("Patitas"). Las entidades de dominio principales son:
1. **Dueño (`Dueno`)**: Cliente responsable del paciente.
2. **Mascota (`Mascota`)**: Paciente animal sujeto a baja lógica y controles clínicos. Tiene `FechaRegistro` (`DateTime?`, UTC, inmutable; `NULL` en las 10 mascotas históricas previas a la migración).
3. **Atención Médica (`AtencionMedica`)**: Registro clínico inmutable (con prescripciones, estudios complementarios y procedimientos quirúrgicos) asociado a la historia clínica del paciente. La historia clínica es una proyección derivada, no una tabla.
4. **Turno (`Turno`)**: Agenda con estados `Pendiente`, `Confirmado`, `EnAtencion`, `Finalizado` y `Cancelado`.
5. **Disponibilidad (`DisponibilidadVeterinario`)**: Franjas semanales por veterinario, día y jornada.
6. **Usuario (`Usuario`)**, **Token revocado (`TokenRevocado`)**, **Registro de operaciones (`RegistroOperacionUsuario`)**, **Historial de propietarios (`HistorialPropietario`)** y **Reporte (`Reporte`)**.

Roles: `Administrador` (usuarios y reportes), `Veterinario` (clínica, atención y turnos propios) y `Recepcionista` (dueños, mascotas y agenda).

---

## 2. Comandos CLI Operativos

Todos los comandos deben ejecutarse desde la raíz del repositorio:

### Compilación
```bash
dotnet build SistemaVeterinaria.slnx
```

### Ejecución de Pruebas (687 tests: 400 unitarios + 287 de integración)
```bash
dotnet test SistemaVeterinaria.slnx
```

### Ejecutar Pruebas Específicas
```bash
# Solo pruebas unitarias de lógica de negocio (xUnit + Moq)
dotnet test tests/BusinessLogic.Tests/BusinessLogic.Tests.csproj

# Solo pruebas de integración HTTP (WebApplicationFactory + SQLite en memoria)
dotnet test tests/API.Tests/API.Tests.csproj
```

### Configuración obligatoria antes de ejecutar la API (clave JWT)
La API **no arranca** sin `JwtSettings:SecretKey` de al menos 32 bytes UTF-8. No existe ninguna clave versionada; se configura una sola vez por máquina con .NET User Secrets (el `UserSecretsId` está en `API/API.csproj`) o con la variable de entorno `JwtSettings__SecretKey`:
```bash
dotnet user-secrets set "JwtSettings:SecretKey" "<CLAVE_PROPIA_DE_AL_MENOS_32_CARACTERES>" --project API/API.csproj
```
Nunca escribir claves reales en código, documentación, pruebas ni informes. Las pruebas de integración inyectan su propia clave de prueba.

### Ejecución de la API
```bash
dotnet run --project API/API.csproj
```
* **URLs (`API/Properties/launchSettings.json`):** `http://localhost:5274` y `https://localhost:7207`.
* **Especificación OpenAPI (solo `Development`):** `https://localhost:7207/openapi/v1.json` (o `http://localhost:5274/openapi/v1.json`). **Scalar no está implementado**; no hay paquete ni mapeo de Scalar.
* **Base de datos SQLite:** `API/veterinaria.db` (cadena `Data Source=veterinaria.db;Cache=Shared`). Al arrancar, `DbInitializer` ejecuta `Database.MigrateAsync()` (aplica las migraciones pendientes) y siembra datos de prueba solo si la base está vacía. Está excluida de Git (`*.db`, `*.db-shm`, `*.db-wal`).

### Migraciones (EF Core)
```bash
dotnet ef migrations add <Nombre> --project DataAccess/DataAccess.csproj --startup-project API/API.csproj
dotnet ef database update --project DataAccess/DataAccess.csproj --startup-project API/API.csproj
```
Las migraciones viven en `DataAccess/Migrations/` (11 al día de hoy, la última `AddFechaRegistroMascota`). **Antes de migrar `API/veterinaria.db`, crear una copia de respaldo** y verificar `PRAGMA integrity_check`.

---

## 3. Convenciones de Arquitectura N-Tier

El sistema sigue un flujo unidireccional y desacoplado estricto:

$$\text{Controller} \longrightarrow \text{Service} \longrightarrow \text{Repository} \longrightarrow \text{DbContext}$$

### Responsabilidades por Capa:
1. **`API/` (Controllers)**:
   * Solo recibe DTOs y gestiona el protocolo HTTP.
   * Inyección de dependencias obligatoria por constructor (`I...Service`).
   * Manejo de errores mediante bloques `try/catch` explícitos mapeando excepciones tipadas de `Shared/Exceptions/` a códigos HTTP específicos.
   * **CERO lógica de negocio en controladores**.
   * Rate limiting nativo de ASP.NET Core (`AuthPolicy`, ventana fija por IP) aplicado con `[EnableRateLimiting("AuthPolicy")]` a `POST /api/auth/login` y `POST /api/auth/cambiar-password-temporal`; responde **429**. Configurable en `RateLimiting:AuthPermitLimit` (10) y `RateLimiting:AuthWindowSeconds` (60). Las pruebas de integración lo elevan para no interferir.

2. **`BusinessLogic/` (Services)**:
   * Toda la validación de entrada y reglas de negocio viven exclusivamente aquí.
   * Inyección de dependencias por constructor (`I...Repository`).
   * **PROHIBIDO el uso de LINQ-to-entities**: Toda consulta debe resolverse llamando a métodos descriptivos del repositorio. *(Deuda conocida: `SMELL-01` y la recomendación 8 siguen pendientes; el código existente aún tiene filtrados LINQ en memoria en algunos servicios.)*
   * Mapeo manual con métodos privados `MapToResponseDTO(...)`. No usar AutoMapper.
   * Lanza excepciones tipadas ante cualquier violación de reglas o validaciones.
   * `VeterinarioLockManager` serializa en memoria, por veterinario y por turno, las operaciones críticas de agenda (crear, confirmar, reprogramar, cancelar, configurar disponibilidad y generar seguimientos crónicos). Reutilizarlo en cualquier operación nueva de agenda.

3. **`DataAccess/` (Repositories & DbContext)**:
   * Maneja EF Core (`VeterinariaDbContext`).
   * Asignación de identificadores: `entity.Id = Guid.NewGuid()` dentro de `CreateAsync(...)`.
   * En consultas de solo lectura, invocar obligatoriamente `.AsNoTracking()`.
   * En `OnModelCreating`, configurar siempre `OnDelete(DeleteBehavior.Restrict)` en relaciones hijas.
   * Operaciones compuestas (atención + seguimiento crónico, reemplazo de disponibilidad) usan transacciones explícitas de EF Core.
   * `DbInitializer` aplica migraciones (`Database.MigrateAsync()`) y siembra datos iniciales de prueba si la base está vacía.
   * Índices únicos de integridad de agenda (recomendación 5), traducidos a **409** cuando se violan:
     * `IX_Turnos_VeterinarioId_FechaHora` (filtrado: `Estado IN ('Confirmado','EnAtencion','Finalizado')`).
     * `IX_DisponibilidadesVeterinarios_VeterinarioId_DiaSemana_Jornada`.
     * `IX_Turnos_AtencionMedicaId_FechaHora` (filtrado: `AtencionMedicaId IS NOT NULL AND Estado <> 'Cancelado'`).
     * Además existen índices únicos para `AtencionMedica.TurnoId` y para el DNI del dueño.

4. **`Shared/`**:
   * Contiene DTOs de transporte (`<Entidad>CreateDTO`, `<Entidad>UpdateDTO`, `<Entidad>ResponseDTO`).
   * Contiene excepciones tipadas, cada una en su propio archivo (`NotFoundException` → 404, `ValidationException` → 400, conflictos de negocio → 409, autorización de recurso → 403).

5. **`bruno/`**:
   * Colección de requests HTTP para Bruno (129 requests) organizada por carpetas temáticas: `auth`, `duenos`, `mascotas`, `atenciones-medicas`, `historia-clinica`, `turnos`, `usuarios` y `reportes`.

---

## 4. Endpoints y Roles (según el código)

| Controlador / Ruta base | Autorización | Endpoints principales |
|---|---|---|
| `AuthController` — `api/auth` | Anónimo (`login`, `cambiar-password-temporal`, con rate limiting); `[Authorize]` (`logout`, `verificar-sesion`) | `POST login`, `POST cambiar-password-temporal`, `POST logout`, `GET verificar-sesion` |
| `DuenosController` — `api/duenos` | `Recepcionista`, `Veterinario` | `GET`, `GET {id}`, `POST`, `PUT {id}`, `DELETE {id}` |
| `MascotasController` — `api/mascotas` | `Recepcionista`, `Veterinario` | `GET`, `GET {id}`, `GET dueno/{duenoId}`, `POST`, `PUT {id}`, `PATCH {id}/desactivar` (o `DELETE {id}`, baja lógica), `PATCH {id}/reasignar-dueno` |
| `AtencionesMedicasController` — `api/atenciones-medicas` (alias `api/atenciones`) | `Veterinario` | `GET`, `GET {id}`, `GET mascota/{mascotaId}`, `POST`; `PUT` y `DELETE` responden siempre 409 (RN-01) |
| `HistoriaClinicaController` — `api/historias-clinicas` | `Veterinario` | `GET {mascotaId}`, `GET mascota/{mascotaId}`, `GET /api/mascotas/{mascotaId}/historia-clinica`, `GET` (búsqueda) |
| `TurnosController` — `api/turnos` | `Recepcionista`, `Veterinario`; `iniciar-atencion` y `finalizar` solo `Veterinario` | `GET`, `GET {id}`, `POST`, `PUT {id}`, `POST {id}/confirmar`, `POST {id}/iniciar-atencion`, `POST {id}/finalizar`, `PATCH`/`POST {id}/cancelar`, `GET`/`PUT`/`POST disponibilidad/{veterinarioId}` |
| `UsuariosController` — `api/usuarios` | `Administrador` | `GET`, `GET {id}`, `POST {id}/desbloquear`, `POST {id}/restablecer-password` |
| `ReportesController` — `api/reportes` | `Administrador` | `POST` o `POST generar`, `GET`, `GET {id}` |

---

## 5. Matriz de Reglas de Negocio Implementadas

1. **RN-01 (Inmutabilidad de Atenciones Médicas)**:
   * Las atenciones registradas son inmutables.
   * Métodos `PUT` y `DELETE` en `AtencionesMedicasController` arrojan `AtencionInmutableException` (**409 Conflict**).
2. **RN-03 y RN-04 (Aptitud para Vacunación)**:
   * Para registrar `VacunaAplicada`, el paciente debe tener `EsAptoVacunacion == true`.
   * Si no es apto y se intenta registrar vacuna, arroja `PacienteNoAptoVacunacionException` (**409 Conflict**).
3. **RN-05 (Unicidad de DNI)**:
   * El DNI del dueño es único. Si ya existe, arroja `DniDuplicadoException` (**409 Conflict**).
4. **RN-06 (Asociación Obligatoria a Dueño)**:
   * Toda mascota requiere un dueño previamente registrado. Si no existe, arroja `DuenoNotFoundException` (**404 Not Found**).
5. **RN-07 y MEJ-03 (Baja Lógica de Mascotas)**:
   * La baja de una mascota es lógica (`Activa = false`). No se elimina físicamente.
   * Baja exitosa: Devuelve **200 OK** con confirmación y estado `Inactiva`.
   * Intento de baja sobre mascota ya inactiva: Arroja `MascotaYaInactivaException` (**409 Conflict**).
6. **RN-07 (Restricción Clínica)**:
   * Una mascota con `Activa == false` no puede recibir nuevas atenciones médicas ni turnos. Arroja `MascotaInactivaException` (**409 Conflict**).
7. **Fecha de Última Visita**:
   * Se actualiza automáticamente al persistir con éxito una atención médica.
8. **Restricción Relacional**:
   * No se permite eliminar un dueño si tiene mascotas asociadas. Arroja `DuenoConMascotasException` (**409 Conflict**).
9. **RN-08 (No superposición de turnos)**:
   * Un veterinario no puede tener dos turnos `Confirmado`, `EnAtencion` o `Finalizado` en el mismo horario (`HorarioNoDisponibleException`, **409**). Reforzado por índice único filtrado y por `VeterinarioLockManager`.
10. **RN-09 (Máquina de estados del turno)**:
    * `Pendiente → Confirmado → EnAtencion → Finalizado`; cancelable desde `Pendiente` o `Confirmado`. Registra `FechaHoraInicioAtencion` y `FechaHoraFinAtencion` (UTC).
    * Finalizar exige una atención médica vinculada (`AtencionMedica.TurnoId`, `1:1` opcional; `TurnoSinAtencionMedicaException` → 409). Se admiten atenciones espontáneas con `TurnoId = null`.
    * Turnos `EnAtencion`, `Finalizado` o `Cancelado` no se reprograman ni se cancelan.
11. **RN-10 (Disponibilidad horaria)**:
    * Los turnos solo se agendan dentro de franjas activas del veterinario, alineados a bloques de 30 minutos. No se admiten duplicados de día y jornada (índice único, **409**). El reemplazo de disponibilidad es atómico (DELETE antes de INSERT en una transacción).
12. **RN-11 (Seguimiento crónico)**:
    * Si `EsEnfermedadCronica` es `true` debe enviarse `SeguimientoCronico`; genera turnos futuros `Pendiente` vinculados con `Turno.AtencionMedicaId` (`1:N`), dentro de la misma transacción que la atención (rollback total ante fallos). Índice único por `(AtencionMedicaId, FechaHora)` excluyendo cancelados (**409**, `SeguimientoCronicoDuplicadoException`).
13. **RN-12 (Alerta de pérdida de peso)**:
    * Si el peso baja más de 10 % respecto de la consulta anterior, se activa `alertaDesnutricion`.
14. **Autorización horizontal de turnos (recomendación 6)**:
    * Un `Veterinario` solo opera sobre sus propios turnos y no puede reasignarlos (`TurnoVeterinarioNoAutorizadoException`, **403**). `Recepcionista` opera sobre cualquier turno.
15. **Métricas de mascotas por período (recomendación 4)**:
    * `ReporteService` filtra el módulo `Mascotas` con `MascotaRepository.GetByRangoFechasAsync(desde, hasta)` por `FechaRegistro`. Las mascotas históricas con `FechaRegistro = NULL` **no se contabilizan** en reportes de altas por período. `FechaRegistro` se asigna al registrar y no se modifica al actualizar.
16. **Seguridad de autenticación (recomendación 7)**:
    * Bloqueo de cuenta a los 5 intentos fallidos (se mantiene junto con el rate limiting por IP, **429**), tokens JWT revocables y secreto JWT externalizado (User Secrets o variable de entorno, mínimo 32 bytes).

---

## 6. Estado de las Recomendaciones de la Auditoría

| # | Recomendación | Estado |
|---|---|---|
| 1 | Máquina de estados de turno con `EnAtencion` | Implementada |
| 2 | Atomicidad de atención médica y seguimiento crónico | Implementada |
| 3 | Historia clínica como proyección derivada (documentada) | Implementada |
| 4 | `FechaRegistro` de mascotas y métricas por período | Implementada |
| 5 | Integridad de agenda: bloqueos, transacciones e índices únicos | Implementada |
| 6 | Autorización horizontal en turnos | Implementada |
| 7 | Secreto JWT externalizado y rate limiting | Implementada |
| 8 | Refactorización de servicios extensos y estados tipados | **Pendiente** |
| 9 | Actualización de README y AGENTS.md | Implementada (este documento) |
