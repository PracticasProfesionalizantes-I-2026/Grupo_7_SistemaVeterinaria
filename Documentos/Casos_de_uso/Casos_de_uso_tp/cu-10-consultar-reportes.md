# Caso de Uso: Consultar Reportes

> Especificación elaborada siguiendo la guía `GUIA-Especificacion-Casos-de-Uso.md` (sección 3).
> Implementación del módulo de auditoría y consulta de reportes con regla de autorización por rol exclusivo (**RN-01**) e inmutabilidad de solo lectura (**RN-02**).

| Campo | Valor |
| --- | --- |
| **ID del Caso de Uso** | CU-10 |
| **Nombre** | Consultar Reportes |
| **Actor Principal** | Dueño de la Veterinaria (Administrador) |
| **Alcance / Nivel** | Sistema; meta de usuario |
| **Stakeholders e intereses** | Dueño de la Veterinaria → acceder a los reportes consolidados de atenciones, vacunas, dueños, mascotas y turnos para tomar decisiones gerenciales; Clínica Veterinaria → garantizar el control de acceso a información estadística confidencial |
| **Disparador (Trigger)** | El administrador selecciona la opción "Reportes" desde el menú principal para consultar el histórico de reportes |
| **Prioridad / Frecuencia** | Media; baja/media frecuencia (consultas semanales o mensuales) |
| **Reglas de negocio relacionadas** | RN-01 (acceso restringido exclusivamente a usuarios con rol de Administrador / Dueño de Veterinaria); RN-02 (la consulta es de solo lectura y no altera la información almacenada) |

---

### 1. BREVE DESCRIPCIÓN
Permite al administrador o dueño de la veterinaria consultar los reportes generados recientemente para visualizar métricas e información consolidada sobre las atenciones médicas realizadas, vacunas aplicadas, mascotas registradas, dueños registrados y turnos del sistema.

### 2. PRECONDICIONES
- El actor debe haber iniciado sesión con un Token JWT válido que contenga el claim de rol `Administrador` o `DuenoVeterinaria` (**RN-01**).
- La Capa de Persistencia debe estar operativa y accesible.

### 3. FLUJO PRINCIPAL (Camino Feliz - HTTP 200)
1. El Actor envía una petición al endpoint `GET /api/reportes` (para el listado) o `GET /api/reportes/{id}` (para el detalle de un reporte específico) con el Token JWT en el encabezado `Authorization`.
2. La **Capa de Presentación** (`ReportesController.GetReporteById`) valida los permisos del rol mediante el atributo `[Authorize(Roles = "Administrador,DuenoVeterinaria")]` (**RN-01**) y comprueba el formato del parámetro de ruta.
3. La **Capa de Negocio** (`ReporteService.GetReporteByIdAsync`) recupera el reporte solicitado de la base de datos sin modificar su estado (**RN-02**).
4. La **Capa de Persistencia** ejecuta la consulta de solo lectura (`AsNoTracking()`) sobre la tabla `Reportes`.
5. El Sistema devuelve un código **200 OK** con los datos estructurados del reporte (`ReporteResponseDTO`).

### 4. FLUJOS ALTERNATIVOS (Caminos Tristes / Excepciones)

* **1a. Usuario no autenticado (HTTP 401 Unauthorized):**
  1. Si en el Paso 1 la petición no contiene un Token JWT o el mismo está vencido.
  2. El middleware de autenticación rechaza la petición.
  3. El Sistema devuelve un código **401 Unauthorized**. Fin del caso de uso.

* **2a. Usuario sin rol de Administrador / Dueño (HTTP 403 Forbidden):**
  1. Si en el Paso 2 el usuario autenticado tiene un rol distinto (ej. `Recepcionista` o `Veterinario`), violando la regla **RN-01**.
  2. La Capa de Presentación (filtro de autorización) deniega el acceso al recurso.
  3. El Sistema devuelve un código **403 Forbidden** con el mensaje: `"Acceso denegado: se requieren permisos de Administrador."`. Fin del caso de uso.

* **2b. Identificador de reporte con formato inválido (HTTP 400 Bad Request):**
  1. Si en el Paso 2 el parámetro `{id}` no cumple con el formato esperado.
  2. La Capa de Presentación rechaza la petición por error de ruta.
  3. El Sistema devuelve un código **400 Bad Request**. Fin del caso de uso.

* **3a. Reporte no encontrado (HTTP 404 Not Found):**
  1. Si en el Paso 3 el identificador proporcionado no corresponde a ningún reporte registrado.
  2. La Capa de Negocio lanza la excepción `ReporteNotFoundException`.
  3. El Sistema devuelve un código **404 Not Found** con el mensaje: `"El reporte solicitado no existe."`. Fin del caso de uso.

* **4a. Error de acceso a datos (HTTP 500 Internal Server Error):**
  1. Si en el Paso 4 se produce una falla imprevista en la base de datos.
  2. El middleware global captura el error.
  3. El Sistema devuelve un código **500 Internal Server Error**. Fin del caso de uso.

### 5. SUB-VARIACIONES (opcional)
1. Consulta del historial de reportes generados (`GET /api/reportes`).
2. Consulta de visualización de métricas en pantalla (`GET /api/reportes/{id}`).
3. Descarga del reporte en formato binario (PDF/Excel) mediante `GET /api/reportes/{id}/exportar?formato=pdf`.

### 6. POSTCONDICIONES
- La información del reporte queda visualizada en la interfaz del administrador.
- No se modifica ninguna información almacenada en el sistema (**RN-02**).

---

## Anexo: matrices de referencia

### Códigos HTTP usados

| Código HTTP | Nombre Técnico | Contexto de Aplicación en el Caso de Uso |
| --- | --- | --- |
| `200` | OK | Retorno exitoso de la lista o del contenido del reporte solicitado. |
| `400` | Bad Request | Formato de identificador de reporte inválido. |
| `401` | Unauthorized | Falta de autenticación o token expirado. |
| `403` | Forbidden | Acceso no autorizado: rol insuficiente (RN-01). |
| `404` | Not Found | El reporte solicitado no existe en la base de datos. |
| `500` | Internal Server Error | Falla no controlada al consultar la persistencia. |

### Nota: Validación vs. Verificación aplicada

- **Validación (Presentación, → 400/401/403):** Se valida el formato de la URL y se comprueba el rol de Administrador en el pipeline de autorización `[Authorize(Roles = "Administrador,DuenoVeterinaria")]` (**RN-01**).
- **Verificación (Negocio, → 404):** Validación de existencia del reporte en el repositorio y armado del DTO de solo lectura (**RN-02**).

### Matriz de trazabilidad CU-10 → Test

| Paso del CU | Excepción / Código | Test unitario (BusinessLogic) | Test integración (HTTP) |
| --- | --- | --- | --- |
| Flujo principal | `200 OK` | `GetReporteByIdAsync_WithValidId_ReturnsReporteDTO` | `GetReporteById_AsAdmin_Returns200OK` |
| 1a. Sin token | `401 Unauthorized` | — (middleware de autenticación) | `GetReportes_WithoutToken_Returns401Unauthorized` |
| 2a. Rol insuficiente | `403 Forbidden` | — (filtro de autorización por rol) | `GetReportes_AsRecepcionista_Returns403Forbidden` |
| 3a. Reporte inexistente | `404 Not Found` | `GetReporteByIdAsync_WhenNotExists_ThrowsReporteNotFoundException` | `GetReporteById_WhenNotExists_Returns404NotFound` |

> Regla de oro: cada flujo del caso de uso debe tener al menos un test. En los flujos resueltos en la Capa de Presentación el test aplicable es el de integración HTTP. Los tests se ejecutan con `dotnet test SistemaVeterinaria.slnx`.
