# Contexto Operativo para Agentes de IA — Sistema Veterinaria

Este documento define el contexto operativo, directivas de arquitectura, convenciones técnicas y comandos CLI para cualquier agente de Inteligencia Artificial que opere sobre el repositorio **Grupo_7_SistemaVeterinaria**.

---

## 1. Propósito del Proyecto

El proyecto implementa una **API RESTful N-Tier en .NET 10** con **SQLite y EF Core 10** para la gestión clínica y administrativa de una veterinaria ("Patitas"). Sus tres entidades principales son:
1. **Dueño (`Dueno`)**: Cliente responsable del paciente.
2. **Mascota (`Mascota`)**: Paciente animal sujeto a baja lógica y controles clínicos.
3. **Atención Médica (`AtencionMedica`)**: Registro clínico inmutable asociado a la historia clínica del paciente.

---

## 2. Comandos CLI Operativos

Todos los comandos deben ejecutarse desde la raíz del repositorio:

### Compilación
```bash
dotnet build SistemaVeterinaria.slnx
```

### Ejecución de Pruebas (38 tests: 22 unitarios + 16 de integración)
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

### Ejecución de la API
```bash
dotnet run --project API/API.csproj
```
* **Endpoint de documentación Scalar:** `https://localhost:7198/scalar/v1`
* **Base de datos SQLite:** Se inicializa automáticamente al arrancar con `DbInitializer` (`veterinaria.db`).

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

2. **`BusinessLogic/` (Services)**:
   * Toda la validación de entrada y reglas de negocio viven exclusivamente aquí.
   * Inyección de dependencias por constructor (`I...Repository`).
   * **PROHIBIDO el uso de LINQ-to-entities**: Toda consulta debe resolverse llamando a métodos descriptivos del repositorio.
   * Mapeo manual con métodos privados `MapToResponseDTO(...)`. No usar AutoMapper.
   * Lanza excepciones tipadas ante cualquier violación de reglas o validaciones.

3. **`DataAccess/` (Repositories & DbContext)**:
   * Maneja EF Core (`VeterinariaDbContext`).
   * Asignación de identificadores: `entity.Id = Guid.NewGuid()` dentro de `CreateAsync(...)`.
   * En consultas de solo lectura, invocar obligatoriamente `.AsNoTracking()`.
   * En `OnModelCreating`, configurar siempre `OnDelete(DeleteBehavior.Restrict)` en relaciones hijas.
   * `DbInitializer` se encarga de aplicar migraciones (`Database.MigrateAsync()`) y sembrar datos iniciales de prueba si la base está vacía.

4. **`Shared/`**:
   * Contiene DTOs de transporte (`<Entidad>CreateDTO`, `<Entidad>UpdateDTO`, `<Entidad>ResponseDTO`).
   * Contiene excepciones tipadas, cada una en su propio archivo (`NotFoundException` → 404, `ValidationException` → 400, conflictos de negocio → 409).

5. **`bruno/`**:
   * Colección de requests HTTP para Bruno organizada por carpetas temáticas (`duenos`, `mascotas`, `atenciones-medicas`).

---

## 4. Matriz de Reglas de Negocio Implementadas

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
   * Una mascota con `Activa == false` no puede recibir nuevas atenciones médicas. Arroja `MascotaInactivaException` (**409 Conflict**).
7. **Fecha de Última Visita**:
   * Se actualiza automáticamente al persistir con éxito una atención médica.
8. **Restricción Relacional**:
   * No se permite eliminar un dueño si tiene mascotas asociadas. Arroja `DuenoConMascotasException` (**409 Conflict**).
