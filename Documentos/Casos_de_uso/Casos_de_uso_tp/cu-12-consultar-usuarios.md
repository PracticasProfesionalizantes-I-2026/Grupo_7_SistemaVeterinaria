# Caso de Uso: Consultar Usuarios

> Especificación elaborada siguiendo la guía `GUIA-Especificacion-Casos-de-Uso.md` (sección 3).
> Gestión administrativa de cuentas de usuario — Consulta del estado y datos de los usuarios registrados en el sistema.

| Campo | Valor |
| --- | --- |
| **ID del Caso de Uso** | CU-12 |
| **Nombre** | Consultar Usuarios |
| **Actor Principal** | Administrador |
| **Alcance / Nivel** | Sistema; meta de usuario |
| **Stakeholders e intereses** | Administrador → conocer el estado de las cuentas del personal para detectar cuentas bloqueadas, verificar roles asignados y tomar acciones correctivas; Sistema → garantizar que solo el Administrador acceda a la información de usuarios |
| **Disparador (Trigger)** | El Administrador accede a la sección Gestión de Usuarios desde el menú principal |
| **Prioridad / Frecuencia** | Alta; frecuencia media (ante situaciones de bloqueo de cuentas, revisiones periódicas o gestión de personal) |
| **Reglas de negocio relacionadas** | Ninguna (aplica control de acceso exclusivo al rol Administrador) |

---

### 1. BREVE DESCRIPCIÓN
Permite al Administrador consultar el listado completo de usuarios registrados en el sistema, visualizar su identificación, rol asignado y estado de cuenta, y seleccionar un usuario para acceder a las acciones administrativas disponibles (desbloqueo y restablecimiento de contraseña). Esta funcionalidad es de acceso exclusivo para el rol Administrador.

### 2. PRECONDICIONES
- El Administrador debe contar con una sesión activa en el sistema.
- El sistema debe estar disponible y operativo.

### 3. FLUJO PRINCIPAL (Camino Feliz)
1. El Administrador accede a la sección **Gestión de Usuarios** desde el menú principal del sistema.
2. El sistema muestra el listado de usuarios registrados, incluyendo para cada uno al menos: identificación del usuario (nombre de usuario), rol asignado (Recepcionista, Veterinario o Administrador) y estado de la cuenta (Activo, Bloqueado o Inactivo).
3. El Administrador puede buscar un usuario específico utilizando criterios de búsqueda (por nombre de usuario o rol).
4. El Administrador selecciona un usuario del listado.
5. El sistema muestra el detalle del usuario seleccionado, incluyendo su estado actual y las acciones disponibles según dicho estado (Desbloquear cuenta y/o Restablecer contraseña).

### 4. FLUJOS ALTERNATIVOS (Caminos Tristes / Excepciones)

* **A1 — Sin resultados en la búsqueda:**
  1. En el Paso 3, el Administrador aplica un criterio de búsqueda que no coincide con ningún usuario registrado.
  2. El sistema informa que no se encontraron usuarios que coincidan con el criterio ingresado.
  3. El Administrador puede modificar el criterio de búsqueda o consultar el listado completo.

* **A2 — Acceso denegado por rol insuficiente:**
  1. Un usuario con rol distinto al Administrador intenta acceder a la sección Gestión de Usuarios.
  2. El sistema deniega el acceso e informa que esta sección es de uso exclusivo del Administrador.
  3. Fin del caso de uso.

* **A3 — Error de carga del listado:**
  1. En el Paso 2, ocurre una falla al intentar recuperar la lista de usuarios desde el sistema.
  2. El sistema informa el error e invita al Administrador a reintentar la operación.
  3. Fin del caso de uso.

### 5. SUB-VARIACIONES (opcional)
1. El listado puede ordenarse o filtrarse por estado de cuenta (por ejemplo, mostrando primero las cuentas bloqueadas) para facilitar la gestión.

### 6. POSTCONDICIONES
- El Administrador visualiza el estado actualizado de los usuarios registrados.
- No se produce ningún cambio en los datos de los usuarios como resultado de esta consulta.
- El Administrador queda posicionado para ejecutar, si lo requiere, las acciones de desbloqueo (CU-13) o restablecimiento de contraseña (CU-14) sobre el usuario seleccionado.

---

## Anexo: matrices de referencia

### Códigos HTTP usados

| Código HTTP | Nombre Técnico | Contexto de Aplicación en el Caso de Uso |
| --- | --- | --- |
| `200` | OK | Listado de usuarios recuperado y visualizado correctamente. |
| `403` | Forbidden | Acceso denegado por rol insuficiente (A2). |
| `500` | Internal Server Error | Error no controlado al recuperar el listado de usuarios (A3). |

### Nota: Validación vs. Verificación aplicada

- **Validación (Presentación):** Verificación de que el Administrador posea sesión activa y rol correspondiente.
- **Verificación (Negocio / Seguridad):** Control de acceso exclusivo al rol Administrador; recuperación del estado real de cada cuenta desde la capa de persistencia.

### Matriz de trazabilidad CU-12 → Test

| Paso del CU | Excepción / Código | Test unitario (BusinessLogic) | Test integración (HTTP) |
| --- | --- | --- | --- |
| Flujo principal | `200 OK` | `GetAllUsersAsync_ReturnsUserListWithStatusAndRole` | `GetUsers_WithAdminRole_Returns200OK` |
| A1. Sin resultados | `200 OK` (lista vacía) | `GetAllUsersAsync_WithNoMatchingCriteria_ReturnsEmptyList` | `GetUsers_WithNonMatchingFilter_Returns200OKEmptyList` |
| A2. Acceso denegado | `403 Forbidden` | — (control de acceso por rol) | `GetUsers_WithNonAdminRole_Returns403Forbidden` |
| A3. Error de carga | `500 Internal Server Error` | `GetAllUsersAsync_OnDatabaseFailure_ThrowsPersistenceException` | `GetUsers_OnInternalError_Returns500InternalServerError` |

> Regla de oro: cada flujo del caso de uso debe tener al menos un test. En los flujos resueltos en la Capa de Presentación el test aplicable es el de integración HTTP.
