# Caso de Uso: Gestionar Mascotas

> Especificación elaborada siguiendo la guía `GUIA-Especificacion-Casos-de-Uso.md` (sección 3).
> Implementación del ciclo de vida de mascotas con reglas de negocio RN-06 (asociación de mascota a dueño registrado), RN-07 (baja lógica y preservación histórica de mascotas) y procedimiento de reasignación de titularidad con historial de propietarios.
> Incorpora la restricción de permisos administrativos exclusivos para Veterinario y Recepcionista, excluyendo al Administrador (AMB-02).
> Incorpora la especificación de respuestas HTTP para la baja lógica de mascotas: 200 OK en baja exitosa y 409 Conflict ante mascota previamente inactiva (MEJ-03).

| Campo | Valor |
| --- | --- |
| **ID del Caso de Uso** | CU-07 |
| **Nombre** | Gestionar Mascotas |
| **Actor Principal** | Veterinario/a o Recepcionista |
| **Alcance / Nivel** | Sistema; meta de usuario |
| **Stakeholders e intereses** | Recepcionista / Veterinario → registrar y mantener actualizados los datos biométricos y de identificación de los pacientes animales; Dueño de la Mascota → contar con el registro de su mascota para su atención clínica; Clínica Veterinaria → garantizar la integridad de las historias clínicas, la trazabilidad y la inmutabilidad de los antecedentes |
| **Disparador (Trigger)** | El usuario selecciona el módulo "Mascotas" para registrar una nueva mascota, consultar el listado, actualizar datos, solicitar su desactivación (baja lógica) o cambiar el dueño asociado |
| **Prioridad / Frecuencia** | Alta; muy alta frecuencia diaria |
| **Reglas de negocio relacionadas** | RN-01 (inmutabilidad de las atenciones médicas); RN-06 (asociación obligatoria a un dueño previamente registrado); RN-07 (baja lógica y preservación histórica de la historia clínica) |

---

### 1. BREVE DESCRIPCIÓN
Permite al personal de la clínica veterinaria (Veterinario/a o Recepcionista) registrar una nueva mascota, consultar los datos de un paciente existente, modificar su información (nombre, especie, raza, sexo, edad/fecha de nacimiento, peso, observaciones), gestionar su desactivación mediante baja lógica, o reasignar la mascota a un nuevo dueño registrado en el sistema, vinculándola indefectiblemente a un dueño registrado y preservando en todos los casos su historia clínica completa.

### 2. PRECONDICIONES
- El actor debe haber iniciado sesión con rol de Recepcionista o Veterinario (el rol Administrador no cuenta con permisos para operaciones sobre datos administrativos de mascotas).
- Para registrar una mascota, el dueño debe encontrarse previamente registrado en el sistema (**RN-06**).
- El sistema debe encontrarse disponible y operativo.

### 3. FLUJO PRINCIPAL (Camino Feliz — Alta de mascota)
1. El usuario accede al módulo Mascotas y selecciona la opción de registrar una nueva mascota.
2. El sistema solicita los datos de la mascota (nombre, especie, raza, sexo, fecha de nacimiento o edad, peso, observaciones) y el dueño al que se asociará.
3. El usuario completa los datos y selecciona al dueño previamente registrado.
4. El sistema verifica que el dueño exista en el sistema (**RN-06**) y que los datos ingresados sean válidos.
5. El sistema registra la nueva mascota en estado **Activa** y genera automáticamente su Historia Clínica inicial.
6. El sistema confirma el registro exitoso de la mascota.

### 4. FLUJOS ALTERNATIVOS (Caminos Tristes / Excepciones)

* **1a. Datos obligatorios faltantes o inválidos:**
  1. En el Paso 3, el usuario omite campos obligatorios (nombre, especie, sexo o dueño) o ingresa valores inválidos (peso negativo, fecha de nacimiento futura).
  2. El sistema informa el o los campos con error y solicita su corrección.
  3. No se registra la mascota. El usuario puede corregir los datos e intentar nuevamente.

* **1b. Dueño no registrado:**
  1. En el Paso 4, el dueño seleccionado o ingresado no existe en el sistema, incumpliendo **RN-06**.
  2. El sistema informa que el dueño especificado no se encuentra registrado.
  3. No se registra la mascota. Fin del caso de uso.

* **2a. Mascota no encontrada al consultar, editar o desactivar:**
  1. El usuario intenta acceder a una mascota cuyo identificador no existe en el sistema.
  2. El sistema informa que no se encontró la mascota solicitada.
  3. Fin del caso de uso.

* **2b. Intento de desactivar mascota ya inactiva (HTTP 409 Conflict):**
  1. Si un usuario autorizado (Recepcionista o Veterinario) intenta dar de baja o desactivar una mascota que ya se encuentra en estado Inactiva, incumpliendo la regla **RN-07**.
  2. La Capa de Negocio frena la operación y lanza la excepción `MascotaYaInactivaException`.
  3. El Sistema rechaza la solicitud y devuelve un código **409 Conflict**, informando que la mascota ya se encuentra inactiva. No se realizan modificaciones adicionales en el sistema. Fin del caso de uso.

* **A1 — Reasignación: mascota inactiva:**
  1. El usuario selecciona la opción Cambiar dueño sobre una mascota en estado Inactiva.
  2. El sistema informa que no es posible cambiar el dueño porque la mascota está inactiva.
  3. No se modifica la asociación existente. Fin del caso de uso.

* **A2 — Reasignación: nuevo dueño no registrado:**
  1. Durante el flujo de reasignación, el usuario intenta seleccionar un dueño que no existe en el sistema.
  2. El sistema informa que el nuevo dueño debe estar registrado previamente.
  3. No se realiza la reasignación. El usuario puede seleccionar otro dueño registrado o cancelar la operación.

* **A3 — Reasignación: selección del mismo dueño actual:**
  1. Durante el flujo de reasignación, el usuario selecciona como nuevo dueño al propietario ya asociado a la mascota.
  2. El sistema informa que la mascota ya se encuentra asociada a ese dueño y que no se requiere ningún cambio.
  3. No se realiza ningún cambio ni registro. Fin del caso de uso.

* **A4 — Reasignación: cancelación antes de confirmar:**
  1. El usuario inicia la reasignación pero decide cancelar antes de confirmar la operación.
  2. El sistema conserva la asociación original sin modificaciones.
  3. No se registra ninguna reasignación. Fin del caso de uso.

* **A5 — Reasignación: error al guardar:**
  1. Ocurre un error al intentar registrar el cambio de dueño o el historial de propietarios en el sistema.
  2. El sistema informa que la operación no pudo completarse.
  3. El sistema conserva el dueño anterior y no registra una reasignación incompleta.
  4. El usuario puede volver a intentar la operación.

* **6a. Error interno del sistema:**
  1. Ocurre un fallo imprevisto al interactuar con el sistema durante cualquier operación de esta gestión.
  2. El sistema informa el error al usuario.
  3. Fin del caso de uso.

### 5. SUB-VARIACIONES (opcional)
1. **Alta de mascota:** Registro de un nuevo paciente animal en estado Activa asociado a su dueño.
2. **Modificación de datos:** Actualización de peso, raza, sexo, observaciones y otros datos biométricos de la mascota.
3. **Baja lógica / Desactivación (HTTP 200 OK):** Cuando una mascota está en estado Activa y un usuario autorizado (Recepcionista o Veterinario) solicita su baja: el sistema solicita confirmación; una vez confirmada la operación, el sistema cambia su estado a Inactiva, conserva su Historia Clínica y todos sus registros históricos (**RN-07**) sin eliminar físicamente ningún registro, y devuelve un código **200 OK** con la confirmación de la operación y el estado actualizado de la mascota. La mascota inactiva no podrá recibir nuevos turnos ni nuevas atenciones médicas.
4. **Reasignación de dueño (Cambiar dueño):** Cambio del propietario asociado a una mascota activa a un nuevo dueño registrado. El procedimiento completo se describe en la sección de Flujo de Reasignación a continuación.

### 5.1 FLUJO DE REASIGNACIÓN DE DUEÑO (Sub-variación 4)

Este sub-flujo se inicia cuando el usuario selecciona la opción **Cambiar dueño** sobre una mascota.

1. El usuario accede a la sección de Gestión de Mascotas y busca y selecciona la mascota que desea reasignar.
2. El sistema muestra los datos de la mascota, su estado actual y el dueño actual asociado.
3. El usuario selecciona la opción **Cambiar dueño**.
4. El sistema verifica que la mascota se encuentre en estado **Activa**. Si está inactiva, aplica el flujo **A1**.
5. El sistema presenta la opción de búsqueda y selección del nuevo dueño.
6. El usuario busca y selecciona al nuevo dueño, que debe estar previamente registrado en el sistema. Si el dueño no existe, aplica **A2**. Si coincide con el dueño actual, aplica **A3**.
7. El sistema muestra los datos del dueño actual y del nuevo dueño seleccionado para que el usuario los confirme.
8. El usuario solicita realizar la reasignación.
9. El sistema presenta un mensaje de confirmación indicando el cambio que se realizará.
10. El usuario confirma la operación. Si cancela, aplica **A4**.
11. El sistema actualiza la asociación de la mascota con el nuevo dueño.
12. El sistema registra el cambio en el historial de propietarios, incluyendo: mascota afectada, dueño anterior, nuevo dueño, fecha y hora de la reasignación, y usuario del sistema que realizó la operación.
13. El sistema conserva íntegramente la Historia Clínica de la mascota y todos sus registros históricos sin modificación alguna (**RN-01**, **RN-07**).
14. Los turnos futuros de la mascota se mantienen con sus fechas y horarios originales, mostrando al nuevo dueño como responsable actual.
15. El sistema confirma que la reasignación se realizó correctamente.

### 6. POSTCONDICIONES
- **Alta:** Se registra la mascota en estado Activa junto con su Historia Clínica inicial, vinculada al dueño indicado (**RN-06**).
- **Modificación:** Se actualizan los datos biométricos o descriptivos de la mascota. La Historia Clínica no se modifica.
- **Baja lógica (HTTP 200 OK):** La mascota pasa a estado Inactiva, devolviendo confirmación de la operación y su estado actualizado; conserva su Historia Clínica y todos sus registros históricos para consulta (**RN-07**) sin eliminación física, quedando inhabilitada para recibir nuevos turnos o nuevas atenciones médicas.
- **Reasignación:** La mascota queda asociada al nuevo dueño. La Historia Clínica permanece intacta (**RN-01**, **RN-07**). Se conserva el historial de propietarios con el registro del cambio. Los turnos futuros muestran al nuevo dueño como responsable actual. No se generan nuevas historias clínicas ni se eliminan registros existentes.
- En ningún caso se realiza la eliminación física del registro de la mascota ni de su Historia Clínica.

---

## Anexo: matrices de referencia

### Códigos HTTP usados

| Código HTTP | Nombre Técnico | Contexto de Aplicación en el Caso de Uso |
| --- | --- | --- |
| `201` | Created | Creación exitosa del recurso Mascota y su Historia Clínica vinculada. |
| `200` | OK | Retorno exitoso de consulta, actualización, baja lógica exitosa (confirmación y estado Inactiva) o reasignación de la mascota. |
| `400` | Bad Request | Parámetros inválidos, campos obligatorios ausentes, peso negativo o selección del mismo dueño actual en reasignación. |
| `404` | Not Found | Dueño o mascota no encontrados en el sistema (RN-06); nuevo dueño no registrado en reasignación. |
| `409` | Conflict | Intento de dar de baja una mascota que ya está en estado Inactiva (RN-07); intento de reasignar una mascota inactiva. |
| `500` | Internal Server Error | Error no controlado durante la persistencia de datos. |

### Nota: Validación vs. Verificación aplicada

- **Validación (Presentación):** Presencia obligatoria de nombre, especie, sexo y dueño; rangos numéricos positivos para peso; longitudes máximas.
- **Verificación (Negocio / Seguridad):** Validación de existencia del dueño (**RN-06**); verificación del estado de la mascota antes de desactivar (**RN-07**) o reasignar; verificación de que el nuevo dueño sea distinto al actual; integridad de la Historia Clínica (**RN-01**); registro del historial de propietarios.

### Nota sobre el historial de propietarios

El historial de propietarios es un registro de auditoría de las reasignaciones de titularidad. Su propósito es mantener la trazabilidad de los cambios de propietario de cada mascota a lo largo del tiempo. Este historial no podrá ser eliminado ni modificado. No se exponen funcionalidades de administración del historial en esta versión.

**Punto pendiente para decisión del equipo:** La forma en que los propietarios históricos se visualizan dentro de la Historia Clínica (por ejemplo, si las atenciones pasadas muestran al dueño que existía en ese momento o solo al dueño actual) no ha sido definida. Se recomienda resolverlo en una revisión posterior antes de la implementación del módulo de Historia Clínica.

### Matriz de trazabilidad CU-07 → Test

| Paso del CU | Excepción / Código | Test unitario (BusinessLogic) | Test integración (HTTP) |
| --- | --- | --- | --- |
| Flujo principal (alta) | `201 Created` | `CreateMascotaAsync_WithValidData_SavesMascotaAndHistoriaClinica` | `CreateMascota_WithValidData_Returns201Created` |
| 1a. Datos faltantes o inválidos | `400 Bad Request` | — (validación DataAnnotations) | `CreateMascota_WithMissingNombre_Returns400BadRequest` |
| 1b. Dueño inexistente | `404 Not Found` | `CreateMascotaAsync_WhenDuenoNotExists_ThrowsDuenoNotFoundException` | `CreateMascota_WhenDuenoNotExists_Returns404NotFound` |
| Baja lógica exitosa | `200 OK` | `DesactivarMascotaAsync_WithActiveMascota_ChangesStateToInactive` | `DesactivarMascota_WithActiveMascota_Returns200OK` |
| 2b. Desactivar ya inactiva | `409 Conflict` | `DesactivarMascotaAsync_WhenAlreadyInactive_ThrowsMascotaYaInactivaException` | `DesactivarMascota_WhenAlreadyInactive_Returns409Conflict` |
| A1. Reasignar mascota inactiva | `409 Conflict` | `ReasignarDuenoAsync_WhenMascotaIsInactive_ThrowsMascotaInactivaException` | `ReasignarDueno_WhenMascotaIsInactive_Returns409Conflict` |
| A2. Nuevo dueño no registrado | `404 Not Found` | `ReasignarDuenoAsync_WhenNuevoDuenoNotFound_ThrowsDuenoNotFoundException` | `ReasignarDueno_WhenNuevoDuenoNotFound_Returns404NotFound` |
| A3. Mismo dueño actual | `400 Bad Request` | `ReasignarDuenoAsync_WhenSameDueno_ThrowsInvalidOperationException` | `ReasignarDueno_WhenSameDueno_Returns400BadRequest` |
| Reasignación exitosa | `200 OK` | `ReasignarDuenoAsync_WithValidData_UpdatesDuenoAndSavesHistorial` | `ReasignarDueno_WithValidData_Returns200OK` |
| A5. Error al guardar reasignación | `500 Internal Server Error` | `ReasignarDuenoAsync_OnDatabaseFailure_ThrowsPersistenceException` | `ReasignarDueno_OnInternalError_Returns500InternalServerError` |

> Regla de oro: cada flujo del caso de uso debe tener al menos un test. En los flujos resueltos en la Capa de Presentación el test aplicable es el de integración HTTP. Los tests se ejecutan con `dotnet test SistemaVeterinaria.slnx`.
