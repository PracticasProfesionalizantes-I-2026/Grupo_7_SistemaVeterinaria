# Caso de Uso: Registrar Dueño

> Especificación elaborada siguiendo la guía `GUIA-Especificacion-Casos-de-Uso.md` (sección 3).
> Reglas de negocio RN-01 (DNI único), RN-02 (campos obligatorios) y normalización de espacios implementadas en la Capa de Negocio con su respectiva cobertura de pruebas unitarias e integración.

| Campo | Valor |
| --- | --- |
| **ID del Caso de Uso** | CU-02 |
| **Nombre** | Registrar Dueño |
| **Actor Principal** | Recepcionista |
| **Alcance / Nivel** | Sistema; meta de usuario |
| **Stakeholders e intereses** | Recepcionista → dar de alta de forma rápida y confiable a los clientes de la clínica; Dueño de la Mascota → quedar registrado en el padrón para poder vincular a sus animales y gestionar turnos; Veterinaria → asegurar la unicidad y veracidad de los datos de contacto |
| **Disparador (Trigger)** | La recepcionista selecciona la opción "Registrar Dueño" desde el módulo de administración de dueños |
| **Prioridad / Frecuencia** | Alta; media/alta frecuencia (altas de nuevos clientes) |
| **Reglas de negocio relacionadas** | RN-01 (DNI único por dueño); RN-02 (campos obligatorios de contacto); RN-03 (asociación 1 a N con mascotas) |

---

### 1. BREVE DESCRIPCIÓN
Permite a la recepcionista registrar un nuevo dueño en el sistema ingresando sus datos personales y de contacto (nombre, apellido, DNI, teléfono, domicilio y correo electrónico) para que quede disponible y pueda vincularse posteriormente a una o más mascotas.

### 2. PRECONDICIONES
- La recepcionista debe contar con una sesión activa y un Token JWT válido con permisos de escritura sobre el recurso Dueños.
- La Capa de Persistencia debe estar disponible y accesible.
- El cliente no debe encontrarse registrado previamente con el mismo número de DNI (**RN-01**).

### 3. FLUJO PRINCIPAL (Camino Feliz - HTTP 201)
1. El Actor envía una petición al endpoint `POST /api/duenos` con un cuerpo JSON que contiene los datos del dueño (`nombre`, `apellido`, `dni`, `telefono`, `domicilio`, `email`).
2. La **Capa de Presentación** (`DuenosController.CreateDueno`) valida que el JSON sea estructuralmente correcto y que los campos requeridos estén presentes (`[Required]`, `[MaxLength]`, `[EmailAddress]` en `DuenoCreateDTO`).
3. La **Capa de Negocio** (`DuenoService.CreateDuenoAsync`) normaliza las cadenas de texto aplicando `Trim()`, verifica la regla de negocio **RN-01** comprobando que no exista otro dueño con el mismo DNI (`ExistsByDniAsync`) y construye la entidad `Dueno`.
4. La **Capa de Persistencia** genera un nuevo identificador único (`Id`) y guarda el registro en la tabla `Duenos`.
5. El Sistema devuelve un código **201 Created** con el recurso creado (`DuenoResponseDTO`) y el encabezado `Location` apuntando a `GET /api/duenos/{id}`.

### 4. FLUJOS ALTERNATIVOS (Caminos Tristes / Excepciones)

* **1a. JSON inválido o malformado (HTTP 400 Bad Request):**
  1. Si en el Paso 1 el cuerpo de la petición no tiene un formato JSON válido o se encuentra vacío.
  2. El Sistema (Capa de Presentación / model binding) rechaza la solicitud.
  3. El Sistema devuelve un código **400 Bad Request**. Fin del caso de uso.

* **2a. Dato obligatorio faltante (HTTP 400 Bad Request):**
  1. Si en el Paso 2 el JSON no incluye `nombre`, `apellido`, `dni` o `telefono` (**RN-02**).
  2. La Capa de Presentación detecta la falla de validación (`ModelState.IsValid == false`).
  3. El Sistema devuelve un código **400 Bad Request** detallando el o los campos requeridos faltantes. Fin del caso de uso.

* **2b. Formato de datos inválido (HTTP 400 Bad Request):**
  1. Si en el Paso 2 el `dni` contiene caracteres no numéricos, o el `email` no cumple el estándar de formato de correo electrónico.
  2. La Capa de Presentación rechaza la petición por validación de esquema.
  3. El Sistema devuelve un código **400 Bad Request** con el mensaje de formato inválido. Fin del caso de uso.

* **2c. Campo con solo espacios en blanco (HTTP 400 Bad Request):**
  1. Si en el Paso 2 un campo obligatorio llega compuesto únicamente por espacios (ej. `"   "`).
  2. La Capa de Negocio aplica `Trim()` y al quedar vacío lanza una excepción de validación (`ValidationException`).
  3. El Sistema devuelve un código **400 Bad Request** indicando que el campo no puede estar en blanco. Fin del caso de uso.

* **3a. DNI duplicado (HTTP 409 Conflict):**
  1. Si en el Paso 3 la verificación de dominio detecta que ya existe un dueño registrado con ese número de DNI, violando la regla **RN-01**.
  2. La Capa de Negocio frena la ejecución y lanza la excepción `DniDuplicadoException`.
  3. El Sistema devuelve un código **409 Conflict** con el mensaje: `"Ya existe un dueño registrado con el DNI {dni}."`. Fin del caso de uso.

* **4a. Error interno en la persistencia (HTTP 500 Internal Server Error):**
  1. Si en el Paso 4 ocurre un fallo inesperado al interactuar con la base de datos.
  2. El middleware de excepciones captura el error no controlado.
  3. El Sistema devuelve un código **500 Internal Server Error**. Fin del caso de uso.

### 5. SUB-VARIACIONES (opcional)
1. El actor puede realizar el alta desde el formulario web del sistema, desde la colección de pruebas HTTP de Bruno (`POST Create Dueno.bru`) o desde Swagger UI. En todos los casos el esquema del payload y la respuesta son idénticos.

### 6. POSTCONDICIONES
- Se crea un nuevo registro persistente en la tabla `Duenos` con identificador único.
- El nuevo dueño queda visible en las búsquedas (`GET /api/duenos`) y habilitado para asociarle mascotas (**RN-03**).

---

## Anexo: matrices de referencia

### Códigos HTTP usados

| Código HTTP | Nombre Técnico | Contexto de Aplicación en el Caso de Uso |
| --- | --- | --- |
| `201` | Created | Persistencia exitosa del nuevo recurso Dueño en el sistema. |
| `400` | Bad Request | Formato JSON incorrecto, campos obligatorios ausentes o formato de email/DNI inválido. |
| `409` | Conflict | Violación de regla de unicidad de DNI (RN-01: DNI ya registrado). |
| `500` | Internal Server Error | Falla no controlada durante la persistencia en la base de datos. |

### Nota: Validación vs. Verificación aplicada

- **Validación (Presentación, → 400):** Presencia de campos obligatorios (`nombre`, `apellido`, `dni`, `telefono`), longitudes máximas y formato de correo electrónico mediante DataAnnotations en `DuenoCreateDTO`.
- **Verificación (Negocio, → 400/409):** Normalización de strings (`Trim()`), verificación de strings de solo espacios (`ValidationException` → 400) y verificación de unicidad de DNI contra la base de datos (`ExistsByDniAsync` → `DniDuplicadoException` → 409).

### Matriz de trazabilidad CU-02 → Test

| Paso del CU | Excepción / Código | Test unitario (BusinessLogic) | Test integración (HTTP) |
| --- | --- | --- | --- |
| Flujo principal | `201 Created` | `CreateDuenoAsync_WithValidData_TrimsAndSavesDueno` | `CreateDueno_WithValidData_Returns201Created` |
| 1a. JSON inválido | `400 Bad Request` | — (model binding en Presentación) | `CreateDueno_WithMalformedJson_Returns400BadRequest` |
| 2a. Campo obligatorio faltante | `400 Bad Request` | — (validación DataAnnotations) | `CreateDueno_WithMissingRequiredFields_Returns400BadRequest` |
| 2b. Formato inválido | `400 Bad Request` | — (validación DataAnnotations) | `CreateDueno_WithInvalidEmailFormat_Returns400BadRequest` |
| 2c. Campo con solo espacios | `400 Bad Request` | `CreateDuenoAsync_WithWhitespaceFields_ThrowsValidationException` | `CreateDueno_WithWhitespaceOnlyField_Returns400BadRequest` |
| 3a. DNI duplicado | `409 Conflict` | `CreateDuenoAsync_WhenDniAlreadyExists_ThrowsDniDuplicadoException` | `CreateDueno_WhenDuplicateDni_Returns409Conflict` |

> Regla de oro: cada flujo del caso de uso debe tener al menos un test. En los flujos resueltos en la Capa de Presentación el test aplicable es el de integración HTTP. Los tests se ejecutan con `dotnet test SistemaVeterinaria.slnx`.
