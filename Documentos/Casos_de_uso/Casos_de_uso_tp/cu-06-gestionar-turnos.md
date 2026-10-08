# Caso de Uso: Gestionar Turnos

> Especificación elaborada siguiendo la guía `GUIA-Especificacion-Casos-de-Uso.md` (sección 3).
> Implementación del ciclo de vida de los turnos con reglas de negocio RN-06 (asociación a dueño registrado), RN-07 (restricción en mascotas inactivas), RN-08 (no solapamiento de agenda por veterinario) y RN-09 (inmutabilidad de turnos finalizados).
> Incorpora el procedimiento de cancelación con motivo obligatorio, registro de fecha/hora, usuario responsable y liberación de horario (INF-05).

| Campo | Valor |
| --- | --- |
| **ID del Caso de Uso** | CU-06 |
| **Nombre** | Gestionar Turnos |
| **Actor Principal** | Veterinario/a o Recepcionista |
| **Actores Secundarios** | Ninguno |
| **Alcance / Nivel** | Sistema; meta de usuario |
| **Stakeholders e intereses** | Recepcionista / Veterinario → coordinar, registrar, reprogramar y cancelar turnos de manera fluida, con motivo de cancelación y sin conflictos de agenda; Dueño de la Mascota → asegurar una cita confirmada para su animal en el día y horario pactado; Clínica Veterinaria → optimizar la ocupación de consultorios, auditar cancelaciones y resguardar el historial |
| **Disparador (Trigger)** | El usuario selecciona la opción "Turnos" desde el menú principal y elige registrar, reprogramar o cancelar una cita |
| **Prioridad / Frecuencia** | Alta; muy alta frecuencia diaria |
| **Reglas de negocio relacionadas** | RN-06 (asociación de mascota a dueño registrado); RN-07 (mascotas inactivas no pueden recibir nuevos turnos); RN-08 (no solapamiento de turnos para un mismo veterinario); RN-09 (inmutabilidad de turnos finalizados) |

---

### 1. BREVE DESCRIPCIÓN
Permite a la recepcionista o al veterinario registrar un nuevo turno, reprogramar la fecha/hora de una cita existente o cancelar un turno programado. Para la cancelación de un turno se exige el ingreso obligatorio de un motivo y confirmación previa; el sistema registra automáticamente la fecha y hora de cancelación y la identidad del usuario responsable, cambiando el estado del turno a Cancelado y conservándolo en el historial sin eliminación física ni posibilidad de reactivación. La cancelación exitosa libera el horario en la agenda del profesional para permitir nuevas reservas (RN-08), impidiendo la cancelación de turnos que ya se encuentren finalizados (RN-09).

### 2. PRECONDICIONES
- El actor debe contar con una sesión activa y un Token JWT con rol de `Recepcionista` o `Veterinario`.
- Para registrar un nuevo turno, la mascota debe estar registrada en el sistema, encontrarse en estado **Activa** (**RN-07**) y estar asociada a un dueño registrado (**RN-06**).
- El veterinario asignado debe encontrarse registrado y habilitado en la clínica.
- Para cancelar o modificar un turno, este debe existir en el sistema y no encontrarse en estado **Finalizado** (**RN-09**).

### 3. FLUJO PRINCIPAL (Camino Feliz)

#### 3.1 Alta de turno
1. El usuario accede a la sección Gestión de Turnos y solicita registrar una nueva cita, ingresando los datos correspondientes (`mascotaId`, `veterinarioId`, `fechaHora`, `motivoConsulta`).
2. El sistema valida la presencia de los campos requeridos y que la fecha/hora programada no pertenezca al pasado.
3. El sistema verifica que la mascota exista, se encuentre activa (**RN-07**) y esté vinculada a un dueño (**RN-06**); que el veterinario exista y esté habilitado; y comprueba la disponibilidad horaria verificando que el profesional no posea otro turno activo asignado en ese horario (**RN-08**).
4. El sistema registra el nuevo turno en la base de datos con estado `"Confirmado"`.
5. El sistema confirma la creación exitosa del turno y presenta sus datos completos.

#### 3.2 Cancelar turno
1. La Recepcionista o el Veterinario accede a la sección Gestión de Turnos.
2. Busca y selecciona el turno que desea cancelar.
3. El sistema muestra la información del turno: mascota, dueño asociado, veterinario, fecha, horario y estado actual.
4. El usuario selecciona la opción **Cancelar turno**.
5. El sistema verifica que el turno pueda cancelarse según su estado y las reglas vigentes.
6. El sistema solicita ingresar el motivo de cancelación.
7. El usuario ingresa el motivo.
8. El usuario solicita continuar con la cancelación.
9. El sistema muestra una confirmación que identifica el turno y advierte que su horario volverá a estar disponible.
10. El usuario confirma la operación.
11. El sistema cambia el estado del turno a **Cancelado**.
12. El sistema registra automáticamente la fecha y hora de cancelación y el usuario responsable.
13. El sistema conserva los datos originales del turno y su motivo de cancelación.
14. El horario queda nuevamente disponible para registrar un turno nuevo, respetando las reglas de disponibilidad.
15. El sistema informa que la cancelación se realizó correctamente.

### 4. FLUJOS ALTERNATIVOS (Caminos Tristes / Excepciones)

* **1a. JSON inválido o malformado (HTTP 400 Bad Request):**
  1. Si en el envío de una petición el cuerpo contiene formato JSON corrupto o tipos de datos incompatibles.
  2. La Capa de Presentación rechaza la petición.
  3. El Sistema devuelve un código **400 Bad Request**. Fin del caso de uso.

* **2a. Datos obligatorios faltantes o fecha en el pasado (HTTP 400 Bad Request):**
  1. Durante el alta o modificación, faltan campos requeridos o la fecha programada es anterior a la fecha y hora actual del sistema.
  2. El sistema detecta el error de validación.
  3. El sistema informa el campo inválido y solicita su corrección. Fin del caso de uso.

* **3a. Horario no disponible / Solapamiento de turno (HTTP 409 Conflict):**
  1. Si durante el alta o reprogramación la verificación de agenda detecta que el veterinario ya tiene un turno reservado para ese mismo horario, violando la regla **RN-08**.
  2. El sistema frena la operación e informa que el veterinario seleccionado ya posee un turno en dicho horario.
  3. No se registra el turno. Fin del caso de uso.

* **3b. Mascota o Dueño no registrado (HTTP 404 Not Found):**
  1. Si el `mascotaId` no existe en la base de datos o no se encuentra asociado a un dueño registrado (**RN-06**).
  2. El sistema informa que la mascota o el dueño especificado no existe. Fin del caso de uso.

* **3c. Veterinario inexistente o inactivo (HTTP 404 Not Found):**
  1. Si el `veterinarioId` no corresponde a un profesional registrado y habilitado.
  2. El sistema informa que el profesional veterinario no se encuentra disponible. Fin del caso de uso.

* **3d. Modificar un turno ya finalizado (HTTP 409 Conflict):**
  1. Si durante una operación de modificación de fecha/hora o profesional el turno se encuentra en estado `"Finalizado"`, violando la regla **RN-09**.
  2. El sistema frena la operación e informa: `"No es posible modificar un turno finalizado."`. Fin del caso de uso.

* **3e. Intento de asignar turno a mascota inactiva (HTTP 409 Conflict):**
  1. Si en el alta o reprogramación la mascota seleccionada se encuentra en estado `"Inactiva"`, violando la regla **RN-07**.
  2. El sistema frena la operación e informa: `"No es posible asignar un nuevo turno a una mascota inactiva."`. Fin del caso de uso.

* **4a. Error de persistencia en base de datos (HTTP 500 Internal Server Error):**
  1. Si ocurre un fallo imprevisto al intentar persistir los datos de alta o reprogramación en la base de datos.
  2. El sistema captura el error y devuelve un código **500 Internal Server Error**. Fin del caso de uso.

* **A1 — Motivo de cancelación vacío:**
  1. En el Paso 7 u 8 del flujo de cancelación, el usuario intenta continuar sin ingresar un motivo o ingresa únicamente espacios en blanco.
  2. El sistema muestra el mensaje: `"Debe ingresar un motivo de cancelación"`.
  3. El sistema impide completar la operación.
  4. El turno conserva su estado original.
  5. El horario continúa ocupado en la agenda.
  6. El usuario puede ingresar un motivo válido o abandonar la operación.

* **A2 — Cancelación no confirmada:**
  1. En el Paso 10 del flujo de cancelación, el usuario decide no confirmar la operación (cancela la acción o cierra el diálogo de confirmación).
  2. El sistema conserva el turno sin modificaciones en su estado original.
  3. No se registra una cancelación.
  4. El horario continúa ocupado en la agenda. Fin del caso de uso.

* **A3 — Turno finalizado:**
  1. En el Paso 5 del flujo de cancelación, el sistema detecta que el usuario intenta cancelar un turno que se encuentra en estado Finalizado.
  2. El sistema informa que los turnos finalizados no pueden cancelarse, respetando la regla **RN-09**.
  3. No se permite realizar la operación. Fin del caso de uso.

* **A4 — Turno previamente cancelado:**
  1. En el Paso 5 del flujo de cancelación, el usuario selecciona un turno que ya se encuentra en estado Cancelado.
  2. El sistema informa que el turno ya fue cancelado previamente.
  3. No permite repetir la cancelación ni reactivar el turno.
  4. El registro histórico permanece intacto sin alteraciones. Fin del caso de uso.

* **A5 — Error durante la cancelación:**
  1. En el Paso 11 o 12 del flujo de cancelación, ocurre un error imprevisto del sistema que impide registrar correctamente la cancelación o los metadatos asociados.
  2. El sistema informa que la operación no pudo completarse.
  3. El turno conserva su estado anterior.
  4. El horario no se libera.
  5. No se informa falsamente que la cancelación fue exitosa.
  6. El usuario podrá reintentar la operación.

### 5. SUB-VARIACIONES (opcional)
1. **Creación / Alta de turno:** Registra una nueva cita médica para una mascota activa asociada a su dueño en estado Confirmado.
2. **Reprogramación de turno:** Actualiza la fecha/hora o profesional del turno siempre que este no se encuentre Finalizado (**RN-09**) ni Cancelado, validando la disponibilidad de agenda (**RN-08**).
3. **Cancelación de turno:** Procedimiento detallado en el Flujo Principal 3.2, mediante el cual el turno pasa al estado `"Cancelado"` con motivo obligatorio, registro de fecha/hora y usuario responsable, liberando el horario en la agenda para nuevas asignaciones y preservando el registro histórico sin eliminación física ni posibilidad de reactivación.

### 6. POSTCONDICIONES
- **Alta de turno exitosa:** Se crea el registro en la tabla `Turnos` con estado Confirmado y el horario queda ocupado en la agenda del profesional (**RN-08**).
- **Modificación exitosa:** Se actualizan la fecha/hora o profesional del turno, liberando el horario anterior y ocupando el nuevo horario validado.
- **Cancelación de turno exitosa (Flujo 3.2):**
  - El turno cambia su estado a **Cancelado** de forma irreversible (no puede reactivarse).
  - El turno cancelado permanece íntegramente registrado en la base de datos como registro histórico (no se elimina físicamente), almacenando el motivo obligatorio de cancelación, la fecha y hora exacta de la cancelación y el usuario responsable.
  - El horario que ocupaba el turno cancelado queda liberado en el control de disponibilidad de la agenda, permitiendo registrar una nueva cita para cualquier mascota con el mismo veterinario (**RN-08**).
  - El nuevo turno que reserve ese horario tendrá su propia identidad y registro independiente; la existencia previa del turno cancelado no produce conflicto ni falsa superposición horaria (**RN-08**).
  - Las atenciones médicas y la Historia Clínica de la mascota permanecen inalteradas (**RN-01**).
- **Operación cancelada, rechazada o con error (Flujos A1 a A5):**
  - El turno conserva su estado original sin cambios.
  - El horario permanece ocupado en la agenda.
  - No se generan registros históricos de cancelación.

---

## Anexo: matrices de referencia

### Códigos HTTP usados

| Código HTTP | Nombre Técnico | Contexto de Aplicación en el Caso de Uso |
| --- | --- | --- |
| `201` | Created | Registro exitoso del nuevo turno médico. |
| `200` | OK | Confirmación de modificación o cancelación exitosa del turno. |
| `400` | Bad Request | Parámetros incompletos, fecha en el pasado, motivo de cancelación vacío o JSON inválido. |
| `404` | Not Found | Mascota, dueño o veterinario inexistente en el sistema (RN-06), o turno no encontrado. |
| `409` | Conflict | Solapamiento de horario (RN-08), alteración o cancelación de turno finalizado (RN-09), cancelación de turno ya cancelado o asignación a mascota inactiva (RN-07). |
| `500` | Internal Server Error | Falla no controlada de persistencia o comunicación en la base de datos. |

### Consideraciones sobre Liberación de Horario, Irreversibilidad y Trazabilidad

1. **Liberación de horario y compatibilidad con RN-08:**
   - La regla **RN-08** prohíbe la superposición de turnos para un mismo profesional.
   - El control de disponibilidad horaria evalúa exclusivamente aquellos turnos que mantengan una reserva vigente del horario (estados activos tales como `Pendiente`, `Confirmado` o `EnAtencion`).
   - Al cancelarse un turno, este deja de computar como ocupado en el control de disponibilidad; por lo tanto, el horario queda disponible de inmediato para ser reservado por otro turno con el mismo veterinario.
   - Todo nuevo turno asignado en ese horario contará con su propio identificador y registro independiente; bajo ninguna circunstancia se sobrescribe ni se reutiliza el registro del turno cancelado.
2. **Irreversibilidad de la cancelación:**
   - El estado `Cancelado` es un estado terminal e irreversible.
   - El sistema no permite reactivar un turno cancelado ni pasarlo a ningún otro estado operativo.
   - Si el dueño requiere una nueva cita para la misma mascota, deberá registrarse un nuevo turno desde cero, sujeto a disponibilidad de agenda.
3. **Información histórica y reportes:**
   - Todo turno cancelado conserva en su registro: ID del turno, mascota asociada, dueño vinculado, veterinario asignado, fecha y horario originales, estado `Cancelado`, motivo obligatorio de cancelación, fecha y hora de cancelación, y usuario responsable.
   - Estos registros históricos quedan disponibles permanentemente para su consulta en la agenda (**CU-05**) y en los reportes operativos del sistema (**CU-11**).

### Nota: Validación vs. Verificación aplicada

- **Validación (Presentación, → 400 / UI):** Presencia de identificadores requeridos, formato ISO de fecha/hora, restricción de fechas en el pasado, ingreso obligatorio del motivo de cancelación no vacío (`"Debe ingresar un motivo de cancelación"`) y confirmación del usuario antes de proceder.
- **Verificación (Negocio, → 404/409):** Validación de existencia de mascota y dueño (**RN-06**), comprobación de estado activo de la mascota (**RN-07**), verificación de no solapamiento en la agenda del profesional (**RN-08**), comprobación de que el turno a cancelar o modificar no esté en estado Finalizado (**RN-09**) y verificación de que no haya sido cancelado con anterioridad.

### Matriz de trazabilidad CU-06 → Test

| Paso del CU / Escenario | Excepción / Código | Test unitario (BusinessLogic) | Test integración (HTTP / UI) |
| --- | --- | --- | --- |
| Flujo principal — Alta de turno | `201 Created` | `CreateTurnoAsync_WithValidData_SavesAndReturnsTurnoDTO` | `CreateTurno_WithValidData_Returns201Created` |
| Flujo principal — Cancelar turno | `200 OK` | `CancelTurnoAsync_WithValidMotivo_SetsCancelledAndLogsAudit` | `CancelTurno_WithValidMotivo_Returns200OK` |
| 1a. JSON inválido | `400 Bad Request` | — (model binding en Presentación) | `CreateTurno_WithMalformedPayload_Returns400BadRequest` |
| 2a. Fecha en el pasado | `400 Bad Request` | — (validación DataAnnotations) | `CreateTurno_WithPastDate_Returns400BadRequest` |
| 3a. Solapamiento de horario | `409 Conflict` | `CreateTurnoAsync_WithOverlappingSchedule_ThrowsHorarioNoDisponibleException` | `CreateTurno_WhenScheduleOverlaps_Returns409Conflict` |
| 3b. Mascota inexistente | `404 Not Found` | `CreateTurnoAsync_WhenMascotaNotFound_ThrowsMascotaNotFoundException` | `CreateTurno_WhenMascotaNotFound_Returns404NotFound` |
| 3d. Modificar turno finalizado | `409 Conflict` | `UpdateTurnoAsync_WhenTurnoIsFinalizado_ThrowsTurnoFinalizadoException` | `UpdateTurno_WhenTurnoIsFinalizado_Returns409Conflict` |
| 3e. Mascota inactiva | `409 Conflict` | `CreateTurnoAsync_WhenMascotaIsInactive_ThrowsMascotaInactivaException` | `CreateTurno_WhenMascotaIsInactive_Returns409Conflict` |
| 4a. Error de persistencia en alta | `500 Internal Server Error` | `CreateTurnoAsync_WhenDbFails_ThrowsException` | `CreateTurno_WhenDbFails_Returns500InternalServerError` |
| A1. Motivo de cancelación vacío | `400 Bad Request` | `CancelTurnoAsync_WhenMotivoIsEmpty_ThrowsValidationException` | `CancelTurno_WhenMotivoIsEmpty_Returns400BadRequest` |
| A2. Cancelación no confirmada | N/A (UI Diálogo) | — (cancelación cancelada en interfaz de usuario) | `Turno_AlCancelarConfirmacion_ConservaEstadoYHorarioOcupado` |
| A3. Turno finalizado al cancelar | `409 Conflict` | `CancelTurnoAsync_WhenTurnoIsFinalizado_ThrowsTurnoFinalizadoException` | `CancelTurno_WhenTurnoIsFinalizado_Returns409Conflict` |
| A4. Turno previamente cancelado | `409 Conflict` | `CancelTurnoAsync_WhenTurnoAlreadyCancelled_ThrowsTurnoYaCanceladoException` | `CancelTurno_WhenAlreadyCancelled_Returns409Conflict` |
| A5. Error durante la cancelación | `500 Internal Server Error` | `CancelTurnoAsync_WhenDbFails_ThrowsExceptionAndRollbacks` | `CancelTurno_WhenDbFails_Returns500AndPreservesStatus` |
| Liberación de horario / Nueva reserva | `201 Created` | `CreateTurnoAsync_InCancelledSlot_SucceedsWithoutOverlapConflict` | `CreateTurno_InCancelledSlot_Returns201Created` |

> Regla de oro: cada flujo del caso de uso debe tener al menos un test. En los flujos resueltos en la Capa de Presentación el test aplicable es el de integración HTTP o prueba de interfaz de usuario. Los tests se ejecutan con `dotnet test SistemaVeterinaria.slnx`.

