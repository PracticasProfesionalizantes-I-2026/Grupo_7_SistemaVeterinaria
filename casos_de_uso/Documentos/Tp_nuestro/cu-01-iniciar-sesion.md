# Caso de Uso: Iniciar Sesión

> Especificación elaborada siguiendo la guía `GUIA-Especificacion-Casos-de-Uso.md` (sección 3).
> Control de autenticación y autorización basado en credenciales y emisión de Tokens JWT con roles asignados.

| Campo | Valor |
| --- | --- |
| **ID del Caso de Uso** | CU-01 |
| **Nombre** | Iniciar Sesión |
| **Actor Principal** | Recepcionista / Veterinario/a / Dueño de la Veterinaria |
| **Alcance / Nivel** | Sistema; meta de usuario |
| **Stakeholders e intereses** | Personal de la Veterinaria → ingresar de forma segura a sus módulos operativos según su rol; Administración → garantizar la seguridad, auditoría de accesos y protección de datos mediante autenticación por JWT |
| **Disparador (Trigger)** | El usuario ingresa sus credenciales en la pantalla de inicio de sesión y selecciona "Iniciar Sesión" |
| **Prioridad / Frecuencia** | Alta; muy alta frecuencia (al inicio de cada jornada laboral o expiración de token) |
| **Reglas de negocio relacionadas** | RN-01 (usuarios registrados y activos); RN-02 (protección y hash de credenciales); RN-03 (control de acceso basado en roles) |

---

### 1. BREVE DESCRIPCIÓN
Permite a cualquier usuario autorizado (Recepcionista, Veterinario o Dueño) autenticarse en el sistema mediante sus credenciales (usuario/email y contraseña) para obtener un token de sesión JWT y acceder a las funciones permitidas según su rol.

### 2. PRECONDICIONES
- El usuario debe estar previamente registrado en la tabla `Usuarios` del sistema.
- La cuenta del usuario debe encontrarse en estado activo (**RN-01**).
- La Capa de Persistencia debe estar disponible y accesible.

### 3. FLUJO PRINCIPAL (Camino Feliz - HTTP 200)
1. El Actor envía una petición al endpoint `POST /api/auth/login` con un cuerpo JSON que contiene sus credenciales (`nombreUsuario` o `email`, y `password`).
2. La **Capa de Presentación** (`AuthController.Login`) valida que la estructura del JSON sea válida y que los campos requeridos no estén vacíos (`[Required]` sobre `LoginRequestDTO`).
3. La **Capa de Negocio** (`AuthService.AuthenticateAsync`) busca al usuario en la base de datos, verifica el hash de la contraseña (**RN-02**) y comprueba que el usuario esté activo (**RN-01**).
4. La **Capa de Negocio** construye los claims de identidad con el rol correspondiente (**RN-03**) y genera un Token JWT firmado digitalmente.
5. La **Capa de Persistencia** actualiza la fecha y hora del último acceso del usuario en la tabla `Usuarios`.
6. El Sistema devuelve un código **200 OK** con el Token JWT, tiempo de expiración y los datos de perfil y rol del usuario (`LoginResponseDTO`).

### 4. FLUJOS ALTERNATIVOS (Caminos Tristes / Excepciones)

* **1a. JSON inválido o malformado (HTTP 400 Bad Request):**
  1. Si en el Paso 1 el cuerpo de la petición no tiene un formato JSON válido o llega vacío.
  2. El Sistema (Capa de Presentación / model binding) rechaza la solicitud.
  3. El Sistema devuelve un código **400 Bad Request** con el mensaje descriptivo del error de sintaxis. Fin del caso de uso.

* **2a. Credenciales con campos obligatorios vacíos (HTTP 400 Bad Request):**
  1. Si en el Paso 2 falta el usuario o la contraseña, o alguno de ellos contiene una cadena vacía (`""`) o solo espacios (`"   "`).
  2. La Capa de Presentación detecta la falla de validación del modelo (`ModelState.IsValid == false`).
  3. El Sistema devuelve un código **400 Bad Request** indicando: `"El nombre de usuario/email y la contraseña son obligatorios."`. Fin del caso de uso.

* **3a. Usuario inexistente o contraseña incorrecta (HTTP 401 Unauthorized):**
  1. Si en el Paso 3 el nombre de usuario no existe en la base de datos o el hash de la contraseña no coincide con el almacenado (**RN-02**).
  2. La Capa de Negocio lanza la excepción `InvalidCredentialsException`.
  3. El Sistema devuelve un código **401 Unauthorized** con el mensaje: `"Credenciales inválidas. Verifique su usuario y contraseña."`. Fin del caso de uso.

* **3b. Usuario inactivo o deshabilitado (HTTP 403 Forbidden):**
  1. Si en el Paso 3 las credenciales son correctas pero el usuario tiene estado inactivo o bloqueado (**RN-01**).
  2. La Capa de Negocio lanza la excepción `UserInactiveException`.
  3. El Sistema devuelve un código **403 Forbidden** con el mensaje: `"El usuario no se encuentra habilitado para operar en el sistema."`. Fin del caso de uso.

* **5a. Error interno al generar la sesión o registrar acceso (HTTP 500 Internal Server Error):**
  1. Si en el Paso 5 ocurre una falla técnica en la base de datos o en la firma criptográfica del token.
  2. El Sistema captura el error no controlado a través del middleware global de excepciones.
  3. El Sistema devuelve un código **500 Internal Server Error** registrando el evento en los logs. Fin del caso de uso.

### 5. SUB-VARIACIONES (opcional)
1. El usuario puede autenticarse utilizando su nombre de usuario o su dirección de correo electrónico institucional.
2. La petición puede ser emitida desde la interfaz web SPA, desde una aplicación móvil o mediante herramientas de prueba de API (Bruno, Swagger, Postman). En todos los casos el contrato HTTP y el DTO de respuesta son idénticos.

### 6. POSTCONDICIONES
- Se emite un Token JWT firmado que acredita la identidad y rol del usuario.
- El cliente almacena el token para autorizar las solicitudes subsiguientes en el encabezado `Authorization: Bearer <token>`.
- Queda registrada la fecha/hora del último acceso exitoso en la tabla `Usuarios`.

---

## Anexo: matrices de referencia

### Códigos HTTP usados

| Código HTTP | Nombre Técnico | Contexto de Aplicación en el Caso de Uso |
| --- | --- | --- |
| `200` | OK | Autenticación exitosa y retorno del Token JWT con perfil del usuario. |
| `400` | Bad Request | Formato JSON incorrecto o datos obligatorios faltantes (usuario o contraseña en blanco). |
| `401` | Unauthorized | Fallo de autenticación por usuario inexistente o contraseña incorrecta. |
| `403` | Forbidden | Acceso denegado debido a que la cuenta del usuario está inactiva (RN-01). |
| `500` | Internal Server Error | Error no controlado en la firma del token o en el acceso a la base de datos. |

### Nota: Validación vs. Verificación aplicada

- **Validación (Presentación, → 400):** Se verifica sintaxis JSON, presencia obligatoria de campos (`[Required]` sobre `LoginRequestDTO`) y longitudes mínimas/máximas.
- **Verificación (Negocio, → 401/403):** Se verifica la existencia del usuario, correspondencia del hash criptográfico de contraseña (**RN-02**), verificación de estado activo (**RN-01**) y asignación de claims de roles (**RN-03**).

### Matriz de trazabilidad CU-01 → Test

| Paso del CU | Excepción / Código | Test unitario (BusinessLogic) | Test integración (HTTP) |
| --- | --- | --- | --- |
| Flujo principal | `200 OK` | `AuthenticateAsync_WithValidCredentials_ReturnsJwtTokenDTO` | `Login_WithValidCredentials_Returns200OKAndToken` |
| 1a. JSON inválido | `400 Bad Request` | — (model binding de ASP.NET Core) | `Login_WithMalformedJson_Returns400BadRequest` |
| 2a. Campos faltantes | `400 Bad Request` | — (validación DataAnnotations en Controller) | `Login_WithEmptyCredentials_Returns400BadRequest` |
| 3a. Credenciales erróneas | `401 Unauthorized` | `AuthenticateAsync_WithWrongPassword_ThrowsInvalidCredentialsException` | `Login_WithInvalidPassword_Returns401Unauthorized` |
| 3b. Usuario inactivo | `403 Forbidden` | `AuthenticateAsync_WithInactiveUser_ThrowsUserInactiveException` | `Login_WithInactiveUser_Returns403Forbidden` |

> Regla de oro: cada flujo del caso de uso debe tener al menos un test. En los flujos resueltos en la Capa de Presentación el test aplicable es el de integración HTTP. Los tests se ejecutan con `dotnet test SistemaVeterinaria.slnx`.
