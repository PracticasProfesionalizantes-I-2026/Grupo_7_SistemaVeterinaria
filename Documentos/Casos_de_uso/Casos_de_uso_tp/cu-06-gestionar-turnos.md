# Caso de Uso: Gestionar Turnos

> Especificación elaborada siguiendo la guía `GUIA-Especificacion-Casos-de-Uso.md` (sección 3).
> Implementación del ciclo de vida de los turnos con reglas de negocio RN-06 (asociación a dueño registrado), RN-07 (restricción en mascotas inactivas), RN-08 (no solapamiento de agenda por veterinario en bloques de 30 minutos), RN-09 (inmutabilidad de turnos finalizados), RN-10 (disponibilidad horaria configurable del profesional) y RN-11 (generación automática de turnos para seguimiento crónico).
> Incorpora la especificación del ciclo de vida completo de 5 estados (`Pendiente`, `Confirmado`, `EnAtencion`, `Finalizado`, `Cancelado`) y transiciones formalizadas (`Pendiente → Confirmado`, `Pendiente → Cancelado`, `Confirmado → Cancelado`, `Confirmado → EnAtencion`, `EnAtencion → Finalizado`).
> Incorpora el inicio de atención (`POST /api/turnos/{id}/iniciar-atencion`) y la finalización (`POST /api/turnos/{id}/finalizar`) restringidos exclusivamente al veterinario asignado al turno (identificado mediante JWT), el registro automático en UTC de `FechaHoraInicioAtencion` y `FechaHoraFinAtencion`, la exigencia de contar con una atención médica vinculada (`AtencionMedica.TurnoId`) antes de finalizar y la prohibición de modificar o cancelar un turno en estado `EnAtencion`.
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
Permite al personal de la clínica veterinaria (Recepcionista o Veterinario, según la operación) gestionar el ciclo de vida completo de los turnos de atención a través de una máquina de estados finita de cinco (5) estados exclusivos: **`Pendiente`**, **`Confirmado`**, **`EnAtencion`**, **`Finalizado`** y **`Cancelado`**.

Las transiciones permitidas son:
- **`Pendiente → Confirmado`:** Confirmación manual de turnos pendientes (ej. generados por seguimiento crónico) por Recepcionista o Veterinario, validando disponibilidad horaria, ausencia de solapamiento y paciente activo (`POST /api/turnos/{id}/confirmar`).
- **`Pendiente → Cancelado`:** Cancelación directa de turnos pendientes por Recepcionista o Veterinario con motivo obligatorio (`POST /api/turnos/{id}/cancelar` o `PATCH /api/turnos/{id}/cancelar`).
- **`Confirmado → Cancelado`:** Cancelación de turnos previamente confirmados por Recepcionista o Veterinario con motivo obligatorio, liberando el bloque horario de 30 minutos (`POST /api/turnos/{id}/cancelar` o `PATCH /api/turnos/{id}/cancelar`).
- **`Confirmado → EnAtencion`:** Inicio de la consulta médica autorizado **exclusivamente para el veterinario asignado al turno** (identificado a partir del token JWT) mediante `POST /api/turnos/{id}/iniciar-atencion`. Registra automáticamente `FechaHoraInicioAtencion` con la fecha y hora real del sistema en UTC.
- **`EnAtencion → Finalizado`:** Finalización de la consulta autorizada **exclusivamente para el veterinario asignado al turno** mediante `POST /api/turnos/{id}/finalizar`. Exige obligatoriamente que exista una atención médica registrada y vinculada al turno (`AtencionMedica.TurnoId = Turno.Id`), y registra automáticamente `FechaHoraFinAtencion` en UTC (`>= FechaHoraInicioAtencion`).

**Vinculación con atenciones médicas y diferencia con seguimiento crónico:**
- Un turno en estado `EnAtencion` no puede reprogramarse (`PUT /api/turnos/{id}`) ni cancelarse (`POST|PATCH /api/turnos/{id}/cancelar`), rechazándose tales intentos con `TurnoEnAtencionException` (`409 Conflict`).
- Mientras un turno se encuentra `EnAtencion`, el veterinario asignado registra la atención médica (CU-04) vinculándola mediante `AtencionMedica.TurnoId` (relación opcional 1:1, admitiéndose como máximo una única atención médica por turno). Registrar la atención médica no finaliza automáticamente el turno; habilita que el veterinario asignado invoque luego la finalización explícita (`EnAtencion → Finalizado`).
- Se conserva la posibilidad de registrar **atenciones médicas espontáneas** sin turno previo (`AtencionMedica.TurnoId = null`).
- **Diferencia entre el turno de consulta y los turnos de seguimiento crónico:** `AtencionMedica.TurnoId` vincula opcionalmente (1:1) la atención médica con el turno de consulta que se está atendiendo en ese momento, mientras que `Turno.AtencionMedicaId` vincula (1:N) los turnos futuros generados automáticamente en estado `Pendiente` para el seguimiento de una patología crónica con la atención médica que los originó.
- Las marcas temporales `FechaHoraInicioAtencion` y `FechaHoraFinAtencion` se almacenan en la entidad `Turno` para permitir en versiones futuras el cálculo de estadísticas de duración de consultas y facturación por tiempo, sin implementar estadísticas de duración ni cobros en la versión actual.

Cada veterinario cuenta con su propia configuración parametrizable de días y horarios de atención (jornadas de mañana, tarde o ambas con sus respectivos horarios de inicio y fin), debiendo asignarse los turnos exclusivamente dentro de dicha disponibilidad (**RN-10**) y sin superposición horaria (**RN-08**), considerando como ocupados los bloques con turnos en estado `Confirmado`, `EnAtencion` o `Finalizado`. La cancelación (permitida solo desde `Pendiente` o `Confirmado`) exige un motivo obligatorio y confirmación previa; registra automáticamente la fecha/hora y usuario responsable, cambia el estado a `Cancelado` de manera irreversible y libera el bloque de 30 minutos para nuevas reservas, conservando el registro histórico para auditoría y reportes. Los turnos en estado `Finalizado` son estrictamente inmutables: no pueden cancelarse, reprogramarse ni volver a transicionar (**RN-09**).

Asimismo, este caso de uso implementa el mecanismo de generación y almacenamiento automático de una serie de turnos de seguimiento con estado inicial `Pendiente` durante los próximos seis meses para mascotas que reciban un diagnóstico de enfermedad crónica en una atención médica (CU-04 / **RN-11**). La generación respeta la frecuencia indicada (quincenal, mensual, bimestral o trimestral) y la jornada preferida del dueño (mañana, tarde o indistinta), buscando disponibilidad en bloques de 30 minutos en la agenda del veterinario sin superposiciones y sin alternar automáticamente entre mañana y tarde ante una preferencia explícita. Cada turno de la serie generada cuenta con trazabilidad a la atención médica de origen (`AtencionMedicaId`) y puede confirmarse, cancelarse o reprogramarse individualmente sin afectar al resto de la serie.

Quedan fuera del alcance de esta versión la gestión de feriados, vacaciones, licencias del personal y excepciones especiales de agenda.

### 2. PRECONDICIONES
- El actor debe contar con una sesión activa y un Token JWT con rol de `Recepcionista` o `Veterinario`.
- Para registrar un nuevo turno manual, la mascota debe estar registrada en el sistema, encontrarse en estado **Activa** (**RN-07**) y estar asociada a un dueño registrado (**RN-06**).
- El veterinario asignado debe encontrarse registrado, habilitado y contar con disponibilidad horaria configurada para el día y rango solicitado (**RN-10**).
- Para confirmar un turno (`POST /api/turnos/{id}/confirmar`), el turno debe encontrarse en estado **`Pendiente`** y su fecha/hora no debe pertenecer al pasado.
- Para iniciar la atención de un turno (`POST /api/turnos/{id}/iniciar-atencion`), el turno debe encontrarse en estado **`Confirmado`**, la mascota debe estar activa y el usuario autenticado debe ser exclusivamente el **veterinario asignado al turno** (`turno.VeterinarioId == usuarioId`).
- Para finalizar un turno (`POST /api/turnos/{id}/finalizar`), el turno debe encontrarse en estado **`EnAtencion`**, el usuario autenticado debe ser exclusivamente el **veterinario asignado al turno**, y debe existir una atención médica registrada y vinculada al turno (`AtencionMedica.TurnoId == Turno.Id`).
- Para cancelar o reprogramar un turno, este debe existir en el sistema y encontrarse en estado **`Pendiente`** o **`Confirmado`** (no se permite cancelar ni reprogramar turnos en estado **`EnAtencion`**, **`Finalizado`** (**RN-09**) ni previamente **`Cancelado`**).
- Para la invocación del mecanismo de generación automática por enfermedad crónica, debe registrarse una atención médica con diagnóstico crónico y parámetros válidos de frecuencia y jornada preferida (**RN-11**).

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

#### 3.5 Confirmar turno manual o de seguimiento (Pendiente → Confirmado)
1. La Recepcionista o el Veterinario envía una petición `POST /api/turnos/{id}/confirmar` para un turno en estado `Pendiente`.
2. El sistema verifica que el turno exista (si no existe, lanza `TurnoNotFoundException` → 404 Not Found).
3. El sistema adquiere el bloqueo por concurrencia para el veterinario actuante (`VeterinarioLockManager`) y recarga el estado del turno desde base de datos (`ReloadAsync`).
4. Valida que el turno se encuentre efectivamente en estado `Pendiente` (si no, lanza `TurnoNoPendienteException` → 409 Conflict).
5. Valida que la mascota se encuentre activa (**RN-07**) y que la fecha y hora del turno no pertenezcan al pasado.
6. Verifica que el turno se encuentre dentro de la disponibilidad configurada del veterinario (**RN-10**) y que no exista otro turno que ocupe el horario (`Confirmado`, `EnAtencion` o `Finalizado`) en ese mismo bloque de 30 minutos (**RN-08**).
7. Cambia el estado del turno a **`Confirmado`** y persiste la actualización en base de datos.
8. Retorna el turno actualizado con código **200 OK**.

#### 3.6 Iniciar atención del turno (Confirmado → EnAtencion)
1. El Veterinario autenticado envía una petición `POST /api/turnos/{id}/iniciar-atencion` sobre un turno confirmado de su agenda (sin enviar `UsuarioId` en el cuerpo de la solicitud; la identidad se toma exclusivamente del claim `ClaimTypes.NameIdentifier` del JWT).
2. El sistema verifica que el usuario tenga el rol `Veterinario` (si es `Recepcionista` o `Administrador`, devuelve **403 Forbidden**) y que el turno exista (si no existe, lanza `TurnoNotFoundException` → **404 Not Found**).
3. El sistema adquiere el bloqueo por concurrencia para el veterinario (`VeterinarioLockManager.AdquirirBloqueoAsync`) y recarga el turno desde base de datos (`ReloadAsync`).
4. Verifica que el turno se encuentre en estado **`Confirmado`** (si está `Pendiente` lanza `TurnoNoConfirmadoException` → **409 Conflict**; si ya está `EnAtencion` lanza `TurnoEnAtencionException` → **409 Conflict**; si está `Cancelado` o `Finalizado` lanza la excepción de estado correspondiente → **409 Conflict**).
5. Comprueba que el veterinario autenticado sea el profesional activo asignado al turno (`turno.VeterinarioId == usuarioId`); en caso contrario lanza `TurnoVeterinarioNoAutorizadoException` → **403 Forbidden**.
6. Verifica que la mascota asociada continúe activa (**RN-07**; si está inactiva lanza `MascotaInactivaException` → **409 Conflict**).
7. Cambia el estado del turno a **`EnAtencion`**, registra automáticamente `FechaHoraInicioAtencion = DateTime.UtcNow` y persiste la actualización en base de datos.
8. Retorna el turno actualizado con código **200 OK**.

#### 3.7 Finalización del turno (EnAtencion → Finalizado)
1. Una vez registrada la atención médica vinculada al turno (`AtencionMedica.TurnoId = Turno.Id`), el Veterinario asignado envía una petición `POST /api/turnos/{id}/finalizar` (identificándose mediante el JWT autenticado).
2. El sistema verifica que el usuario tenga el rol `Veterinario` (si es `Recepcionista` o `Administrador`, devuelve **403 Forbidden**) y que el turno exista (`TurnoNotFoundException` → **404 Not Found**).
3. El sistema adquiere el bloqueo por concurrencia para el veterinario (`VeterinarioLockManager.AdquirirBloqueoAsync`) y recarga el turno desde base de datos (`ReloadAsync`).
4. Valida que el turno se encuentre estrictamente en estado **`EnAtencion`** (si está `Pendiente` o `Confirmado` lanza `TurnoNoEnAtencionException` → **409 Conflict**; si está `Cancelado` o ya `Finalizado` lanza la excepción de estado correspondiente → **409 Conflict**).
5. Comprueba que el veterinario autenticado sea el profesional activo asignado al turno (`turno.VeterinarioId == usuarioId`); si no coincide lanza `TurnoVeterinarioNoAutorizadoException` → **403 Forbidden**.
6. Verifica mediante `IAtencionMedicaRepository.ExistsByTurnoIdAsync(id)` que exista una atención médica vinculada directamente a dicho turno (`AtencionMedica.TurnoId == id`). Si no existe atención médica vinculada, lanza `TurnoSinAtencionMedicaException` → **409 Conflict**.
7. Cambia el estado del turno a **`Finalizado`**, registra automáticamente `FechaHoraFinAtencion` en UTC (garantizando `FechaHoraFinAtencion >= FechaHoraInicioAtencion`) y persiste la actualización en base de datos.
8. A partir de este momento, el turno adquiere carácter estrictamente inmutable (**RN-09**): no puede cancelarse, reprogramarse, confirmarse, reiniciarse ni refinalizarse.
9. Retorna el turno finalizado con código **200 OK**.

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

* **3i. Intento de confirmar turno que no está en estado Pendiente (HTTP 409 Conflict):**
  1. Si en el flujo 3.5 el usuario intenta confirmar un turno que se encuentra en un estado distinto a `Pendiente` (por ejemplo, ya `Confirmado`, `EnAtencion`, `Cancelado` o `Finalizado`).
  2. La Capa de Negocio lanza `TurnoNoPendienteException`.
  3. El sistema devuelve un código **409 Conflict** informando el estado actual del turno. Fin del caso de uso.

* **3j. Intento de confirmar turno con fecha en el pasado (HTTP 400 Bad Request):**
  1. Si en el flujo 3.5 la fecha y hora del turno a confirmar es anterior a la fecha y hora actual UTC.
  2. La Capa de Negocio lanza `ValidationException`.
  3. El sistema devuelve un código **400 Bad Request** informando que no es posible confirmar turnos pasados. Fin del caso de uso.

* **3k. Intento de iniciar atención desde un estado distinto a Confirmado (HTTP 409 Conflict):**
  1. Si en el flujo 3.6 el veterinario intenta iniciar la atención de un turno que se encuentra en estado `Pendiente`, `EnAtencion`, `Cancelado` o `Finalizado`.
  2. La Capa de Negocio lanza `TurnoNoConfirmadoException` (o `TurnoEnAtencionException` / `TurnoCanceladoException` / `TurnoYaFinalizadoException`).
  3. El sistema devuelve un código **409 Conflict** sin alterar el estado del turno. Fin del caso de uso.

* **3l. Intento de iniciar o finalizar un turno por un usuario no autorizado o veterinario distinto al asignado (HTTP 403 Forbidden):**
  1. Si en los flujos 3.6 o 3.7 la petición proviene de un usuario con rol `Recepcionista` o `Administrador`, o bien de un `Veterinario` distinto al profesional asignado en `turno.VeterinarioId`.
  2. El filtro de autorización o la Capa de Negocio (`TurnoVeterinarioNoAutorizadoException`) rechaza la operación.
  3. El sistema devuelve un código **403 Forbidden** sin modificar el turno. Fin del caso de uso.

* **3m. Intento de finalizar turno que no está en estado EnAtencion (HTTP 409 Conflict):**
  1. Si en el flujo 3.7 el veterinario intenta finalizar un turno que se encuentra en estado `Pendiente`, `Confirmado`, `Cancelado` o ya `Finalizado`.
  2. La Capa de Negocio lanza `TurnoNoEnAtencionException` (o `TurnoCanceladoException` / `TurnoYaFinalizadoException`).
  3. El sistema devuelve un código **409 Conflict** informando que únicamente los turnos en estado `EnAtencion` pueden finalizarse. Fin del caso de uso.

* **3n. Intento de finalizar turno EnAtencion sin atención médica vinculada (HTTP 409 Conflict):**
  1. Si en el flujo 3.7 el turno se encuentra en estado `EnAtencion`, pero aún no se ha registrado una atención médica vinculada mediante `AtencionMedica.TurnoId == Turno.Id`.
  2. La Capa de Negocio lanza `TurnoSinAtencionMedicaException`.
  3. El sistema devuelve un código **409 Conflict** y mantiene el turno en estado `EnAtencion` hasta que se registre la atención médica correspondiente. Fin del caso de uso.

* **3o. Intento de modificar o cancelar un turno en estado EnAtencion (HTTP 409 Conflict):**
  1. Si durante el flujo de reprogramación (`PUT /api/turnos/{id}`) o cancelación (`POST|PATCH /api/turnos/{id}/cancelar`) el turno se encuentra en estado `EnAtencion`.
  2. La Capa de Negocio lanza `TurnoEnAtencionException`.
  3. El sistema devuelve un código **409 Conflict** y preserva intactos el horario, motivo y estado del turno. Fin del caso de uso.

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

* **A3 — Turno finalizado o en atención:**
  1. En el Paso 5 del flujo de cancelación, el sistema detecta que el usuario intenta cancelar un turno que se encuentra en estado `Finalizado` o `EnAtencion`.
  2. El sistema informa que los turnos en atención o finalizados no pueden cancelarse, respetando la regla **RN-09**.
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
2. **Reprogramación de turno:** Actualiza la fecha/hora o profesional del turno siempre que este se encuentre en estado `Pendiente` o `Confirmado` (no `EnAtencion`, `Finalizado` (**RN-09**) ni `Cancelado`), validando la disponibilidad de agenda (**RN-08**, **RN-10**). Si el turno pertenece a una serie de seguimiento crónico, la reprogramación es estrictamente individual y no altera los restantes turnos de la serie.
3. **Cancelación de turno:** Procedimiento detallado en el Flujo Principal 3.2, mediante el cual el turno en estado `Pendiente` o `Confirmado` pasa al estado `"Cancelado"` con motivo obligatorio, registro de fecha/hora y usuario responsable, liberando el bloque de 30 minutos en la agenda para nuevas asignaciones y preservando el registro histórico sin eliminación física ni posibilidad de reactivación. Si el turno cancelado pertenece a una serie crónica, los restantes turnos de la serie permanecen vigentes.
4. **Generación automática de turnos para seguimiento crónico:** Mecanismo detallado en el Flujo Principal 3.4, mediante el cual se genera y almacena una serie de turnos con estado `Pendiente` a lo largo de seis meses para pacientes diagnosticados con patologías crónicas, respetando frecuencia, jornada preferida y agenda configurada (**RN-11**).
5. **Confirmación de turno:** Transición explícita de `Pendiente` a `Confirmado` mediante `POST /api/turnos/{id}/confirmar`, validando pertenencia a la agenda del veterinario, ausencia de solapamientos y paciente activo.
6. **Inicio de atención del turno:** Transición explícita de `Confirmado` a `EnAtencion` mediante `POST /api/turnos/{id}/iniciar-atencion`, reservada exclusivamente al veterinario asignado al turno y registrando `FechaHoraInicioAtencion` en UTC.
7. **Finalización de turno:** Transición explícita de `EnAtencion` a `Finalizado` mediante `POST /api/turnos/{id}/finalizar`, reservada exclusivamente al veterinario asignado al turno tras verificar la existencia de la atención médica vinculada (`AtencionMedica.TurnoId`), registrando `FechaHoraFinAtencion` en UTC y otorgándole carácter inmutable permanente (**RN-09**).

### 6. POSTCONDICIONES
- **Alta de turno manual exitosa:** Se crea el registro en la tabla `Turnos` con estado Confirmado, origen `Manual`, y el bloque de 30 minutos queda ocupado en la agenda del profesional (**RN-08**).
- **Reprogramación exitosa:** Se actualiza la fecha/hora o profesional del turno, liberando el bloque horario anterior y ocupando el nuevo bloque validado de 30 minutos.
- **Confirmación de turno exitosa (Flujo 3.5):**
  - El turno pasa a estado **Confirmado**.
  - El bloque de 30 minutos se mantiene reservado en la agenda del profesional (**RN-08**).
- **Inicio de atención exitoso (Flujo 3.6):**
  - El turno pasa a estado **EnAtencion** y se persiste `FechaHoraInicioAtencion` en UTC.
  - El turno queda habilitado para vincular su única atención médica (`AtencionMedica.TurnoId`) y bloqueado contra modificaciones o cancelaciones.
- **Finalización de turno exitosa (Flujo 3.7):**
  - El turno pasa a estado **Finalizado** y se persiste `FechaHoraFinAtencion` en UTC (`>= FechaHoraInicioAtencion`).
  - El turno adquiere carácter estrictamente inmutable (**RN-09**), no pudiendo cancelarse, reprogramarse, confirmarse, reiniciarse ni refinalizarse.
- **Generación automática de seguimiento crónico exitosa (Flujo 3.4):**
  - Se crea la serie de turnos con estado `Pendiente`, origen `SeguimientoCronico` (`EsAutomatico = true`), vinculados a la `AtencionMedicaId` de origen (**RN-11**).
  - Cada turno de la serie queda habilitado para ser confirmado, reprogramado o cancelado de manera independiente.
- **Cancelación de turno exitosa (Flujo 3.2):**
  - El turno cambia su estado a **Cancelado** de forma irreversible (no puede reactivarse), sea que provenía de `Pendiente` o de `Confirmado`.
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
| `200` | OK | Confirmación de turno, inicio de atención, finalización, reprogramación, cancelación exitosa o configuración de disponibilidad. |
| `400` | Bad Request | Parámetros incompletos, fecha en el pasado, intervalo distinto a 30 minutos, motivo de cancelación vacío o JSON inválido. |
| `401` | Unauthorized | Solicitud sin token JWT o con reclamos de identidad ausentes/inválidos. |
| `403` | Forbidden | Rol no autorizado (`Administrador` en agenda; `Recepcionista` al intentar iniciar o finalizar una atención), o intento de un `Veterinario` de iniciar/finalizar un turno de otro profesional o configurar la disponibilidad horaria de otro veterinario (`TurnoVeterinarioNoAutorizadoException`). |
| `404` | Not Found | Mascota, dueño o veterinario inexistente en el sistema (RN-06), o turno no encontrado. |
| `409` | Conflict | Solapamiento de horario (RN-08), horario fuera de disponibilidad del veterinario (RN-10), intento de modificar o cancelar un turno `EnAtencion` o `Finalizado` (RN-09), intento de iniciar atención desde un estado distinto a `Confirmado`, intento de finalizar un turno que no está `EnAtencion` o que no posee atención médica vinculada (`TurnoSinAtencionMedicaException`), cancelación de turno ya cancelado, asignación a mascota inactiva (RN-07) o serie crónica ya generada para la atención médica (RN-11). |
| `500` | Internal Server Error | Falla no controlada de persistencia o comunicación en la base de datos. |

### Consideraciones sobre Disponibilidad, Turnos de 30 Minutos y Búsqueda Automática

1. **Configuración parametrizable de disponibilidad de veterinarios (RN-10):**
   - La disponibilidad horaria de cada veterinario se almacena de forma configurable en el sistema, no mediante valores fijos en el código.
   - Para cada día de la semana (Lunes a Domingo), se pueden habilitar jornadas de mañana, tarde o ambas, cada una con su horario de inicio y finalización.
   - **Permisos de configuración:** La `Recepcionista` puede configurar la disponibilidad horaria de cualquier veterinario activo, mientras que un usuario con rol `Veterinario` únicamente puede configurar su propia disponibilidad horaria (validando su identidad desde el token JWT); cualquier intento de modificar la disponibilidad de otro veterinario es rechazado con `403 Forbidden`. El `Administrador` no posee acceso (`403 Forbidden`).
   - Todo turno se asigna estrictamente dentro de los bloques de 30 minutos comprendidos entre el inicio y fin de la jornada habilitada para ese día.
   - Quedan fuera del alcance de esta versión feriados, vacaciones, licencias del personal y excepciones especiales de calendario.
2. **Ocupación y liberación de horario y compatibilidad con RN-08:**
   - La regla **RN-08** prohíbe la superposición de turnos para un mismo profesional en bloques fijos de 30 minutos.
   - El control de disponibilidad horaria (`ExisteSuperposicionAsync`) considera como ocupados los bloques con turnos en estado `Confirmado`, `EnAtencion` o `Finalizado` (los turnos en estado `Pendiente` no bloquean disponibilidad hasta que se confirman).
   - Al cancelarse un turno (desde `Pendiente` o `Confirmado`), este deja de computar como ocupado en el control de disponibilidad; por lo tanto, el bloque de 30 minutos queda disponible de inmediato para ser reservado por otro turno con el mismo veterinario.
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

### Máquina de Estados y Ciclo de Vida de los Turnos (RN-09)

- **Estados del Turno:** El sistema implementa una máquina de estados finita con cinco (5) estados exclusivos: `Pendiente`, `Confirmado`, `EnAtencion`, `Finalizado` y `Cancelado`.
- **Matriz de Transiciones Permitidas:**

| Estado Inicial | Transición / Acción | Estado Destino | Endpoint / Mecanismo | Roles / Condiciones Autorizadas |
| --- | --- | --- | --- | --- |
| `Pendiente` | Confirmar | `Confirmado` | `POST /api/turnos/{id}/confirmar` | Recepcionista, Veterinario |
| `Pendiente` | Cancelar | `Cancelado` | `POST\|PATCH /api/turnos/{id}/cancelar` | Recepcionista, Veterinario (con motivo obligatorio) |
| `Confirmado` | Cancelar | `Cancelado` | `POST\|PATCH /api/turnos/{id}/cancelar` | Recepcionista, Veterinario (con motivo obligatorio) |
| `Confirmado` | Iniciar atención | `EnAtencion` | `POST /api/turnos/{id}/iniciar-atencion` | Exclusivamente el Veterinario asignado al turno (registra `FechaHoraInicioAtencion` en UTC) |
| `EnAtencion` | Finalizar | `Finalizado` | `POST /api/turnos/{id}/finalizar` | Exclusivamente el Veterinario asignado al turno y con `AtencionMedica` vinculada (`AtencionMedica.TurnoId = Turno.Id`; registra `FechaHoraFinAtencion` en UTC) |
| `EnAtencion` | *(Reprogramar / Cancelar)* | — | *(Rechazado con 409 Conflict — `TurnoEnAtencionException`)* | — |
| `Finalizado` | *(Inmutable)* | — | *(Operación rechazada con 409 Conflict)* | — |
| `Cancelado` | *(Inmutable)* | — | *(Operación rechazada con 409 Conflict)* | — |

### Nota: Validación vs. Verificación aplicada

- **Validación (Presentación, → 400 / UI):** Presencia de identificadores requeridos, formato ISO de fecha/hora, restricción de fechas en el pasado, alineación a bloques de 30 minutos, ingreso obligatorio del motivo de cancelación no vacío (`"Debe ingresar un motivo de cancelación"`) y confirmación del usuario antes de proceder.
- **Verificación (Negocio, → 403/404/409):** Validación de existencia de mascota y dueño (**RN-06**), comprobación de estado activo de la mascota (**RN-07**), verificación de pertenencia del horario a la jornada configurada del profesional (**RN-10**), verificación de no solapamiento en la agenda del profesional (**RN-08**), comprobación de titularidad del veterinario asignado para iniciar y finalizar (`TurnoVeterinarioNoAutorizadoException` → `403`), comprobación de que el turno a cancelar o modificar no esté en estado `EnAtencion` ni `Finalizado` (**RN-09**), verificación de estado previo para confirmación (`Pendiente`), inicio de atención (`Confirmado`) y finalización (`EnAtencion`), comprobación de existencia de atención médica vinculada antes de finalizar (`TurnoSinAtencionMedicaException`) y verificación de no duplicación de serie crónica (**RN-11**).

### Matriz de trazabilidad CU-06 → Test

| Paso del CU / Escenario | Excepción / Código | Test unitario (BusinessLogic) | Test integración (HTTP / UI) |
| --- | --- | --- | --- |
| Flujo principal — Alta de turno | `201 Created` | `CreateManualAsync_DatosValidos_RetornaTurnoResponseDTOConEstadoConfirmado` | `AgendaYOperacionesDeTurnos_ContinuanFuncionandoCorrectamente` |
| Flujo principal — Cancelar turno | `200 OK` | `CancelarAsync_TurnoConfirmado_CambiaEstadoACanceladoYRegistraAuditoria` | `Cancelar_ConMotivoInasistencia_Retorna200OKYRegistraInasistencia` |
| Flujo principal — Reprogramar turno | `200 OK` | `UpdateAsync_TurnoConfirmadoValido_ActualizaFechaHoraYMotivo` | `AgendaYOperacionesDeTurnos_ContinuanFuncionandoCorrectamente` |
| Flujo 3.4 — Generación crónica exitosa | `200 OK / 201 Created` | `GenerarTurnosSeguimientoCronicoAsync_ConFrecuenciaValida_GeneraSerieSeisMeses` | `Create_SeguimientoCronico_ConVeterinarioConDisponibilidad_PersisteTurnosYRetorna201CreatedSinConflictoTracking` |
| Flujo 3.5 — Confirmar turno pendiente | `200 OK` | `ConfirmarAsync_TurnoExistenteEnEstadoPendiente_ConfirmaTurnoYRetornaTurnoResponseDTO` | `Confirmar_ConRolRecepcionista_TurnoPendienteValido_Retorna200OKYEstadoConfirmado` |
| Flujo 3.6 — Iniciar atención (Confirmado → EnAtencion) | `200 OK` | `IniciarAtencionAsync_TurnoConfirmadoVeterinarioAsignado_CambiaAEnAtencionYRegistraFechaHoraInicioUtc` | `IniciarAtencion_VeterinarioAsignado_TurnoConfirmado_Retorna200OKYRegistraFechaHoraInicioAtencion` |
| Flujo 3.7 — Finalizar turno (EnAtencion + atención vinculada → Finalizado) | `200 OK` | `FinalizarAsync_TurnoEnAtencionConAtencionMedicaVinculada_CambiaEstadoAFinalizadoYRegistraFechaFin` | `Finalizar_ConRolVeterinarioAsignado_ConAtencionMedicaVinculada_Retorna200OKYEstadoFinalizado` |
| 3a. Solapamiento de horario (incluye `EnAtencion`) | `409 Conflict` | `ExisteSuperposicionAsync_TurnoEnAtencionEnMismoHorario_RetornaTrue` | `Confirmar_CuandoHorarioYaOcupadoPorTurnoConfirmado_Retorna409Conflict` |
| 3d / 3o. Modificar o cancelar turno `EnAtencion` o `Finalizado` | `409 Conflict` | `UpdateAsync_TurnoEnEstadoEnAtencion_LanzaTurnoEnAtencionException` / `CancelarAsync_TurnoEnEstadoEnAtencion_LanzaTurnoEnAtencionException` | `ModificarOCancelar_TurnoEnAtencion_Retorna409Conflict_SinAlterarDatos` |
| 3i. Confirmar turno no pendiente | `409 Conflict` | `ConfirmarAsync_TurnoNoEstaEnEstadoPendiente_LanzaTurnoNoPendienteException` | `Confirmar_CuandoHorarioYaOcupadoPorTurnoConfirmado_Retorna409Conflict` |
| 3k. Iniciar atención desde estado no confirmado | `409 Conflict` | `IniciarAtencionAsync_TurnoNoConfirmado_LanzaExcepcionCorrespondiente` | `IniciarAtencion_DesdeEstadoNoConfirmado_Retorna409Conflict_SinAlterarEstado` |
| 3l. Iniciar o finalizar por otro veterinario o Recepcionista | `403 Forbidden` | `IniciarAtencionAsync_VeterinarioDistintoAlAsignado_LanzaTurnoVeterinarioNoAutorizadoException` / `FinalizarAsync_VeterinarioDistintoAlAsignado_LanzaTurnoVeterinarioNoAutorizadoException` | `IniciarAtencion_PorOtroVeterinarioDistintoAlAsignado_Retorna403Forbidden_SinModificarTurno` / `IniciarAtencion_ConRolRecepcionista_Retorna403Forbidden_SinModificarTurno` / `Finalizar_ConRolRecepcionista_Retorna403Forbidden_SinModificarEstado` |
| 3m. Finalizar turno fuera de `EnAtencion` | `409 Conflict` | `FinalizarAsync_TurnoNoEstaEnEstadoEnAtencion_LanzaTurnoNoEnAtencionException` | `Finalizar_TurnoConfirmadoSinIniciarAtencion_Retorna409Conflict_SinModificarEstado` |
| 3n. Finalizar turno `EnAtencion` sin atención médica vinculada | `409 Conflict` | `FinalizarAsync_TurnoEnAtencionSinAtencionMedicaVinculada_LanzaTurnoSinAtencionMedicaException` | `Finalizar_TurnoEnAtencionSinAtencionMedicaVinculada_Retorna409Conflict_SinModificarEstado` |
| A1. Motivo de cancelación vacío | `400 Bad Request` | `CancelarAsync_MotivoCancelacionVacio_LanzaValidationException` | `Cancelar_ConMotivoVacio_Retorna400BadRequest` |
| A4. Turno previamente cancelado | `409 Conflict` | `CancelarAsync_TurnoYaCancelado_LanzaTurnoYaCanceladoException` | `Cancelar_TurnoYaCancelado_Retorna409Conflict` |

> Regla de oro: cada flujo del caso de uso debe tener al menos un test. En los flujos resueltos en la Capa de Presentación el test aplicable es el de integración HTTP o prueba de interfaz de usuario. Los tests se ejecutan con `dotnet test SistemaVeterinaria.slnx`.

