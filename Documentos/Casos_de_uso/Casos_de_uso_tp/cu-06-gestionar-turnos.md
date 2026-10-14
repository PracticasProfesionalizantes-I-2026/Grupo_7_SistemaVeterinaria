# Caso de Uso: Gestionar Turnos

> Especificación elaborada siguiendo la guía `GUIA-Especificacion-Casos-de-Uso.md` (sección 3).
> Implementación del ciclo de vida de los turnos con reglas de negocio RN-06 (asociación a dueño registrado), RN-07 (restricción en mascotas inactivas), RN-08 (no solapamiento de agenda por veterinario en bloques de 30 minutos), RN-09 (inmutabilidad de turnos finalizados), RN-10 (disponibilidad horaria configurable del profesional) y RN-11 (generación automática de turnos para seguimiento crónico).
> Incorpora el procedimiento de cancelación con motivo obligatorio, registro de fecha/hora, usuario responsable y liberación de horario (INF-05).
> Incorpora la configuración parametrizable de días y horarios de atención por veterinario (jornadas de mañana, tarde o ambas) con turnos de duración fija de 30 minutos.
> Incorpora el mecanismo de generación y almacenamiento automático de turnos de seguimiento para mascotas con diagnóstico de enfermedad crónica durante los próximos seis meses.

| Campo | Valor |
| --- | --- |
| **ID del Caso de Uso** | CU-06 |
| **Nombre** | Gestionar Turnos |
| **Actor Principal** | Veterinario/a o Recepcionista |
| **Actores Secundarios** | Ninguno |
| **Alcance / Nivel** | Sistema; meta de usuario |
| **Stakeholders e intereses** | Recepcionista / Veterinario → coordinar, registrar, reprogramar y cancelar turnos en bloques de 30 minutos sin conflictos de agenda, auditar cancelaciones con motivo y disponer de series automáticas para seguimiento crónico; Dueño de la Mascota → asegurar citas confirmadas en días y horarios pactados y contar con cronograma previsible para patologías crónicas; Clínica Veterinaria → optimizar la ocupación de consultorios, respetar las disponibilidades médicas configuradas y asegurar el seguimiento clínico |
| **Disparador (Trigger)** | El usuario selecciona la opción "Turnos" desde el menú principal para registrar, reprogramar o cancelar una cita manualmente, o el sistema invoca internamente el mecanismo de generación tras registrarse una atención médica con enfermedad crónica (CU-04) |
| **Prioridad / Frecuencia** | Alta; muy alta frecuencia diaria |
| **Reglas de negocio relacionadas** | RN-06 (asociación de mascota a dueño registrado); RN-07 (mascotas inactivas no pueden recibir nuevos turnos); RN-08 (no solapamiento de turnos para un mismo veterinario en bloques de 30 min); RN-09 (inmutabilidad de turnos finalizados); RN-10 (disponibilidad horaria configurable del profesional); RN-11 (generación automática de seguimiento crónico) |

---

### 1. BREVE DESCRIPCIÓN
Permite a la recepcionista o al veterinario registrar un nuevo turno manual en un bloque de 30 minutos, reprogramar la fecha/hora de una cita existente o cancelar un turno programado. Cada veterinario cuenta con su propia configuración parametrizable de días y horarios de atención (jornadas de mañana, tarde o ambas con sus respectivos horarios de inicio y fin), debiendo asignarse los turnos exclusivamente dentro de dicha disponibilidad (RN-10) y sin superposición horaria (RN-08). La cancelación exige un motivo obligatorio y confirmación previa; registra automáticamente la fecha/hora y usuario responsable, cambia el estado a Cancelado de manera irreversible y libera el bloque de 30 minutos para nuevas reservas, conservando el registro histórico para auditoría y reportes. Los turnos finalizados no pueden modificarse ni cancelarse (RN-09).

Asimismo, este caso de uso implementa el mecanismo de generación y almacenamiento automático de una serie de turnos de seguimiento con estado inicial `Pendiente` durante los próximos seis meses para mascotas que reciban un diagnóstico de enfermedad crónica en una atención médica (CU-04). La generación respeta la frecuencia indicada (quincenal, mensual, bimestral o trimestral) y la jornada preferida del dueño (mañana, tarde o indistinta), buscando disponibilidad en bloques de 30 minutos en la agenda del veterinario sin superposiciones y sin alternar automáticamente entre mañana y tarde ante una preferencia explícita. Cada turno de la serie generada cuenta con trazabilidad a la atención médica de origen y puede cancelarse o reprogramarse individualmente sin afectar al resto de la serie.

Quedan fuera del alcance de esta versión la gestión de feriados, vacaciones, licencias del personal y excepciones especiales de agenda.

### 2. PRECONDICIONES
- El actor debe contar con una sesión activa y un Token JWT con rol de `Recepcionista` o `Veterinario`.
- Para registrar un nuevo turno manual, la mascota debe estar registrada en el sistema, encontrarse en estado **Activa** (**RN-07**) y estar asociada a un dueño registrado (**RN-06**).
- El veterinario asignado debe encontrarse registrado, habilitado y contar con disponibilidad horaria configurada para el día y rango solicitado (**RN-10**).
- Para cancelar o reprogramar un turno, este debe existir en el sistema y no encontrarse en estado **Finalizado** (**RN-09**) ni previamente **Cancelado**.
- Para la invocación del mecanismo de generación automática por enfermedad crónica, debe existir una atención médica registrada (CU-04) con diagnóstico crónico y no deben existir turnos de seguimiento generados previamente para dicha atención.

### 3. FLUJO PRINCIPAL (Camino Feliz)

#### 3.1 Alta de turno manual
1. El usuario accede a la sección Gestión de Turnos y solicita registrar una nueva cita, ingresando: `mascotaId`, `veterinarioId`, `fechaHora`, `motivoConsulta`.
2. El sistema valida la presencia de los campos requeridos, que la fecha/hora no pertenezca al pasado y que corresponda a un bloque válido de 30 minutos.
3. El sistema verifica que la mascota exista, se encuentre activa (**RN-07**) y esté vinculada a un dueño (**RN-06**); que el veterinario exista y esté habilitado; que el horario solicitado se encuentre dentro de los días y rangos horarios configurados para el veterinario (**RN-10**); y comprueba que el profesional no posea otro turno activo asignado en ese bloque de 30 minutos (**RN-08**).
4. El sistema registra el nuevo turno en la base de datos con estado `"Confirmado"`, origen `"Manual"` (`EsAutomatico = false`).
5. El sistema confirma la creación exitosa del turno y presenta sus datos completos (código HTTP 201 Created).

#### 3.2 Cancelar turno (manual o de serie crónica)
1. La Recepcionista o el Veterinario accede a la sección Gestión de Turnos.
2. Busca y selecciona el turno que desea cancelar (sea este manual o perteneciente a una serie de seguimiento crónico).
3. El sistema muestra la información del turno: mascota, dueño asociado, veterinario, fecha, horario, origen y estado actual.
4. El usuario selecciona la opción **Cancelar turno**.
5. El sistema verifica que el turno pueda cancelarse según su estado (no finalizado según **RN-09** y no previamente cancelado).
6. El sistema solicita ingresar obligatoriamente el motivo de cancelación.
7. El usuario ingresa el motivo.
8. El usuario solicita continuar con la cancelación.
9. El sistema muestra una confirmación que identifica el turno y advierte que su horario volverá a estar disponible (aclarando que si pertenece a una serie crónica, los demás turnos de la serie no se verán afectados).
10. El usuario confirma la operación.
11. El sistema cambia el estado del turno a **Cancelado**.
12. El sistema registra automáticamente la fecha y hora de cancelación (UTC) y la identidad del usuario responsable extraída del token JWT.
13. El sistema conserva los datos originales del turno y su motivo de cancelación en la base de datos sin eliminación física.
14. El bloque de 30 minutos queda nuevamente disponible para registrar un nuevo turno en la agenda del profesional (**RN-08**).
15. El sistema informa que la cancelación se realizó correctamente (código HTTP 200 OK).

#### 3.3 Reprogramar turno
1. El usuario busca y selecciona un turno no finalizado (**RN-09**) ni cancelado.
2. Ingresa la nueva fecha y hora solicitada para el turno.
3. El sistema valida que la nueva fecha no pertenezca al pasado, que corresponda a un bloque de 30 minutos dentro de la disponibilidad configurada del veterinario (**RN-10**) y que el profesional no tenga otro turno activo en ese horario (**RN-08**).
4. El sistema actualiza la fecha/hora del turno, liberando el bloque horario anterior y reservando el nuevo bloque validado. Si el turno pertenecía a una serie crónica, la reprogramación es individual y no altera las fechas de los restantes turnos de la serie.
5. El sistema confirma la reprogramación exitosa (código HTTP 200 OK).

#### 3.4 Mecanismo de generación automática de turnos de seguimiento por enfermedad crónica
*(Invocado internamente por el sistema como consecuencia del registro de una atención médica en CU-04)*
1. El mecanismo recibe: `mascotaId`, `veterinarioId`, `atencionMedicaId`, `fechaAtencion`, `frecuencia` (`Quincenal`, `Mensual`, `Bimestral`, `Trimestral`) y `jornadaPreferida` (`Manana`, `Tarde`, `Indistinta`).
2. Verifica la idempotencia: comprueba que no existan turnos de seguimiento generados previamente para esa misma `atencionMedicaId` (**RN-11**).
3. Valida que la mascota exista y esté activa (**RN-07**) y que el veterinario posea configuración de días y horarios de atención (**RN-10**).
4. Calcula las fechas proyectadas a lo largo de un período de seis (6) meses desde la fecha de atención:
   - **Quincenal:** cada 14 días (aproximadamente 12 turnos).
   - **Mensual:** cada mes (aproximadamente 6 turnos).
   - **Bimestral:** cada 2 meses (aproximadamente 3 turnos).
   - **Trimestral:** cada 3 meses (aproximadamente 2 turnos).
5. Para cada fecha proyectada de la serie, ejecuta la búsqueda automática de disponibilidad:
   a. Evalúa si el veterinario atiende en dicha fecha según su configuración semanal (**RN-10**).
   b. Busca el primer bloque libre de 30 minutos dentro del rango horario de la jornada preferida del veterinario (mañana, tarde o cualquiera si es indistinta).
   c. Si no encuentra disponibilidad en esa fecha o el profesional no atiende ese día, avanza la búsqueda al siguiente día de atención habilitado para el profesional.
   d. Continúa la búsqueda respetando estrictamente la jornada seleccionada: **nunca cambia automáticamente de mañana a tarde ni de tarde a mañana cuando se indicó una preferencia específica**.
   e. Al encontrar un bloque libre de 30 minutos sin superposición (**RN-08**), genera el turno con estado `"Pendiente"`, origen `"SeguimientoCronico"`, `EsAutomatico = true`, asociando `mascotaId`, `veterinarioId`, `atencionMedicaId` y motivo `"Control y seguimiento de enfermedad crónica"`.
   f. Si no consigue disponibilidad dentro del período permitido respetando la jornada elegida, registra e informa la contingencia sin asignar turnos fuera de las condiciones aprobadas.
6. Persiste en la base de datos la serie de turnos generados vinculados a la atención médica en una operación atómica.
7. Retorna el conjunto de turnos generados con sus fechas y horarios asignados para ser reportados en la respuesta de la atención.

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

* **3f. Horario fuera de la disponibilidad configurada del veterinario (HTTP 409 Conflict):**
  1. Si durante el alta o reprogramación manual, el horario seleccionado no coincide con un bloque válido de 30 minutos o se encuentra fuera de los días y rangos horarios habilitados para el profesional (mañana/tarde), violando la regla **RN-10**.
  2. El sistema frena la operación e informa que el veterinario no atiende en el día u horario solicitado. Fin del caso de uso.

* **3g. Solicitud duplicada de turnos de seguimiento para la misma atención médica (HTTP 409 Conflict):**
  1. Si se invoca el mecanismo de generación automática para una `atencionMedicaId` que ya posee una serie de turnos de seguimiento previamente registrada en el sistema (**RN-11**).
  2. El sistema frena la generación e informa: `"Ya existen turnos de seguimiento generados para la atención médica indicada."`. Fin del caso de uso.

* **3h. Falta de disponibilidad para fecha de serie crónica dentro de la jornada preferida:**
  1. Si durante la búsqueda automática para una fecha de la serie no se encuentra ningún bloque disponible de 30 minutos en la jornada preferida (mañana o tarde) para esa fecha ni en los días de atención habilitados subsiguientes.
  2. El sistema no altera la preferencia indicada (no cambia de mañana a tarde ni viceversa) y no asigna un turno fuera de las condiciones aprobadas.
  3. El sistema registra e informa la contingencia indicando la fecha en la que no se pudo obtener cupo disponible para que el personal coordine manualmente. Fin del flujo.

* **4a. Error de persistencia en base de datos (HTTP 500 Internal Server Error):**
  1. Si ocurre un fallo imprevisto al intentar persistir los datos de alta, reprogramación o la serie automática en la base de datos.
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
1. **Creación / Alta de turno manual:** Registra una nueva cita médica para una mascota activa asociada a su dueño en estado Confirmado, en un bloque de 30 minutos dentro de la disponibilidad configurada del veterinario (**RN-08**, **RN-10**).
2. **Reprogramación de turno:** Actualiza la fecha/hora o profesional del turno siempre que este no se encuentre Finalizado (**RN-09**) ni Cancelado, validando la disponibilidad de agenda (**RN-08**, **RN-10**). Si el turno pertenece a una serie de seguimiento crónico, la reprogramación es estrictamente individual y no altera los restantes turnos de la serie.
3. **Cancelación de turno:** Procedimiento detallado en el Flujo Principal 3.2, mediante el cual el turno pasa al estado `"Cancelado"` con motivo obligatorio, registro de fecha/hora y usuario responsable, liberando el bloque de 30 minutos en la agenda para nuevas asignaciones y preservando el registro histórico sin eliminación física ni posibilidad de reactivación. Si el turno cancelado pertenece a una serie crónica, los restantes turnos de la serie permanecen vigentes.
4. **Generación automática de turnos para seguimiento crónico:** Mecanismo detallado en el Flujo Principal 3.4, mediante el cual se genera y almacena una serie de turnos con estado `Pendiente` a lo largo de seis meses para pacientes diagnosticados con patologías crónicas, respetando frecuencia, jornada preferida y agenda configurada (**RN-11**).

### 6. POSTCONDICIONES
- **Alta de turno manual exitosa:** Se crea el registro en la tabla `Turnos` con estado Confirmado, origen `Manual`, y el bloque de 30 minutos queda ocupado en la agenda del profesional (**RN-08**).
- **Reprogramación exitosa:** Se actualiza la fecha/hora o profesional del turno, liberando el bloque horario anterior y ocupando el nuevo bloque validado de 30 minutos.
- **Generación automática de seguimiento crónico exitosa (Flujo 3.4):**
  - Se crea la serie de turnos con estado `Pendiente`, origen `SeguimientoCronico` (`EsAutomatico = true`), vinculados a la `AtencionMedicaId` de origen (**RN-11**).
  - Cada turno ocupa un bloque de 30 minutos en las fechas y jornadas calculadas sin solapamientos (**RN-08**, **RN-10**).
  - Cada turno de la serie queda habilitado para ser reprogramado o cancelado de manera independiente.
- **Cancelación de turno exitosa (Flujo 3.2):**
  - El turno cambia su estado a **Cancelado** de forma irreversible (no puede reactivarse).
  - El turno cancelado permanece íntegramente registrado en la base de datos como registro histórico (no se elimina físicamente), almacenando el motivo obligatorio de cancelación, la fecha y hora exacta de la cancelación (UTC) y el usuario responsable.
  - El bloque de 30 minutos que ocupaba el turno cancelado queda liberado en el control de disponibilidad de la agenda, permitiendo registrar una nueva cita para cualquier mascota con el mismo veterinario (**RN-08**).
  - Si el turno pertenecía a una serie crónica, los demás turnos de la serie continúan programados y activos.
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
| `201` | Created | Registro exitoso del nuevo turno médico manual. |
| `200` | OK | Confirmación de modificación, reprogramación o cancelación exitosa del turno. |
| `400` | Bad Request | Parámetros incompletos, fecha en el pasado, intervalo distinto a 30 minutos, motivo de cancelación vacío o JSON inválido. |
| `404` | Not Found | Mascota, dueño o veterinario inexistente en el sistema (RN-06), o turno no encontrado. |
| `409` | Conflict | Solapamiento de horario (RN-08), horario fuera de disponibilidad del veterinario (RN-10), alteración o cancelación de turno finalizado (RN-09), cancelación de turno ya cancelado, asignación a mascota inactiva (RN-07) o serie crónica ya generada para la atención médica (RN-11). |
| `500` | Internal Server Error | Falla no controlada de persistencia o comunicación en la base de datos. |

### Consideraciones sobre Disponibilidad, Turnos de 30 Minutos y Búsqueda Automática

1. **Configuración parametrizable de disponibilidad de veterinarios (RN-10):**
   - La disponibilidad horaria de cada veterinario se almacena de forma configurable en el sistema, no mediante valores fijos en el código.
   - Para cada día de la semana (Lunes a Domingo), se pueden habilitar jornadas de mañana, tarde o ambas, cada una con su horario de inicio y finalización.
   - Todo turno se asigna estrictamente dentro de los bloques de 30 minutos comprendidos entre el inicio y fin de la jornada habilitada para ese día.
   - Quedan fuera del alcance de esta versión feriados, vacaciones, licencias del personal y excepciones especiales de calendario.
2. **Liberación de horario y compatibilidad con RN-08:**
   - La regla **RN-08** prohíbe la superposición de turnos para un mismo profesional en bloques fijos de 30 minutos.
   - El control de disponibilidad horaria evalúa exclusivamente aquellos turnos que mantengan una reserva vigente del horario (estados activos tales como `Pendiente`, `Confirmado` o `EnAtencion`).
   - Al cancelarse un turno, este deja de computar como ocupado en el control de disponibilidad; por lo tanto, el bloque de 30 minutos queda disponible de inmediato para ser reservado por otro turno con el mismo veterinario.
   - Todo nuevo turno asignado en ese horario contará con su propio identificador y registro independiente; bajo ninguna circunstancia se sobrescribe ni se reutiliza el registro del turno cancelado.
3. **Búsqueda automática de disponibilidad para seguimiento crónico (RN-11):**
   - El sistema calcula las fechas proyectadas a 6 meses según la frecuencia seleccionada (quincenal, mensual, bimestral, trimestral).
   - Para cada fecha, busca el primer bloque de 30 minutos libre dentro de la jornada preferida del dueño (mañana o tarde) según los horarios del veterinario. Si la preferencia es indistinta, busca en cualquier jornada habilitada de ese día.
   - Si no hay cupo o el profesional no atiende ese día, avanza la búsqueda al siguiente día de atención habilitado del veterinario.
   - **Regla estricta:** La búsqueda automática respeta la jornada preferida y nunca cambia automáticamente de mañana a tarde ni de tarde a mañana cuando se indicó una preferencia específica.
   - **Idempotencia:** No se permite generar más de una serie de seguimiento para una misma atención médica.
   - **Independencia operativa:** Cada turno generado en la serie posee su propio ID y estado (`Pendiente`), pudiendo ser confirmado, reprogramado o cancelado de forma individual con motivo obligatorio y registro de auditoría, sin afectar a los restantes turnos de la serie.
4. **Reglas de Tolerancia y Asignación de la Serie Aprobadas:**
   - **Límite máximo de búsqueda hacia adelante (7 días):** Si en la fecha proyectada no existe disponibilidad en la jornada preferida, el sistema buscará en los siguientes días de atención del profesional hasta un máximo de siete (7) días corridos posteriores a la fecha ideal calculada. Superado dicho plazo, se considerará que no existe cupo para ese período para evitar desfasar el esquema clínico de controles.
   - **Política de asignación parcial tolerante con reporte de pendientes:** Si en alguna de las fechas proyectadas de la serie no se consigue disponibilidad dentro del margen de 7 días y en la jornada preferida, el sistema registrará todos los turnos que sí hayan obtenido disponibilidad y retornará en la respuesta el listado de turnos asignados junto con las fechas que no pudieron cubrirse (`fechasPendientes[]`), permitiendo al personal de la clínica coordinar manualmente esas citas específicas sin perder los turnos asignados del plan.

### Nota: Validación vs. Verificación aplicada

- **Validación (Presentación, → 400 / UI):** Presencia de identificadores requeridos, formato ISO de fecha/hora, restricción de fechas en el pasado, alineación a bloques de 30 minutos, ingreso obligatorio del motivo de cancelación no vacío (`"Debe ingresar un motivo de cancelación"`) y confirmación del usuario antes de proceder.
- **Verificación (Negocio, → 404/409):** Validación de existencia de mascota y dueño (**RN-06**), comprobación de estado activo de la mascota (**RN-07**), verificación de pertenencia del horario a la jornada configurada del profesional (**RN-10**), verificación de no solapamiento en la agenda del profesional (**RN-08**), comprobación de que el turno a cancelar o modificar no esté en estado Finalizado (**RN-09**), verificación de no cancelación previa y verificación de no duplicación de serie crónica (**RN-11**).

### Matriz de trazabilidad CU-06 → Test

| Paso del CU / Escenario | Excepción / Código | Test unitario (BusinessLogic) | Test integración (HTTP / UI) |
| --- | --- | --- | --- |
| Flujo principal — Alta de turno | `201 Created` | `CreateTurnoAsync_WithValidData_SavesAndReturnsTurnoDTO` | `CreateTurno_WithValidData_Returns201Created` |
| Flujo principal — Cancelar turno | `200 OK` | `CancelTurnoAsync_WithValidMotivo_SetsCancelledAndLogsAudit` | `CancelTurno_WithValidMotivo_Returns200OK` |
| Flujo principal — Reprogramar turno | `200 OK` | `UpdateTurnoAsync_WithValidNewSchedule_UpdatesAndReturnsTurnoDTO` | `UpdateTurno_WithValidSchedule_Returns200OK` |
| Flujo 3.4 — Generación crónica exitosa | `200 OK / 201 Created` | `GenerarTurnosSeguimientoCronicoAsync_WithValidFrequency_GeneratesSixMonthsSeries` | `GenerarTurnosCronicos_WithValidData_ReturnsSuccessWithTurnosList` |
| 1a. JSON inválido | `400 Bad Request` | — (model binding en Presentación) | `CreateTurno_WithMalformedPayload_Returns400BadRequest` |
| 2a. Fecha en el pasado | `400 Bad Request` | — (validación DataAnnotations) | `CreateTurno_WithPastDate_Returns400BadRequest` |
| 3a. Solapamiento de horario | `409 Conflict` | `CreateTurnoAsync_WithOverlappingSchedule_ThrowsHorarioNoDisponibleException` | `CreateTurno_WhenScheduleOverlaps_Returns409Conflict` |
| 3b. Mascota inexistente | `404 Not Found` | `CreateTurnoAsync_WhenMascotaNotFound_ThrowsMascotaNotFoundException` | `CreateTurno_WhenMascotaNotFound_Returns404NotFound` |
| 3c. Veterinario no encontrado/inactivo | `404 Not Found` | `CreateTurnoAsync_WhenVeterinarioNotFound_ThrowsVeterinarioNotFoundException` | `CreateTurno_WhenVeterinarioNotFound_Returns404NotFound` |
| 3d. Modificar turno finalizado | `409 Conflict` | `UpdateTurnoAsync_WhenTurnoIsFinalizado_ThrowsTurnoFinalizadoException` | `UpdateTurno_WhenTurnoIsFinalizado_Returns409Conflict` |
| 3e. Mascota inactiva | `409 Conflict` | `CreateTurnoAsync_WhenMascotaIsInactive_ThrowsMascotaInactivaException` | `CreateTurno_WhenMascotaIsInactive_Returns409Conflict` |
| 3f. Horario fuera de disponibilidad | `409 Conflict` | `CreateTurnoAsync_OutsideVeterinarioAvailability_ThrowsHorarioNoDisponibleException` | `CreateTurno_WhenOutsideAvailability_Returns409Conflict` |
| 3g. Serie crónica duplicada | `409 Conflict` | `GenerarTurnosCronicosAsync_WhenAlreadyGenerated_ThrowsConflictException` | `GenerarTurnosCronicos_WhenDuplicate_Returns409Conflict` |
| 3h. Búsqueda respeta jornada preferida | `200 OK / 201 Created` | `GenerarTurnosCronicosAsync_RespectsPreferredShiftWithoutSwitching` | `GenerarTurnosCronicos_RespectsShift_ReturnsAssignedSlots` |
| 4a. Error de persistencia en alta | `500 Internal Server Error` | `CreateTurnoAsync_WhenDbFails_ThrowsException` | `CreateTurno_WhenDbFails_Returns500InternalServerError` |
| A1. Motivo de cancelación vacío | `400 Bad Request` | `CancelTurnoAsync_WhenMotivoIsEmpty_ThrowsValidationException` | `CancelTurno_WhenMotivoIsEmpty_Returns400BadRequest` |
| A2. Cancelación no confirmada | N/A (UI Diálogo) | — (cancelación cancelada en interfaz de usuario) | `Turno_AlCancelarConfirmacion_ConservaEstadoYHorarioOcupado` |
| A3. Turno finalizado al cancelar | `409 Conflict` | `CancelTurnoAsync_WhenTurnoIsFinalizado_ThrowsTurnoFinalizadoException` | `CancelTurno_WhenTurnoIsFinalizado_Returns409Conflict` |
| A4. Turno previamente cancelado | `409 Conflict` | `CancelTurnoAsync_WhenTurnoAlreadyCancelled_ThrowsTurnoYaCanceladoException` | `CancelTurno_WhenAlreadyCancelled_Returns409Conflict` |
| A5. Error durante la cancelación | `500 Internal Server Error` | `CancelTurnoAsync_WhenDbFails_ThrowsExceptionAndRollbacks` | `CancelTurno_WhenDbFails_Returns500AndPreservesStatus` |
| Cancelación individual en serie crónica | `200 OK` | `CancelTurnoAsync_IndividualChronicTurno_CancelsOnlyTargetTurnoAndPreservesOthers` | `CancelTurno_ChronicTurno_CancelsOnlyTargetAndPreservesOthers` |
| Liberación de horario / Nueva reserva | `201 Created` | `CreateTurnoAsync_InCancelledSlot_SucceedsWithoutOverlapConflict` | `CreateTurno_InCancelledSlot_Returns201Created` |

> Regla de oro: cada flujo del caso de uso debe tener al menos un test. En los flujos resueltos en la Capa de Presentación el test aplicable es el de integración HTTP o prueba de interfaz de usuario. Los tests se ejecutan con `dotnet test SistemaVeterinaria.slnx`.

