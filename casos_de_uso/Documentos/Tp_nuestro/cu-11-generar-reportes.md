# Caso de Uso: Generar Reportes

> Especificación elaborada siguiendo la guía `GUIA-Especificacion-Casos-de-Uso.md` (sección 3).
> Implementación del motor de generación de reportes con reglas RN-01 (autorización por rol exclusivo de Administrador), RN-02 (coherencia de rango temporal), RN-03 (parámetros de consulta obligatorios) y RN-04 (persistencia automática en el histórico de reportes).

| Campo | Valor |
| --- | --- |
| **ID del Caso de Uso** | CU-11 |
| **Nombre** | Generar Reportes |
| **Actor Principal** | Dueño de la Veterinaria (Administrador) |
| **Alcance / Nivel** | Sistema; meta de usuario |
| **Stakeholders e intereses** | Dueño de la Veterinaria → obtener métricas agregadas de atenciones, vacunas, turnos, altas de pacientes y dueños en períodos determinados; Clínica Veterinaria → auditar el desempeño clínico y comercial y guardar registro histórico |
| **Disparador (Trigger)** | El administrador selecciona la opción "Generar Reporte" desde la sección de reportes, completa los parámetros y solicita su emisión |
| **Prioridad / Frecuencia** | Media; baja/media frecuencia (generación periódica bajo demanda) |
| **Reglas de negocio relacionadas** | RN-01 (autorización restringida a rol de Administrador / Dueño); RN-02 (rango de fechas válido: fechaDesde <= fechaHasta); RN-03 (selección obligatoria de al menos un tipo de reporte y motivo); RN-04 (persistencia de cada reporte generado en la base de datos) |

---

### 1. BREVE DESCRIPCIÓN
Permite al administrador o dueño de la veterinaria generar nuevos reportes estadísticos y operacionales del sistema para consolidar información sobre atenciones médicas realizadas, vacunas aplicadas, mascotas registradas, dueños dados de alta y turnos en un período específico, registrando el resultado en el historial de reportes.

### 2. PRECONDICIONES
- El actor debe haber iniciado sesión y poseer un Token JWT con claim de rol `Administrador` o `DuenoVeterinaria` (**RN-01**).
- Debe existir información registrada en los módulos consultados dentro del período seleccionado.
- La Capa de Persistencia debe estar operativa para consolidar y almacenar el reporte.

### 3. FLUJO PRINCIPAL (Camino Feliz - HTTP 201)
1. El Actor envía una petición al endpoint `POST /api/reportes/generar` con un cuerpo JSON que contiene el `motivo`, la lista de `tiposReporte[]` (`Atenciones`, `Vacunaciones`, `Mascotas`, `Duenos`, `Turnos`), `fechaDesde` y `fechaHasta`.
2. La **Capa de Presentación** (`ReportesController.GenerarReporte`) valida la autorización del rol (`[Authorize(Roles = "Administrador,DuenoVeterinaria")]`) y valida que los campos requeridos estén presentes en `GenerarReporteRequestDTO` (**RN-03**).
3. La **Capa de Negocio** (`ReporteService.GenerarReporteAsync`) valida que la `fechaDesde` sea menor o igual a la `fechaHasta` (**RN-02**), ejecuta las consultas de agregación y cálculo estadístico sobre los repositorios correspondientes, y estructura el resultado.
4. La **Capa de Persistencia** guarda el documento consolidado en la tabla `Reportes` con su fecha de generación y usuario solicitante (**RN-04**).
5. El Sistema devuelve un código **201 Created** con el reporte generado (`ReporteResponseDTO`), su identificador y la URL para su posterior consulta o exportación.

### 4. FLUJOS ALTERNATIVOS (Caminos Tristes / Excepciones)

* **1a. Usuario no autenticado (HTTP 401 Unauthorized):**
  1. Si en el Paso 1 la petición no posee encabezado `Authorization: Bearer <token>`.
  2. El middleware de autenticación rechaza la petición.
  3. El Sistema devuelve un código **401 Unauthorized**. Fin del caso de uso.

* **2a. Usuario sin permisos de Administrador (HTTP 403 Forbidden):**
  1. Si en el Paso 2 el usuario autenticado pertenece al rol `Recepcionista` o `Veterinario`, violando la regla **RN-01**.
  2. La Capa de Presentación rechaza el acceso por falta de autorización.
  3. El Sistema devuelve un código **403 Forbidden**. Fin del caso de uso.

* **2b. Campos obligatorios incompletos (HTTP 400 Bad Request):**
  1. Si en el Paso 2 falta el `motivo` o la lista de `tiposReporte` se encuentra vacía, violando la regla **RN-03**.
  2. La Capa de Presentación detecta el error de validación (`ModelState.IsValid == false`).
  3. El Sistema devuelve un código **400 Bad Request** con el mensaje: `"Debe especificar el motivo y al menos un tipo de reporte a generar."`. Fin del caso de uso.

* **3a. Rango de fechas incoherente (HTTP 400 Bad Request):**
  1. Si en el Paso 3 la `fechaDesde` es posterior a la `fechaHasta` (`fechaDesde > fechaHasta`), violando la regla **RN-02**.
  2. La Capa de Negocio lanza la excepción `RangoFechasInvalidoException`.
  3. El Sistema devuelve un código **400 Bad Request** indicando: `"La fecha inicial no puede ser posterior a la fecha final."`. Fin del caso de uso.

* **4a. Error de generación o guardado (HTTP 500 Internal Server Error):**
  1. Si en el Paso 4 ocurre un error imprevisto al procesar los datos o persistir en la base de datos.
  2. El middleware global intercepta el error.
  3. El Sistema devuelve un código **500 Internal Server Error**. Fin del caso de uso.

### 5. SUB-VARIACIONES (opcional)
1. **Reporte individual por módulo:** Generación exclusiva de estadísticas de vacunaciones o atenciones.
2. **Reporte consolidado integral:** Generación con todos los tipos de reportes seleccionados simultáneamente.
3. **Selección de formato de exportación:** Generación con salida JSON para la web o formato PDF/Excel para descarga directa.

### 6. POSTCONDICIONES
- Se crea y persiste un nuevo registro en la tabla `Reportes` con sus métricas calculadas (**RN-04**).
- El reporte queda disponible de forma permanente en el historial de reportes para futuras consultas (**CU-10**).

---

## Anexo: matrices de referencia

### Códigos HTTP usados

| Código HTTP | Nombre Técnico | Contexto de Aplicación en el Caso de Uso |
| --- | --- | --- |
| `201` | Created | Creación y persistencia exitosa del nuevo reporte estadístico. |
| `400` | Bad Request | Parámetros obligatorios ausentes o rango de fechas incoherente (RN-02, RN-03). |
| `401` | Unauthorized | Falta de token de autenticación válido en la petición. |
| `403` | Forbidden | Acceso no autorizado: rol insuficiente (RN-01). |
| `500` | Internal Server Error | Falla técnica no controlada durante el cálculo o guardado del reporte. |

### Nota: Validación vs. Verificación aplicada

- **Validación (Presentación, → 400/401/403):** Control de acceso por rol `[Authorize(Roles = "Administrador,DuenoVeterinaria")]` (**RN-01**) y validación de campos obligatorios mediante `[Required]`, `[MinLength(1)]` sobre `tiposReporte` en `GenerarReporteRequestDTO` (**RN-03**).
- **Verificación (Negocio, → 400):** Verificación lógica del rango temporal (`fechaDesde <= fechaHasta`) (**RN-02**) y compilación estadística de los registros en los repositorios de dominio.

### Matriz de trazabilidad CU-11 → Test

| Paso del CU | Excepción / Código | Test unitario (BusinessLogic) | Test integración (HTTP) |
| --- | --- | --- | --- |
| Flujo principal | `201 Created` | `GenerarReporteAsync_WithValidParameters_GeneratesAndSavesReporte` | `GenerarReporte_WithValidData_Returns201Created` |
| 1a. Sin token | `401 Unauthorized` | — (middleware de autenticación) | `GenerarReporte_WithoutToken_Returns401Unauthorized` |
| 2a. Rol insuficiente | `403 Forbidden` | — (filtro de autorización por rol) | `GenerarReporte_AsVeterinario_Returns403Forbidden` |
| 2b. Tipos vacíos | `400 Bad Request` | — (validación DataAnnotations) | `GenerarReporte_WithEmptyTipos_Returns400BadRequest` |
| 3a. Fechas invertidas | `400 Bad Request` | `GenerarReporteAsync_WhenFechaDesdeAfterFechaHasta_ThrowsRangoFechasInvalidoException` | `GenerarReporte_WhenInvalidDateRange_Returns400BadRequest` |

> Regla de oro: cada flujo del caso de uso debe tener al menos un test. En los flujos resueltos en la Capa de Presentación el test aplicable es el de integración HTTP. Los tests se ejecutan con `dotnet test SistemaVeterinaria.slnx`.
