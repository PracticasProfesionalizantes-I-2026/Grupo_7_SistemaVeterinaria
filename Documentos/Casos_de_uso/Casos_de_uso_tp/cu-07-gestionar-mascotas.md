# Caso de Uso: Gestionar Mascotas

> Especificación elaborada siguiendo la guía `GUIA-Especificacion-Casos-de-Uso.md` (sección 3).
> Implementación del ciclo de vida de mascotas con reglas RN-01 (asociación obligatoria a dueño), RN-02 (restricción de eliminación por integridad histórica de atenciones y turnos), RN-03 (validación de campos obligatorios) y RN-04 (existencia previa del dueño).

| Campo | Valor |
| --- | --- |
| **ID del Caso de Uso** | CU-07 |
| **Nombre** | Gestionar Mascotas |
| **Actor Principal** | Veterinario/a o Recepcionista |
| **Alcance / Nivel** | Sistema; meta de usuario |
| **Stakeholders e intereses** | Recepcionista / Veterinario → registrar y mantener actualizados los datos biométricos y de identificación de los pacientes animales; Dueño de la Mascota → contar con el registro de su mascota para su atención clínica; Clínica Veterinaria → garantizar la integridad de las historias clínicas y la trazabilidad |
| **Disparador (Trigger)** | El usuario selecciona el módulo "Mascotas" para registrar una nueva mascota, consultar el listado, actualizar datos o solicitar su baja |
| **Prioridad / Frecuencia** | Alta; muy alta frecuencia diaria |
| **Reglas de negocio relacionadas** | RN-01 (asociación obligatoria a un dueño registrado); RN-02 (prohibición de eliminación con atenciones o turnos asociados); RN-03 (completitud de campos obligatorios); RN-04 (preexistencia del dueño en el sistema) |

---

### 1. BREVE DESCRIPCIÓN
Permite al veterinario o a la recepcionista registrar una nueva mascota, consultar los datos de un paciente existente, modificar su información (nombre, especie, raza, sexo, edad/fecha de nacimiento, peso, observaciones) o gestionar su baja, vinculándola indefectiblemente a un dueño registrado.

### 2. PRECONDICIONES
- El actor debe haber iniciado sesión y poseer un Token JWT válido con rol de `Recepcionista`, `Veterinario` o `Administrador`.
- Para registrar una mascota, el dueño debe encontrarse previamente registrado en el sistema (**RN-01**, **RN-04**).
- La Capa de Persistencia debe encontrarse disponible.

### 3. FLUJO PRINCIPAL (Camino Feliz - HTTP 201)
1. El Actor envía una petición al endpoint `POST /api/mascotas` con un cuerpo JSON que contiene los datos de la mascota (`duenoId`, `nombre`, `especie`, `raza`, `sexo`, `fechaNacimiento` o `edad`, `peso`, `observaciones`).
2. La **Capa de Presentación** (`MascotasController.CreateMascota`) valida que la estructura del JSON sea correcta y que los campos requeridos estén presentes (`[Required]`, `[MaxLength]` en `MascotaCreateDTO`).
3. La **Capa de Negocio** (`MascotaService.CreateMascotaAsync`) normaliza las cadenas con `Trim()`, verifica la existencia del dueño asociado en la base de datos (**RN-01**, **RN-04**), e inicializa la entidad `Mascota` vinculando automáticamente su registro inicial de `HistoriaClinica`.
4. La **Capa de Persistencia** genera el identificador único (`Id`) y guarda el registro en la tabla `Mascotas` junto a su historia clínica inicial.
5. El Sistema devuelve un código **201 Created** con la información de la mascota (`MascotaResponseDTO`) y la cabecera `Location`.

### 4. FLUJOS ALTERNATIVOS (Caminos Tristes / Excepciones)

* **1a. JSON inválido o malformado (HTTP 400 Bad Request):**
  1. Si en el Paso 1 el cuerpo de la petición no cumple con la sintaxis JSON.
  2. La Capa de Presentación rechaza la petición.
  3. El Sistema devuelve un código **400 Bad Request**. Fin del caso de uso.

* **2a. Campos obligatorios de la mascota faltantes (HTTP 400 Bad Request):**
  1. Si en el Paso 2 faltan campos como `nombre`, `especie`, `sexo` o `duenoId` (**RN-03**).
  2. La Capa de Presentación detecta la falla de validación (`ModelState.IsValid == false`).
  3. El Sistema devuelve un código **400 Bad Request** detallando los campos faltantes. Fin del caso de uso.

* **2b. Valores biométricos o fechas inválidas (HTTP 400 Bad Request):**
  1. Si en el Paso 2 el `peso` es un número negativo o la `fechaNacimiento` es una fecha futura.
  2. La Capa de Presentación rechaza los datos por violación de rango.
  3. El Sistema devuelve un código **400 Bad Request** con el mensaje de error correspondiente. Fin del caso de uso.

* **3a. Dueño no registrado o inexistente (HTTP 404 Not Found):**
  1. Si en el Paso 3 el `duenoId` no existe en la tabla `Duenos`, violando las reglas **RN-01** y **RN-04**.
  2. La Capa de Negocio frena la creación y lanza la excepción `DuenoNotFoundException`.
  3. El Sistema devuelve un código **404 Not Found** con el mensaje: `"El dueño especificado no se encuentra registrado en el sistema."`. Fin del caso de uso.

* **3b. Mascota no encontrada al consultar, editar o eliminar (HTTP 404 Not Found):**
  1. Si durante una operación de consulta (`GET`), actualización (`PUT`) o baja (`DELETE`) el identificador no existe en la base de datos.
  2. La Capa de Negocio lanza `MascotaNotFoundException`.
  3. El Sistema devuelve un código **404 Not Found**. Fin del caso de uso.

* **3c. Intento de eliminar mascota con atenciones o turnos asociados (HTTP 409 Conflict):**
  1. Si durante la operación de eliminación (`DELETE /api/mascotas/{id}`) la mascota posee atenciones médicas, vacunas o turnos registrados, violando la regla **RN-02**.
  2. La Capa de Negocio frena la eliminación física y lanza la excepción `MascotaConHistorialException`.
  3. El Sistema devuelve un código **409 Conflict** con el mensaje: `"No se puede eliminar la mascota porque posee historial clínico o turnos asociados."`. Fin del caso de uso.

* **4a. Error de persistencia en base de datos (HTTP 500 Internal Server Error):**
  1. Si en el Paso 4 se produce un error imprevisto al guardar en la base de datos.
  2. El middleware global captura la excepción.
  3. El Sistema devuelve un código **500 Internal Server Error**. Fin del caso de uso.

### 5. SUB-VARIACIONES (opcional)
1. **Alta de mascota:** Creación de un nuevo paciente animal asociado a su dueño (`POST /api/mascotas`).
2. **Modificación de datos:** Actualización de peso, raza, sexo u observaciones (`PUT /api/mascotas/{id}`).
3. **Baja lógica:** Desactivación de la mascota en lugar de eliminación física para preservar registros históricos.

### 6. POSTCONDICIONES
- Se crea, modifica o da de baja el registro en la tabla `Mascotas`.
- La mascota queda habilitada y disponible en las listas de selección para agendar turnos y registrar atenciones médicas.

---

## Anexo: matrices de referencia

### Códigos HTTP usados

| Código HTTP | Nombre Técnico | Contexto de Aplicación en el Caso de Uso |
| --- | --- | --- |
| `201` | Created | Creación exitosa del recurso Mascota y su historia clínica vinculada. |
| `200` | OK | Retorno exitoso de consulta o actualización de datos de la mascota. |
| `204` | No Content | Eliminación exitosa del registro de la mascota (sin cuerpo de respuesta). |
| `400` | Bad Request | Parámetros inválidos, campos obligatorios ausentes o peso negativo. |
| `404` | Not Found | Dueño o mascota no encontrados en la base de datos. |
| `409` | Conflict | Violación de regla RN-02 (eliminación impedida por historial clínico o turnos activos). |
| `500` | Internal Server Error | Error no controlado durante la persistencia de datos. |

### Nota: Validación vs. Verificación aplicada

- **Validación (Presentación, → 400):** Presencia obligatoria de `nombre`, `especie`, `sexo` y `duenoId`, rangos numéricos positivos para peso y longitudes máximas (`MascotaCreateDTO`).
- **Verificación (Negocio, → 404/409):** Validación de existencia del dueño en la base de datos (**RN-01**, **RN-04**) y verificación de antecedentes clínicos antes de permitir la baja (**RN-02**).

### Matriz de trazabilidad CU-07 → Test

| Paso del CU | Excepción / Código | Test unitario (BusinessLogic) | Test integración (HTTP) |
| --- | --- | --- | --- |
| Flujo principal | `201 Created` | `CreateMascotaAsync_WithValidData_SavesMascotaAndHistoriaClinica` | `CreateMascota_WithValidData_Returns201Created` |
| 1a. JSON inválido | `400 Bad Request` | — (model binding en Presentación) | `CreateMascota_WithInvalidPayload_Returns400BadRequest` |
| 2a. Campos faltantes | `400 Bad Request` | — (validación DataAnnotations) | `CreateMascota_WithMissingNombre_Returns400BadRequest` |
| 3a. Dueño inexistente | `404 Not Found` | `CreateMascotaAsync_WhenDuenoNotExists_ThrowsDuenoNotFoundException` | `CreateMascota_WhenDuenoNotExists_Returns404NotFound` |
| 3c. Eliminar con historial | `409 Conflict` | `DeleteMascotaAsync_WhenHasMedicalHistory_ThrowsMascotaConHistorialException` | `DeleteMascota_WhenHasMedicalHistory_Returns409Conflict` |

> Regla de oro: cada flujo del caso de uso debe tener al menos un test. En los flujos resueltos en la Capa de Presentación el test aplicable es el de integración HTTP. Los tests se ejecutan con `dotnet test SistemaVeterinaria.slnx`.
