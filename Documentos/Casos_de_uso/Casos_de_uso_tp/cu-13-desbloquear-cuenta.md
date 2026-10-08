# Caso de Uso: Desbloquear Cuenta de Usuario

> Especificación elaborada siguiendo la guía `GUIA-Especificacion-Casos-de-Uso.md` (sección 3).
> Gestión administrativa de cuentas de usuario — Desbloqueo de cuentas bloqueadas por el Administrador, sin modificación de la contraseña vigente.

| Campo | Valor |
| --- | --- |
| **ID del Caso de Uso** | CU-13 |
| **Nombre** | Desbloquear Cuenta de Usuario |
| **Actor Principal** | Administrador |
| **Alcance / Nivel** | Sistema; meta de usuario |
| **Stakeholders e intereses** | Administrador → restablecer el acceso de un usuario legítimo cuya cuenta fue bloqueada por superar el límite de intentos fallidos; Usuario afectado → recuperar su acceso al sistema; Sistema → garantizar que el desbloqueo sea una operación controlada, auditada y exclusiva del Administrador |
| **Disparador (Trigger)** | El Administrador selecciona la opción "Desbloquear cuenta" para un usuario con estado Bloqueado desde la sección Gestión de Usuarios |
| **Prioridad / Frecuencia** | Alta; frecuencia media-baja (ante bloqueos por intentos fallidos reportados por el personal) |
| **Reglas de negocio relacionadas** | Ninguna (aplica restricciones funcionales de seguridad y control de acceso; el límite de 5 intentos está definido en los requerimientos funcionales de autenticación) |

---

### 1. BREVE DESCRIPCIÓN
Permite al Administrador desbloquear la cuenta de un usuario que se encuentra en estado Bloqueado como consecuencia de haber superado el límite de cinco intentos fallidos consecutivos de inicio de sesión. Al confirmar el desbloqueo, el sistema cambia el estado de la cuenta a Activo y restablece a cero el contador de intentos fallidos, sin modificar la contraseña vigente. La operación queda registrada con el Administrador responsable y la fecha y hora de ejecución. El desbloqueo de una cuenta y el restablecimiento de su contraseña son operaciones independientes.

### 2. PRECONDICIONES
- El Administrador debe contar con una sesión activa en el sistema.
- El usuario seleccionado debe encontrarse en estado **Bloqueado**.
- El sistema debe estar disponible y operativo.

### 3. FLUJO PRINCIPAL (Camino Feliz)
1. El Administrador accede a la sección Gestión de Usuarios y selecciona un usuario con estado **Bloqueado** (puede llegar desde CU-12).
2. El sistema muestra el detalle del usuario, indicando su estado: **Bloqueado**.
3. El Administrador selecciona la opción **Desbloquear cuenta**.
4. El sistema solicita confirmación de la operación, informando que la cuenta pasará al estado Activo y el contador de intentos fallidos se restablecerá a cero.
5. El Administrador confirma la operación.
6. El sistema cambia el estado de la cuenta a **Activo** y restablece el contador de intentos fallidos a cero, sin modificar la contraseña vigente.
7. El sistema registra la operación: usuario afectado, tipo de operación (Desbloqueo), Administrador responsable y fecha y hora.
8. El sistema informa que el desbloqueo se realizó correctamente.

### 4. FLUJOS ALTERNATIVOS (Caminos Tristes / Excepciones)

* **A1 — La cuenta no está bloqueada:**
  1. En el Paso 2 o 3, el sistema detecta que la cuenta seleccionada no se encuentra en estado Bloqueado (por ejemplo, ya fue desbloqueada previamente por otra acción concurrente).
  2. El sistema informa que la cuenta no se encuentra bloqueada y que la operación no puede realizarse.
  3. Fin del caso de uso.

* **A2 — El Administrador cancela la confirmación:**
  1. En el Paso 5, el Administrador decide no confirmar la operación.
  2. El sistema cancela el proceso y conserva el estado anterior de la cuenta sin realizar cambios.
  3. Fin del caso de uso.

* **A3 — Error durante el desbloqueo:**
  1. En el Paso 6 o 7, ocurre una falla imprevista en el sistema al intentar actualizar el estado de la cuenta o registrar la operación.
  2. El sistema informa que no fue posible completar el desbloqueo y conserva el estado anterior de la cuenta (no registra el desbloqueo como exitoso si no pudo completarse).
  3. Fin del caso de uso.

* **A4 — Acceso denegado por rol insuficiente:**
  1. Un usuario con rol distinto al Administrador intenta ejecutar esta operación.
  2. El sistema deniega el acceso e informa que esta acción es de uso exclusivo del Administrador.
  3. Fin del caso de uso.

### 5. SUB-VARIACIONES (opcional)
1. El Administrador puede optar por realizar también el restablecimiento de contraseña (CU-14) de forma independiente, antes o después del desbloqueo, si el usuario también olvidó su contraseña.

### 6. POSTCONDICIONES
- La cuenta del usuario pasa de estado **Bloqueado** a estado **Activo**.
- El contador de intentos fallidos queda restablecido en cero.
- La contraseña vigente del usuario no es modificada.
- Queda registrada la operación de desbloqueo con el Administrador responsable, la fecha y la hora.
- El usuario podrá iniciar sesión utilizando su contraseña vigente, siempre que cumpla las demás condiciones de autenticación.

---

## Anexo: matrices de referencia

### Códigos HTTP usados

| Código HTTP | Nombre Técnico | Contexto de Aplicación en el Caso de Uso |
| --- | --- | --- |
| `200` | OK | Desbloqueo realizado correctamente; estado de la cuenta actualizado y operación registrada. |
| `400` | Bad Request | La cuenta seleccionada no se encuentra en estado Bloqueado (A1). |
| `403` | Forbidden | Acceso denegado por rol insuficiente (A4). |
| `409` | Conflict | Conflicto de estado: la cuenta ya fue desbloqueada por otra acción concurrente (A1 — variante concurrente). |
| `500` | Internal Server Error | Error no controlado durante la actualización del estado de la cuenta o el registro de la operación (A3). |

### Nota: Validación vs. Verificación aplicada

- **Validación (Presentación):** Verificación de que el Administrador posea sesión activa y rol correspondiente; confirmación explícita de la operación antes de ejecutarla.
- **Verificación (Negocio / Seguridad):** Verificación del estado actual de la cuenta (debe ser Bloqueado); actualización atómica del estado y del contador; registro de auditoría de la operación; garantía de que no se modifica la contraseña; integridad transaccional (no registrar como éxito si ocurre un error).

### Matriz de trazabilidad CU-13 → Test

| Paso del CU | Excepción / Código | Test unitario (BusinessLogic) | Test integración (HTTP) |
| --- | --- | --- | --- |
| Flujo principal | `200 OK` | `UnlockAccountAsync_WithLockedUser_SetsActiveAndResetsCounter` | `UnlockAccount_WithLockedUser_Returns200OK` |
| A1. Cuenta no bloqueada | `400 Bad Request` | `UnlockAccountAsync_WithNonLockedUser_ThrowsInvalidOperationException` | `UnlockAccount_WithActiveUser_Returns400BadRequest` |
| A2. Administrador cancela | — (cancelación en UI, sin llamada al backend) | — | — |
| A3. Error durante el desbloqueo | `500 Internal Server Error` | `UnlockAccountAsync_OnDatabaseFailure_ThrowsPersistenceException` | `UnlockAccount_OnInternalError_Returns500InternalServerError` |
| A4. Acceso denegado | `403 Forbidden` | — (control de acceso por rol) | `UnlockAccount_WithNonAdminRole_Returns403Forbidden` |

> Regla de oro: cada flujo del caso de uso debe tener al menos un test. En los flujos resueltos en la Capa de Presentación el test aplicable es el de integración HTTP.
