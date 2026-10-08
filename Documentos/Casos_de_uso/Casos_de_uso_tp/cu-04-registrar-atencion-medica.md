# Caso de Uso: Registrar Atención Médica

> Especificación elaborada siguiendo la guía `GUIA-Especificacion-Casos-de-Uso.md` (sección 3).
> Implementación del registro clínico con reglas de negocio RN-01 (inmutabilidad de atenciones médicas), RN-02 (integración de registros clínicos a la historia clínica), RN-03 (evaluación de aptitud para vacunación), RN-04 (vacunación condicionada a aptitud clínica) y RN-07 (restricción de atención en mascotas inactivas).
> Incorpora el control de navegación y advertencia de cambios sin guardar (INF-04).
> Incorpora el registro opcional de procedimientos quirúrgicos dentro de la atención médica y explicita la exclusión de consentimiento digital y firma en la primera entrega (INF-06).
> Incorpora la actualización automática de la fecha de última visita de la mascota al guardar exitosamente la atención médica (INF-07).
> Incorpora la aclaración sobre registro de estudios médicos y resultados diferidos (AMB-01).

| Campo | Valor |
| --- | --- |
| **ID del Caso de Uso** | CU-04 |
| **Nombre** | Registrar Atención Médica |
| **Actor Principal** | Veterinario/a |
| **Actores Secundarios** | Ninguno |
| **Alcance / Nivel** | Sistema; meta de usuario |
| **Stakeholders e intereses** | Veterinario/a → asentar de forma rigurosa el diagnóstico, evolución, prescripciones, estudios, procedimientos quirúrgicos y vacunaciones realizadas; Dueño de la Mascota → recibir indicaciones claras de tratamiento y constancia de procedimientos/vacunas aplicadas; Clínica Veterinaria → cumplir normativas de historia clínica inmutable y control sanitario |
| **Disparador (Trigger)** | El veterinario selecciona la opción "Nueva Atención" desde la historia clínica de una mascota |
| **Prioridad / Frecuencia** | Alta; muy alta frecuencia (se ejecuta en cada acto médico veterinario) |
| **Reglas de negocio relacionadas** | RN-01 (inmutabilidad de las atenciones médicas); RN-02 (integración de registros clínicos a la historia clínica); RN-03 (evaluación de aptitud para vacunación durante la atención médica); RN-04 (vacunación condicionada a aptitud clínica); RN-07 (mascotas inactivas no pueden registrar nuevas atenciones médicas) |

---

### 1. BREVE DESCRIPCIÓN
Permite al veterinario asentar una nueva atención médica en la historia clínica de una mascota activa, registrando el motivo de consulta, anamnesis, examen físico, diagnóstico, tratamiento y observaciones, con la posibilidad de extender la consulta agregando prescripciones de medicamentos, solicitud o adjunto de estudios complementarios, registro opcional de procedimientos quirúrgicos realizados durante la consulta, y de manera opcional la evaluación de aptitud para vacunación mediante checklist clínica (APTO/NO APTO) con el consecuente registro de vacunas aplicadas en caso de resultar apto.

El Veterinario puede registrar estudios y sus resultados durante una atención médica. No se permite incorporar información a una atención previamente guardada. Si los resultados llegan posteriormente, deberán registrarse mediante una nueva atención médica.

Al guardarse exitosamente la atención médica, el sistema actualiza de forma automática la fecha de última visita de la mascota en su información general e Historia Clínica utilizando la fecha de dicha atención (asegurando que siempre represente la atención más reciente por fecha y pasando de «Sin visitas registradas» a la fecha actual en caso de ser su primera atención), como parte de la misma transacción de guardado y sin requerir ninguna acción adicional ni manual por parte del Veterinario.

El registro de procedimientos quirúrgicos es **opcional**: no todas las consultas veterinarias incluyen una cirugía, y la ausencia de un procedimiento quirúrgico no impide registrar una atención médica común. Cuando se registra un procedimiento quirúrgico, se consigna el tipo de procedimiento, su descripción y observaciones/complicaciones (opcionales), asignándose automáticamente la fecha actual y al Veterinario responsable a partir del profesional autenticado. El diagnóstico y tratamiento no se duplican por pertenecer a la atención médica. El consentimiento digital y la firma del dueño quedan expresamente excluidos de esta primera entrega y previstos para una etapa futura.

El sistema controla el abandono del formulario durante la carga, advirtiendo al profesional cuando existen modificaciones sin guardar (tanto clínicas como quirúrgicas) para evitar la pérdida involuntaria de información, permitiendo decidir entre continuar editando o descartar los cambios. Si no existen modificaciones pendientes, permite salir sin advertencia. Asimismo, ante errores durante el guardado, el sistema conserva los datos ingresados para permitir correcciones y reintentos, sin registrar atenciones incompletas, sin actualizar erróneamente la fecha de última visita y sin alterar la inmutabilidad de las atenciones existentes.

### 2. PRECONDICIONES
- El veterinario debe haber iniciado sesión y contar con un Token JWT activo con rol de `Veterinario`.
- La mascota debe encontrarse registrada en el sistema, en estado **Activa** (**RN-07**), y contar con una historia clínica vinculada.
- La Capa de Persistencia debe estar operativa y lista para transacciones ACID.

### 3. FLUJO PRINCIPAL (Camino Feliz - HTTP 201)
1. El Veterinario accede al formulario de "Nueva Atención" desde la historia clínica de una mascota activa y completa los datos clínicos (`mascotaId`, `motivoConsulta`, `anamnesis`, `examenFisico`, `diagnostico`, `tratamiento`, `observaciones`) y, de forma opcional:
   - Prescripciones farmacológicas (`prescripciones[]`).
   - Estudios complementarios (`estudios[]`).
   - Procedimiento quirúrgico (`procedimientoQuirurgico`: `tipoProcedimiento`, `descripcion`, y opcionalmente `observacionesComplicaciones`).
   - Evaluación de aptitud para vacunación (`evaluacionAptitud`) y vacunas aplicadas (`vacunacion`).
   Una vez ingresada la información, solicita registrar la atención médica enviando una petición al endpoint `POST /api/atenciones`.
2. La **Capa de Presentación** (`AtencionesController.CreateAtencion`) valida la estructura del payload y verifica que los campos obligatorios del DTO principal y de los sub-objetos estén completos (`AtencionCreateDTO`). Si se incluyó procedimiento quirúrgico, valida que contenga obligatoriamente `tipoProcedimiento` y `descripcion`.
3. La **Capa de Negocio** (`AtencionService.CreateAtencionAsync`) valida la existencia de la mascota y comprueba que se encuentre en estado **Activa** (**RN-07**), valida su historia clínica, extrae el ID del veterinario autenticado desde los claims del token (asignándolo como profesional de la atención y como Veterinario responsable del procedimiento quirúrgico si lo hubiera), registra la fecha de realización automáticamente correspondiente a la atención actual, y asegura la trazabilidad clínica (**RN-02**). Si se incluye evaluación de aptitud para vacunación mediante la checklist clínica, valida y registra el resultado (`"APTO"` o `"NO_APTO"`) (**RN-03**). Si se incluye registro de vacuna, comprueba que la evaluación de aptitud marque estrictamente resultado `"APTO"` (**RN-04**). Asimismo, evalúa la actualización de la fecha de última visita de la mascota (`FechaUltimaVisita`) para que refleje la fecha de la atención actual (o preserve la más reciente en caso de existir registros previos con fecha posterior).
4. La **Capa de Persistencia** ejecuta una transacción en base de datos (`DbContext.SaveChangesAsync`), creando el registro inmutable en `Atenciones` (**RN-01**), actualizando automáticamente la fecha de última visita en la entidad `Mascotas` como parte indivisible de la misma transacción (estableciéndola por primera vez si la mascota no tenía visitas previas), y guardando en cascada las prescripciones, solicitudes de estudio, el procedimiento quirúrgico (si fue ingresado), el resultado de la evaluación de aptitud y las vacunas aplicadas vinculadas a dicha atención y a la historia clínica (**RN-02**).
5. El Sistema devuelve un código **201 Created** con el detalle completo de la atención registrada (`AtencionResponseDTO`), su identificador generado y la confirmación de la fecha de última visita actualizada en la mascota.

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

* **3a. Mascota o Historia Clínica no encontrada (HTTP 404 Not Found):**
  1. Si en el Paso 3 el `mascotaId` no corresponde a ninguna mascota registrada en el sistema.
  2. La Capa de Negocio frena la ejecución y lanza `MascotaNotFoundException`.
  3. El Sistema devuelve un código **404 Not Found**. Fin del caso de uso.

* **3b. Intento de registrar vacuna con paciente evaluado NO APTO o sin evaluación (HTTP 409 Conflict):**
  1. Si en el Paso 3 se intenta registrar una vacuna pero la evaluación de aptitud clínica arroja resultado `"NO APTO"` o no fue efectuada, violando la regla **RN-04**.
  2. La Capa de Negocio frena el registro de la vacuna y lanza la excepción `PacienteNoAptoVacunacionException`.
  3. El Sistema devuelve un código **409 Conflict** con el mensaje: `"No es posible registrar la vacuna: el paciente no cumple con las condiciones clínicas de aptitud (resultado NO APTO o evaluación ausente)."`. *(Nota: Si el profesional decide no aplicar la vacuna ante una evaluación no apta, la atención médica puede registrarse y guardarse exitosamente conservando el resultado NO APTO).* Fin del caso de uso.

* **3c. Intento de registrar atención médica en mascota inactiva (HTTP 409 Conflict):**
  1. Si en el Paso 3 se detecta que la mascota se encuentra en estado `"Inactiva"`, violando la regla **RN-07**.
  2. La Capa de Negocio frena la operación y lanza la excepción `MascotaInactivaException`.
  3. El Sistema devuelve un código **409 Conflict** con el mensaje: `"No es posible registrar una nueva atención médica para una mascota inactiva."`. Fin del caso de uso.

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
2. **Atención médica con prescripción múltiple:** Permite agregar una lista con múltiples medicamentos recetados con sus respectivas dosis y frecuencias.
3. **Atención médica con solicitud de estudios:** Permite solicitar estudios diagnósticos (radiografías, análisis de sangre, ecografías) o adjuntar resultados existentes. El Veterinario puede registrar estudios y sus resultados durante una atención médica. No se permite incorporar información a una atención previamente guardada. Si los resultados llegan posteriormente, deberán registrarse mediante una nueva atención médica.
4. **Atención médica con evaluación de aptitud y vacunación:** El profesional completa la checklist clínica (sin fiebre, buen estado general, sin enfermedad infecciosa aguda, autorización veterinaria). Al resultar `"APTO"`, el sistema habilita el registro de la vacuna aplicada (lote, vacuna, fecha de próxima dosis), persistiendo ambos registros.
5. **Atención médica con paciente evaluado NO APTO para vacunación:** El profesional completa la checklist clínica y el resultado es `"NO APTO"`. El sistema bloquea el registro de la vacunación pero permite guardar y finalizar normalmente la atención médica, asentando el resultado `"NO APTO"` de la evaluación en la historia clínica.
6. **Atención médica con procedimiento quirúrgico:** El Veterinario selecciona la opción de registrar una cirugía realizada durante la consulta, completando el tipo de procedimiento (ej. castración, sutura, extracción de tumor), la descripción de la intervención y, de corresponder, observaciones o complicaciones. La fecha y el Veterinario responsable son asignados automáticamente por el sistema a partir de la atención actual. Al guardar, el procedimiento se registra vinculado a la atención y a la Historia Clínica (**RN-01**, **RN-02**).

### 6. POSTCONDICIONES
- **Registro exitoso (Camino feliz):**
  - La atención médica queda registrada de forma inmutable (**RN-01**) en la tabla `Atenciones`, pasando a formar parte formal de la historia clínica de la mascota, incluyendo el resultado de la evaluación de aptitud si fue efectuada (`APTO` o `NO APTO`) (**RN-03**).
  - Las prescripciones, estudios y vacunas aplicadas (en caso de resultar `APTO`) quedan asociadas automáticamente a la atención y a la historia clínica (**RN-02**).
  - Si se registró un procedimiento quirúrgico, este queda persistido de forma inmutable (**RN-01**) vinculado a la atención médica y a la historia clínica de la mascota (**RN-02**), identificando al Veterinario responsable, fecha de realización, tipo, descripción y observaciones o complicaciones, sin posibilidad de modificación o eliminación posterior.
  - La fecha de última visita de la mascota se actualiza automáticamente en su ficha y en la Historia Clínica con la fecha de la atención guardada (asegurando que represente la atención más reciente registrada). Si la mascota no registraba visitas previas («Sin visitas registradas»), queda fijada la fecha de esta primera atención.
  - Si se aplicaron vacunas con aptitud aprobada, se actualizan los indicadores de última consulta y próximas vacunas de la mascota.
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
| `201` | Created | Registro exitoso de la atención médica (con o sin vacunas/estudios/prescripciones/procedimiento quirúrgico/evaluación) y actualización de última visita. |
| `400` | Bad Request | Faltan datos clínicos obligatorios, prescripción incompleta o datos obligatorios del procedimiento quirúrgico ausentes (`tipoProcedimiento`, `descripcion`). |
| `404` | Not Found | La mascota especificada no existe en la Capa de Persistencia. |
| `409` | Conflict | Violación de regla de negocio RN-04 (vacunación sin aptitud APTO) o RN-07 (intento de registrar atención en mascota inactiva). |
| `500` | Internal Server Error | Error no controlado durante la transacción de guardado en la base de datos. |

### Consideraciones de Navegación y Limitaciones Técnicas

- **Acciones de navegación controladas:** La advertencia modal de abandono con datos sin guardar aplica ante eventos de navegación dentro de la aplicación gestionados por el sistema:
  - Regresar a la pantalla anterior.
  - Seleccionar otra sección o módulo del sistema.
  - Seleccionar otra mascota o consultar otra Historia Clínica.
  - Cancelar el registro de la atención mediante el botón correspondiente.
  - Cerrar el formulario de registro.
- **Limitaciones técnicas ante eventos externos:** El sistema no cuenta con autoguardado periódico ni recuperación de borradores ante acciones imprevistas fuera del control del ciclo de navegación de la aplicación (tales como cierre forzado de pestaña o ventana del navegador, recarga manual de página `F5` / `Ctrl+R` o pérdida de conexión de red/energía). No se promete recuperación automática de datos bajo estas contingencias, quedando señaladas dichas limitaciones para su evaluación técnica futura.

### Actualización Automática de la Fecha de Última Visita (INF-07)

- **Operación atómica e indivisible:** La actualización de la fecha de última visita de la mascota se ejecuta de forma automática dentro de la misma transacción ACID de guardado de la atención médica (CU-04). No requiere ninguna acción adicional del Veterinario ni admite modificación manual.
- **Primera atención del paciente:** En mascotas que no cuentan con atenciones previas en su historial, la fecha de última visita figura como «Sin visitas registradas». Al guardarse con éxito su primera consulta médica, se establece automáticamente la fecha de dicha atención.
- **Consistencia temporal (atención más reciente):** La fecha de última visita siempre representa la atención más reciente por fecha. En caso de coexistir o procesarse atenciones con fechas anteriores a la última registrada, el sistema no reemplaza una fecha más reciente por una anterior, preservando la coherencia cronológica.
- **Preservación ante fallas o abandono:** Si ocurre un error de guardado (Flujo 4a) o el Veterinario confirma el abandono del formulario sin guardar (Flujo A1), la fecha de última visita de la mascota permanece estrictamente inalterada.
- **Cumplimiento de RN-01:** La actualización de la fecha de última visita es un metadato de estado en la entidad mascota y no modifica ni altera el contenido inmutable de las atenciones médicas previamente guardadas en la Historia Clínica, respetando **RN-01**.

### Registro Quirúrgico y Exclusiones de Alcance (INF-06)

- **Integración del procedimiento quirúrgico:** Los procedimientos quirúrgicos se registran exclusivamente como parte opcional de una atención médica (CU-04). Quedan vinculados a la mascota, a su Historia Clínica, a la atención médica en la que se realizaron y al Veterinario responsable actuante. No existe un caso de uso independiente ni un módulo separado de cirugías.
- **Campos aprobados:** Tipo de procedimiento, fecha de realización (automática), descripción de la intervención, Veterinario responsable (automático) y observaciones/complicaciones (opcional). El diagnóstico y tratamiento pertenecen a la atención general y no se duplican en el registro quirúrgico.
- **Exclusión de gestión quirúrgica compleja:** No se incluye gestión de quirófanos, anestesia, insumos, costos, facturación ni programación quirúrgica independiente.
- **Exclusión de consentimiento digital y firma:** En esta primera entrega (MVP), el consentimiento digital del dueño, la captura de firma digital o electrónica, el almacenamiento de documentos firmados y la validación digital de autorizaciones quedan expresamente **excluidos del alcance de software** y previstos para una etapa futura. Esta exclusión técnica no exime ni altera las obligaciones legales o profesionales que competen al ejercicio médico veterinario.

### Nota: Validación vs. Verificación aplicada

- **Validación (Presentación, → 400 / UI):** Se validan formatos de texto, presencia de campos obligatorios clínicos (`motivoConsulta`, `diagnostico`, `tratamiento`), campos requeridos del procedimiento quirúrgico (`tipoProcedimiento`, `descripcion`), esquemas válidos en las colecciones hijas (`PrescripcionDTO`, `EstudioDTO`) y la detección de cambios pendientes en el formulario antes de la navegación.
- **Verificación (Negocio, → 404/409):** Comprobación de existencia y estado activo de la mascota (**RN-07**), validación de su historia clínica e integración de registros (**RN-02**), evaluación de aptitud (**RN-03**), verificación estricta de la aptitud para vacunación (`APTO`) antes de habilitar el registro de la vacuna (**RN-04**), persistencia inmutable de la atención médica y sus procedimientos asociados (**RN-01**) y actualización automática atómica de la fecha de última visita de la mascota.

### Matriz de trazabilidad CU-04 → Test

| Paso del CU | Excepción / Código | Test unitario (BusinessLogic) | Test integración (HTTP / UI) |
| --- | --- | --- | --- |
| Flujo principal (Apto + Vacuna) | `201 Created` | `CreateAtencionAsync_WithCompleteData_SavesAtencionAndRelatedEntities` | `CreateAtencion_WithValidData_Returns201Created` |
| Flujo principal (Evaluado NO APTO) | `201 Created` | `CreateAtencionAsync_WhenVaccinationUnfitWithoutVaccine_SavesAtencionWithUnfitEvaluation` | `CreateAtencion_WhenVaccinationUnfitWithoutVaccine_Returns201Created` |
| Flujo principal (Con procedimiento quirúrgico) | `201 Created` | `CreateAtencionAsync_WithSurgicalProcedure_SavesAtencionAndProcedure` | `CreateAtencion_WithValidSurgicalProcedure_Returns201Created` |
| Flujo principal (Actualización última visita) | `201 Created` | `CreateAtencionAsync_WhenSaved_UpdatesMascotaFechaUltimaVisita` | `CreateAtencion_WhenSaved_Returns201AndUpdatesFechaUltimaVisita` |
| 1a. JSON inválido | `400 Bad Request` | — (model binding de ASP.NET Core) | `CreateAtencion_WithMalformedPayload_Returns400BadRequest` |
| 2a. Campos clínicos faltantes | `400 Bad Request` | — (validación DataAnnotations) | `CreateAtencion_WithMissingDiagnostico_Returns400BadRequest` |
| 2b. Prescripción incompleta | `400 Bad Request` | — (validación DataAnnotations en DTO) | `CreateAtencion_WithIncompletePrescripcion_Returns400BadRequest` |
| 2c. Procedimiento quirúrgico incompleto | `400 Bad Request` | — (validación DataAnnotations en DTO quirúrgico) | `CreateAtencion_WithIncompleteSurgicalProcedure_Returns400BadRequest` |
| 3a. Mascota inexistente | `404 Not Found` | `CreateAtencionAsync_WhenMascotaNotExists_ThrowsMascotaNotFoundException` | `CreateAtencion_WhenMascotaNotExists_Returns404NotFound` |
| 3b. Vacuna en paciente no apto | `409 Conflict` | `CreateAtencionAsync_WithUnfitVaccination_ThrowsPacienteNoAptoVacunacionException` | `CreateAtencion_WhenVaccinationUnfit_Returns409Conflict` |
| 3c. Mascota inactiva | `409 Conflict` | `CreateAtencionAsync_WhenMascotaIsInactive_ThrowsMascotaInactivaException` | `CreateAtencion_WhenMascotaIsInactive_Returns409Conflict` |
| 4a. Error de persistencia / Reintento | `500 Internal Server Error` | `CreateAtencionAsync_WhenDbFails_ThrowsExceptionAndRollbacks` | `CreateAtencion_WhenDbFails_Returns500AndPreservesFormData` |
| A1. Salir con cambios pendientes | N/A (Modal UI) | — (Detección de estado 'dirty' / cambios pendientes clínicos y quirúrgicos) | `FormularioAtencion_AlIntentarSalirConCambios_MuestraAdvertenciaYPermiteDecidir` |
| A2. Salir sin cambios pendientes | N/A (Navegación UI) | — (Verificación de formulario limpio / sin modificaciones) | `FormularioAtencion_AlSalirSinCambios_NavegaSinAdvertencia` |

> Regla de oro: cada flujo del caso de uso debe tener al menos un test. En los flujos resueltos en la Capa de Presentación el test aplicable es el de integración HTTP o prueba de interfaz de usuario. Los tests se ejecutan con `dotnet test SistemaVeterinaria.slnx`.

