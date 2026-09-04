# Caso de Uso: Cerrar Sesión

> Especificación elaborada siguiendo la guía `GUIA-Especificacion-Casos-de-Uso.md` (sección 3).
> Implementación del cierre de sesión seguro con revocación de tokens JWT / Refresh Tokens (**RN-01**) y auditoría de desconexión.

| Campo | Valor |
| --- | --- |
| **ID del Caso de Uso** | CU-09 |
| **Nombre** | Cerrar Sesión |
| **Actor Principal** | Recepcionista / Veterinario/a / Dueño de la Veterinaria |
| **Alcance / Nivel** | Sistema; meta de usuario |
| **Stakeholders e intereses** | Usuario del Sistema → cerrar de forma segura su cuenta en terminales compartidas; Administración → garantizar que las sesiones finalizadas no puedan ser reutilizadas mediante navegación del navegador o reenvío de tokens |
| **Disparador (Trigger)** | El usuario selecciona la opción "Cerrar Sesión" desde el menú de usuario |
| **Prioridad / Frecuencia** | Alta; alta frecuencia diaria (al finalizar la jornada o al cambiar de usuario en el puesto) |
| **Reglas de negocio relacionadas** | RN-01 (invalidación de token: ninguna funcionalidad protegida podrá ser accedida tras el cierre de sesión sin reautenticación previa) |

---

### 1. BREVE DESCRIPCIÓN
Permite a un usuario autenticado finalizar su sesión de manera segura, invalidando su token de sesión activo y retornando a la pantalla de inicio de sesión.

### 2. PRECONDICIONES
- El usuario debe poseer una sesión activa y un Token JWT válido en el encabezado `Authorization: Bearer <token>`.
- La Capa de Persistencia debe estar disponible para registrar la revocación de tokens de refresco o auditoría.

### 3. FLUJO PRINCIPAL (Camino Feliz - HTTP 200)
1. El Actor envía una petición al endpoint `POST /api/auth/logout` incluyendo el Token JWT en el encabezado `Authorization`.
2. La **Capa de Presentación** (`AuthController.Logout`) comprueba la presencia del token y los claims del usuario autenticado.
3. La **Capa de Negocio** (`AuthService.LogoutAsync`) revoca los Refresh Tokens asociados al usuario (**RN-01**) y registra el evento de cierre de sesión en la bitácora de auditoría.
4. La **Capa de Persistencia** actualiza el estado de los tokens en la tabla `RefreshTokens` marcándolos como revocados.
5. El Sistema devuelve un código **200 OK** con el mensaje de confirmación y el cliente elimina el token almacenado en local/sessionStorage, redirigiendo a la pantalla de login.

### 4. FLUJOS ALTERNATIVOS (Caminos Tristes / Excepciones)

* **1a. Petición sin token de autenticación o token expirado (HTTP 401 Unauthorized):**
  1. Si en el Paso 1 la petición no incluye encabezado de autorización o el token es inválido.
  2. El middleware de autenticación JWT de ASP.NET Core rechaza la petición.
  3. El Sistema devuelve un código **401 Unauthorized**. Fin del caso de uso.

* **3a. Cierre de sesión idempotente / Token ya revocado (HTTP 200 OK):**
  1. Si en el Paso 3 el token o refresh token ya se encontraba revocado previamente.
  2. La Capa de Negocio procesa la petición de forma segura sin lanzar excepción.
  3. El Sistema devuelve un código **200 OK** confirmando la finalización de sesión en el cliente.

* **4a. Error al actualizar estado de auditoría (HTTP 500 Internal Server Error):**
  1. Si en el Paso 4 se produce un error imprevisto en la base de datos al asentar la revocación.
  2. El middleware global captura el error.
  3. El Sistema devuelve un código **500 Internal Server Error**. Fin del caso de uso.

### 5. SUB-VARIACIONES (opcional)
1. Cierre de sesión individual en el dispositivo actual (`POST /api/auth/logout`).
2. Cierre de sesión global en todos los dispositivos (`POST /api/auth/logout-all`).

### 6. POSTCONDICIONES
- Los tokens de sesión y refresh tokens quedan revocados e inhabilitados (**RN-01**).
- La interfaz de usuario redirige al formulario de inicio de sesión y limpia el almacenamiento local.

---

## Anexo: matrices de referencia

### Códigos HTTP usados

| Código HTTP | Nombre Técnico | Contexto de Aplicación en el Caso de Uso |
| --- | --- | --- |
| `200` | OK | Cierre de sesión exitoso y confirmación de revocación. |
| `401` | Unauthorized | Intento de cierre de sesión sin token válido en la cabecera. |
| `500` | Internal Server Error | Error no controlado en la persistencia del estado de revocación. |

### Nota: Validación vs. Verificación aplicada

- **Validación (Presentación, → 401):** Verificación de firma y vigencia del token JWT en el middleware de autenticación.
- **Verificación (Negocio, → 200/500):** Revocación de refresh tokens en base de datos y auditoría de seguridad (**RN-01**).

### Matriz de trazabilidad CU-09 → Test

| Paso del CU | Excepción / Código | Test unitario (BusinessLogic) | Test integración (HTTP) |
| --- | --- | --- | --- |
| Flujo principal | `200 OK` | `LogoutAsync_WithValidToken_RevokesRefreshTokens` | `Logout_WithValidBearerToken_Returns200OK` |
| 1a. Token ausente/inválido | `401 Unauthorized` | — (middleware de autenticación) | `Logout_WithoutAuthorizationHeader_Returns401Unauthorized` |
| 3a. Token ya revocado | `200 OK` | `LogoutAsync_WhenAlreadyRevoked_CompletesSuccessfully` | `Logout_WhenTokenAlreadyRevoked_Returns200OK` |

> Regla de oro: cada flujo del caso de uso debe tener al menos un test. En los flujos resueltos en la Capa de Presentación el test aplicable es el de integración HTTP. Los tests se ejecutan con `dotnet test SistemaVeterinaria.slnx`.
