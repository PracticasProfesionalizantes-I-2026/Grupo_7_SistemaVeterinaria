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
  3. En lugar de conceder acceso a las funcionalidades del sistema, el sistema redirige al usuario a la pantalla de **Cambio obligatorio de contraseña**.
  4. El usuario ingresa una nueva contraseña y su confirmación.
  5. El sistema verifica que ambas coincidan y que la nueva contraseña cumpla las condiciones de seguridad establecidas.
  6. Si los datos son válidos, el sistema almacena la nueva contraseña de forma segura y desactiva la condición de cambio obligatorio.
  7. El sistema concede acceso al usuario según su rol correspondiente.
  8. Si el usuario abandona el proceso sin completar el cambio, la condición de cambio obligatorio permanece pendiente y el usuario no podrá acceder a las demás funcionalidades del sistema.

### 5. SUB-VARIACIONES (opcional)
1. El usuario puede autenticarse utilizando su nombre de usuario o su dirección de correo electrónico registrada.
2. El acceso se realiza a través de la interfaz web del sistema de gestión veterinaria.

### 6. POSTCONDICIONES
- Si la autenticación es exitosa sin condición de cambio pendiente, el usuario ingresa al sistema con la interfaz y permisos correspondientes a su rol (Recepcionista, Veterinario o Administrador), y el contador de intentos fallidos queda restablecido en cero.
- Si la autenticación es exitosa pero la contraseña es temporal, el usuario es redirigido al cambio obligatorio de contraseña; una vez completado, accede al sistema con sus permisos de rol.
- Si se alcanza el quinto intento fallido consecutivo, la cuenta pasa a estado bloqueado y requerirá la intervención del Administrador para su desbloqueo (el cual también restablecerá el contador a cero).
- Queda registrado el resultado del intento de inicio de sesión (éxito, fallo o bloqueo) para fines de auditoría y seguridad.

---

## Anexo: matrices de referencia

### Códigos HTTP usados

| Código HTTP | Nombre Técnico | Contexto de Aplicación en el Caso de Uso |
| --- | --- | --- |
| `200` | OK | Autenticación exitosa, restablecimiento del contador de intentos a cero y acceso concedido al usuario según su rol. |
| `302` | Found / Redirect | Redirección al flujo de cambio obligatorio de contraseña cuando la contraseña es temporal (A8). |
| `400` | Bad Request | Formato de solicitud inválido o campos obligatorios vacíos (A5); nueva contraseña inválida en cambio obligatorio (A8). |
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
| A8. Contraseña temporal — redirección | `302 / 200 OK` | `AuthenticateAsync_WithTemporaryPassword_DetectsRequiredChange` | `Login_WithTemporaryPassword_RedirectsToForcePasswordChange` |
| A8. Contraseña temporal — cambio exitoso | `200 OK` | `ChangePassword_WithValidNewPassword_ClearsTemporaryFlag` | `ForceChangePassword_WithValidData_Returns200OK` |
| A8. Contraseña temporal — datos inválidos | `400 Bad Request` | `ChangePassword_WithMismatchedPasswords_ThrowsValidationException` | `ForceChangePassword_WithMismatchedPasswords_Returns400BadRequest` |

> Regla de oro: cada flujo del caso de uso debe tener al menos un test. En los flujos resueltos en la Capa de Presentación el test aplicable es el de integración HTTP.
