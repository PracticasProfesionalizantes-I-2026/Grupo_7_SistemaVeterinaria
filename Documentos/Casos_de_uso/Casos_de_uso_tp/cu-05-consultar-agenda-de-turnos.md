# Caso de Uso: Consultar Agenda de Turnos

> Especificación elaborada siguiendo la guía `GUIA-Especificacion-Casos-de-Uso.md` (sección 3).
> Implementación del módulo de consulta y visualización de la agenda de atención profesional con reglas de visualización de turnos (**RN-01**) e idempotencia de consulta (**RN-02**).

| Campo | Valor |
| --- | --- |
| **ID del Caso de Uso** | CU-05 |
| **Nombre** | Consultar Agenda de Turnos |
| **Actor Principal** | Veterinario/a o Recepcionista |
| **Alcance / Nivel** | Sistema; meta de usuario |
| **Stakeholders e intereses** | Veterinario/a → consultar su cronograma de atenciones diarias y conocer los pacientes asignados; Recepcionista → verificar disponibilidad horaria de consultorios y médicos para coordinar nuevas citas; Administración → monitorear la ocupación y puntualidad del servicio |
| **Disparador (Trigger)** | El usuario selecciona la opción "Agenda" o "Turnos" desde el menú de navegación |
| **Prioridad / Frecuencia** | Alta; muy alta frecuencia diaria |
| **Reglas de negocio relacionadas** | RN-01 (la agenda expone exclusivamente turnos válidos registrados en el sistema); RN-02 (la operación de consulta es estrictamente de solo lectura y no altera ningún estado) |

---

### 1. BREVE DESCRIPCIÓN
Permite al veterinario o a la recepcionista consultar el listado y cronograma de turnos registrados en el sistema, visualizando la agenda por fecha, horario, mascota, dueño, motivo de consulta, estado del turno y profesional asignado, con capacidad de filtrado multicriterio.

### 2. PRECONDICIONES
- El usuario debe haber iniciado sesión y poseer un Token JWT válido con rol de `Recepcionista`, `Veterinario` o `Administrador`.
- Deben existir turnos y profesionales registrados en la Capa de Persistencia para visualizar información.
- La Capa de Persistencia debe encontrarse disponible.

### 3. FLUJO PRINCIPAL (Camino Feliz - HTTP 200)
1. El Actor envía una petición al endpoint `GET /api/turnos` especificando parámetros opcionales de filtrado en la URL (`fecha`, `veterinarioId`, `estado`, `mascotaId`).
2. La **Capa de Presentación** (`TurnosController.GetAgenda`) valida el formato de las fechas y la coherencia sintáctica de los parámetros de consulta (`AgendaFilterDTO`).
3. La **Capa de Negocio** (`TurnoService.GetAgendaAsync`) aplica los criterios de búsqueda sobre los turnos del sistema (**RN-01**), garantizando que no se modifique el estado de las citas (**RN-02**), e incluye la información del dueño, la mascota y el veterinario responsable.
4. La **Capa de Persistencia** ejecuta la consulta de lectura (`AsNoTracking()`) sobre la tabla `Turnos` resolviendo las relaciones necesarias.
5. El Sistema devuelve un código **200 OK** con la colección de turnos encontrados (`IEnumerable<TurnoAgendaResponseDTO>`).

### 4. FLUJOS ALTERNATIVOS (Caminos Tristes / Excepciones)

* **1a. Parámetros de fecha o filtros inválidos (HTTP 400 Bad Request):**
  1. Si en el Paso 1 el parámetro `fecha` no corresponde a una fecha válida (ej. `YYYY-MM-DD` mal formado) o el `estado` no coincide con los valores permitidos del enum (`Pendiente`, `Confirmado`, `EnAtencion`, `Finalizado`, `Cancelado`).
  2. La Capa de Presentación rechaza la petición por validación de parámetros.
  3. El Sistema devuelve un código **400 Bad Request**. Fin del caso de uso.

* **3a. No existen turnos para los criterios aplicados (HTTP 200 OK con colección vacía):**
  1. Si en el Paso 3 la búsqueda no arroja ningún turno registrado para la fecha o filtros seleccionados.
  2. La Capa de Negocio procesa la respuesta retornando una lista vacía `[]`.
  3. El Sistema devuelve un código **200 OK** con una lista vacía y un mensaje informativo indicando que no hay turnos programados.

* **3b. Veterinario consultado inexistente (HTTP 404 Not Found):**
  1. Si en el Paso 3 se incluye un `veterinarioId` específico que no existe en la base de datos.
  2. La Capa de Negocio lanza la excepción `VeterinarioNotFoundException`.
  3. El Sistema devuelve un código **404 Not Found** con el mensaje: `"El profesional veterinario especificado no existe."`. Fin del caso de uso.

* **4a. Error de acceso a datos (HTTP 500 Internal Server Error):**
  1. Si en el Paso 4 ocurre una falla en el servidor de base de datos.
  2. El middleware de manejo de errores captura la excepción.
  3. El Sistema devuelve un código **500 Internal Server Error**. Fin del caso de uso.

### 5. SUB-VARIACIONES (opcional)
1. **Vista diaria de agenda:** Consulta los turnos de la fecha actual por defecto.
2. **Vista por profesional:** El veterinario autenticado consulta únicamente su propia agenda de turnos.
3. **Filtro por estado de atención:** Permite visualizar exclusivamente turnos confirmados o turnos cancelados.

### 6. POSTCONDICIONES
- La agenda de turnos queda expuesta en la interfaz para su visualización y gestión.
- No se produce ningún cambio de estado en los turnos ni en las entidades vinculadas (**RN-02**).

---

## Anexo: matrices de referencia

### Códigos HTTP usados

| Código HTTP | Nombre Técnico | Contexto de Aplicación en el Caso de Uso |
| --- | --- | --- |
| `200` | OK | Retorno exitoso del listado o agenda de turnos filtrados. |
| `400` | Bad Request | Formato de fecha inválido o valor de filtro no reconocido. |
| `404` | Not Found | El veterinario especificado en el filtro de agenda no existe. |
| `500` | Internal Server Error | Error no controlado en la conexión con la base de datos. |

### Nota: Validación vs. Verificación aplicada

- **Validación (Presentación, → 400):** Comprobación de formato ISO en fechas y validación de tipos enum en los query parameters del controller.
- **Verificación (Negocio, → 404):** Validación de existencia del profesional en la base de datos y armado de proyecciones DTO de lectura sin tracking.

### Matriz de trazabilidad CU-05 → Test

| Paso del CU | Excepción / Código | Test unitario (BusinessLogic) | Test integración (HTTP) |
| --- | --- | --- | --- |
| Flujo principal | `200 OK` | `GetAgendaAsync_WithValidFilters_ReturnsTurnosListDTO` | `GetAgenda_WithDateFilter_Returns200OKAndTurnosList` |
| 1a. Fecha inválida | `400 Bad Request` | — (validación de binding de query params) | `GetAgenda_WithInvalidDateFormat_Returns400BadRequest` |
| 3a. Sin turnos | `200 OK` | `GetAgendaAsync_WhenNoTurnos_ReturnsEmptyCollection` | `GetAgenda_WhenNoTurnosExist_Returns200OKWithEmptyList` |
| 3b. Veterinario no existe | `404 Not Found` | `GetAgendaAsync_WhenVeterinarioNotExists_ThrowsVeterinarioNotFoundException` | `GetAgenda_WhenVeterinarioNotExists_Returns404NotFound` |

> Regla de oro: cada flujo del caso de uso debe tener al menos un test. En los flujos resueltos en la Capa de Presentación el test aplicable es el de integración HTTP. Los tests se ejecutan con `dotnet test SistemaVeterinaria.slnx`.
