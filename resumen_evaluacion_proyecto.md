# Resumen de Evaluación Integral del Proyecto — Sistema Veterinaria "Patitas"

**Repositorio:** `Grupo_7_SistemaVeterinaria`
**Fecha de evaluación:** 08/11/2026
**Alcance:** documentación de negocio, casos de uso, README, arquitectura, código fuente y suite de pruebas.
**Verificación técnica:** `dotnet test SistemaVeterinaria.slnx` → **539 pruebas aprobadas (314 unitarias + 225 de integración), 0 fallidas.**

---

## 1. Visión General

El proyecto implementa una API RESTful para la gestión clínica y administrativa de una veterinaria, construida sobre **.NET 10**, **EF Core 10** y **SQLite**, con una arquitectura **N-Tier** estricta:

```
Controllers (API)
      ↓
Services (BusinessLogic)
      ↓
Repositories (DataAccess)
      ↓
VeterinariaDbContext (SQLite)
```

Las tres entidades centrales del dominio son **Dueño**, **Mascota** y **Atención Médica**, complementadas por **Turno**, **DisponibilidadVeterinario**, **Usuario**, **HistorialPropietario** y **Reporte**. La documentación de negocio define las reglas RN-01 a RN-12 y un conjunto de requerimientos no funcionales (rendimiento, disponibilidad, seguridad, respaldo, modularidad). Los 14 casos de uso (CU-01 a CU-14) cubren autenticación, dueños, mascotas, historia clínica, atenciones, agenda/turnos, reportes y administración de usuarios.

La evaluación se organizó en tres fases:

1. **Trazabilidad documentación ↔ casos de uso** (actores, requisitos huérfanos, RNF en pre/postcondiciones).
2. **Gap Analysis** (requisitos faltantes, *scope creep*, adherencia a RNF).
3. **Auditoría estática de calidad** (bugs críticos, defectos menores, *code smells* y SOLID).

**Conclusión anticipada:** el proyecto presenta una base arquitectónica sólida, una cobertura de pruebas sobresaliente y un dominio bien modelado. Los hallazgos relevantes no son estructurales sino de **consistencia funcional en flujos secundarios**: estados de turno inalcanzables, operaciones compuestas sin transacción, métricas de reporte fuera de período, controles de autorización horizontal y algunas divergencias entre la documentación y el código.

---

## 2. Fortalezas

### 2.1 Arquitectura y separación de responsabilidades
- Flujo unidireccional respetado: los controladores solo traducen HTTP ↔ DTO y delegan en servicios; no contienen lógica de negocio.
- Inyección de dependencias por constructor en todas las capas (`API/Program.cs`).
- Repositorios como única puerta de acceso a EF Core; los servicios no ejecutan LINQ-to-entities.
- Mapeo manual explícito a DTOs (`MapToResponseDTO`), sin dependencia de AutoMapper, lo que facilita el control del contrato público.

### 2.2 Manejo de errores tipado
- Excepciones de dominio en `Shared/Exceptions/` (`NotFoundException`, `ValidationException`, conflictos de negocio) mapeadas a códigos HTTP específicos (400/404/409).
- Uso correcto de **409 Conflict** para inmutabilidad de atenciones (`AtencionInmutableException`), DNI duplicado, mascota inactiva, paciente no apto para vacunación y turno finalizado.
- *Middleware* global de excepciones en `API/Program.cs` como red de seguridad.

### 2.3 Persistencia y modelo de datos
- `DeleteBehavior.Restrict` configurado sistemáticamente en relaciones hijas (`VeterinariaDbContext.cs`), evitando borrados en cascada accidentales.
- Índices únicos en `Username`, `Email`, `Dni` y `Jti`.
- `.AsNoTracking()` en consultas de solo lectura.
- Actualización de `FechaUltimaVisita` dentro del mismo `SaveChanges` que crea la atención (consistencia de RN-07 / INF-07).
- Baja lógica de mascotas (`Activa = false`) correctamente implementada.

### 2.4 Seguridad
- Hash de contraseñas con **PBKDF2** (`PasswordHasherService`).
- JWT con validación de emisor, audiencia, firma y expiración.
- Revocación de tokens por `Jti` validada en cada request.
- Bloqueo temporal de cuenta por intentos fallidos y contraseña temporal.
- Autorización por roles con `[Authorize(Roles = ...)]` en los controladores sensibles.

### 2.5 Pruebas y documentación
- **539 pruebas automatizadas** (314 unitarias con xUnit + Moq; 225 de integración con `WebApplicationFactory` y SQLite en memoria), todas en verde.
- Casos de uso con matrices de trazabilidad, flujos alternativos, pre/postcondiciones y referencias a pruebas.
- Colección Bruno organizada por dominio (`duenos`, `mascotas`, `atenciones-medicas`).
- `SEGUIMIENTO_CORRECCIONES.md` evidencia un proceso de mejora continua (38 hallazgos históricos, 29 resueltos).

---

## 3. Gap Analysis

### 3.1 Trazabilidad documentación ↔ casos de uso

**Cobertura de actores.** Los actores definidos en la documentación de negocio (Recepcionista, Veterinario, Administrador) están reflejados en los CU y en las políticas de autorización. La matriz es consistente en los flujos principales.

**Requisitos huérfanos / inconsistencias detectadas:**

| Elemento documentado | Estado en código | Observación |
|---|---|---|
| `HistoriaClinica` como entidad persistida y `HistoriaClinicaNotFoundException` (CU-03, CU-07) | **No existe entidad ni excepción**; la historia clínica se deriva virtualmente de las atenciones | CU-07 afirma que crear una mascota "genera automáticamente su Historia Clínica inicial", pero no se persiste registro alguno. |
| `DuenoConMascotasActivasException` (CU-08) | El código usa `DuenoConMascotasException` | Divergencia de nomenclatura entre especificación y excepción real. |
| Flujo de turno **Finalizado** (CU-06, RN-09) | Solo se consume el estado; **ningún código lo asigna** | La transición a `EnAtencion`/`Finalizado` no existe en servicios ni controladores. |
| RNF de rendimiento (≤ 2 s), disponibilidad, respaldo y modularidad | No aparecen como criterios de aceptación en pre/postcondiciones de los CU | Los RNF quedan fuera de la trazabilidad verificable. |

**CU sin requisito de negocio explícito o con alcance ampliado:** CU-12 (consultar usuarios), CU-13 (desbloquear cuenta) y CU-14 (restablecer contraseña) responden a necesidades operativas razonables, pero no están formulados como requisitos en el documento de negocio; deben registrarse formalmente para evitar *scope creep* no trazado.

### 3.2 Requisitos faltantes y *scope creep*

**Faltantes o parcialmente implementados:**
- **Transición de estados de turno:** no existe endpoint ni lógica para pasar un turno a `EnAtencion` o `Finalizado`; el estado final solo puede aparecer por datos sembrados manualmente.
- **Vínculo explícito atención ↔ turno:** `Turno.AtencionMedicaId` existe, pero el flujo de atención no marca el turno asociado como finalizado.
- **Métricas de reporte por período para mascotas:** no hay `FechaRegistro` en `Mascota`, por lo que "mascotas registradas en el período" no puede calcularse correctamente.
- **Soporte de múltiples vacunas por atención:** el modelo usa un único campo `VacunaAplicada` (string).
- **Auditoría de accesos / trazabilidad de cambios:** no se registra quién consulta historias clínicas ni cambios administrativos.

**Scope creep:**
- Generación automática de series de turnos por seguimiento crónico (RN-11) y alertas de desnutrición, funcionalidad valiosa pero que excede el núcleo mínimo de los CU base y añade complejidad transaccional.
- Reportes con múltiples módulos y persistencia de reportes generados (`Reporte`), por encima de lo estrictamente pedido en CU-10/CU-11.

### 3.3 Adherencia a Requerimientos No Funcionales

| RNF | Nivel de cumplimiento | Evidencia / brecha |
|---|---|---|
| **Rendimiento (≤ 2 s)** | Parcial | No hay pruebas de carga ni medición; consultas de reportes cargan colecciones completas en memoria (`GetAllAsync`). |
| **Seguridad** | Alto | PBKDF2, JWT, revocación, bloqueo de cuenta y roles. Brecha: secreto JWT de desarrollo versionado y ausencia de *rate limiting* en login. |
| **Disponibilidad** | No verificada | Sin health checks, *retry policies* ni mecanismos de tolerancia a fallos de BD más allá de capturas puntuales. |
| **Respaldo / recuperación** | No implementado | SQLite sin estrategia de backup documentada ni automatizada. |
| **Modularidad / mantenibilidad** | Media | Capas bien separadas, pero servicios de 670–740 líneas y lógica duplicada. |
| **Compatibilidad / documentación API** | Parcial | `MapOpenApi()` activo, pero el README anuncia Scalar y el paquete/mapeo Scalar no está presente. |

### 3.4 Diseño vs. código — brechas técnicas relevantes

1. **Estado de turno inalcanzable (RN-09).** `Turno.Estado` se inicializa en `"Confirmado"` y los servicios validan `"Finalizado"`, pero no existe transición que lo establezca. El flujo de CU-06 queda efectivamente muerto salvo por datos sembrados en pruebas.
2. **Operación compuesta sin transacción.** `AtencionMedicaService.CreateAsync` persiste la atención (línea ~245) y **después** genera los turnos de seguimiento crónico. Solo captura `DbUpdateException`, `DbException` y `TimeoutException`; cualquier otra excepción deja la atención guardada y responde error, habilitando duplicados en un reintento.
3. **Métrica de mascotas fuera de período.** `ReporteService` obtiene `GetAllAsync()` para el módulo Mascotas (líneas ~165-175) e ignora `fechaDesde`/`fechaHasta`.
4. **Borrado de dueño con historial.** `DuenoService.DeleteAsync` solo valida mascotas activas/inactivas vía `HasMascotasAsync`; si el dueño fue reasignado y solo queda referenciado en `HistorialPropietario`, el `DeleteBehavior.Restrict` lanza `DbUpdateException` no controlada → **500**.
5. **Condiciones de carrera.** No hay índice único en `(VeterinarioId, FechaHora)` ni en la serie de seguimiento; la validación "consultar y luego insertar" permite doble reserva bajo concurrencia.
6. **Reemplazo de disponibilidad no atómico.** `DisponibilidadVeterinarioRepository.SetDisponibilidadesAsync` elimina todo y luego inserta sin transacción explícita; un fallo intermedio puede dejar al veterinario sin agenda.
7. **Autorización horizontal ausente en turnos.** Un Veterinario autenticado puede modificar/cancelar turnos de otro profesional, porque `TurnosController`/`TurnoService` no comparan el `VeterinarioId` del turno con el del token.

### 3.5 Calidad estática, SOLID y *code smells*

- **God class:** `AtencionMedicaService` (~740 líneas) concentra validación, mapeo, vacunación, alertas, seguimiento crónico y coordinación con turnos. Viola SRP y dificulta el testeo aislado.
- **Dependencia opcional:** `ITurnoService?` inyectado como nullable genera ramas `if (_turnoService != null)` y oculta un acoplamiento obligatorio.
- **Duplicación:** `CalcularFechasIdeales` y el mapeo de `TurnoResponseDTO` aparecen replicados entre `AtencionMedicaService` y `TurnoService`.
- **Métodos largos y anidamiento profundo** en `TurnoService` (~672 líneas) y `ReporteService` (~340 líneas).
- **Strings mágicos** para estados (`"Confirmado"`, `"Finalizado"`, `"Cancelado"`) en lugar de un `enum` o constantes centralizadas.
- **Consultas sobrecargadas:** `GetAllAsync()` para agregaciones de reporte impacta memoria y rendimiento.
- **Nombres inconsistentes** entre documentación y excepciones (`DuenoConMascotasActivasException` vs `DuenoConMascotasException`).
- **Documentación desactualizada:** `AGENTS.md` declara 146 pruebas frente a las 539 reales; el README anuncia Scalar sin implementación.

---

## 4. Conclusión de Arquitectura y Calidad

### 4.1 Valoración global

| Dimensión | Valoración | Comentario |
|---|---|---|
| Arquitectura | **Muy buena** | N-Tier limpia, dependencias unidireccionales, contratos claros. |
| Modelo de dominio | **Buena** | Entidades coherentes y reglas centralizadas; falta historia clínica persistida y transiciones de estado. |
| Seguridad | **Buena** | Base sólida (PBKDF2, JWT, revocación, roles); faltan *rate limiting* y gestión de secretos. |
| Consistencia transaccional | **Mejorable** | Operaciones compuestas sin transacción y carreras en reservas. |
| Calidad de código | **Buena con deuda** | Capas correctas, pero servicios extensos y lógica duplicada. |
| Pruebas | **Excelente** | 539/539 en verde, unitarias e integración. |
| Documentación | **Buena con brechas** | CU detallados y trazables; RNF no verificables y algunas divergencias funcionales. |

### 4.2 Riesgo residual

El sistema es **apto para un entorno académico/demostrativo y cercano a producción controlada**. Los riesgos altos se concentran en:

1. El flujo de turno finalizado (RN-09) es inalcanzable en código real.
2. La creación de atención + seguimiento crónico no es atómica y puede duplicar registros clínicos ante reintentos.
3. La historia clínica no existe como entidad persistida pese a lo especificado en CU-03/CU-07.

Los riesgos medios afectan principalmente reportes, borrado de dueños con historial, concurrencia de agenda y autorización horizontal.

### 4.3 Recomendaciones priorizadas

1. **Implementar la máquina de estados de turno** (`Pendiente → Confirmado → EnAtencion → Finalizado/Cancelado`) y vincularla al alta de atención.
2. **Envolver `CreateAsync` de atenciones en una transacción** o mover la generación de seguimiento a un patrón *outbox* idempotente.
3. **Definir el modelo de Historia Clínica** (entidad y excepción) o ajustar formalmente los CU-03/CU-07 para reflejar el enfoque derivado.
4. **Corregir métricas de reporte** añadiendo `FechaRegistro` a `Mascota` y filtrando por rango.
5. **Añadir restricciones únicas** en agenda y series, y transacciones en el reemplazo de disponibilidad.
6. **Incorporar autorización a nivel de recurso** para turnos de veterinarios.
7. **Externalizar el secreto JWT** y aplicar *rate limiting* a login y cambio de contraseña.
8. **Refactorizar servicios extensos** extrayendo colaboradores (seguimiento crónico, mapeo, cálculo de fechas) y reemplazar strings de estado por un tipo fuerte.
9. **Actualizar README y AGENTS.md** (conteo de pruebas y endpoint de documentación).

### 4.4 Dictamen

El proyecto demuestra **madurez arquitectónica y una cultura de pruebas destacable**. La brecha principal no es de diseño estructural sino de **cierre funcional de flujos secundarios y consistencia transaccional**. Atendiendo los nueve puntos anteriores —especialmente los tres primeros— el sistema puede considerarse listo para un despliegue controlado con garantías razonables de calidad e integridad clínica.

> El detalle accionable de cada hallazgo, con archivo, línea/módulo y corrección propuesta, se encuentra en `issues_github_pendientes.md`.
