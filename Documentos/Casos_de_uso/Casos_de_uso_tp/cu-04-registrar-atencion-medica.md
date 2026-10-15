# Caso de Uso: Registrar Atención Médica

> Especificación elaborada siguiendo la guía `GUIA-Especificacion-Casos-de-Uso.md` (sección 3).
> Implementación del registro clínico con reglas de negocio RN-01 (inmutabilidad de atenciones médicas), RN-02 (integración de registros clínicos a la historia clínica), RN-03 (evaluación in situ de aptitud para vacunación), RN-04 (vacunación condicionada a aptitud clínica en la misma consulta), RN-07 (restricción de atención en mascotas inactivas), RN-11 (generación automática de turnos para seguimiento crónico) y RN-12 (alerta de desnutrición por variación de peso > 10%).
> Incorpora la identificación obligatoria del profesional veterinario a partir de la sesión autenticada (claims del token JWT), impidiendo su asignación libre o manual.
> Incorpora la detección clínica y registro histórico de la alerta de desnutrición (Nivel 1 / RN-12) ante disminuciones de peso estrictamente superiores al 10 % respecto al peso de referencia, con carácter informativo y no bloqueante.
> Incorpora la especificación funcional definitiva sobre vacunación y aptitud clínica in situ: la condición de APTO tiene validez exclusiva para esa atención y ese mismo día, sin requerir entidad independiente de alta médica ni cómputo de vencimientos.
> Incorpora la activación del servicio interno de seguimiento de enfermedades crónicas (Nivel 3 / CU-06 / RN-11) con tolerancia ante falta de cupos (reporte de fechas pendientes para coordinación manual sin bloquear el guardado de la atención).
> Incorpora el control de navegación y advertencia de cambios sin guardar (INF-04).
> Incorpora el registro opcional de procedimientos quirúrgicos dentro de la atención médica y explicita la exclusión de consentimiento digital y firma en la primera entrega (INF-06).
> Incorpora la actualización automática y atómica de la fecha de última visita de la mascota al guardar la atención médica, con regla estricta de no retroceso (INF-07).
> Incorpora la aclaración sobre registro de estudios médicos y resultados diferidos (AMB-01).

| Campo | Valor |
| --- | --- |
| **ID del Caso de Uso** | CU-04 |
| **Nombre** | Registrar Atención Médica |
| **Actor Principal** | Veterinario/a (identificado mediante token JWT) |
| **Actores Secundarios** | Ninguno |
| **Alcance / Nivel** | Sistema; meta de usuario |
| **Stakeholders e intereses** | Veterinario/a → asentar de forma rigurosa el diagnóstico, evolución, peso, prescripciones, estudios, procedimientos quirúrgicos, vacunas y activar planes de seguimiento crónico; Dueño de la Mascota → recibir indicaciones claras de tratamiento, constancia de procedimientos/vacunas aplicadas y turnos programados de control; Clínica Veterinaria → cumplir normativas de historia clínica inmutable, control sanitario, alertas de salud y continuidad asistencial |
| **Disparador (Trigger)** | El veterinario selecciona la opción "Nueva Atención" desde la historia clínica de una mascota |
| **Prioridad / Frecuencia** | Alta; muy alta frecuencia (se ejecuta en cada acto médico veterinario) |
| **Reglas de negocio relacionadas** | RN-01 (inmutabilidad de las atenciones médicas); RN-02 (integración de registros clínicos a la historia clínica); RN-03 (evaluación in situ de aptitud para vacunación); RN-04 (vacunación condicionada a aptitud clínica en la misma consulta); RN-07 (mascotas inactivas no pueden registrar nuevas atenciones médicas); RN-11 (generación automática de seguimiento crónico); RN-12 (alerta de desnutrición por variación de peso > 10%) |

---

### 1. BREVE DESCRIPCIÓN
Permite al veterinario asentar una nueva atención médica en la historia clínica de una mascota activa, registrando obligatoriamente el motivo de consulta, anamnesis, examen físico, diagnóstico, tratamiento, peso del paciente (`pesoRegistrado`) y observaciones opcionales, con la posibilidad de extender la consulta incorporando prescripciones de medicamentos, solicitud o adjunto de estudios complementarios, registro opcional de procedimientos quirúrgicos realizados durante la consulta, evaluación de aptitud para vacunación mediante checklist clínica in situ (APTO/NO APTO) con el consecuente registro de vacunas aplicadas en caso de resultar apto, y activación de seguimiento programado ante diagnóstico de patologías crónicas.

**Identificación del profesional actuante:** Cada atención médica debe identificar de manera inequívoca al veterinario que la realizó. Dicho identificador (`VeterinarioId`) se obtiene directamente de los claims de la sesión autenticada del usuario (token JWT), impidiendo que sea un dato ingresado libre o arbitrariamente por el cliente. Asimismo, si la consulta incluye un procedimiento quirúrgico, el profesional autenticado queda asignado de forma automática como el Veterinario responsable del mismo.

**Nivel 1: Alerta de desnutrición (RN-12):** Durante el examen físico se registra el peso de la mascota. El sistema compara automáticamente dicho peso con el peso registrado en la atención médica inmediatamente anterior del paciente. Si la mascota no posee atenciones médicas previas, el sistema utiliza como base comparativa el peso de referencia consignado en la ficha de la mascota (`Mascota.Peso`), cuando sea un valor válido (> 0). Si no existe un peso anterior válido, el sistema no calcula variación ni genera alerta. Cuando la disminución de peso resulte estrictamente superior al 10 % respecto al peso de referencia, el sistema activa automáticamente la alerta de desnutrición (`AlertaDesnutricion = true`), la cual se conserva de manera inmutable como parte del registro histórico de la atención médica. El sistema informa el porcentaje exacto de disminución y el peso de referencia utilizado. La activación de esta alerta cumple una función clínico-preventiva y **no impide guardar la atención médica**.

**Nivel 3: Seguimiento de enfermedades crónicas (RN-11 / CU-06):** Cuando el veterinario registre un diagnóstico de enfermedad crónica, podrá indicar en el mismo acto la frecuencia de seguimiento (`Quincenal`, `Mensual`, `Bimestral`, `Trimestral`) y la jornada preferida del dueño (`Manana`, `Tarde`, `Indistinta`). Al confirmarse el guardado de la atención médica, el sistema activa internamente el servicio de generación de turnos ya desarrollado en el CU-06, generando una serie de turnos de seguimiento con estado `Pendiente` a lo largo de los seis (6) meses posteriores a la consulta, respetando estrictamente las disponibilidades configuradas del profesional y los bloques de 30 minutos sin solapamiento (RN-08, RN-10). Los turnos generados quedan vinculados a la atención médica que originó el seguimiento (`AtencionMedicaId`). Si en alguna de las fechas proyectadas no se encuentra disponibilidad dentro de la ventana de búsqueda (7 días) y en la jornada preferida, el sistema registrará todos los turnos que sí pudieron asignarse e informará cuáles fechas quedaron pendientes de coordinación manual. **La falta total o parcial de disponibilidad de horarios no impedirá guardar la atención médica**.

**Evaluación de aptitud clínica y vacunación (RN-03, RN-04):** La evaluación de aptitud se realiza dentro de la misma atención médica, inmediatamente antes de aplicar la vacuna, mediante una checklist clínica (sin fiebre, buen estado general, sin enfermedad infecciosa aguda y autorización profesional). La condición de APTO tiene validez únicamente para esa atención y ese mismo día, sin requerir la creación de una entidad independiente de «Alta Médica» ni el cómputo de fechas de vencimiento. Solo se puede registrar una vacuna cuando la mascota fue declarada APTO en la misma atención médica a la que se vincula la inoculación. Si el resultado es NO APTO, el sistema impide registrar la vacuna, pero **permite guardar normalmente la atención médica y asentar el motivo de la no aptitud clínica**. Una evaluación favorable de una consulta anterior no habilita automáticamente la aplicación de vacunas en consultas posteriores.

**Fecha de última visita (INF-07):** Al guardarse exitosamente la atención médica, el sistema actualiza automáticamente la fecha de última visita de la mascota en su información general e Historia Clínica utilizando la fecha de dicha atención. La atención médica y la actualización de la última visita deben guardarse de forma indivisible dentro de una misma transacción de base de datos. La fecha de última visita no debe retroceder si ya existe o se procesa una atención con fecha posterior.

**Registro de estudios y procedimientos quirúrgicos:** El Veterinario puede registrar estudios y sus resultados durante una atención médica. No se permite incorporar información a una atención previamente guardada. Si los resultados llegan posteriormente, deberán registrarse mediante una nueva atención médica (AMB-01). El registro de procedimientos quirúrgicos es opcional; cuando se registra, se consigna tipo, descripción y observaciones/complicaciones, quedando excluidos en esta primera entrega la firma y el consentimiento digital del dueño (INF-06).

**Inmutabilidad y control de abandono (RN-01, INF-04):** Una atención médica guardada no puede ser modificada ni eliminada bajo ninguna circunstancia. El sistema controla el abandono del formulario, advirtiendo al profesional ante modificaciones clínicas o quirúrgicas sin guardar para prevenir pérdidas involuntarias de información.

### 2. PRECONDICIONES
- El veterinario debe haber iniciado sesión y contar con un Token JWT activo con rol de `Veterinario`. A partir de los claims de dicho token (`sub` / `NameIdentifier`), el sistema obtiene de forma obligatoria el `VeterinarioId` actuante.
- La mascota debe encontrarse registrada en el sistema, en estado **Activa** (**RN-07**), y contar con una historia clínica vinculada.
- La Capa de Persistencia debe estar operativa y lista para transacciones ACID.

### 3. FLUJO PRINCIPAL (Camino Feliz - HTTP 201)
1. El Veterinario accede al formulario de "Nueva Atención" desde la historia clínica de una mascota activa y completa los datos clínicos obligatorios:
   - `mascotaId`
   - `motivoConsulta`
   - `anamnesis`
   - `examenFisico`
   - `diagnostico`
   - `tratamiento`
   - `pesoRegistrado` (valor decimal positivo)
   - `observaciones` (opcional)
   Y, de forma opcional:
   - Prescripciones farmacológicas (`prescripciones[]`).
   - Estudios complementarios (`estudios[]`).
   - Procedimiento quirúrgico (`procedimientoQuirurgico`: `tipoProcedimiento`, `descripcion`, y opcionalmente `observacionesComplicaciones`).
   - Evaluación in situ de aptitud para vacunación (`evaluacionAptitud`: checklist clínica) y vacunas aplicadas (`vacunacion`: vacuna, lote, fecha próxima dosis —condicionado a resultado APTO—).
   - Parámetros de seguimiento crónico ante diagnóstico de enfermedad crónica (`seguimientoCronico`: `frecuencia`, `jornadaPreferida`).
   *(Nota: El identificador del profesional `veterinarioId` no se ingresa en el payload, ya que el sistema lo obtiene indefectiblemente de la sesión autenticada).*
   Una vez completada la carga, solicita registrar la atención médica enviando una petición al endpoint `POST /api/atenciones`.
2. La **Capa de Presentación** (`AtencionesController.CreateAtencion`) valida la estructura del payload y verifica que los campos obligatorios del DTO principal y de los sub-objetos estén completos (`AtencionCreateDTO`):
   - Valida que `motivoConsulta`, `anamnesis`, `examenFisico`, `diagnostico` y `tratamiento` no estén vacíos.
   - Valida que `pesoRegistrado` sea un valor numérico estrictamente mayor a cero.
   - Si se incluyó procedimiento quirúrgico, valida que contenga `tipoProcedimiento` y `descripcion`.
   - Si se incluyó solicitud de seguimiento crónico, valida que contenga una `frecuencia` válida (`Quincenal`, `Mensual`, `Bimestral`, `Trimestral`) y una `jornadaPreferida` válida (`Manana`, `Tarde`, `Indistinta`).
   - Si se incluyó prescripción, valida medicamento, dosis, frecuencia y duración.
3. La **Capa de Negocio** (`AtencionService.CreateAtencionAsync`):
   a. Valida la existencia de la mascota y comprueba que se encuentre en estado **Activa** (**RN-07**).
   b. Extrae el `VeterinarioId` del veterinario autenticado desde los claims del token JWT, asignándolo como profesional de la atención y como cirujano responsable si se registró un procedimiento quirúrgico.
   c. Registra la fecha y hora de realización automáticamente correspondiente al momento del acto médico, asegurando la trazabilidad clínica (**RN-02**).
   d. **Cálculo de variación de peso y alerta de desnutrición (Nivel 1 / RN-12):**
      - Consulta la atención médica inmediatamente anterior de la mascota para obtener su `PesoRegistrado`.
      - Si no existen atenciones previas registradas, toma como peso de referencia el consignado en la ficha de la mascota (`Mascota.Peso`), siempre que sea un valor válido (> 0).
      - Si no existe un peso anterior válido, no calcula variación ni genera alerta (`AlertaDesnutricion = false`).
      - Si existe un peso de referencia válido, calcula el porcentaje de disminución:
        $$\text{PorcentajeDisminucion} = \frac{\text{PesoReferencia} - \text{PesoRegistrado}}{\text{PesoReferencia}} \times 100$$
      - Si la disminución es estrictamente superior al 10 % ($\text{PorcentajeDisminucion} > 10.0\%$), activa `AlertaDesnutricion = true`, asienta el `PorcentajeDisminucionPeso` y el `PesoReferenciaUtilizado`, y compone el mensaje de alerta clínica informativa. La alerta no interrumpe ni impide continuar con el registro de la atención médica.
   e. **Evaluación in situ de aptitud clínica y vacunación (RN-03, RN-04):**
      - Si se incluye la evaluación mediante la checklist clínica, valida y registra el resultado (`"APTO"` o `"NO_APTO"`), el cual posee validez exclusivamente para esta atención y este día (**RN-03**).
      - Si se incluye el registro de una vacuna aplicada, comprueba que la evaluación de aptitud en esta misma atención médica marque estrictamente resultado `"APTO"` (**RN-04**). Si el resultado fue `"NO_APTO"` o no se evaluó la aptitud, lanza la excepción `PacienteNoAptoVacunacionException`.
      - Si el paciente resultó `"NO_APTO"` pero el profesional no ingresó vacunas a aplicar, se permite continuar normalmente registrando la consulta y guardando el resultado y motivo de la no aptitud.
   f. **Actualización de Fecha de Última Visita (INF-07):**
      - Evalúa la fecha de la consulta actual respecto a la `FechaUltimaVisita` registrada en `Mascota`.
      - Aplica la regla de no retroceso: actualiza `FechaUltimaVisita` al valor de la fecha actual si no había visitas previas o si la fecha actual es posterior; preserva la fecha más reciente si ya existiera una atención con fecha posterior.
   g. **Seguimiento de enfermedades crónicas (Nivel 3 / RN-11 / CU-06):**
      - Si la atención incluye solicitud de seguimiento crónico, invoca internamente el servicio `ITurnoService.GenerarTurnosSeguimientoCronicoAsync` del CU-06.
      - El servicio busca y genera turnos con estado `Pendiente` a lo largo de seis meses respetando la frecuencia, la jornada preferida y la disponibilidad del veterinario (bloques de 30 minutos sin solapamiento, RN-08, RN-10).
      - Si alguna fecha no pudo ser asignada por falta de disponibilidad horaria dentro de la ventana permitida, el servicio genera los turnos que sí obtuvieron cupo y reporta el listado de fechas no asignadas para coordinación manual (`fechasPendientesManual[]`). La falta de disponibilidad no impide guardar la atención médica.
4. La **Capa de Persistencia** ejecuta una transacción ACID indivisible en base de datos (`DbContext.SaveChangesAsync`):
   - Crea el registro inmutable en `Atenciones` (**RN-01**), almacenando el profesional autenticado, peso, estado de alerta de desnutrición, porcentaje de variación, peso de referencia utilizado, motivo, anamnesis, examen físico, diagnóstico y tratamiento.
   - Actualiza la fecha de última visita en la entidad `Mascotas` como parte de la misma transacción (INF-07).
   - Guarda en cascada las prescripciones, solicitudes de estudio, el procedimiento quirúrgico (si fue ingresado) y el resultado de la evaluación de aptitud y vacunas aplicadas (**RN-02**).
   - Persiste en la tabla `Turnos` la serie de turnos de seguimiento generados vinculados a la `AtencionMedicaId` (**RN-11**).
5. El Sistema devuelve un código **201 Created** con el detalle completo de la atención registrada (`AtencionMedicaResponseDTO`), incluyendo:
   - Identificador generado de la atención e identificación del profesional autenticado.
   - Datos clínicos, peso registrado y resultado de la alerta de desnutrición (`alertaDesnutricion`, `porcentajeDisminucionPeso`, `pesoReferenciaUtilizado`, `mensajeAlerta`).
   - Confirmación de la fecha de última visita actualizada en la mascota.
   - Detalle de prescripciones, estudios y procedimiento quirúrgico registrados.
   - Resultado de aptitud y vacunas aplicadas (si correspondió).
   - Lista de turnos de seguimiento crónico programados (`turnosSeguimiento[]`) y fechas que quedaron pendientes para coordinación manual (`fechasPendientesManual[]`).

### 4. FLUJOS ALTERNATIVOS (Caminos Tristes / Excepciones)

* **1a. JSON inválido o malformado (HTTP 400 Bad Request):**
  1. Si en el Paso 1 el cuerpo de la petición contiene sintaxis inválida o campos con tipos no coincidentes.
  2. El Sistema (Capa de Presentación / model binding) rechaza la petición por falla en el model binding.
  3. El Sistema devuelve un código **400 Bad Request**. Fin del caso de uso.

* **2a. Campos obligatorios de atención faltantes (HTTP 400 Bad Request):**
  1. Si en el Paso 2 faltan campos obligatorios (`motivoConsulta`, `anamnesis`, `examenFisico`, `diagnostico` o `tratamiento`) o llegan vacíos.
  2. La Capa de Presentación rechaza la petición (`ModelState.IsValid == false`).
  3. El Sistema devuelve un código **400 Bad Request** con la lista de campos faltantes. Fin del caso de uso.

* **2b. Datos de prescripción incompletos (HTTP 400 Bad Request):**
  1. Si en el Paso 2 se incluye un objeto de prescripción farmacológica pero carece de `medicamento`, `dosis`, `frecuencia` o `duracion`.
  2. La Capa de Presentación rechaza la solicitud por validación de esquema en la colección de prescripciones.
  3. El Sistema devuelve un código **400 Bad Request** indicando: `"Los campos de la prescripción médica son obligatorios."`. Fin del caso de uso.

* **2c. Información de procedimiento quirúrgico incompleta (HTTP 400 Bad Request):**
  1. Si en el Paso 2 el Veterinario selecciona o incluye el registro de un procedimiento quirúrgico pero omite el tipo de procedimiento (`tipoProcedimiento`) o su descripción (`descripcion`).
  2. La Capa de Presentación rechaza la solicitud por validación de esquema en el sub-objeto de procedimiento quirúrgico (`ModelState.IsValid == false`).
  3. El Sistema devuelve un código **400 Bad Request** informando qué datos requeridos del procedimiento están incompletos (`"El tipo de procedimiento quirúrgico y la descripción de la intervención son obligatorios."`).
  4. El sistema conserva los datos en el formulario permitiendo completarlos antes de guardar, sin registrar un procedimiento quirúrgico incompleto. Fin del caso de uso.

* **2d. Información de seguimiento crónico incompleta o inválida (HTTP 400 Bad Request):**
  1. Si en el Paso 2 se solicita seguimiento para enfermedad crónica pero se omite la frecuencia o la jornada preferida, o se envían valores no válidos para dichos campos.
  2. La Capa de Presentación rechaza la petición por validación de esquema en el sub-objeto de seguimiento crónico (`ModelState.IsValid == false`).
  3. El Sistema devuelve un código **400 Bad Request** indicando los campos requeridos (`"La frecuencia de seguimiento y la jornada preferida son obligatorias para el seguimiento de enfermedad crónica."`). Fin del caso de uso.

* **2e. Peso registrado ausente o menor/igual a cero (HTTP 400 Bad Request):**
  1. Si en el Paso 2 se omite el campo `pesoRegistrado` o se envía un valor menor o igual a cero (`pesoRegistrado <= 0`).
  2. La Capa de Presentación rechaza la solicitud (`ModelState.IsValid == false`).
  3. El Sistema devuelve un código **400 Bad Request** con el mensaje: `"El peso registrado de la mascota es obligatorio y debe ser mayor a cero."`. Fin del caso de uso.

* **3a. Mascota o Historia Clínica no encontrada (HTTP 404 Not Found):**
  1. Si en el Paso 3 el `mascotaId` no corresponde a ninguna mascota registrada en el sistema.
  2. La Capa de Negocio frena la ejecución y lanza `MascotaNotFoundException`.
  3. El Sistema devuelve un código **404 Not Found**. Fin del caso de uso.

* **3b. Intento de registrar vacuna con paciente evaluado NO APTO o sin evaluación (HTTP 409 Conflict):**
  1. Si en el Paso 3 se intenta registrar una vacuna pero la evaluación de aptitud clínica en esta consulta arroja resultado `"NO APTO"` o no fue efectuada, violando la regla **RN-04**.
  2. La Capa de Negocio frena el registro de la vacuna y lanza la excepción `PacienteNoAptoVacunacionException`. (Una evaluación APTO de una consulta previa no tiene validez y no habilita la inoculación).
  3. El Sistema devuelve un código **409 Conflict** con el mensaje: `"No es posible registrar la vacuna: el paciente no cumple con las condiciones clínicas de aptitud (resultado NO APTO o evaluación ausente en la consulta actual)."`. *(Nota: Si el profesional decide no aplicar la vacuna ante una evaluación no apta, la atención médica puede registrarse y guardarse exitosamente conservando el resultado NO APTO y asentando el motivo).* Fin del caso de uso.

* **3c. Intento de registrar atención médica en mascota inactiva (HTTP 409 Conflict):**
  1. Si en el Paso 3 se detecta que la mascota se encuentra en estado `"Inactiva"`, violando la regla **RN-07**.
  2. La Capa de Negocio frena la operación y lanza la excepción `MascotaInactivaException`.
  3. El Sistema devuelve un código **409 Conflict** con el mensaje: `"No es posible registrar una nueva atención médica para una mascota inactiva."`. Fin del caso de uso.

* **3d. Falta de disponibilidad para turnos de seguimiento crónico (Flujo tolerante / HTTP 201 Created):**
  1. Si durante la ejecución del mecanismo de generación de turnos crónicos (CU-06) para una o más fechas proyectadas no se encuentran bloques disponibles de 30 minutos dentro del rango de 7 días y en la jornada preferida.
  2. El sistema no fuerza la asignación en horarios indebidos ni conmuta automáticamente entre turnos de mañana y tarde cuando se indicó una preferencia explícita.
  3. El sistema registra todos los turnos que sí pudieron ser asignados y recopila las fechas que no pudieron cubrirse.
  4. La transacción de la atención médica no se interrumpe y se guarda con éxito (HTTP 201 Created).
  5. La respuesta informa los turnos asignados y la lista de fechas que requieren coordinación manual (`fechasPendientesManual[]`). Fin del flujo.

* **4a. Error al guardar / Falla en la persistencia (HTTP 500 Internal Server Error):**
  1. Si en el Paso 4 ocurre un error durante la transacción de persistencia en base de datos (rollback automático de la transacción) o fallo de comunicación con el servidor.
  2. El middleware de excepciones intercepta el fallo y el Sistema devuelve un código **500 Internal Server Error**.
  3. El sistema informa al Veterinario mediante un mensaje de error que la atención médica no pudo guardarse, evitando cualquier mensaje falso o confirmación indebida de registro.
  4. La transacción en base de datos se revierte íntegramente (rollback automático); la fecha de última visita de la mascota no se actualiza (permanece intacta con su valor previo o como «Sin visitas registradas»). El sistema conserva intactos en el formulario todos los datos clínicos y quirúrgicos previamente ingresados (motivo, anamnesis, examen físico, diagnóstico, tratamiento, observaciones, prescripciones, estudios, procedimiento quirúrgico y checklists).
  5. El Veterinario permanece en el formulario y puede revisar la información cargada, corregir datos o reintentar el guardado sin perder los datos ingresados. No se emite confirmación falsa de registro, no se actualiza erróneamente la fecha de última visita y no queda registrada una atención o procedimiento quirúrgico parcial como resultado exitoso.

* **A1 — Abandonar atención con cambios sin guardar:**
  1. Durante el Paso 1, el Veterinario ingresa o modifica información clínica o quirúrgica en el formulario de atención médica.
  2. Antes de guardar, el Veterinario intenta abandonar el formulario mediante una acción de navegación controlada por el sistema (regresar a la pantalla anterior, seleccionar otra sección del sistema, seleccionar otra mascota o historia clínica, cancelar el registro de la atención o cerrar el formulario de registro).
  3. El sistema detecta que existen cambios o datos ingresados sin guardar.
  4. El sistema muestra un mensaje de confirmación con la siguiente advertencia:
     - **Título:** `¿Desea salir sin guardar?`
     - **Mensaje:** `Los datos ingresados se perderán si abandona esta atención.`
     - **Opciones:** `Continuar editando` | `Salir sin guardar`
  5. El Veterinario selecciona una de las opciones disponibles:
     - **Si selecciona "Continuar editando" (o cierra la advertencia sin seleccionar una opción):**
       6. El sistema cierra la advertencia.
       7. El Veterinario permanece en el formulario de atención médica.
       8. Los datos ingresados se conservan intactos. No se registra ni se descarta la atención ni el procedimiento quirúrgico. El Veterinario puede continuar la carga o solicitar el guardado.
     - **Si selecciona "Salir sin guardar":**
       6. El sistema descarta los datos no guardados.
       7. El Veterinario abandona el formulario y se redirige a la sección o pantalla solicitada.
       8. La atención médica no queda registrada en el sistema. No se generan registros clínicos ni quirúrgicos a partir de los datos descartados, y la fecha de última visita de la mascota permanece sin modificaciones. Fin del caso de uso.

* **A2 — Salir sin cambios pendientes:**
  1. Durante el Paso 1, el Veterinario intenta abandonar el formulario de atención médica (mediante cualquiera de las acciones de navegación controladas) sin haber ingresado ni modificado información.
  2. El sistema detecta que no existen cambios pendientes ni información cargada en el formulario.
  3. El sistema permite salir inmediatamente hacia la sección o pantalla solicitada, sin mostrar la advertencia.
  4. La atención médica no queda registrada. Fin del caso de uso.

### 5. SUB-VARIACIONES (opcional)
1. **Atención médica simple (sin cirugía):** Contiene únicamente la evaluación clínica, diagnóstico y tratamiento ambulatorio sin medicación especial, vacunas, estudios ni procedimientos quirúrgicos. El Veterinario no selecciona registrar cirugía y el sistema guarda la atención normalmente sin información quirúrgica.
2. **Atención médica con alerta de desnutrición detectada (Nivel 1):** El peso registrado de la mascota presenta una disminución estrictamente superior al 10 % respecto al peso de la consulta inmediatamente anterior (o de la ficha de la mascota). El sistema activa `AlertaDesnutricion = true`, almacena el porcentaje de disminución y el peso de referencia utilizado en el registro histórico, e incluye la advertencia clínica en la respuesta sin impedir el guardado de la atención médica (**RN-12**).
3. **Atención médica con prescripción múltiple:** Permite agregar una lista con múltiples medicamentos recetados con sus respectivas dosis y frecuencias.
4. **Atención médica con solicitud de estudios:** Permite solicitar estudios diagnósticos (radiografías, análisis de sangre, ecografías) o adjuntar resultados existentes. El Veterinario puede registrar estudios y sus resultados durante una atención médica. No se permite incorporar información a una atención previamente guardada. Si los resultados llegan posteriormente, deberán registrarse mediante una nueva atención médica (AMB-01).
5. **Atención médica con evaluación de aptitud y vacunación:** El profesional completa la checklist clínica in situ inmediatamente antes de la aplicación. Al resultar `"APTO"` (validez circunscripta a esa consulta y día), el sistema habilita el registro de la vacuna aplicada (vacuna, lote, fecha de próxima dosis), persistiendo ambos registros vinculados a la atención (**RN-03**, **RN-04**).
6. **Atención médica con paciente evaluado NO APTO para vacunación:** El profesional completa la checklist clínica y el resultado es `"NO APTO"`. El sistema bloquea el registro de la vacunación pero permite guardar y finalizar normalmente la atención médica, asentando el resultado `"NO APTO"` y el motivo de no aptitud clínica en la historia clínica.
7. **Atención médica con procedimiento quirúrgico:** El Veterinario selecciona la opción de registrar una cirugía realizada durante la consulta, completando el tipo de procedimiento (ej. castración, sutura, extracción de tumor), la descripción de la intervención y, de corresponder, observaciones o complicaciones. La fecha y el Veterinario responsable son asignados automáticamente por el sistema a partir de la atención actual. Al guardar, el procedimiento se registra vinculado a la atención y a la Historia Clínica (**RN-01**, **RN-02**).
8. **Atención médica con diagnóstico de enfermedad crónica y seguimiento programado:** Al registrar una patología crónica, el Veterinario indica la frecuencia de control y la jornada preferida, activándose de forma automática la generación de la serie de turnos de seguimiento durante los próximos seis meses a través de CU-06 (**RN-11**). Los turnos asignados quedan en estado `Pendiente` vinculados a la atención médica, y si existieran fechas sin cupo disponible, se reportan para coordinación manual sin impedir el guardado exitoso de la atención.

### 6. POSTCONDICIONES
- **Registro exitoso (Camino feliz):**
  - La atención médica queda registrada de forma inmutable (**RN-01**) en la tabla `Atenciones`, pasando a formar parte formal de la historia clínica de la mascota, identificando al veterinario autenticado, el peso registrado y el estado de la alerta de desnutrición (`AlertaDesnutricion`, porcentaje de disminución y peso de referencia) si correspondió (**RN-12**).
  - La fecha de última visita de la mascota se actualiza automáticamente en su ficha y en la Historia Clínica con la fecha de la atención guardada (asegurando que represente la atención más reciente registrada). Si la mascota no registraba visitas previas («Sin visitas registradas»), queda fijada la fecha de esta primera atención. Si existiera una atención con fecha posterior, la fecha de última visita no retrocede (**INF-07**).
  - Las prescripciones, estudios y vacunas aplicadas (en caso de resultar `APTO`) quedan asociadas automáticamente a la atención y a la historia clínica (**RN-02**). No se crea ninguna entidad independiente de «Alta Médica» ni se establecen fechas de vencimiento de aptitud.
  - Si se registró un procedimiento quirúrgico, este queda persistido de forma inmutable (**RN-01**) vinculado a la atención médica y a la historia clínica de la mascota (**RN-02**), identificando al Veterinario responsable (extraído del token), fecha de realización, tipo, descripción y observaciones o complicaciones, sin posibilidad de modificación o eliminación posterior.
  - Si la atención médica incluye indicación de seguimiento por enfermedad crónica, quedan generados y persistidos los turnos de la serie a seis meses en estado `Pendiente` (CU-06), vinculados a la atención médica, al paciente y al profesional actuante (**RN-11**), reportando las fechas no cubiertas para su coordinación manual si hubo indisponibilidad parcial.
- **Salida o abandono del formulario (Flujos A1 / A2):**
  - Si el profesional abandona el formulario saliendo sin guardar o sin cambios pendientes, ningún dato clínico o quirúrgico es persistido en la base de datos ni formará parte de la historia clínica.
  - La atención descartada no se computa como atención realizada ni afecta los antecedentes históricos del paciente.
  - No se crea un registro quirúrgico a partir de información descartada.
  - La fecha de última visita de la mascota permanece inalterada.
- **Falla en el guardado (Flujo 4a):**
  - Ante una falla de persistencia o error de red, la transacción se revierte íntegramente; no se registra una atención incompleta ni un procedimiento quirúrgico parcial, la fecha de última visita de la mascota no se actualiza, y los datos clínicos y quirúrgicos permanecen cargados en el formulario para permitir la corrección o un nuevo intento de guardado por parte del Veterinario.

---

## Anexo: matrices de referencia

### Códigos HTTP usados

| Código HTTP | Nombre Técnico | Contexto de Aplicación en el Caso de Uso |
| --- | --- | --- |
| `201` | Created | Registro exitoso de la atención médica (con o sin vacunas/estudios/prescripciones/procedimiento quirúrgico/evaluación/alerta de desnutrición/turnos de seguimiento crónico) y actualización atómica de última visita. |
| `400` | Bad Request | Faltan datos clínicos obligatorios, peso registrado ausente o menor/igual a cero (`pesoRegistrado <= 0`), prescripción incompleta, datos quirúrgicos incompletos o datos de seguimiento crónico inválidos/incompletos. |
| `404` | Not Found | La mascota especificada no existe en la Capa de Persistencia. |
| `409` | Conflict | Violación de regla de negocio RN-04 (vacunación sin aptitud APTO en la consulta actual) o RN-07 (intento de registrar atención en mascota inactiva). |
| `500` | Internal Server Error | Error no controlado durante la transacción de guardado en la base de datos (rollback automático completo). |

### Identificación del Profesional desde la Sesión Autenticada

- **Extracción forzada desde token JWT:** El identificador del veterinario responsable (`VeterinarioId`) se extrae de forma obligatoria desde los claims del token JWT de la sesión autenticada (`ClaimTypes.NameIdentifier` / claim `sub`).
- **Inviolabilidad de autoría médica:** El cliente HTTP o usuario de la interfaz no envía el identificador del veterinario en el cuerpo del payload (`AtencionCreateDTO`). Esto garantiza que ningún usuario pueda atribuir indebidamente un acto clínico o procedimiento quirúrgico a otro colega.
- **Asignación quirúrgica unificada:** Cuando la atención incorpora un procedimiento quirúrgico opcional, el veterinario autenticado se asigna automáticamente como el cirujano responsable de dicha intervención sin requerir selección adicional.

### Nivel 1: Detección y Registro Histórico de Alerta de Desnutrición (RN-12)

- **Algoritmo de comparación y jerarquía de peso de referencia:**
  1. *Atención médica anterior:* El sistema busca la atención médica inmediatamente anterior registrada para la mascota (ordenada por fecha descendente) y toma su `PesoRegistrado`.
  2. *Peso de referencia de la ficha:* Si la mascota no registra consultas médicas previas, el sistema utiliza como base el peso inicial registrado en la ficha del paciente (`Mascota.Peso`), siempre que dicho valor sea positivo y válido (> 0).
  3. *Ausencia de peso previo válido:* Si la mascota no tiene atenciones previas y su ficha carece de un peso de referencia válido (0 o nulo), el sistema no calcula porcentaje de variación ni genera alerta de desnutrición.
- **Cálculo de la variación:**
  $$\text{PorcentajeDisminucion} = \frac{\text{PesoReferencia} - \text{PesoRegistrado}}{\text{PesoReferencia}} \times 100$$
- **Criterio de activación:**
  - Cuando la disminución de peso sea estrictamente superior al 10 % ($\text{PorcentajeDisminucion} > 10.0\%$), se activa automáticamente `AlertaDesnutricion = true`.
  - Si la disminución es igual o menor al 10 %, o si el paciente aumentó de peso, `AlertaDesnutricion` permanece en `false`.
- **Preservación histórica inmutable:** Los datos de la alerta (`AlertaDesnutricion`, `PorcentajeDisminucionPeso` y `PesoReferenciaUtilizado`) se almacenan de forma permanente e inmutable en la entidad `AtencionMedica`, asegurando su disponibilidad para consultas posteriores en la Historia Clínica y auditorías sanitarias.
- **Naturaleza no bloqueante:** La alerta de desnutrición constituye una herramienta clínica de advertencia preventiva; en ningún caso obstaculiza, rechaza ni interrumpe el guardado exitoso de la atención médica (código HTTP 201 Created).

### Nivel 3: Seguimiento Crónico y Tolerancia ante Indisponibilidad de Agenda (RN-11 / CU-06)

- **Activación integrada e indivisible:** La generación de la serie de turnos de seguimiento para patologías crónicas se activa automáticamente al confirmarse el guardado de la atención médica. El Veterinario consigna la indicación seleccionando la frecuencia (quincenal, mensual, bimestral o trimestral) y la jornada preferida del dueño (mañana, tarde o indistinta) dentro del mismo formulario de consulta.
- **Delegación de mecanismo al CU-06:** CU-04 delega la búsqueda de disponibilidad y la reserva de los bloques de 30 minutos al servicio interno de turnos (`ITurnoService.GenerarTurnosSeguimientoCronicoAsync`), vinculando la `AtencionMedicaId` a cada turno generado. No requiere que el personal ejecute un endpoint independiente ni tareas manuales adicionales.
- **Respeto a las reglas de agenda del CU-06:**
  - Se proyectan fechas de control a lo largo de un horizonte de seis (6) meses desde la fecha de atención.
  - La búsqueda se restringe estrictamente a bloques de 30 minutos sin solapamiento (RN-08) dentro de los días y horarios habilitados del veterinario (RN-10).
  - La búsqueda respeta la jornada elegida y nunca cambia automáticamente de mañana a tarde ni viceversa ante una preferencia explícita.
  - La búsqueda hacia adelante tiene un límite máximo de siete (7) días corridos desde la fecha ideal proyectada.
- **Política de tolerancia y reporte de turnos pendientes:**
  - Si en alguna o todas las fechas de la serie no se consigue cupo dentro del margen de 7 días respetando la jornada preferida, el sistema registra todos los turnos que sí hayan obtenido disponibilidad.
  - La falta total o parcial de disponibilidad horaria **no impide ni bloquea el guardado exitoso de la atención médica**.
  - La respuesta HTTP 201 Created informa el listado de turnos programados (`turnosSeguimiento[]`) y detalla expresamente las fechas que no pudieron cubrirse (`fechasPendientesManual[]`), permitiendo al personal administrativo coordinar manualmente esas citas específicas sin perder los datos de la consulta ni los turnos asignados del plan.

### Evaluación de Aptitud para Vacunación y Descarte de Entidad Externa de Alta Médica (RN-03, RN-04)

- **Evaluación clínica in situ:** La evaluación de aptitud se realiza dentro de la misma atención médica inmediatamente antes del acto de inoculación mediante una checklist clínica (sin fiebre, buen estado general, sin enfermedad infecciosa aguda y autorización profesional).
- **Validez temporal estricta:** La condición de APTO resultante posee vigencia exclusiva para esa atención médica y ese mismo día calendario.
- **Descarte de entidad independiente de "Alta Médica":** No se requiere crear una entidad separada de «Alta Médica» ni modelar fechas de expiración o tablas satélite de altas clínicas en la base de datos.
- **Condición excluyente para vacunar:** Solo se puede registrar una vacuna cuando la mascota fue declarada APTO en la misma atención médica a la que se vincula la inoculación (**RN-04**).
- **No retroactividad:** Una evaluación APTO registrada en una consulta previa no habilita automáticamente la aplicación de vacunas en atenciones posteriores; cada acto vacunal exige una nueva evaluación clínica in situ.
- **Manejo de resultado NO APTO:** Si el paciente resulta NO APTO (o no fue evaluado), el sistema bloquea estrictamente el registro de la vacuna (HTTP 409 Conflict ante intento de guardarla con vacuna), pero permite al profesional finalizar y guardar normalmente la atención médica sin vacunas, conservando en la Historia Clínica el resultado NO APTO y el motivo de la no aptitud como antecedente clínico relevante.

### Actualización Automática de la Fecha de Última Visita y Regla de No Retroceso (INF-07)

- **Operación atómica e indivisible:** La actualización de la fecha de última visita de la mascota se ejecuta de forma automática dentro de la misma transacción ACID de guardado de la atención médica (CU-04). No requiere ninguna acción adicional del Veterinario ni admite modificación manual.
- **Primera atención del paciente:** En mascotas que no cuentan con atenciones previas en su historial, la fecha de última visita figura como «Sin visitas registradas». Al guardarse con éxito su primera consulta médica, se establece automáticamente la fecha de dicha atención.
- **Consistencia temporal y regla estricta de no retroceso:** La fecha de última visita siempre representa la atención más reciente cronológicamente. Si se procesa o registra una atención médica con fecha anterior a una ya existente en el sistema, la fecha de última visita no debe retroceder, preservando en todo momento la fecha de la visita más reciente de la mascota.
- **Preservación ante fallas o abandono:** Si ocurre un error de guardado (Flujo 4a) o el Veterinario confirma el abandono del formulario sin guardar (Flujo A1), la fecha de última visita de la mascota permanece estrictamente inalterada.
- **Cumplimiento de RN-01:** La actualización de la fecha de última visita es un metadato de estado en la entidad mascota y no modifica ni altera el contenido inmutable de las atenciones médicas previamente guardadas en la Historia Clínica, respetando **RN-01**.

### Registro Quirúrgico y Exclusiones de Alcance (INF-06)

- **Integración del procedimiento quirúrgico:** Los procedimientos quirúrgicos se registran exclusivamente como parte opcional de una atención médica (CU-04). Quedan vinculados a la mascota, a su Historia Clínica, a la atención médica en la que se realizaron y al Veterinario responsable actuante. No existe un caso de uso independiente ni un módulo separado de cirugías.
- **Campos aprobados:** Tipo de procedimiento, fecha de realización (automática), descripción de la intervención, Veterinario responsable (automático a partir del usuario autenticado) y observaciones/complicaciones (opcional). El diagnóstico y tratamiento pertenecen a la atención general y no se duplican en el registro quirúrgico.
- **Exclusión de gestión quirúrgica compleja:** No se incluye gestión de quirófanos, anestesia, insumos, costos, facturación ni programación quirúrgica independiente.
- **Exclusión de consentimiento digital y firma:** En esta primera entrega (MVP), el consentimiento digital del dueño, la captura de firma digital o electrónica, el almacenamiento de documentos firmados y la validación digital de autorizaciones quedan expresamente **excluidos del alcance de software** y previstos para una etapa futura. Esta exclusión técnica no exime ni altera las obligaciones legales o profesionales que competen al ejercicio médico veterinario.

### Consideraciones de Navegación y Limitaciones Técnicas

- **Acciones de navegación controladas:** La advertencia modal de abandono con datos sin guardar aplica ante eventos de navegación dentro de la aplicación gestionados por el sistema:
  - Regresar a la pantalla anterior.
  - Seleccionar otra sección o módulo del sistema.
  - Seleccionar otra mascota o consultar otra Historia Clínica.
  - Cancelar el registro de la atención mediante el botón correspondiente.
  - Cerrar el formulario de registro.
- **Limitaciones técnicas ante eventos externos:** El sistema no cuenta con autoguardado periódico ni recuperación de borradores ante acciones imprevistas fuera del control del ciclo de navegación de la aplicación (tales como cierre forzado de pestaña o ventana del navegador, recarga manual de página `F5` / `Ctrl+R` o pérdida de conexión de red/energía). No se promete recuperación automática de datos bajo estas contingencias, quedando señaladas dichas limitaciones para su evaluación técnica futura.

### Nota: Validación vs. Verificación aplicada

- **Validación (Presentación, → 400 / UI):** Se validan formatos de texto, presencia de campos obligatorios clínicos (`motivoConsulta`, `diagnostico`, `tratamiento`), obligatoriedad de `pesoRegistrado` con valor numérico mayor a cero (`pesoRegistrado > 0`), campos requeridos del procedimiento quirúrgico (`tipoProcedimiento`, `descripcion`), valores admitidos para la frecuencia y jornada de seguimiento crónico, esquemas válidos en las colecciones hijas (`PrescripcionDTO`, `EstudioDTO`) y la detección de cambios pendientes en el formulario antes de la navegación.
- **Verificación (Negocio, → 404/409):** Comprobación de existencia y estado activo de la mascota (**RN-07**), extracción e imputación segura del `VeterinarioId` del profesional autenticado desde el JWT, cálculo jerárquico de variación de peso y activación de alerta de desnutrición (**RN-12**), evaluación in situ de aptitud clínica (**RN-03**), verificación estricta de la aptitud para vacunación (`APTO` en la consulta actual) antes de habilitar el registro de la vacuna (**RN-04**), persistencia inmutable de la atención médica y sus procedimientos asociados (**RN-01**), invocación del mecanismo de generación de turnos crónicos con tolerancia a indisponibilidad de agenda (**RN-11**) y actualización automática atómica de la fecha de última visita de la mascota aplicando la regla de no retroceso (**INF-07**).

### Matriz de trazabilidad CU-04 → Test

| Paso del CU / Escenario | Excepción / Código | Test unitario (BusinessLogic) | Test integración (HTTP / UI) |
| --- | --- | --- | --- |
| Flujo principal (Apto + Vacuna) | `201 Created` | `CreateAtencionAsync_WithCompleteData_SavesAtencionAndRelatedEntities` | `CreateAtencion_WithValidData_Returns201Created` |
| Flujo principal (Evaluado NO APTO sin vacuna) | `201 Created` | `CreateAtencionAsync_WhenVaccinationUnfitWithoutVaccine_SavesAtencionWithUnfitEvaluation` | `CreateAtencion_WhenVaccinationUnfitWithoutVaccine_Returns201Created` |
| Flujo principal (Con procedimiento quirúrgico) | `201 Created` | `CreateAtencionAsync_WithSurgicalProcedure_SavesAtencionAndProcedure` | `CreateAtencion_WithValidSurgicalProcedure_Returns201Created` |
| Flujo principal (Actualización última visita) | `201 Created` | `CreateAtencionAsync_WhenSaved_UpdatesMascotaFechaUltimaVisita` | `CreateAtencion_WhenSaved_Returns201AndUpdatesFechaUltimaVisita` |
| Flujo principal (No retroceso de última visita) | `201 Created` | `CreateAtencionAsync_WhenOlderThanCurrentLastVisit_DoesNotRetrocedeFechaUltimaVisita` | `CreateAtencion_WhenPastDate_PreservesNewerFechaUltimaVisita` |
| Flujo principal (Identificación profesional JWT) | `201 Created` | `CreateAtencionAsync_SetsVeterinarioIdFromAuthenticatedUserClaims` | `CreateAtencion_ExtractsVeterinarioIdFromJwtTokenClaims` |
| Flujo principal (Alerta de desnutrición > 10% vs previa) | `201 Created` | `CreateAtencionAsync_WhenWeightDecreasesOverTenPercent_SetsAlertaDesnutricionTrue` | `CreateAtencion_WhenWeightDropOverTenPercent_Returns201WithAlertaDesnutricion` |
| Flujo principal (Alerta de desnutrición > 10% vs ficha) | `201 Created` | `CreateAtencionAsync_WhenFirstAtencionAndWeightDropOverTenPercentVsFicha_SetsAlertaTrue` | `CreateAtencion_WhenWeightDropVsFicha_Returns201WithAlerta` |
| Flujo principal (Sin alerta con disminución <= 10%) | `201 Created` | `CreateAtencionAsync_WhenWeightDecreaseTenPercentOrLess_SetsAlertaFalse` | `CreateAtencion_WhenNormalWeightVariation_Returns201WithAlertaFalse` |
| Flujo principal (Sin cálculo de alerta por falta de peso previo) | `201 Created` | `CreateAtencionAsync_WhenNoPreviousWeightAvailable_DoesNotCalculateVariationNorAlerta` | `CreateAtencion_WithoutPreviousWeight_Returns201WithAlertaFalse` |
| Flujo principal (Con seguimiento crónico completo) | `201 Created` | `CreateAtencionAsync_WithChronicDisease_GeneratesFollowUpTurnosSeries` | `CreateAtencion_WithChronicDisease_Returns201AndTurnosList` |
| Flujo 3d (Seguimiento crónico con cupos parciales) | `201 Created` | `CreateAtencionAsync_WhenPartialAvailabilityInFollowUp_SavesAtencionAndReportsPendingDates` | `CreateAtencion_WhenPartialTurnoAvailability_Returns201WithFechasPendientes` |
| 1a. JSON inválido | `400 Bad Request` | — (model binding de ASP.NET Core) | `CreateAtencion_WithMalformedPayload_Returns400BadRequest` |
| 2a. Campos clínicos faltantes | `400 Bad Request` | — (validación DataAnnotations) | `CreateAtencion_WithMissingDiagnostico_Returns400BadRequest` |
| 2b. Prescripción incompleta | `400 Bad Request` | — (validación DataAnnotations en DTO) | `CreateAtencion_WithIncompletePrescripcion_Returns400BadRequest` |
| 2c. Procedimiento quirúrgico incompleto | `400 Bad Request` | — (validación DataAnnotations en DTO quirúrgico) | `CreateAtencion_WithIncompleteSurgicalProcedure_Returns400BadRequest` |
| 2d. Seguimiento crónico incompleto | `400 Bad Request` | — (validación DataAnnotations en DTO de seguimiento) | `CreateAtencion_WithIncompleteChronicFollowUp_Returns400BadRequest` |
| 2e. Peso ausente o menor/igual a cero | `400 Bad Request` | — (validación DataAnnotations en DTO `pesoRegistrado > 0`) | `CreateAtencion_WithZeroOrNegativeWeight_Returns400BadRequest` |
| 3a. Mascota inexistente | `404 Not Found` | `CreateAtencionAsync_WhenMascotaNotExists_ThrowsMascotaNotFoundException` | `CreateAtencion_WhenMascotaNotExists_Returns404NotFound` |
| 3b. Vacuna en paciente no apto | `409 Conflict` | `CreateAtencionAsync_WithUnfitVaccination_ThrowsPacienteNoAptoVacunacionException` | `CreateAtencion_WhenVaccinationUnfit_Returns409Conflict` |
| 3b. Vacuna con evaluación APTO de consulta pasada | `409 Conflict` | `CreateAtencionAsync_WhenVaccinationAptoOnlyInPreviousVisit_ThrowsPacienteNoAptoVacunacionException` | `CreateAtencion_WhenAptoInPastVisit_Returns409Conflict` |
| 3c. Mascota inactiva | `409 Conflict` | `CreateAtencionAsync_WhenMascotaIsInactive_ThrowsMascotaInactivaException` | `CreateAtencion_WhenMascotaIsInactive_Returns409Conflict` |
| 4a. Error de persistencia / Reintento | `500 Internal Server Error` | `CreateAtencionAsync_WhenDbFails_ThrowsExceptionAndRollbacks` | `CreateAtencion_WhenDbFails_Returns500AndPreservesFormData` |
| A1. Salir con cambios pendientes | N/A (Modal UI) | — (Detección de estado 'dirty' / cambios pendientes clínicos y quirúrgicos) | `FormularioAtencion_AlIntentarSalirConCambios_MuestraAdvertenciaYPermiteDecidir` |
| A2. Salir sin cambios pendientes | N/A (Navegación UI) | — (Verificación de formulario limpio / sin modificaciones) | `FormularioAtencion_AlSalirSinCambios_NavegaSinAdvertencia` |

> Regla de oro: cada flujo del caso de uso debe tener al menos un test. En los flujos resueltos en la Capa de Presentación el test aplicable es el de integración HTTP o prueba de interfaz de usuario. Los tests se ejecutan con `dotnet test SistemaVeterinaria.slnx`.

