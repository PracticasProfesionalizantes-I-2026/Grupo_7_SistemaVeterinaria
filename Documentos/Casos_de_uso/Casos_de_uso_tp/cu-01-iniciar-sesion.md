# Caso de Uso: Iniciar Sesión

> Especificación elaborada siguiendo la guía `GUIA-Especificacion-Casos-de-Uso.md` (sección 3).
> Control de autenticación, control de intentos fallidos consecutivos, bloqueo de cuentas, contraseña temporal y autorización de usuarios según rol.

| Campo | Valor |
| --- | --- |
| **ID del Caso de Uso** | CU-01 |
| **Nombre** | Iniciar Sesión |
| **Actor Principal** | Recepcionista / Veterinario/a / Administrador |
| **Alcance / Nivel** | Sistema; meta de usuario |
| **Stakeholders e intereses** | Personal de la Veterinaria → ingresar de forma segura a sus módulos operativos según su rol; Administración → garantizar la seguridad, auditar accesos, prevenir accesos no autorizados mediante control de intentos fallidos y resguardar la información del sistema |
| **Disparador (Trigger)** | El usuario ingresa a la aplicación y solicita autenticarse en la pantalla de inicio de sesión |
| **Prioridad / Frecuencia** | Alta; muy alta frecuencia (al inicio de cada jornada laboral o al iniciar una sesión) |
| **Reglas de negocio relacionadas** | Ninguna (aplica control de acceso, restricciones funcionales de autenticación y seguridad) |

---

### 1. BREVE DESCRIPCIÓN
Permite a cualquier usuario autorizado (Recepcionista, Veterinario o Administrador) autenticarse en el sistema mediante sus credenciales (nombre de usuario/correo electrónico y contraseña) para acceder a las funciones permitidas según su rol. El sistema controla los intentos fallidos de inicio de sesión, bloquea la cuenta al alcanzar el límite de cinco intentos consecutivos y gestiona el cambio obligatorio de contraseña cuando el usuario posee una contraseña temporal asignada por el Administrador.

### 2. PRECONDICIONES
- El usuario debe encontrarse previamente registrado en el sistema.
- El sistema y el servicio de autenticación deben encontrarse disponibles y operativos.

### 3. FLUJO PRINCIPAL (Camino Feliz)
1. El usuario accede a la pantalla de inicio de sesión.
2. El sistema solicita sus credenciales (nombre de usuario/correo electrónico y contraseña).
3. El usuario ingresa sus credenciales y solicita iniciar sesión.
4. El sistema verifica que la cuenta exista y pueda autenticarse.
5. Si las credenciales son correctas y la cuenta no está bloqueada ni inactiva, el sistema permite el acceso según el rol correspondiente (Recepcionista, Veterinario o Administrador).
6. El sistema restablece a cero el contador de intentos fallidos de la cuenta.

### 4. FLUJOS ALTERNATIVOS (Caminos Tristes / Excepciones)

* **A1 — Credenciales incorrectas:**
  1. En el Paso 4, el usuario ingresa una contraseña incorrecta para una cuenta existente y no bloqueada.
  2. El sistema registra e incrementa en uno el contador de intentos fallidos para esa cuenta.
  3. Si todavía no alcanzó el quinto intento fallido consecutivo, el sistema informa que las credenciales son incorrectas.
  4. El usuario puede volver a intentar iniciar sesión.

* **A2 — Quinto intento fallido:**
  1. En el Paso 4, el usuario ingresa una contraseña incorrecta por quinta vez consecutiva.
  2. El sistema incrementa el contador alcanzando el quinto intento fallido consecutivo y bloquea automáticamente la cuenta.
  3. El sistema informa: "Usuario bloqueado. Contacte al Administrador".
  4. No se permite el acceso al sistema. Fin del caso de uso.

* **A3 — Cuenta previamente bloqueada:**
  1. En el Paso 4, el usuario intenta iniciar sesión con una cuenta que se encuentra bloqueada.
  2. El sistema detecta el estado de bloqueo e impide el acceso, independientemente de la contraseña ingresada.
  3. El sistema informa: "Usuario bloqueado. Contacte al Administrador".
  4. No se permite el acceso al sistema. Fin del caso de uso.

* **A4 — Inicio de sesión correcto antes del límite:**
  1. En el Paso 4, el usuario ingresa credenciales correctas para una cuenta no bloqueada que registra intentos fallidos previos acumulados (entre 1 y 4).
  2. El sistema permite el acceso según el rol correspondiente.
  3. El sistema restablece a cero el contador de intentos fallidos de la cuenta.

* **A5 — Credenciales con campos obligatorios vacíos:**
  1. En el Paso 3, el usuario intenta iniciar sesión omitiendo el nombre de usuario/correo electrónico o la contraseña.
  2. El sistema informa que el nombre de usuario y la contraseña son obligatorios.
  3. El usuario puede completar los datos requeridos e intentar nuevamente.

* **A6 — Cuenta inactiva o deshabilitada:**
  1. En el Paso 4, el usuario intenta iniciar sesión con una cuenta que se encuentra en estado inactivo o deshabilitada administrativamente (distinto al bloqueo por intentos fallidos).
  2. El sistema informa que el usuario no se encuentra habilitado para operar en el sistema.
  3. No se permite el acceso al sistema. Fin del caso de uso.

* **A7 — Indisponibilidad o falla del sistema:**
  1. En el Paso 4 o 5, ocurre un fallo imprevisto de comunicación o indisponibilidad en la base de datos o servicio de autenticación.
  2. El sistema informa la imposibilidad de procesar la solicitud en ese momento.
  3. No se permite el acceso al sistema. Fin del caso de uso.

* **A8 — Inicio de sesión con contraseña temporal (cambio obligatorio):**
  1. En el Paso 4, el sistema verifica que las credenciales son correctas y que la cuenta no está bloqueada, pero detecta que la contraseña del usuario es temporal y que el cambio es obligatorio (fue restablecida por el Administrador).
  2. El sistema restablece a cero el contador de intentos fallidos.
  3. En lugar de emitir un token JWT de acceso y conceder ingreso al sistema, la API devuelve una respuesta **200 OK** con `requiereCambioPassword: true` y sin token de sesión (`token: null`). Esto instruye al cliente a dirigir al usuario a la pantalla de **Cambio obligatorio de contraseña** (endpoint `POST /api/auth/cambiar-password-temporal`).
  4. El usuario ingresa su contraseña actual (temporal), una nueva contraseña y su confirmación.
  5. El sistema verifica que la contraseña actual sea correcta, que ambas contraseñas nuevas coincidan y que cumplan los requisitos de complejidad (al menos 8 caracteres, incluyendo mayúscula, minúscula y número).
  6. Si los datos son válidos, el sistema almacena el hash de la nueva contraseña y desactiva la condición de cambio obligatorio (`RequiereCambioPassword = false`).
  7. El usuario puede ahora iniciar sesión con su nueva contraseña definitiva y obtener su token de acceso según su rol correspondiente.
  8. Si el usuario abandona el proceso sin completar el cambio, la condición de cambio obligatorio permanece pendiente y no se emitirá token de acceso hasta que se concrete el cambio.

### 5. SUB-VARIACIONES (opcional)
1. El usuario puede autenticarse utilizando su nombre de usuario o su dirección de correo electrónico registrada.
2. El acceso se realiza a través de la interfaz web o cliente HTTP del sistema de gestión veterinaria.

### 6. POSTCONDICIONES
- Si la autenticación es exitosa sin condición de cambio pendiente, se emite un Token JWT firmado y el usuario ingresa al sistema con la interfaz y permisos correspondientes a su rol (Recepcionista, Veterinario o Administrador), quedando el contador de intentos fallidos restablecido en cero.
- Si la autenticación es exitosa pero la contraseña es temporal, no se emite token de sesión y se indica al cliente la obligación de completar el cambio de contraseña; una vez completado, el usuario podrá autenticarse normalmente.
- Si se alcanza el quinto intento fallido consecutivo, la cuenta pasa a estado bloqueado y requerirá la intervención del Administrador para su desbloqueo (el cual también restablecerá el contador a cero).
- Queda registrado el resultado del intento de inicio de sesión (éxito, fallo o bloqueo) para fines de auditoría y seguridad.

---

## Anexo: matrices de referencia

### Códigos HTTP usados

| Código HTTP | Nombre Técnico | Contexto de Aplicación en el Caso de Uso |
| --- | --- | --- |
| `200` | OK | Autenticación exitosa, restablecimiento del contador de intentos a cero y emisión de token JWT según rol. En A8 también se retorna 200 OK indicando `requiereCambioPassword: true` (omitiendo la entrega del token) para que el cliente gestione el cambio obligatorio. |
| `302` | Found / Redirect | *(Aclaración arquitectural)*: En aplicaciones web con vistas renderizadas en servidor representaría redirección HTTP; en esta API RESTful desacoplada se responde con 200 OK y carga informativa `{ "requiereCambioPassword": true, "token": null }` sin emitir token de sesión. |
| `400` | Bad Request | Formato de solicitud inválido o campos obligatorios vacíos (A5); contraseñas que no coinciden o incumplen complejidad en cambio obligatorio (A8). |
| `401` | Unauthorized | Contraseña incorrecta (A1); incrementa el contador de intentos fallidos si no se ha alcanzado el límite. |
| `403` | Forbidden | Acceso denegado por quinto intento fallido consecutivo (A2), cuenta previamente bloqueada (A3) o cuenta inactiva (A6). |
| `500` | Internal Server Error | Error no controlado en el servicio de autenticación o indisponibilidad en la persistencia (A7). |

### Nota: Validación vs. Verificación aplicada

- **Validación (Presentación):** Presencia obligatoria de campos (nombre de usuario/correo electrónico y contraseña no vacíos), sintaxis de la solicitud y coincidencia de nueva contraseña con su confirmación en el cambio obligatorio.
- **Verificación (Negocio / Seguridad):** Búsqueda de la cuenta, verificación de contraseña, verificación de estado de la cuenta (activa/bloqueada/inactiva), detección de condición de contraseña temporal, incremento del contador de intentos fallidos, bloqueo automático al 5to intento consecutivo, restablecimiento del contador a cero en inicio exitoso o desbloqueo administrativo, y asignación de perfil/rol.

### Matriz de trazabilidad CU-01 → Test

| Paso del CU | Excepción / Código | Test unitario (BusinessLogic) | Test integración (HTTP) |
| --- | --- | --- | --- |
| Flujo principal | `200 OK` | `AuthenticateAsync_WithValidCredentials_ReturnsSuccessAndResetsFailedAttempts` | `Login_WithValidCredentials_Returns200OK` |
| A1. Credenciales incorrectas | `401 Unauthorized` | `AuthenticateAsync_WithWrongPassword_IncrementsFailedAttempts` | `Login_WithInvalidPassword_Returns401Unauthorized` |
| A2. Quinto intento fallido | `403 Forbidden` | `AuthenticateAsync_OnFifthFailedAttempt_LocksUserAccount` | `Login_OnFifthFailedAttempt_Returns403Forbidden_UserLocked` |
| A3. Cuenta previamente bloqueada | `403 Forbidden` | `AuthenticateAsync_WithLockedAccount_ThrowsAccountLockedException` | `Login_WithLockedAccount_Returns403Forbidden` |
| A4. Login correcto con intentos previos | `200 OK` | `AuthenticateAsync_WithValidCredentialsAfterFailures_ResetsCounterToZero` | `Login_WithValidCredentialsAfterFailures_Returns200OK` |
| A5. Campos obligatorios vacíos | `400 Bad Request` | — (validación en presentación / DataAnnotations) | `Login_WithEmptyCredentials_Returns400BadRequest` |
| A6. Cuenta inactiva | `403 Forbidden` | `AuthenticateAsync_WithInactiveUser_ThrowsUserInactiveException` | `Login_WithInactiveUser_Returns403Forbidden` |
| A7. Error del sistema | `500 Internal Server Error` | `AuthenticateAsync_OnDatabaseFailure_ThrowsPersistenceException` | `Login_OnInternalError_Returns500InternalServerError` |
| A8. Contraseña temporal — detección | `200 OK` | `AuthenticateAsync_WithTemporaryPassword_ReturnsRequiresPasswordChangeFlagWithoutToken` | `Login_WithTemporaryPassword_Returns200OK_WithRequiresChangeFlagAndNoToken` |
| A8. Contraseña temporal — cambio exitoso | `200 OK` | `CambiarPasswordTemporalAsync_WithValidData_UpdatesPasswordAndClearsFlag` | `CambiarPasswordTemporal_WithValidData_Returns200OK_AndEnablesNormalLogin` |
| A8. Contraseña temporal — confirmación dispar | `400 Bad Request` | `CambiarPasswordTemporalAsync_WithMismatchedConfirmation_ThrowsValidationException` | `CambiarPasswordTemporal_WithMismatchedPasswords_Returns400BadRequest` |
| A8. Contraseña temporal — baja complejidad | `400 Bad Request` | `CambiarPasswordTemporalAsync_WithSimplePassword_ThrowsValidationException` | `CambiarPasswordTemporal_WithSimplePassword_Returns400BadRequest` |

> Regla de oro: cada flujo del caso de uso debe tener al menos un test. En los flujos resueltos en la Capa de Presentación el test aplicable es el de integración HTTP.
