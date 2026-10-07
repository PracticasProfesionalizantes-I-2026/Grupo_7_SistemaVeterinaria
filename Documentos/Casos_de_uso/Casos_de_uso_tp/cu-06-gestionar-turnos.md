# Caso de Uso: Gestionar Turnos

> Especificación elaborada siguiendo la guía `GUIA-Especificacion-Casos-de-Uso.md` (sección 3).
> Implementación del ciclo de vida de los turnos con reglas de negocio RN-06 (asociación a dueño registrado), RN-07 (restricción en mascotas inactivas), RN-08 (no solapamiento de agenda por veterinario) y RN-09 (inmutabilidad de turnos finalizados).

| Campo | Valor |
| --- | --- |
| **ID del Caso de Uso** | CU-06 |
| **Nombre** | Gestionar Turnos |
| **Actor Principal** | Veterinario/a o Recepcionista |
| **Alcance / Nivel** | Sistema; meta de usuario |
| **Stakeholders e intereses** | Recepcionista / Veterinario → coordinar, registrar, reprogramar y cancelar turnos de manera fluida y sin conflictos de agenda; Dueño de la Mascota → asegurar una cita confirmada para su animal en el día y horario pactado; Clínica Veterinaria → optimizar la ocupación de consultorios y resguardar el historial |
| **Disparador (Trigger)** | El usuario selecciona la opción "Turnos" desde el menú principal y elige registrar, reprogramar o cancelar una cita |
| **Prioridad / Frecuencia** | Alta; muy alta frecuencia diaria |
| **Reglas de negocio relacionadas** | RN-06 (asociación de mascota a dueño registrado); RN-07 (mascotas inactivas no pueden recibir nuevos turnos); RN-08 (no solapamiento de turnos para un mismo veterinario); RN-09 (inmutabilidad de turnos finalizados) |

---

### 1. BREVE DESCRIPCIÓN
Permite a la recepcionista o al veterinario registrar un nuevo turno, reprogramar la fecha/hora de una cita existente o cancelar un turno programado, validando la disponibilidad de agenda del profesional, el estado activo del paciente y la integridad de las entidades involucradas.

### 2. PRECONDICIONES
- El actor debe contar con una sesión activa y un Token JWT con permisos correspondientes.
- La mascota debe estar registrada en el sistema, encontrarse en estado **Activa** (**RN-07**) y estar asociada a un dueño registrado (**RN-06**).
- El veterinario asignado debe encontrarse registrado y habilitado en la clínica.

### 3. FLUJO PRINCIPAL (Camino Feliz - HTTP 201)
1. El Actor envía una petición al endpoint `POST /api/turnos` con un cuerpo JSON que incluye `mascotaId`, `veterinarioId`, `fechaHora` y `motivoConsulta`.
2. La **Capa de Presentación** (`TurnosController.CreateTurno`) valida la estructura del payload y los atributos de validación (`TurnoCreateDTO`).
3. La **Capa de Negocio** (`TurnoService.CreateTurnoAsync`) verifica la existencia de la mascota y su dueño (**RN-06**), comprueba que la mascota se encuentre en estado **Activa** (**RN-07**), corrobora la existencia del veterinario, y comprueba que el profesional no tenga otro turno asignado en el mismo rango horario aplicando la regla **RN-08**.
4. La **Capa de Persistencia** guarda el nuevo turno en la tabla `Turnos` con estado `"Confirmado"`.
5. El Sistema devuelve un código **201 Created** con los datos completos del turno creado (`TurnoResponseDTO`) y la cabecera `Location`.

### 4. FLUJOS ALTERNATIVOS (Caminos Tristes / Excepciones)

* **1a. JSON inválido o malformado (HTTP 400 Bad Request):**
  1. Si en el Paso 1 el cuerpo de la petición contiene formato JSON corrupto o tipos de datos incompatibles.
  2. La Capa de Presentación rechaza la petición.
  3. El Sistema devuelve un código **400 Bad Request**. Fin del caso de uso.

* **2a. Datos obligatorios faltantes o fecha en el pasado (HTTP 400 Bad Request):**
  1. Si en el Paso 2 faltan campos (`mascotaId`, `veterinarioId`, `fechaHora`) o si la fecha programada es anterior a la fecha y hora actual del sistema.
  2. La Capa de Presentación detecta el error de validación de modelo (`ModelState.IsValid == false`).
  3. El Sistema devuelve un código **400 Bad Request** con el detalle del campo inválido. Fin del caso de uso.

* **3a. Horario no disponible / Solapamiento de turno (HTTP 409 Conflict):**
  1. Si en el Paso 3 la verificación de agenda detecta que el veterinario ya tiene un turno reservado para ese mismo horario, violando la regla **RN-08**.
  2. La Capa de Negocio frena la operación y lanza la excepción `HorarioNoDisponibleException`.
  3. El Sistema devuelve un código **409 Conflict** con el mensaje: `"El veterinario seleccionado ya posee un turno en dicho horario."`. Fin del caso de uso.

* **3b. Mascota o Dueño no registrado (HTTP 404 Not Found):**
  1. Si en el Paso 3 el `mascotaId` no existe en la base de datos o no se encuentra asociado a un dueño registrado (**RN-06**).
  2. La Capa de Negocio lanza la excepción `MascotaNotFoundException`.
  3. El Sistema devuelve un código **404 Not Found**. Fin del caso de uso.

* **3c. Veterinario inexistente o inactivo (HTTP 404 Not Found):**
  1. Si en el Paso 3 el `veterinarioId` no corresponde a un profesional registrado y habilitado.
  2. La Capa de Negocio lanza `VeterinarioNotFoundException`.
  3. El Sistema devuelve un código **404 Not Found**. Fin del caso de uso.

* **3d. Cancelar o modificar un turno ya finalizado (HTTP 409 Conflict):**
  1. Si durante una operación de modificación (`PUT /api/turnos/{id}`) o cancelación (`PATCH /api/turnos/{id}/cancelar`) el turno se encuentra en estado `"Finalizado"`, violando la regla **RN-09**.
  2. La Capa de Negocio lanza la excepción `TurnoFinalizadoException`.
  3. El Sistema devuelve un código **409 Conflict** indicando: `"No es posible modificar ni cancelar un turno finalizado."`. Fin del caso de uso.

* **3e. Intento de asignar turno a mascota inactiva (HTTP 409 Conflict):**
  1. Si en el Paso 3 la mascota seleccionada se encuentra en estado `"Inactiva"`, violando la regla **RN-07**.
  2. La Capa de Negocio frena la operación y lanza la excepción `MascotaInactivaException`.
  3. El Sistema devuelve un código **409 Conflict** con el mensaje: `"No es posible asignar un nuevo turno a una mascota inactiva."`. Fin del caso de uso.

* **4a. Error de persistencia en base de datos (HTTP 500 Internal Server Error):**
  1. Si en el Paso 4 se produce un fallo imprevisto en la base de datos.
  2. El middleware global captura el error.
  3. El Sistema devuelve un código **500 Internal Server Error**. Fin del caso de uso.

### 5. SUB-VARIACIONES (opcional)
1. **Creación de turno:** Registra una nueva cita médica para una mascota activa (`POST /api/turnos`).
2. **Reprogramación de turno:** Actualiza la fecha/hora o profesional del turno (`PUT /api/turnos/{id}`).
3. **Cancelación de turno:** Cambia el estado a `"Cancelado"` liberando el espacio en la agenda (`PATCH /api/turnos/{id}/cancelar`).

### 6. POSTCONDICIONES
- Se crea, modifica o cancela el registro en la tabla `Turnos`.
- La agenda de atención del veterinario se actualiza en tiempo real reflejando la ocupación o liberación del horario.

---

## Anexo: matrices de referencia

### Códigos HTTP usados

| Código HTTP | Nombre Técnico | Contexto de Aplicación en el Caso de Uso |
| --- | --- | --- |
| `201` | Created | Registro exitoso del nuevo turno médico. |
| `200` | OK | Confirmación de modificación o cancelación del turno. |
| `400` | Bad Request | Parámetros incompletos, fecha en el pasado o JSON inválido. |
| `404` | Not Found | Mascota, dueño o veterinario inexistente en el sistema (RN-06). |
| `409` | Conflict | Solapamiento de horario (RN-08), alteración de turno finalizado (RN-09) o asignación a mascota inactiva (RN-07). |
| `500` | Internal Server Error | Falla no controlada de persistencia en la base de datos. |

### Nota: Validación vs. Verificación aplicada

- **Validación (Presentación, → 400):** Presencia de identificadores requeridos, formato ISO de fecha/hora, restricción de fechas en el pasado y longitud máxima del motivo de consulta en `TurnoCreateDTO`.
- **Verificación (Negocio, → 404/409):** Validación de existencia de mascota y dueño (**RN-06**), comprobación de estado activo de la mascota (**RN-07**), verificación de no solapamiento en la agenda del profesional (**RN-08**) y restricción de alteración de turnos finalizados (**RN-09**).

### Matriz de trazabilidad CU-06 → Test

| Paso del CU | Excepción / Código | Test unitario (BusinessLogic) | Test integración (HTTP) |
| --- | --- | --- | --- |
| Flujo principal | `201 Created` | `CreateTurnoAsync_WithValidData_SavesAndReturnsTurnoDTO` | `CreateTurno_WithValidData_Returns201Created` |
| 1a. JSON inválido | `400 Bad Request` | — (model binding en Presentación) | `CreateTurno_WithMalformedPayload_Returns400BadRequest` |
| 2a. Fecha en el pasado | `400 Bad Request` | — (validación DataAnnotations) | `CreateTurno_WithPastDate_Returns400BadRequest` |
| 3a. Solapamiento de horario | `409 Conflict` | `CreateTurnoAsync_WithOverlappingSchedule_ThrowsHorarioNoDisponibleException` | `CreateTurno_WhenScheduleOverlaps_Returns409Conflict` |
| 3b. Mascota inexistente | `404 Not Found` | `CreateTurnoAsync_WhenMascotaNotFound_ThrowsMascotaNotFoundException` | `CreateTurno_WhenMascotaNotFound_Returns404NotFound` |
| 3d. Turno finalizado | `409 Conflict` | `CancelTurnoAsync_WhenTurnoIsFinalizado_ThrowsTurnoFinalizadoException` | `CancelTurno_WhenTurnoIsFinalizado_Returns409Conflict` |
| 3e. Mascota inactiva | `409 Conflict` | `CreateTurnoAsync_WhenMascotaIsInactive_ThrowsMascotaInactivaException` | `CreateTurno_WhenMascotaIsInactive_Returns409Conflict` |

> Regla de oro: cada flujo del caso de uso debe tener al menos un test. En los flujos resueltos en la Capa de Presentación el test aplicable es el de integración HTTP. Los tests se ejecutan con `dotnet test SistemaVeterinaria.slnx`.
