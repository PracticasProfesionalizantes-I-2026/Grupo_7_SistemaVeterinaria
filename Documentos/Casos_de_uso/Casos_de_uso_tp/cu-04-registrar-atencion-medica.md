# Caso de Uso: Registrar Atención Médica

> Especificación elaborada siguiendo la guía `GUIA-Especificacion-Casos-de-Uso.md` (sección 3).
> Implementación del registro clínico con reglas de negocio RN-01 (inmutabilidad de atenciones médicas), RN-02 (integración de registros clínicos a la historia clínica), RN-03 (evaluación de aptitud para vacunación), RN-04 (vacunación condicionada a aptitud clínica) y RN-07 (restricción de atención en mascotas inactivas).

| Campo | Valor |
| --- | --- |
| **ID del Caso de Uso** | CU-04 |
| **Nombre** | Registrar Atención Médica |
| **Actor Principal** | Veterinario/a |
| **Alcance / Nivel** | Sistema; meta de usuario |
| **Stakeholders e intereses** | Veterinario/a → asentar de forma rigurosa el diagnóstico, evolución, prescripciones y vacunaciones realizadas; Dueño de la Mascota → recibir indicaciones claras de tratamiento y constancia de vacunas aplicadas; Clínica Veterinaria → cumplir normativas de historia clínica inmutable y control sanitario |
| **Disparador (Trigger)** | El veterinario selecciona la opción "Nueva Atención" desde la historia clínica de una mascota |
| **Prioridad / Frecuencia** | Alta; muy alta frecuencia (se ejecuta en cada acto médico veterinario) |
| **Reglas de negocio relacionadas** | RN-01 (inmutabilidad de las atenciones médicas); RN-02 (integración de registros clínicos a la historia clínica); RN-03 (evaluación de aptitud para vacunación durante la atención médica); RN-04 (vacunación condicionada a aptitud clínica); RN-07 (mascotas inactivas no pueden registrar nuevas atenciones médicas) |

---

### 1. BREVE DESCRIPCIÓN
Permite al veterinario asentar una nueva atención médica en la historia clínica de una mascota activa, registrando el motivo de consulta, anamnesis, examen físico, diagnóstico, tratamiento y observaciones, con la posibilidad de extender la consulta agregando prescripciones de medicamentos, solicitud o adjunto de estudios complementarios, y de manera opcional la evaluación de aptitud para vacunación mediante checklist clínica (APTO/NO APTO) con el consecuente registro de vacunas aplicadas en caso de resultar apto.

### 2. PRECONDICIONES
- El veterinario debe haber iniciado sesión y contar con un Token JWT activo con rol de `Veterinario`.
- La mascota debe encontrarse registrada en el sistema, en estado **Activa** (**RN-07**), y contar con una historia clínica vinculada.
- La Capa de Persistencia debe estar operativa y lista para transacciones ACID.

### 3. FLUJO PRINCIPAL (Camino Feliz - HTTP 201)
1. El Actor envía una petición al endpoint `POST /api/atenciones` con un cuerpo JSON que contiene los datos de la atención clínica (`mascotaId`, `motivoConsulta`, `anamnesis`, `examenFisico`, `diagnostico`, `tratamiento`, `observaciones`) y de forma opcional las listas de prescripciones (`prescripciones[]`), estudios (`estudios[]`), evaluación de aptitud para vacunación (`evaluacionAptitud`) y registro de vacunas aplicadas (`vacunacion`).
2. La **Capa de Presentación** (`AtencionesController.CreateAtencion`) valida la estructura del payload y verifica que los campos obligatorios del DTO principal y de los sub-objetos estén completos (`AtencionCreateDTO`).
3. La **Capa de Negocio** (`AtencionService.CreateAtencionAsync`) valida la existencia de la mascota y comprueba que se encuentre en estado **Activa** (**RN-07**), valida su historia clínica, extrae el ID del veterinario autenticado desde los claims del token, y asegura la trazabilidad clínica (**RN-02**). Si se incluye evaluación de aptitud para vacunación mediante la checklist clínica, valida y registra el resultado (`"APTO"` o `"NO_APTO"`) (**RN-03**). Si se incluye registro de vacuna, comprueba que la evaluación de aptitud marque estrictamente resultado `"APTO"` (**RN-04**).
4. La **Capa de Persistencia** ejecuta una transacción en base de datos (`DbContext.SaveChangesAsync`), creando el registro inmutable en `Atenciones` (**RN-01**) y guardando en cascada las prescripciones, solicitudes de estudio, el resultado de la evaluación de aptitud y las vacunas aplicadas vinculadas a dicha atención (**RN-02**).
5. El Sistema devuelve un código **201 Created** con el detalle completo de la atención registrada (`AtencionResponseDTO`) y su identificador generado.

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

* **4a. Error en la transacción de persistencia (HTTP 500 Internal Server Error):**
  1. Si en el Paso 4 falla la persistencia en base de datos (rollback automático de la transacción).
  2. El middleware de excepciones intercepta el fallo.
  3. El Sistema devuelve un código **500 Internal Server Error**. Fin del caso de uso.

### 5. SUB-VARIACIONES (opcional)
1. **Atención médica simple:** Contiene únicamente la evaluación clínica, diagnóstico y tratamiento ambulatorio sin medicación especial, vacunas ni evaluación de aptitud.
2. **Atención médica con prescripción múltiple:** Permite agregar una lista con múltiples medicamentos recetados con sus respectivas dosis y frecuencias.
3. **Atención médica con solicitud de estudios:** Permite solicitar estudios diagnósticos (radiografías, análisis de sangre, ecografías) o adjuntar resultados existentes.
4. **Atención médica con evaluación de aptitud y vacunación:** El profesional completa la checklist clínica (sin fiebre, buen estado general, sin enfermedad infecciosa aguda, autorización veterinaria). Al resultar `"APTO"`, el sistema habilita el registro de la vacuna aplicada (lote, vacuna, fecha de próxima dosis), persistiendo ambos registros.
5. **Atención médica con paciente evaluado NO APTO para vacunación:** El profesional completa la checklist clínica y el resultado es `"NO APTO"`. El sistema bloquea el registro de la vacunación pero permite guardar y finalizar normalmente la atención médica, asentando el resultado `"NO APTO"` de la evaluación en la historia clínica.

### 6. POSTCONDICIONES
- La atención médica queda registrada de forma inmutable (**RN-01**) en la tabla `Atenciones`, incluyendo el resultado de la evaluación de aptitud si fue efectuada (`APTO` o `NO APTO`) (**RN-03**).
- Las prescripciones, estudios y vacunas aplicadas (en caso de resultar `APTO`) quedan asociadas automáticamente a la atención y a la historia clínica (**RN-02**).
- Si se aplicaron vacunas con aptitud aprobada, se actualizan los indicadores de última consulta y próximas vacunas de la mascota.

---

## Anexo: matrices de referencia

### Códigos HTTP usados

| Código HTTP | Nombre Técnico | Contexto de Aplicación en el Caso de Uso |
| --- | --- | --- |
| `201` | Created | Registro exitoso de la atención médica (con o sin vacunas/estudios/prescripciones/evaluación). |
| `400` | Bad Request | Faltan datos clínicos obligatorios o datos de prescripción/estudios incompletos. |
| `404` | Not Found | La mascota especificada no existe en la Capa de Persistencia. |
| `409` | Conflict | Violación de regla de negocio RN-04 (vacunación sin aptitud APTO) o RN-07 (intento de registrar atención en mascota inactiva). |
| `500` | Internal Server Error | Error no controlado durante la transacción de guardado en la base de datos. |

### Nota: Validación vs. Verificación aplicada

- **Validación (Presentación, → 400):** Se validan formatos de texto, presencia de campos obligatorios clínicos (`motivoConsulta`, `diagnostico`, `tratamiento`) y esquemas válidos en las colecciones hijas (`PrescripcionDTO`, `EstudioDTO`).
- **Verificación (Negocio, → 404/409):** Comprobación de existencia y estado activo de la mascota (**RN-07**), validación de su historia clínica e integración de registros (**RN-02**), evaluación de aptitud (**RN-03**), verificación estricta de la aptitud para vacunación (`APTO`) antes de habilitar el registro de la vacuna (**RN-04**) y persistencia inmutable de la atención médica (**RN-01**).

### Matriz de trazabilidad CU-04 → Test

| Paso del CU | Excepción / Código | Test unitario (BusinessLogic) | Test integración (HTTP) |
| --- | --- | --- | --- |
| Flujo principal (Apto + Vacuna) | `201 Created` | `CreateAtencionAsync_WithCompleteData_SavesAtencionAndRelatedEntities` | `CreateAtencion_WithValidData_Returns201Created` |
| Flujo principal (Evaluado NO APTO) | `201 Created` | `CreateAtencionAsync_WhenVaccinationUnfitWithoutVaccine_SavesAtencionWithUnfitEvaluation` | `CreateAtencion_WhenVaccinationUnfitWithoutVaccine_Returns201Created` |
| 1a. JSON inválido | `400 Bad Request` | — (model binding de ASP.NET Core) | `CreateAtencion_WithMalformedPayload_Returns400BadRequest` |
| 2a. Campos clínicos faltantes | `400 Bad Request` | — (validación DataAnnotations) | `CreateAtencion_WithMissingDiagnostico_Returns400BadRequest` |
| 2b. Prescripción incompleta | `400 Bad Request` | — (validación DataAnnotations en DTO) | `CreateAtencion_WithIncompletePrescripcion_Returns400BadRequest` |
| 3a. Mascota inexistente | `404 Not Found` | `CreateAtencionAsync_WhenMascotaNotExists_ThrowsMascotaNotFoundException` | `CreateAtencion_WhenMascotaNotExists_Returns404NotFound` |
| 3b. Vacuna en paciente no apto | `409 Conflict` | `CreateAtencionAsync_WithUnfitVaccination_ThrowsPacienteNoAptoVacunacionException` | `CreateAtencion_WhenVaccinationUnfit_Returns409Conflict` |
| 3c. Mascota inactiva | `409 Conflict` | `CreateAtencionAsync_WhenMascotaIsInactive_ThrowsMascotaInactivaException` | `CreateAtencion_WhenMascotaIsInactive_Returns409Conflict` |

> Regla de oro: cada flujo del caso de uso debe tener al menos un test. En los flujos resueltos en la Capa de Presentación el test aplicable es el de integración HTTP. Los tests se ejecutan con `dotnet test SistemaVeterinaria.slnx`.
