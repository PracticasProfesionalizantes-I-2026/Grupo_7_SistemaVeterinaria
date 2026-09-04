# Caso de Uso: Gestionar Dueños

> Especificación elaborada siguiendo la guía `GUIA-Especificacion-Casos-de-Uso.md` (sección 3).
> Implementación del ciclo de vida y administración de clientes con reglas RN-01 (unicidad de DNI en actualizaciones) y RN-02 (restricción de baja definitiva ante existencia de mascotas o historial asociado).

| Campo | Valor |
| --- | --- |
| **ID del Caso de Uso** | CU-08 |
| **Nombre** | Gestionar Dueños |
| **Actor Principal** | Veterinario/a o Recepcionista |
| **Alcance / Nivel** | Sistema; meta de usuario |
| **Stakeholders e intereses** | Recepcionista / Veterinario → consultar el listado de clientes, buscar por DNI/nombre, actualizar datos de contacto y gestionar bajas; Dueño de la Mascota → mantener actualizados sus teléfonos, email y domicilio de contacto ante emergencias de sus mascotas; Clínica Veterinaria → garantizar la integridad y coherencia del padrón de clientes |
| **Disparador (Trigger)** | El usuario ingresa al módulo "Dueños" para buscar, consultar el perfil, editar datos de contacto o solicitar la baja de un dueño |
| **Prioridad / Frecuencia** | Media / Alta; frecuencia diaria |
| **Reglas de negocio relacionadas** | RN-01 (el DNI del dueño es unívoco en el sistema); RN-02 (prohibición de eliminación definitiva de dueños con mascotas, turnos o historias clínicas asociadas) |

---

### 1. BREVE DESCRIPCIÓN
Permite al personal de la clínica veterinaria (Recepcionista o Veterinario) consultar el listado de dueños registrados, buscar por nombre, apellido o DNI, visualizar el detalle de contacto y sus mascotas asociadas, actualizar su información de contacto o gestionar su baja en el sistema.

### 2. PRECONDICIONES
- El actor debe poseer un estado de autenticación activo (Token JWT válido) con rol de `Recepcionista`, `Veterinario` o `Administrador`.
- Debe existir al menos un dueño registrado en la Capa de Persistencia para su consulta o modificación.
- La base de datos debe encontrarse operativa.

### 3. FLUJO PRINCIPAL (Camino Feliz - HTTP 200 / 204)
1. El Actor envía una petición al endpoint `GET /api/duenos/{id}` para consultar el detalle de un dueño, o `PUT /api/duenos/{id}` con el JSON de actualización de datos de contacto (`nombre`, `apellido`, `dni`, `telefono`, `domicilio`, `email`).
2. La **Capa de Presentación** (`DuenosController.UpdateDueno`) valida que el ID de ruta coincida y que los atributos de validación del DTO sean correctos (`DuenoUpdateDTO`).
3. La **Capa de Negocio** (`DuenoService.UpdateDuenoAsync`) recupera al dueño por ID, verifica que el nuevo DNI no esté utilizado por otro dueño distinto (**RN-01**), normaliza los textos con `Trim()` y actualiza los campos.
4. La **Capa de Persistencia** actualiza el registro en la tabla `Duenos` y persiste los cambios.
5. El Sistema devuelve un código **200 OK** con los datos actualizados del dueño y sus mascotas vinculadas (`DuenoDetailResponseDTO`), o un código **204 No Content** en caso de operaciones de eliminación.

### 4. FLUJOS ALTERNATIVOS (Caminos Tristes / Excepciones)

* **1a. JSON o parámetros de búsqueda inválidos (HTTP 400 Bad Request):**
  1. Si en el Paso 1 el identificador de ruta no es válido o el cuerpo JSON está corrupto.
  2. La Capa de Presentación rechaza la petición.
  3. El Sistema devuelve un código **400 Bad Request**. Fin del caso de uso.

* **2a. Datos obligatorios incompletos o vacíos (HTTP 400 Bad Request):**
  1. Si en el Paso 2 faltan campos requeridos como `nombre`, `apellido`, `dni` o `telefono`, o si se ingresan valores solo de espacios en blanco.
  2. La Capa de Presentación detecta la falla de validación (`ModelState.IsValid == false`).
  3. El Sistema devuelve un código **400 Bad Request** con el detalle de campos obligatorios. Fin del caso de uso.

* **3a. Dueño no encontrado (HTTP 404 Not Found):**
  1. Si en el Paso 3 el `id` o el `dni` buscado no corresponde a ningún cliente registrado en la base de datos.
  2. La Capa de Negocio lanza la excepción `DuenoNotFoundException`.
  3. El Sistema devuelve un código **404 Not Found** con el mensaje: `"No se encontró ningún dueño con los datos especificados."`. Fin del caso de uso.

* **3b. DNI ya asignado a otro dueño (HTTP 409 Conflict):**
  1. Si en el Paso 3 al actualizar el DNI se detecta que ya pertenece a otro registro existente, violando la regla **RN-01**.
  2. La Capa de Negocio frena la modificación y lanza `DniDuplicadoException`.
  3. El Sistema devuelve un código **409 Conflict** con el mensaje: `"El DNI {dni} ya se encuentra asignado a otro dueño."`. Fin del caso de uso.

* **3c. Intento de eliminar dueño con mascotas o atenciones asociadas (HTTP 409 Conflict):**
  1. Si durante una solicitud de baja (`DELETE /api/duenos/{id}`) el dueño posee mascotas activas, turnos o atenciones clínicas vinculadas, violando la regla **RN-02**.
  2. La Capa de Negocio frena la eliminación y lanza `DuenoConMascotasActivasException`.
  3. El Sistema devuelve un código **409 Conflict** indicando: `"No es posible eliminar al dueño porque tiene mascotas asociadas o historial clínico activo."`. Fin del caso de uso.

* **4a. Error de persistencia en base de datos (HTTP 500 Internal Server Error):**
  1. Si en el Paso 4 se produce un error imprevisto al actualizar la base de datos.
  2. El middleware global captura el error.
  3. El Sistema devuelve un código **500 Internal Server Error**. Fin del caso de uso.

### 5. SUB-VARIACIONES (opcional)
1. **Búsqueda por filtro:** Búsqueda textual mediante `GET /api/duenos?criterio={texto}` retornando coincidencias por nombre, apellido o DNI.
2. **Consulta con detalle de mascotas:** El endpoint `GET /api/duenos/{id}` incluye la lista de mascotas registradas a nombre del cliente.

### 6. POSTCONDICIONES
- Se actualizan los datos del cliente en la tabla `Duenos`.
- Los cambios de información de contacto quedan disponibles de forma inmediata para todos los módulos de atención y agenda.

---

## Anexo: matrices de referencia

### Códigos HTTP usados

| Código HTTP | Nombre Técnico | Contexto de Aplicación en el Caso de Uso |
| --- | --- | --- |
| `200` | OK | Consulta exitosa o actualización confirmada de los datos del dueño. |
| `204` | No Content | Eliminación exitosa del registro del dueño. |
| `400` | Bad Request | Parámetros inválidos, datos faltantes o cadenas con solo espacios. |
| `404` | Not Found | Dueño inexistente en la Capa de Persistencia. |
| `409` | Conflict | DNI duplicado al modificar (RN-01) o intento de eliminación con mascotas asociadas (RN-02). |
| `500` | Internal Server Error | Falla no controlada de persistencia en la base de datos. |

### Nota: Validación vs. Verificación aplicada

- **Validación (Presentación, → 400):** Se validan la obligatoriedad de campos, tipos numéricos en DNI y formato de email mediante DataAnnotations sobre `DuenoUpdateDTO`.
- **Verificación (Negocio, → 404/409):** Validación de existencia del cliente, verificación de unicidad de DNI excluyendo el propio ID (**RN-01**) y control de integridad referencial previo al borrado (**RN-02**).

### Matriz de trazabilidad CU-08 → Test

| Paso del CU | Excepción / Código | Test unitario (BusinessLogic) | Test integración (HTTP) |
| --- | --- | --- | --- |
| Flujo principal | `200 OK` | `UpdateDuenoAsync_WithValidData_UpdatesAndReturnsDuenoDTO` | `UpdateDueno_WithValidData_Returns200OK` |
| 1a. Payload inválido | `400 Bad Request` | — (model binding de ASP.NET Core) | `UpdateDueno_WithMalformedJson_Returns400BadRequest` |
| 2a. Campos faltantes | `400 Bad Request` | — (validación DataAnnotations) | `UpdateDueno_WithMissingRequiredFields_Returns400BadRequest` |
| 3a. Dueño inexistente | `404 Not Found` | `GetDuenoByIdAsync_WhenNotExists_ThrowsDuenoNotFoundException` | `GetDuenoById_WhenNotExists_Returns404NotFound` |
| 3b. DNI duplicado en update | `409 Conflict` | `UpdateDuenoAsync_WhenDniAlreadyExists_ThrowsDniDuplicadoException` | `UpdateDueno_WhenDuplicateDni_Returns409Conflict` |
| 3c. Eliminar con mascotas | `409 Conflict` | `DeleteDuenoAsync_WhenHasMascotas_ThrowsDuenoConMascotasActivasException` | `DeleteDueno_WhenHasMascotas_Returns409Conflict` |

> Regla de oro: cada flujo del caso de uso debe tener al menos un test. En los flujos resueltos en la Capa de Presentación el test aplicable es el de integración HTTP. Los tests se ejecutan con `dotnet test SistemaVeterinaria.slnx`.
