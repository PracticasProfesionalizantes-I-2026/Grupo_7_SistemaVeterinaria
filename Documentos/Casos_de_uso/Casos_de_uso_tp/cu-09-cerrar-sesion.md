# Caso de Uso: Cerrar Sesión

> Especificación elaborada siguiendo la guía `GUIA-Especificacion-Casos-de-Uso.md` (sección 3).
> Implementación del cierre de sesión seguro con revocación de tokens JWT mediante la entidad `TokenRevocado` y la tabla `TokensRevocados` (sin Refresh Tokens).

| Campo | Valor |
| --- | --- |
| **ID del Caso de Uso** | CU-09 |
| **Nombre** | Cerrar Sesión |
| **Actor Principal** | Recepcionista / Veterinario/a / Administrador |
| **Alcance / Nivel** | Sistema; meta de usuario |
| **Stakeholders e intereses** | Usuario del Sistema → cerrar de forma segura su cuenta en terminales compartidas; Administración → garantizar que las sesiones finalizadas no puedan ser reutilizadas mediante navegación del navegador o reenvío de tokens |
| **Disparador (Trigger)** | El usuario selecciona la opción "Cerrar Sesión" desde el menú de usuario |
| **Prioridad / Frecuencia** | Alta; alta frecuencia diaria (al finalizar la jornada o al cambiar de usuario en el puesto) |
| **Reglas de negocio relacionadas** | Ninguna (aplica gestión de sesiones y seguridad técnica) |

---

### 1. BREVE DESCRIPCIÓN
Permite a un usuario autenticado finalizar su sesión de manera segura, invalidando su token de sesión activo (JWT) mediante su incorporación a la lista de revocación en base de datos (`TokensRevocados`), impidiendo su uso posterior y retornando a la pantalla de inicio de sesión.

### 2. PRECONDICIONES
- El usuario debe poseer una sesión activa y un Token JWT válido en el encabezado `Authorization: Bearer <token>`.
- La Capa de Persistencia debe estar disponible para registrar la revocación del token en la tabla `TokensRevocados`.

### 3. FLUJO PRINCIPAL (Camino Feliz - HTTP 200)
1. El Actor envía una petición al endpoint `POST /api/auth/logout` incluyendo el Token JWT en el encabezado `Authorization: Bearer <token>`.
2. La **Capa de Presentación** (`AuthController.Logout`) comprueba la presencia del token y los claims del usuario autenticado, extrayendo el identificador único del token (claim `jti`), el identificador del usuario y la fecha de expiración.
3. La **Capa de Negocio** (`AuthService.LogoutAsync`) valida los datos y delega en el repositorio correspondiente la revocación del token.
4. La **Capa de Persistencia** (`TokenRevocadoRepository`) persiste la entidad `TokenRevocado` con su `Jti`, `UsuarioId` y fecha de expiración en la tabla `TokensRevocados`.
5. El Sistema devuelve un código **200 OK** con el mensaje de confirmación y el cliente elimina el token almacenado en local/sessionStorage, redirigiendo a la pantalla de login.

### 4. FLUJOS ALTERNATIVOS (Caminos Tristes / Excepciones)

* **1a. Petición sin token de autenticación o token expirado (HTTP 401 Unauthorized):**
  1. Si en el Paso 1 la petición no incluye encabezado de autorización o el token es inválido o expiró.
  2. El middleware de autenticación JWT de ASP.NET Core rechaza la petición.
  3. El Sistema devuelve un código **401 Unauthorized**. Fin del caso de uso.

* **3a. Cierre de sesión idempotente / Token ya revocado (HTTP 200 OK):**
  1. Si en el Paso 3 el identificador del token (`jti`) ya se encontraba previamente registrado en la tabla `TokensRevocados` (por ejemplo, reintento de logout o cierre duplicado con el mismo token).
  2. La Capa de Negocio procesa la petición de forma segura e idempotente comprobando su existencia en `ITokenRevocadoRepository`, evitando duplicar el registro y sin lanzar excepción.
  3. El Sistema devuelve un código **200 OK** confirmando la finalización de sesión en el cliente.
  *(Nota: en endpoints protegidos operativos del sistema, cualquier solicitud posterior con un token revocado es rechazada automáticamente por el middleware mediante el evento `OnTokenValidated` con código HTTP 401 Unauthorized).*

* **4a. Error al persistir estado de revocación (HTTP 500 Internal Server Error):**
  1. Si en el Paso 4 se produce un error imprevisto en la base de datos al asentar la revocación.
  2. El middleware global captura el error.
  3. El Sistema devuelve un código **500 Internal Server Error**. Fin del caso de uso.

### 5. SUB-VARIACIONES (opcional)
1. Cierre de sesión individual en el cliente actual (`POST /api/auth/logout`). (En la arquitectura actual de la API se opera con tokens JWT y tabla de revocación sin Refresh Tokens ni cierre de sesión global multi-dispositivo).

### 6. POSTCONDICIONES
- El identificador del Token JWT (`jti`) queda registrado en la tabla `TokensRevocados`, inhabilitando su utilización para cualquier petición posterior a endpoints protegidos.
- La interfaz de usuario redirige al formulario de inicio de sesión y limpia el almacenamiento local (localStorage/sessionStorage).

---

## Anexo: matrices de referencia

### Códigos HTTP usados

| Código HTTP | Nombre Técnico | Contexto de Aplicación en el Caso de Uso |
| --- | --- | --- |
| `200` | OK | Cierre de sesión exitoso y confirmación de revocación (incluyendo cierre repetido idempotente en 3a). |
| `401` | Unauthorized | Intento de cierre de sesión sin token válido o expirado en la cabecera. |
| `500` | Internal Server Error | Error no controlado en la persistencia del estado de revocación. |

### Nota: Validación vs. Verificación aplicada

- **Validación (Presentación, → 401):** Verificación de firma, formato y vigencia del token JWT en el middleware de autenticación.
- **Verificación (Negocio / Persistencia, → 200/500):** Comprobación y registro del token en la tabla `TokensRevocados` mediante `TokenRevocadoRepository`. Asimismo, el middleware valida en cada solicitud protegida mediante `OnTokenValidated` que el token no figure revocado.

### Matriz de trazabilidad CU-09 → Test

| Paso del CU | Excepción / Código | Test unitario (BusinessLogic) | Test integración (HTTP) |
| --- | --- | --- | --- |
| Flujo principal | `200 OK` | `LogoutAsync_WithValidToken_RevokesTokenSuccessfully` | `Logout_WithValidBearerToken_Returns200OK` |
| 1a. Token ausente/inválido | `401 Unauthorized` | — (middleware de autenticación) | `Logout_WithoutAuthorizationHeader_Returns401Unauthorized` |
| 3a. Token ya revocado (idempotencia) | `200 OK` | `LogoutAsync_WhenAlreadyRevoked_CompletesSuccessfully` | `Logout_WhenTokenAlreadyRevoked_Returns200OK` |
| Endpoint protegido post-logout | `401 Unauthorized` | — (verificado vía repository mock) | `ProtectedEndpoint_AfterLogout_Returns401Unauthorized` |

> Regla de oro: cada flujo del caso de uso debe tener al menos un test. En los flujos resueltos en la Capa de Presentación el test aplicable es el de integración HTTP. Los tests se ejecutan con `dotnet test SistemaVeterinaria.slnx`.
