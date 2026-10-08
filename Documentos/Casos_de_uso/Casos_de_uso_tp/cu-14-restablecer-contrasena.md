# Caso de Uso: Restablecer Contraseña de Usuario

> Especificación elaborada siguiendo la guía `GUIA-Especificacion-Casos-de-Uso.md` (sección 3).
> Gestión administrativa de cuentas de usuario — Restablecimiento de contraseña mediante contraseña temporal, con cambio obligatorio en el siguiente inicio de sesión.

| Campo | Valor |
| --- | --- |
| **ID del Caso de Uso** | CU-14 |
| **Nombre** | Restablecer Contraseña de Usuario |
| **Actor Principal** | Administrador |
| **Alcance / Nivel** | Sistema; meta de usuario |
| **Stakeholders e intereses** | Administrador → permitir que un usuario recupere el acceso al sistema cuando no recuerda su contraseña, sin comprometer la seguridad de la información; Usuario afectado → poder establecer una nueva contraseña propia y segura; Sistema → garantizar que las contraseñas no se almacenen ni expongan en texto claro y que el cambio sea obligatorio en el siguiente inicio de sesión |
| **Disparador (Trigger)** | El Administrador selecciona la opción "Restablecer contraseña" para un usuario desde la sección Gestión de Usuarios |
| **Prioridad / Frecuencia** | Alta; frecuencia baja-media (ante solicitudes del personal por olvido de contraseña) |
| **Reglas de negocio relacionadas** | Ninguna (aplica restricciones funcionales de seguridad y control de acceso) |

---

### 1. BREVE DESCRIPCIÓN
Permite al Administrador restablecer la contraseña de cualquier usuario registrado en el sistema. El sistema genera una contraseña temporal de forma segura, la almacena mediante hash (sin guardarla en texto plano) y marca la cuenta del usuario con la condición de cambio obligatorio de contraseña para su próximo inicio de sesión. El Administrador comunica la contraseña temporal al usuario por un medio seguro. Esta operación no desbloquea una cuenta bloqueada ni afecta el contador de intentos fallidos. La operación queda registrada con el Administrador responsable y la fecha y hora.

### 2. PRECONDICIONES
- El Administrador debe contar con una sesión activa en el sistema.
- El usuario sobre el que se realiza el restablecimiento debe encontrarse registrado en el sistema.
- El sistema debe estar disponible y operativo.

### 3. FLUJO PRINCIPAL (Camino Feliz)
1. El Administrador accede a la sección Gestión de Usuarios y selecciona un usuario (puede llegar desde CU-12).
2. El sistema muestra el detalle del usuario seleccionado y las acciones disponibles.
3. El Administrador selecciona la opción **Restablecer contraseña**.
4. El sistema solicita confirmación de la operación, informando que se generará una contraseña temporal y que el usuario deberá cambiarla obligatoriamente en su próximo inicio de sesión.
5. El Administrador confirma la operación.
6. El sistema genera una contraseña temporal de forma segura.
7. El sistema almacena la nueva contraseña de forma segura (mediante hash), reemplazando la contraseña anterior, y marca la cuenta con la condición de **cambio obligatorio de contraseña**.
8. El sistema muestra la contraseña temporal al Administrador de forma única y no persistente en la interfaz, para que pueda comunicarla al usuario por un medio seguro. El sistema no envía la contraseña automáticamente por correo electrónico ni por ningún otro servicio externo.
9. El sistema registra la operación: usuario afectado, tipo de operación (Restablecimiento de contraseña), Administrador responsable y fecha y hora. El registro no almacena la contraseña temporal.
10. El sistema confirma que la operación se realizó correctamente.

### 4. FLUJOS ALTERNATIVOS (Caminos Tristes / Excepciones)

* **A1 — El Administrador cancela la confirmación:**
  1. En el Paso 5, el Administrador decide no confirmar la operación.
  2. El sistema cancela el proceso y conserva la contraseña anterior del usuario sin realizar cambios.
  3. Fin del caso de uso.

* **A2 — Error durante el restablecimiento:**
  1. En el Paso 7 o 9, ocurre una falla imprevista en el sistema al intentar actualizar la contraseña o registrar la operación.
  2. El sistema informa que no fue posible completar el restablecimiento y conserva la contraseña anterior del usuario (no registra la operación como exitosa si no pudo completarse).
  3. Fin del caso de uso.

* **A3 — Acceso denegado por rol insuficiente:**
  1. Un usuario con rol distinto al Administrador intenta ejecutar esta operación.
  2. El sistema deniega el acceso e informa que esta acción es de uso exclusivo del Administrador.
  3. Fin del caso de uso.

* **A4 — El usuario se encuentra bloqueado:**
  1. En el Paso 2, el sistema detecta que el usuario seleccionado se encuentra en estado Bloqueado.
  2. El sistema permite igualmente realizar el restablecimiento de contraseña, dejando a criterio del Administrador si también desea desbloquear la cuenta mediante la operación CU-13 (ya que el restablecimiento de contraseña no desbloquea automáticamente la cuenta).
  3. El sistema informa al Administrador que la cuenta está bloqueada y que el usuario no podrá acceder hasta que sea desbloqueada.
  4. El proceso continúa desde el Paso 3 si el Administrador decide confirmar el restablecimiento.

### 5. SUB-VARIACIONES (opcional)
1. Si el usuario está bloqueado y olvidó su contraseña, el Administrador puede realizar primero el restablecimiento de contraseña (CU-14) y luego el desbloqueo (CU-13), o en el orden inverso, ya que son operaciones independientes.

### 6. POSTCONDICIONES
- La contraseña del usuario queda reemplazada por la contraseña temporal generada, almacenada de forma segura (hash).
- La cuenta del usuario queda marcada con la condición de **cambio obligatorio de contraseña** para el próximo inicio de sesión.
- El estado de bloqueo y el contador de intentos fallidos de la cuenta no se ven afectados por esta operación.
- El Administrador conoce la contraseña temporal para comunicarla al usuario. Dicha contraseña no queda almacenada ni expuesta permanentemente en el sistema.
- Queda registrada la operación de restablecimiento con el Administrador responsable, la fecha y la hora, sin incluir la contraseña.
- En su próximo inicio de sesión, el usuario deberá completar el cambio obligatorio de contraseña (flujo A8 de CU-01) antes de acceder a las funcionalidades del sistema.

---

## Anexo: matrices de referencia

### Códigos HTTP usados

| Código HTTP | Nombre Técnico | Contexto de Aplicación en el Caso de Uso |
| --- | --- | --- |
| `200` | OK | Restablecimiento completado correctamente; contraseña temporal generada, almacenada y condición de cambio obligatorio activada. |
| `403` | Forbidden | Acceso denegado por rol insuficiente (A3). |
| `404` | Not Found | El usuario especificado no se encuentra registrado en el sistema. |
| `500` | Internal Server Error | Error no controlado durante la generación, almacenamiento o registro de la operación (A2). |

### Nota: Validación vs. Verificación aplicada

- **Validación (Presentación):** Verificación de que el Administrador posea sesión activa y rol correspondiente; confirmación explícita de la operación antes de ejecutarla.
- **Verificación (Negocio / Seguridad):** Verificación de existencia del usuario; generación segura de contraseña temporal; almacenamiento mediante hash (nunca en texto plano); activación de condición de cambio obligatorio; independencia con respecto al estado de bloqueo; registro de auditoría sin incluir datos sensibles; garantía de integridad transaccional.

### Nota de seguridad

El Administrador nunca puede consultar la contraseña actual ni la contraseña temporal de ningún usuario. La contraseña temporal se muestra al Administrador una única vez en la interfaz al momento de su generación; no queda almacenada ni disponible para consulta posterior. No se utilizarán servicios de correo electrónico, SMS u otros servicios externos para la entrega de credenciales.

### Matriz de trazabilidad CU-14 → Test

| Paso del CU | Excepción / Código | Test unitario (BusinessLogic) | Test integración (HTTP) |
| --- | --- | --- | --- |
| Flujo principal | `200 OK` | `ResetPasswordAsync_WithValidUser_GeneratesHashedTempPasswordAndSetsFlag` | `ResetPassword_WithValidUser_Returns200OK` |
| A1. Administrador cancela | — (cancelación en UI, sin llamada al backend) | — | — |
| A2. Error durante el restablecimiento | `500 Internal Server Error` | `ResetPasswordAsync_OnDatabaseFailure_ThrowsPersistenceException` | `ResetPassword_OnInternalError_Returns500InternalServerError` |
| A3. Acceso denegado | `403 Forbidden` | — (control de acceso por rol) | `ResetPassword_WithNonAdminRole_Returns403Forbidden` |
| A4. Usuario bloqueado — restablecimiento permitido | `200 OK` | `ResetPasswordAsync_WithLockedUser_AllowsResetWithoutUnlocking` | `ResetPassword_WithLockedUser_Returns200OK_AccountRemainsLocked` |

> Regla de oro: cada flujo del caso de uso debe tener al menos un test. En los flujos resueltos en la Capa de Presentación el test aplicable es el de integración HTTP.
