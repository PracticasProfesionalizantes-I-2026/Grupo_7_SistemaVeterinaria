# Seguimiento de Correcciones — Sistema Veterinario "Patitas"

Documento maestro de trazabilidad y seguimiento de los hallazgos detectados en la auditoría técnica del proyecto **Grupo_7_SistemaVeterinaria** (API RESTful N-Tier en .NET 10, Entity Framework Core 10 y SQLite).

---

## 1. Resumen general

Se identificaron un total de **38 problemas**, clasificados según su nivel de prioridad:

| Prioridad | Cantidad | Categorías alcanzadas |
| :--- | :---: | :--- |
| **Crítica** | **5** | Seguridad de endpoints (`SEC-01`, `SEC-02`), vulnerabilidad de autenticación (`SEC-03`), identificadores autoincrementales (`BD-01`) e integridad clínica (`BUG-03`). |
| **Alta** | **12** | Escalación de privilegios (`SEC-04`), auditoría de creación (`BD-02`), validaciones de fechas, pesos y dueños (`VAL-01`, `VAL-02`, `VAL-04`, `VAL-05`), manejo de errores (`ERR-01`, `ERR-02`, `ERR-03`), bugs funcionales (`BUG-01`, `BUG-02`) y pruebas de integración (`TEST-01`). |
| **Media** | **13** | Configuración y revocación JWT (`SEC-05`, `SEC-06`), atomicidad transaccional (`BD-03`), validaciones de dominio (`VAL-03`, `VAL-06`, `VAL-07`, `VAL-08`, `VAL-09`), formato de errores (`ERR-04`), flujos de turnos y atenciones (`BUG-04`, `BUG-05`, `BUG-06`) y edición de mascota (`DOC-01`). |
| **Baja** | **8** | Prevención de duplicados (`VAL-10`), alineación documental (`DOC-02`, `DOC-03`, `DOC-04`, `DOC-05`) y calidad de código / arquitectura (`SMELL-01`, `SMELL-02`, `SMELL-03`). |
| **Total** | **38** | *(29 completadas y 9 postergadas como mejoras para futuras versiones).* |

### 1.1. Resumen de estados al cierre de la entrega

| Prioridad original | Total | Completadas | Postergadas | Detalle por código |
| :--- | :---: | :---: | :---: | :--- |
| **Crítica** | **5** | **4** | **1** | **Completadas (4):** `SEC-01`, `SEC-02`, `SEC-03`, `BUG-03`.<br>**Postergada (1):** `BD-01` *(reclasificada como mejora postergada por riesgo técnico transversal)*. |
| **Alta** | **12** | **10** | **2** | **Completadas (10):** `SEC-04`, `VAL-01`, `VAL-02`, `VAL-04`, `VAL-05`, `ERR-01`, `ERR-02`, `ERR-03`, `BUG-02`, `TEST-01`.<br>**Postergadas (2):** `BD-02`, `BUG-01` *(requieren migración de esquema SQLite en entidades principales)*. |
| **Media** | **13** | **11** | **2** | **Completadas (11):** `SEC-05`, `SEC-06`, `VAL-03`, `VAL-06`, `VAL-07`, `VAL-08`, `VAL-09`, `BUG-04`, `BUG-05`, `BUG-06`, `DOC-01`.<br>**Postergadas (2):** `BD-03`, `ERR-04` *(preservación de estabilidad transaccional y contratos JSON existentes)*. |
| **Baja** | **8** | **4** | **4** | **Completadas (4):** `DOC-02`, `DOC-03`, `DOC-04`, `DOC-05`.<br>**Postergadas (4):** `VAL-10`, `SMELL-01`, `SMELL-02`, `SMELL-03` *(mejoras opcionales y refactorizaciones internas sin impacto funcional)*. |
| **Total Hallazgos** | **38** | **29** | **9** | **Suite de pruebas automatizadas:** `539/539` aprobadas (`314` unitarias + `225` de integración). |

---

## 2. Listado completo de problemas

### 2.1. Seguridad, Autenticación y Autorización (`SEC`)

#### SEC-01
- **Código:** SEC-01
- **Problema:** Los endpoints `GET`, `PUT` y `DELETE` de `DuenosController` carecen de autenticación y autorización por roles.
- **Ubicación:** `API/Controllers/DuenosController.cs` (clase `DuenosController`, métodos `GetAll`, `GetById`, `Update`, `Delete`, líneas 20-95).
- **Evidencia:** La clase `DuenosController` no posee el atributo `[Authorize]`. Únicamente el método `Create` (`[HttpPost]`, línea 42) tiene `[Authorize(Roles = "Recepcionista,Veterinario")]`.
- **Consecuencia:** Cualquier usuario anónimo sin iniciar sesión (o un usuario con rol `Administrador`, excluido por RF 5.1 y AMB-02) puede consultar todos los datos personales (nombre, DNI, teléfono, email, dirección) de los clientes, modificarlos o eliminarlos. Incumple las precondiciones de `CU-08`.
- **Prioridad:** Crítica
- **Solución propuesta:** Colocar `[Authorize(Roles = "Recepcionista,Veterinario")]` a nivel de la clase `DuenosController` para proteger la totalidad de sus endpoints.
- **Decisión aprobada:** Aprobada (aplicar `[Authorize(Roles = "Recepcionista,Veterinario")]` a nivel de clase en `DuenosController` conforme a CU-02 y CU-08).
- **Estado:** Completada
- **Solución implementada:** Se agregó el atributo `[Authorize(Roles = "Recepcionista,Veterinario")]` a nivel de la clase `DuenosController` en `API/Controllers/DuenosController.cs` conservando las rutas, restricciones y respuestas funcionales existentes. Se actualizaron y ampliaron las pruebas de integración en `tests/API.Tests/DuenosEndpointsTests.cs` para verificar los endpoints `GET`, `POST`, `PUT` y `DELETE` sin token (`401`), con rol `Administrador` (`403`), con rol `Recepcionista` y con rol `Veterinario` (acceso permitido y comportamiento funcional preservado).
- **Pruebas necesarias:** Pruebas de integración en `DuenosEndpointsTests` verificando que `GET /api/duenos`, `GET /api/duenos/{id}`, `POST /api/duenos`, `PUT /api/duenos/{id}` y `DELETE /api/duenos/{id}` devuelvan `401 Unauthorized` sin token, `403 Forbidden` con rol `Administrador`, y `200 OK` / `201 Created` / `204 No Content` (además de `400`, `404` y `409` en flujos alternativos) con roles `Recepcionista` o `Veterinario`.
- **Resultado de las pruebas:** Exitoso — 27 pruebas ejecutadas en `DuenosEndpointsTests`, 27 superadas, 0 fallidas (`dotnet test tests/API.Tests/API.Tests.csproj --filter FullyQualifiedName~DuenosEndpointsTests`).

---

#### SEC-02
- **Código:** SEC-02
- **Problema:** Los endpoints `GET`, `PUT` y `DELETE` de `AtencionesMedicasController` carecen de autenticación y autorización por roles.
- **Ubicación:** `API/Controllers/AtencionesMedicasController.cs` (clase `AtencionesMedicasController`, métodos `GetAll`, `GetById`, `GetByMascotaId`, `Update`, `Delete`, líneas 13-121) y `API/Controllers/HistoriaClinicaController.cs` (línea 11).
- **Evidencia:** La clase `AtencionesMedicasController` no posee el atributo `[Authorize]`. Solo el método `Create` (`[HttpPost]`, línea 58) tiene `[Authorize(Roles = "Veterinario")]`. Adicionalmente, `HistoriaClinicaController` tenía `[Authorize(Roles = "Veterinario,Administrador")]` a pesar de que el Administrador está limitado exclusivamente a gestión de usuarios y reportes.
- **Consecuencia:** Cualquier usuario anónimo o con rol `Recepcionista` o `Administrador` podía acceder a información clínica reservada exclusivamente al `Veterinario`.
- **Prioridad:** Crítica
- **Solución propuesta:** Proteger la clase `AtencionesMedicasController` con `[Authorize(Roles = "Veterinario")]`, restringir también `HistoriaClinicaController` exclusivamente a `[Authorize(Roles = "Veterinario")]`, conservar los endpoints `PUT` y `DELETE` de atenciones médicas protegidos por dicho rol devolviendo siempre `409 Conflict` (`RN-01` Inmutabilidad) y alinear la documentación de casos de uso y requerimientos (`CU-03`, `CU-05`, `DOCUMENTACION PROYECTO-SistemaVeterinaria.md` e informes de inconsistencias).
- **Decisión aprobada:** Aprobada — (1) `Veterinario` como único rol autorizado para consultar y registrar atenciones médicas e historias clínicas (`Recepcionista` y `Administrador` sin acceso a información clínica, quedando `Administrador` limitado exclusivamente a usuarios y reportes); (2) conservar `PUT` y `DELETE` en `AtencionesMedicasController` protegidos con `[Authorize(Roles = "Veterinario")]` rechazando siempre con `409 Conflict` (`RN-01`); (3) actualizar la documentación relacionada (`CU-03`, `CU-05`, `DOCUMENTACION PROYECTO-SistemaVeterinaria.md`, `INFORME-INCONSISTENCIAS-DOCUMENTACION-CASOS-DE-USO.md`).
- **Estado:** Completada
- **Solución implementada:**
  1. Se agregó `[Authorize(Roles = "Veterinario")]` a nivel de clase en `API/Controllers/AtencionesMedicasController.cs`, protegiendo todos sus endpoints (`GET`, `POST`, `PUT` y `DELETE`) y manteniendo el rechazo `409 Conflict` por `RN-01` en `PUT` y `DELETE`.
  2. Se actualizó `API/Controllers/HistoriaClinicaController.cs` de `[Authorize(Roles = "Veterinario,Administrador")]` a `[Authorize(Roles = "Veterinario")]`.
  3. Se actualizó la documentación en `Documentos/Casos_de_uso/Casos_de_uso_tp/cu-03-consultar-historia-clinica.md`, `Documentos/Casos_de_uso/Casos_de_uso_tp/cu-05-consultar-agenda-de-turnos.md`, `Documentos/Bussines/DOCUMENTACION PROYECTO-SistemaVeterinaria.md`, `INFORME-INCONSISTENCIAS-DOCUMENTACION-CASOS-DE-USO.md` y `Documentos/INFORME-INCONSISTENCIAS-DOCUMENTACION-CASOS-DE-USO.md`.
  4. Se actualizaron y ampliaron las pruebas de integración en `tests/API.Tests/AtencionesMedicasEndpointsTests.cs` y `tests/API.Tests/HistoriaClinicaEndpointsTests.cs`.
- **Pruebas necesarias:** Pruebas de integración en `AtencionesMedicasEndpointsTests` e `HistoriaClinicaEndpointsTests` verificando: sin token (`401`), rol `Recepcionista` (`403`), rol `Administrador` (`403`), rol `Veterinario` (`200 OK` en consultas y `201 Created` en registro), y `PUT`/`DELETE` con rol `Veterinario` devolviendo `409 Conflict` sin alterar ni eliminar los datos médicos.
- **Resultado de las pruebas:** Exitoso — 49 pruebas ejecutadas entre `AtencionesMedicasEndpointsTests` (34) e `HistoriaClinicaEndpointsTests` (15), 49 superadas, 0 fallidas (`dotnet test tests/API.Tests/API.Tests.csproj --filter "FullyQualifiedName~AtencionesMedicasEndpointsTests|FullyQualifiedName~HistoriaClinicaEndpointsTests"`).

---

#### SEC-03
- **Código:** SEC-03
- **Problema:** Bypass de bloqueo por intentos fallidos, cambio de contraseña en cuentas sin flag de contraseña temporal y enumeración de usuarios en `POST /api/auth/cambiar-password-temporal`.
- **Ubicación:** `BusinessLogic/Services/AuthService.cs` (método `CambiarPasswordTemporalAsync`, líneas 111-199).
- **Evidencia:**
  1. El método no verificaba `if (!usuario.RequiereCambioPassword)`, permitiendo invocar este endpoint anónimo sobre cuentas normales que no tenían una contraseña temporal activa.
  2. Cuando `PasswordActual` era incorrecta, no incrementaba `usuario.IntentosFallidos` ni bloqueaba la cuenta al quinto intento.
  3. Cuando el usuario no existía lanzaba `CredencialesInvalidasException("El usuario especificado no existe.")`, y cuando la contraseña no coincidía lanzaba `"La contraseña actual es incorrecta."`.
- **Consecuencia:** Un atacante podía realizar ataques de fuerza bruta ilimitados contra la contraseña de cualquier cuenta (incluido `admin`) usando `/api/auth/cambiar-password-temporal` sin activar nunca el bloqueo de 5 intentos, además de enumerar qué nombres de usuario o correos existían en el sistema.
- **Prioridad:** Crítica
- **Solución propuesta:** En `CambiarPasswordTemporalAsync`: (1) unificar el mensaje de error ante usuario inexistente o contraseña actual incorrecta (`"Nombre de usuario o contraseña incorrectos."`); (2) verificar que `usuario.RequiereCambioPassword == true` antes de permitir el cambio; (3) incrementar `IntentosFallidos` y aplicar el bloqueo en el 5° intento fallido igual que en `LoginAsync`; (4) impedir reutilizar la misma contraseña temporal (`NuevaPassword != PasswordActual`); (5) al completar el cambio, restablecer `IntentosFallidos = 0`, poner `RequiereCambioPassword = false` y devolver `200 OK` con `Token = null` para que el usuario inicie sesión mediante `POST /api/auth/login` con su nueva contraseña definitiva.
- **Decisión aprobada:** Aprobada (Opción B) — Contabilizar intentos fallidos compartiendo el contador `IntentosFallidos` con `LoginAsync` y bloquear la cuenta al 5° intento consecutivo (`403 Forbidden`); exigir `RequiereCambioPassword == true` (verificado luego de autenticar `PasswordActual` para no filtrar estado de cuenta ante credenciales inválidas); exigir `NuevaPassword != PasswordActual` (`400 Bad Request`); mantener validaciones de complejidad y confirmación; unificar mensaje genérico `"Nombre de usuario o contraseña incorrectos."` (`401 Unauthorized`); rechazar cuentas bloqueadas o inactivas (`403 Forbidden`); reiniciar `IntentosFallidos = 0` al cambiar exitosamente; y devolver `200 OK` con `Token = null` (`RequiereCambioPassword = false`), requiriendo login posterior en `/api/auth/login`.
- **Estado:** Completada
- **Solución implementada:**
  1. Se modificó `AuthService.CambiarPasswordTemporalAsync` en `BusinessLogic/Services/AuthService.cs` incorporando la validación `NuevaPassword != PasswordActual`, el mensaje genérico `"Nombre de usuario o contraseña incorrectos."`, la verificación de cuenta bloqueada/inactiva, el incremento y persistencia de `IntentosFallidos` con bloqueo al 5° intento (`UsuarioBloqueadoException`), la verificación `usuario.RequiereCambioPassword == true`, el reinicio `IntentosFallidos = 0`, y el retorno de `LoginResponseDTO` con `Token = null` y `RequiereCambioPassword = false`.
  2. Se actualizó el paso A8 de `Documentos/Casos_de_uso/Casos_de_uso_tp/cu-01-iniciar-sesion.md` para documentar el retorno sin token (`token: null`) en `/api/auth/cambiar-password-temporal`, el inicio de sesión posterior con la contraseña definitiva y el cómputo de intentos fallidos.
  3. Se actualizaron y ampliaron las pruebas unitarias en `tests/BusinessLogic.Tests/AuthServiceTests.cs` y las pruebas de integración en `tests/API.Tests/AuthEndpointsTests.cs` y `tests/API.Tests/UsuariosEndpointsTests.cs`.
- **Pruebas necesarias:** Pruebas unitarias en `AuthServiceTests` y de integración en `AuthEndpointsTests` comprobando: cambio exitoso con `Token == null`, rechazo al reutilizar la contraseña temporal, rechazo cuando `RequiereCambioPassword == false`, incremento de `IntentosFallidos` ante contraseña incorrecta, bloqueo al 5° intento fallido, reinicio del contador tras cambio exitoso, rechazo de cuentas bloqueadas o inactivas (`403`), mensajes genéricos idénticos ante usuario inexistente o contraseña incorrecta (`401`), intentos fallidos alternados entre `/login` y `/cambiar-password-temporal`, invalidez de la contraseña temporal anterior y login exitoso con la nueva contraseña definitiva.
- **Resultado de las pruebas:** Exitoso — 80 pruebas ejecutadas y superadas (0 fallidas): 38 pruebas unitarias en `AuthServiceTests` (22) y `UsuarioServiceTests` (16), y 42 pruebas de integración en `AuthEndpointsTests` (24) y `UsuariosEndpointsTests` (18).

---

#### SEC-04
- **Código:** SEC-04
- **Problema:** Escalación horizontal de privilegios al configurar la disponibilidad horaria de un veterinario (`PUT` / `POST /api/turnos/disponibilidad/{veterinarioId}`).
- **Ubicación:** `API/Controllers/TurnosController.cs` (método `ConfigurarDisponibilidad`, líneas 156-193).
- **Evidencia:** El controlador permite el acceso a `Recepcionista,Veterinario`, pero cuando el usuario autenticado tenía rol `Veterinario`, no comprobaba que el parámetro de ruta `veterinarioId` (ni `dto.VeterinarioId`) coincidiera con su propio identificador en el token JWT (`ClaimTypes.NameIdentifier`).
- **Consecuencia:** Un veterinario autenticado podía modificar, sobrescribir o vaciar la disponibilidad horaria de cualquier otro veterinario de la clínica.
- **Prioridad:** Alta
- **Solución propuesta:** Si el usuario autenticado posee el rol `Veterinario`, verificar que el claim `NameIdentifier` del token JWT sea válido y coincida con `veterinarioId` (y con `dto.VeterinarioId` si se informa); en caso contrario, devolver `403 Forbidden` sin alterar los datos. El rol `Recepcionista` conserva la facultad de administrar la disponibilidad de cualquier veterinario registrado y el rol `Administrador` permanece sin permiso (`403 Forbidden`).
- **Decisión aprobada:** Aprobada — Proteger `PUT` y `POST /api/turnos/disponibilidad/{veterinarioId}` en `TurnosController.ConfigurarDisponibilidad` extrayendo la identidad del usuario autenticado desde el token JWT (`ClaimTypes.NameIdentifier`): (1) si el claim falta o es inválido, devolver `401 Unauthorized`; (2) si el rol es `Veterinario`, permitir configurar únicamente su propia disponibilidad (`usuarioAutenticadoId == veterinarioId`) y devolver `403 Forbidden` si intenta modificar la de otro veterinario; (3) permitir a `Recepcionista` configurar la disponibilidad de cualquier veterinario válido; (4) mantener `403 Forbidden` para `Administrador` y `401 Unauthorized` sin token, conservando todas las validaciones existentes.
- **Estado:** Completada
- **Solución implementada:**
  1. Se modificó `TurnosController.ConfigurarDisponibilidad` en `API/Controllers/TurnosController.cs` (aplicable tanto a `PUT` como a `POST`) para validar la presencia y formato del claim `ClaimTypes.NameIdentifier` en el token JWT (`401 Unauthorized` si es inválido o está ausente) y comprobar que cuando el usuario autenticado tiene rol `Veterinario`, el `veterinarioId` de la ruta (y `dto.VeterinarioId` si no es vacío) coincida estrictamente con `usuarioAutenticadoId`, devolviendo `403 Forbidden` en caso contrario.
  2. Se documentaron los permisos de configuración de disponibilidad (`RN-10`) y los códigos HTTP `401`/`403` en `Documentos/Casos_de_uso/Casos_de_uso_tp/cu-06-gestionar-turnos.md`.
  3. Se creó `tests/API.Tests/TurnosEndpointsTests.cs` con pruebas de integración para `PUT` y `POST` cubriendo todos los escenarios de autorización, preservación de datos ante rechazos y funcionamiento integral de la agenda y gestión de turnos.
- **Pruebas necesarias:** Pruebas de integración en `TurnosEndpointsTests` (probando tanto `PUT` como `POST`) verificando: `Veterinario` modificando su propia disponibilidad (`200 OK`), `Veterinario` intentando modificar la de otro veterinario por ruta o body (`403 Forbidden` sin alterar datos), `Recepcionista` configurando disponibilidad de cualquier veterinario (`200 OK`), `Administrador` (`403 Forbidden` sin alterar datos), solicitud sin token o con claim de identidad inválido (`401 Unauthorized`), veterinario inexistente (`404 Not Found`), horario inválido (`409 Conflict`) y funcionamiento normal de agenda, alta, reprogramación y cancelación de turnos.
- **Resultado de las pruebas:** Exitoso — 44 pruebas ejecutadas y superadas (0 fallidas): 19 pruebas de integración en `TurnosEndpointsTests` y 25 pruebas unitarias en `TurnoServiceTests` (23) y `DisponibilidadVeterinarioRepositoryTests` (2).

---

#### SEC-05
- **Código:** SEC-05
- **Problema:** Clave secreta JWT expuesta en `appsettings.json` y valores por defecto (fallback) discrepantes en el código fuente.
- **Ubicación:** `API/appsettings.json` (línea 6), `API/appsettings.Development.json`, `API/Program.cs` (líneas 49-83) y `BusinessLogic/Services/JwtTokenGenerator.cs` (líneas 20-53).
- **Evidencia:** En `Program.cs` existía un fallback hardcodeado distinto del fallback en `JwtTokenGenerator.cs`, y `API/appsettings.json` contenía una clave activa en la configuración base.
- **Consecuencia:** Si la clave `JwtSettings:SecretKey` no estaba presente en la configuración, los tokens firmados por `JwtTokenGenerator` fallaban siempre la validación en `Program.cs`. Además, mantener claves criptográficas hardcodeadas en el código fuente o en la configuración base de producción constituía un riesgo de seguridad.
- **Prioridad:** Media
- **Solución propuesta:** Eliminar los strings de fallback hardcodeados en `Program.cs`, `JwtTokenGenerator.cs` y las pruebas; dejar `JwtSettings:SecretKey` vacío en `API/appsettings.json`; definir una clave aleatoria exclusiva para desarrollo en `API/appsettings.Development.json` y otra clave independiente en `CustomWebApplicationFactory`; y validar al arranque de la aplicación y en la generación del token que `JwtSettings:SecretKey` esté configurada y posea al menos 32 bytes UTF-8.
- **Decisión aprobada:** Aprobada (Alternativa A) — (1) Vaciar `JwtSettings:SecretKey` en `API/appsettings.json`; (2) configurar una clave aleatoria exclusiva de desarrollo en `API/appsettings.Development.json`; (3) exigir una clave externa en `Production` sin reutilizar la de desarrollo; (4) configurar una clave exclusiva de pruebas automatizadas en `CustomWebApplicationFactory`; (5) eliminar todas las claves fallback en `Program.cs`, `JwtTokenGenerator.cs` y `TurnosEndpointsTests.cs`; (6) unificar la lectura y validación de `JwtSettings:SecretKey` exigiendo al menos 32 bytes UTF-8 (`InvalidOperationException` si falta o es menor); y (7) documentar la advertencia de seguridad en `README.md`.
- **Estado:** Completada
- **Solución implementada:**
  1. Se dejó `"SecretKey": ""` en `API/appsettings.json` y se configuró una clave aleatoria de alta entropía exclusiva para desarrollo en `API/appsettings.Development.json`.
  2. Se eliminaron los fallbacks hardcodeados en `API/Program.cs` y `BusinessLogic/Services/JwtTokenGenerator.cs`, agregando en ambos puntos la validación obligatoria de presencia y longitud mínima (`Encoding.UTF8.GetByteCount(secretKey.Trim()) >= 32` bytes UTF-8) con lanzamiento de `InvalidOperationException`.
  3. Se configuró una clave independiente exclusiva para pruebas en `tests/API.Tests/CustomWebApplicationFactory.cs` y se eliminó el fallback en `tests/API.Tests/TurnosEndpointsTests.cs`.
  4. Se creó `tests/BusinessLogic.Tests/JwtTokenGeneratorTests.cs` y se amplió `tests/API.Tests/AuthEndpointsTests.cs` con pruebas de fallo de arranque ante claves vacías/cortas, rechazo de tokens manipulados y rechazo de tokens firmados con otra clave.
  5. Se agregó la advertencia de seguridad sobre la clave pública de desarrollo y la obligatoriedad de clave externa en producción en `README.md`.
- **Pruebas necesarias:** Pruebas unitarias en `JwtTokenGeneratorTests` (emisión y validación con clave `>= 32` bytes UTF-8 y `InvalidOperationException` ante claves nulas, vacías o `< 32` bytes) y pruebas de integración en `AuthEndpointsTests` y suites de `SEC-01` a `SEC-04` verificando fallo de arranque ante clave inválida, rechazo `401` de tokens manipulados o con otra firma, y funcionamiento completo de todos los roles.
- **Resultado de las pruebas:** Exitoso — 209 pruebas ejecutadas y superadas (0 fallidas): 67 pruebas unitarias (`JwtTokenGeneratorTests` [6], `AuthServiceTests` [22], `UsuarioServiceTests` [16], `TurnoServiceTests` [23]) y 142 pruebas de integración (`DuenosEndpointsTests` [27], `AtencionesMedicasEndpointsTests` [34], `HistoriaClinicaEndpointsTests` [15], `AuthEndpointsTests` [29], `UsuariosEndpointsTests` [18], `TurnosEndpointsTests` [19]).

---

#### SEC-06
- **Código:** SEC-06
- **Problema:** Los tokens JWT emitidos previamente continúan siendo válidos tras el bloqueo, desactivación o restablecimiento de contraseña de un usuario.
- **Ubicación:** `API/Program.cs` (evento `OnTokenValidated`, líneas 86-123) y `BusinessLogic/Services/UsuarioService.cs` (métodos `RestablecerPasswordAsync`, líneas 88-141).
- **Evidencia:** El evento `OnTokenValidated` consultaba únicamente `_tokenRevocadoRepository.EstaRevocadoAsync(jti)`. No verificaba si la cuenta del usuario en base de datos seguía activa (`Activo && !Bloqueado` y `!RequiereCambioPassword`).
- **Consecuencia:** Si un usuario tenía un token JWT activo (vigencia de 8 horas) y su cuenta era bloqueada, desactivada o el Administrador le restablecía la contraseña obligándolo a cambiarla, dicho token podía seguir operando hasta su expiración natural si no se invocaba `/api/auth/logout`.
- **Prioridad:** Media
- **Solución propuesta:** En `OnTokenValidated` (dentro de `Program.cs`), además de verificar que el `jti` no esté revocado, comprobar mediante `IUsuarioRepository` que el usuario exista, esté `Activo`, no esté `Bloqueado` y no tenga `RequiereCambioPassword == true`, conservando la exclusión de `/api/auth/logout` para preservar el cierre de sesión idempotente (`CU-09`).
- **Decisión aprobada:** Aprobada — (1) En `OnTokenValidated` (cuando `!isLogout`), conservar la verificación de revocación por `jti` y validar mediante `IUsuarioRepository.GetByIdAsync(usuarioId)` que el usuario exista, `usuario.Activo == true`, `usuario.Bloqueado == false` y `usuario.RequiereCambioPassword == false`, rechazando con `401 Unauthorized` en caso contrario; (2) mantener la exclusión de `/api/auth/logout` para preservar la idempotencia del cierre de sesión.
- **Estado:** Completada
- **Solución implementada:**
  1. Se actualizó el evento `OnTokenValidated` en `API/Program.cs` para validar el claim `ClaimTypes.NameIdentifier` y consultar `IUsuarioRepository.GetByIdAsync(usuarioId)` en todas las solicitudes autenticadas distintas de `/api/auth/logout`, invocando `context.Fail(...)` (`401 Unauthorized`) si el usuario no existe, está inactivo (`!Activo`), bloqueado (`Bloqueado`) o requiere cambio de contraseña temporal (`RequiereCambioPassword`).
  2. Se incorporaron pruebas de integración en `tests/API.Tests/AuthEndpointsTests.cs` y `tests/API.Tests/UsuariosEndpointsTests.cs` verificando el rechazo `401 Unauthorized` de tokens previos tras bloqueo de cuenta, desactivación de usuario, restablecimiento administrativo de contraseña y usuario inexistente, así como la preservación del cierre de sesión idempotente (`200 OK`).
- **Pruebas necesarias:** Pruebas de integración en `AuthEndpointsTests` y `UsuariosEndpointsTests` (además de las suites de `SEC-01` a `SEC-05`) comprobando que un token emitido antes de bloquear, desactivar o restablecer la contraseña de un usuario sea rechazado con `401 Unauthorized` en endpoints protegidos.
- **Resultado de las pruebas:** Exitoso — 146 pruebas de integración de seguridad ejecutadas y superadas (0 fallidas): `DuenosEndpointsTests` (27), `AtencionesMedicasEndpointsTests` (34), `HistoriaClinicaEndpointsTests` (15), `AuthEndpointsTests` (32), `UsuariosEndpointsTests` (19) y `TurnosEndpointsTests` (19).

---

### 2.2. Base de Datos, Identificadores y Auditoría (`BD`)

#### BD-01
- **Código:** BD-01
- **Problema:** Uso de identificadores `Guid` generados manualmente en lugar de identificadores numéricos autoincrementales (`int`) sugeridos por el profesor como mejora para mascotas, dueños y usuarios (y entidades relacionadas).
- **Ubicación:**
  - Entidades en `DataAccess/Entities/` (`Dueno.cs`, `Mascota.cs`, `Usuario.cs`, `AtencionMedica.cs`, `Turno.cs`, `DisponibilidadVeterinario.cs`, `HistorialPropietario.cs`, `ProcedimientoQuirurgico.cs`, `PrescripcionMedica.cs`, `EstudioComplementario.cs`, `RegistroOperacionUsuario.cs`, `TokenRevocado.cs`, `Reporte.cs`).
  - `DataAccess/VeterinariaDbContext.cs` y `DataAccess/DbInitializer.cs`.
  - Repositorios en `DataAccess/Repositories/`, servicios en `BusinessLogic/Services/`, DTOs en `Shared/DTOs/`, excepciones en `Shared/Exceptions/` y controladores en `API/Controllers/`.
- **Evidencia:** Todas las entidades declaran `public Guid Id { get; set; }` y las claves foráneas son `Guid` / `Guid?`. Los repositorios y servicios asignan manualmente `Guid.NewGuid()` en código C#, y los controladores restringen las rutas con `{id:guid}`.
- **Consecuencia:** Su modificación impacta transversalmente en las 13 entidades, 16 claves foráneas dependientes, migraciones de EF Core, contratos de DTOs, rutas de endpoints, parsing de claims JWT y todas las pruebas automatizadas, implicando un alto riesgo técnico de regresión previo a la entrega.
- **Prioridad:** Mejora postergada (originalmente relevada como Crítica; reclasificada como mejora postergada por riesgo técnico).
- **Solución propuesta:** Migrar los identificadores a `int` autoincremental (`ValueGeneratedOnAdd()` en EF Core / `INTEGER PRIMARY KEY AUTOINCREMENT` en SQLite), unificando las entidades del modelo a `int` (salvo el claim `Jti` de `TokenRevocado` que es un `string` propio del estándar JWT), actualizar rutas `{id:int}` y regenerar la migración de base de datos SQLite en una iteración futura.
- **Decisión aprobada:** Postergar como mejora futura por riesgo técnico, dado que el profesor lo sugirió como mejora y su implementación afecta transversalmente gran parte del sistema (entidades, claves foráneas, migraciones, DTOs, endpoints, JWT y suite de pruebas). Se conserva el esquema actual de identificadores `Guid`.
- **Estado:** Postergada (Mejora — Riesgo técnico)
- **Solución implementada:** No se aplican cambios sobre identificadores ni migraciones para preservar la estabilidad integral del sistema en la entrega.
- **Pruebas necesarias:** No aplica en esta entrega (se mantienen las pruebas sobre identificadores `Guid`).
- **Resultado de las pruebas:** Postergada

---

#### BD-02
- **Código:** BD-02
- **Problema:** Falta registro de fecha y hora de creación y del usuario creador en entidades principales del sistema.
- **Ubicación:**
  - `DataAccess/Entities/Mascota.cs` (carece tanto de `FechaRegistro`/`FechaCreacion` como de `UsuarioCreadorId`).
  - `DataAccess/Entities/Dueno.cs` (posee `FechaRegistro`, pero carece de `UsuarioCreadorId`).
  - `DataAccess/Entities/Turno.cs` (posee `FechaCreacion` y `UsuarioCancelacionId`, pero carece de `UsuarioCreadorId`).
  - `DataAccess/Entities/Usuario.cs` (posee `FechaCreacion`, pero carece de `UsuarioCreadorId`).
  - Controladores y servicios asociados (`MascotasController`, `MascotaService`, `DuenosController`, `DuenoService`, `TurnosController`, `TurnoService`).
- **Evidencia:** La entidad `Mascota` no registra cuándo fue creada ni por qué usuario; `Dueno` y `Turno` no registran qué usuario autenticado dio de alta el registro.
- **Consecuencia:** Imposibilidad de auditar qué usuario creó cada dueño, mascota o turno. Además, la ausencia de fecha de creación en `Mascota` provoca el bug funcional `BUG-01` en el módulo de reportes.
- **Prioridad:** Alta
- **Solución propuesta:** Incorporar `FechaRegistro` (`DateTime`) y `UsuarioCreadorId` (`Guid?`) en `Mascota`, y `UsuarioCreadorId` en `Dueno` y `Turno` (con su correspondiente relación `DeleteBehavior.Restrict` en `VeterinariaDbContext`). Asignar siempre la fecha en el servidor (`DateTime.UtcNow`) y extraer el `UsuarioCreadorId` exclusivamente de los claims del token JWT (`ClaimTypes.NameIdentifier`), sin incluir jamás estos campos en los DTOs de creación o modificación para evitar manipulación desde el cliente.
- **Decisión aprobada:** Postergar para una iteración posterior con ventana de migración de base de datos, priorizando la estabilidad del esquema SQLite y de las pruebas en la entrega actual.
- **Estado:** Postergada (Requiere migración de esquema SQLite)
- **Solución implementada:** No se aplican cambios sobre el esquema de base de datos ni migraciones de EF Core en esta entrega para evitar riesgos sobre `DbInitializer`, `veterinaria.db` y los constructores de servicios/repositorios.
- **Pruebas necesarias:** Pruebas unitarias y de integración que verifiquen que al crear un `Dueno`, `Mascota` o `Turno`, se persistan automáticamente la fecha UTC actual y el ID del usuario autenticado en el token JWT, y que no puedan ser sobrescritos desde el cuerpo JSON de la petición.
- **Resultado de las pruebas:** Postergada

---

#### BD-03
- **Código:** BD-03
- **Problema:** Falta de transacción explícita al persistir una atención médica junto con la actualización de `Mascota.FechaUltimaVisita` y la generación de turnos de seguimiento crónico.
- **Ubicación:** `DataAccess/Repositories/AtencionMedicaRepository.cs` (método `CreateAsync`, líneas 77-143) y `BusinessLogic/Services/AtencionMedicaService.cs` (método `CreateAsync`, líneas 238-279).
- **Evidencia:** `AtencionMedicaService.CreateAsync` persiste primero la atención médica en `_atencionMedicaRepository.CreateAsync` y luego, en otra operación separada fuera de transacción, invoca `_turnoService.GenerarTurnosSeguimientoCronicoAsync`.
- **Consecuencia:** Si ocurre un error inesperado al generar o guardar los turnos de seguimiento crónico, la atención médica (que es inmutable según RN-01) ya quedó guardada sin sus turnos asociados, y el veterinario no puede volver a editarla para reintentar la generación.
- **Prioridad:** Media
- **Solución propuesta:** Asegurar la atomicidad de la operación o permitir que la generación de turnos de seguimiento quede coordinada transaccionalmente desde la capa de persistencia / unidad de trabajo.
- **Decisión aprobada:** Postergar para no alterar el manejo de transacciones de SQLite ni la compatibilidad con los mocks de pruebas unitarias. Actualmente `AtencionMedicaRepository.CreateAsync` persiste en un único `SaveChangesAsync()` la atención médica y `Mascota.FechaUltimaVisita`, y `AtencionMedicaService` cuenta con manejo resiliente ante eventuales incidencias en la agenda de turnos crónicos.
- **Estado:** Postergada (Mejora arquitectónica — Estabilidad de entrega)
- **Solución implementada:** Se mantiene el esquema actual donde la atención médica y `FechaUltimaVisita` se guardan atómicamente en `AtencionMedicaRepository.CreateAsync` y los turnos de seguimiento crónico se generan inmediatamente después con captura controlada de errores.
- **Pruebas necesarias:** Prueba unitaria y de integración verificando la consistencia entre el alta de atención médica con seguimiento crónico, la actualización de `FechaUltimaVisita` y la creación de los turnos correspondientes.
- **Resultado de las pruebas:** Postergada

---

### 2.3. Validaciones e Integridad de Datos (`VAL`)

#### VAL-01
- **Código:** VAL-01
- **Problema:** `Mascota.FechaNacimiento` acepta fechas irreales (como el año `1900` o `0001-01-01`).
- **Ubicación:** `Shared/DTOs/Mascota/MascotaCreateDTO.cs` (líneas 26-27) y `BusinessLogic/Services/MascotaService.cs` (método `ValidateCreateDto`, líneas 210-215).
- **Evidencia:** En `MascotaCreateDTO`, `[Required]` sobre `DateTime` (tipo por valor no anulable) no impedía que un JSON sin el campo tomara el valor por defecto `0001-01-01T00:00:00` (`DateTime.MinValue`). En `MascotaService.ValidateCreateDto` únicamente se verificaba `if (dto.FechaNacimiento > DateTime.UtcNow)`, sin cota inferior alguna.
- **Consecuencia:** El sistema permitía registrar mascotas nacidas en el año `0001` o en `1900`, distorsionando la edad clínica en la historia clínica y los reportes.
- **Prioridad:** Alta
- **Solución propuesta:** Definir la edad máxima aceptable en 30 años (`MaxEdadMascotaAnios = 30`) y validar en `MascotaService.ValidateCreateDto` que `FechaNacimiento` no sea `default(DateTime)`, no sea futura (`<= DateTime.UtcNow`) y no sea anterior a `DateTime.UtcNow.Date.AddYears(-30)`.
- **Decisión aprobada:** Aprobada — Fijar una antigüedad máxima de 30 años para `FechaNacimiento`, rechazando con `400 Bad Request` (`ValidationException`) sin registrar la mascota cuando la fecha esté omitida/`default(DateTime)`, sea futura o supere los 30 años de antigüedad.
- **Estado:** Completada
- **Solución implementada:**
  1. En `BusinessLogic/Services/MascotaService.cs`, se definió la constante `MaxEdadMascotaAnios = 30` y se actualizó `ValidateCreateDto` para rechazar con `ValidationException` (`400 Bad Request`) cuando `dto.FechaNacimiento == default`, `dto.FechaNacimiento > DateTime.UtcNow` o `dto.FechaNacimiento.Date < DateTime.UtcNow.Date.AddYears(-MaxEdadMascotaAnios)`.
  2. Se agregaron pruebas unitarias en `tests/BusinessLogic.Tests/MascotaServiceTests.cs` (fecha por defecto/omitida, año `1900`, mayor a 30 años por un día, fecha futura y límites válidos de `0` y `-30` años).
  3. Se agregaron pruebas de integración en `tests/API.Tests/MascotasEndpointsTests.cs` verificando el rechazo `400 Bad Request` sin persistir en base de datos para `1900-01-01`, JSON sin `fechaNacimiento` (`0001-01-01`), fecha futura y fecha mayor a 30 años, además de autenticar las consultas auxiliares a `/api/duenos` tras `SEC-01`.
- **Pruebas necesarias:** Pruebas unitarias en `MascotaServiceTests` y de integración en `MascotasEndpointsTests` con: `0001-01-01` (`400`), `1900-01-01` (`400`), `> 30` años (`400`), fecha futura (`400`), fecha en el límite exacto de antigüedad (`201`) y fecha actual (`201`).
- **Resultado de las pruebas:** Superadas: 27/27 pruebas unitarias en `MascotaServiceTests` y 171/171 pruebas de integración y regresión (`MascotasEndpointsTests`, `AtencionesMedicasEndpointsTests`, `HistoriaClinicaEndpointsTests`, `DuenosEndpointsTests`, `AuthEndpointsTests`, `UsuariosEndpointsTests` y `TurnosEndpointsTests`) — **198/198 pruebas exitosas**.

---

#### VAL-02
- **Código:** VAL-02
- **Problema:** Rangos de peso poco razonables (`0.01 kg` a `1000 kg`), topes inconsistentes entre `Mascota` y `AtencionMedica`, y falta de validación de límite superior y de cantidad de decimales en los servicios.
- **Ubicación:**
  - `Shared/DTOs/Mascota/MascotaCreateDTO.cs` (línea 29).
  - `Shared/DTOs/Mascota/MascotaUpdateDTO.cs` (línea 19).
  - `Shared/DTOs/AtencionMedica/AtencionMedicaCreateDTO.cs` (línea 31).
  - `BusinessLogic/Services/MascotaService.cs` (`ValidateCreateDto`, `ValidateUpdateDto` y `ValidatePeso`).
  - `BusinessLogic/Services/AtencionMedicaService.cs` (`ValidateCreateDto`, líneas 380-383).
- **Evidencia:** El límite superior en `MascotaCreateDTO` era `1000 kg` mientras que en `AtencionMedicaCreateDTO` era `500 kg`. Los servicios solo validaban `> 0` sin cota mínima realista ni cota máxima. Además, SQLite no aplica la restricción `.HasPrecision(6, 2)` a nivel de motor, permitiendo almacenar valores con más de 2 decimales (ej. `12.345678 kg`).
- **Consecuencia:** Se aceptaban pesos irreales como `0.01 kg` (10 gramos) o `1000 kg` (1 tonelada), mascotas registradas con más de `500 kg` no podían recibir atenciones médicas, y se guardaban pesos con cantidad arbitraria de decimales.
- **Prioridad:** Alta
- **Solución propuesta:** Unificar el rango válido de peso entre `Mascota` y `AtencionMedica` en `0.05 kg` a `200.00 kg` y validar explícitamente en `MascotaService` y `AtencionMedicaService` tanto el rango `[0.05, 200.00]` como que el valor posea como máximo 2 decimales (`decimal.Round(peso, 2) == peso`), rechazando con `400 Bad Request` sin guardar ni redondear.
- **Decisión aprobada:** Aprobada — Unificar las validaciones de `Peso` (`Mascota`, alta y modificación) y `PesoRegistrado` (`AtencionMedica`) con mínimo `0.05 kg`, máximo `200 kg` y máximo `2` decimales, rechazando con `400 Bad Request` (`ValidationException`) cualquier valor fuera de rango o con más de 2 decimales sin guardar ni redondear.
- **Estado:** Completada
- **Solución implementada:**
  1. Se actualizaron `Shared/DTOs/Mascota/MascotaCreateDTO.cs`, `Shared/DTOs/Mascota/MascotaUpdateDTO.cs` y `Shared/DTOs/AtencionMedica/AtencionMedicaCreateDTO.cs` con `[Range(0.05, 200.00, ...)]`.
  2. En `BusinessLogic/Services/MascotaService.cs`, se agregaron las constantes `MinPesoKg = 0.05m` y `MaxPesoKg = 200.00m` y el método `ValidatePeso(decimal peso)` invocado en `ValidateCreateDto` y `ValidateUpdateDto`, rechazando con `ValidationException` valores `< 0.05m`, `> 200.00m` o con más de 2 decimales (`decimal.Round(peso, 2) != peso`).
  3. En `BusinessLogic/Services/AtencionMedicaService.cs` (`ValidateCreateDto`), se incorporó la misma validación de rango `[0.05m, 200.00m]` y precisión máxima de 2 decimales sobre `dto.PesoRegistrado`.
  4. Se agregaron pruebas unitarias y de integración en `MascotaServiceTests`, `AtencionMedicaServiceTests`, `MascotasEndpointsTests` y `AtencionesMedicasEndpointsTests`.
- **Pruebas necesarias:** Pruebas unitarias y de integración probando pesos inválidos (`0`, `-5`, `0.04`, `200.01`, `500`, `1000`, `12.345` con 3 decimales → `400 Bad Request` sin guardar ni modificar) y valores límite válidos (`0.05`, `200.00`, `12.34` → `200`/`201`).
- **Resultado de las pruebas:** Superadas: 88/88 pruebas unitarias en `MascotaServiceTests` y `AtencionMedicaServiceTests`, y 183/183 pruebas de integración y regresión en `API.Tests` (`MascotasEndpointsTests`, `AtencionesMedicasEndpointsTests`, `HistoriaClinicaEndpointsTests`, `DuenosEndpointsTests`, `AuthEndpointsTests`, `UsuariosEndpointsTests` y `TurnosEndpointsTests`) — **271/271 pruebas exitosas**.

---

#### VAL-03
- **Código:** VAL-03
- **Problema:** El cálculo de `PorcentajeDisminucionPeso` guarda porcentajes negativos y puede desbordar `HasPrecision(6, 2)` cuando la mascota aumenta de peso.
- **Ubicación:** `BusinessLogic/Services/AtencionMedicaService.cs` (método `CreateAsync`, líneas 102-121).
- **Evidencia:** El servicio calculaba `var disminucion = pesoReferencia - dto.PesoRegistrado; var porcentaje = decimal.Round((disminucion / pesoReferencia) * 100m, 2);` sin comprobar si `disminucion > 0`.
- **Consecuencia:** Cuando una mascota aumentaba de peso respecto a su consulta anterior (por ejemplo, un cachorro que pasa de `0.10 kg` a `15.00 kg`), `PorcentajeDisminucionPeso` almacenaba un valor negativo de varios miles por ciento, superando `HasPrecision(6, 2)`.
- **Prioridad:** Media
- **Solución propuesta:** Calcular `PorcentajeDisminucionPeso` únicamente cuando exista una disminución real de peso (`disminucion > 0`); si `dto.PesoRegistrado >= pesoReferencia`, asignar `0m` sin activar la alerta de desnutrición.
- **Decisión aprobada:** Aprobada — Cuando la mascota aumente o mantenga su peso (`disminucion <= 0`), registrar `PorcentajeDisminucionPeso = 0m` sin activar la alerta de desnutrición; cuando disminuya (`disminucion > 0`), conservar el cálculo actual y la regla `RN-12` (activar la alerta únicamente si la pérdida supera estrictamente el `10%`).
- **Estado:** Completada
- **Solución implementada:**
  1. En `BusinessLogic/Services/AtencionMedicaService.cs` (`CreateAsync`), se condicionó el cálculo del porcentaje a `if (disminucion > 0)`, asignando `porcentajeDisminucionPeso = 0m` cuando `disminucion <= 0` y preservando intacta la regla `RN-12` (`porcentaje > 10.0m`).
  2. Se agregaron pruebas unitarias en `tests/BusinessLogic.Tests/AtencionMedicaServiceTests.cs` (`CreateAsync_AumentoOMantenimientoDePeso_RegistraPorcentajeCeroYSinAlerta`) y de integración en `tests/API.Tests/AtencionesMedicasEndpointsTests.cs` (`Create_AumentoDePeso_RegistraPorcentajeDisminucionCeroYSinAlerta`).
- **Pruebas necesarias:** Pruebas unitarias en `AtencionMedicaServiceTests` y de integración en `AtencionesMedicasEndpointsTests` verificando que cuando `PesoRegistrado >= PesoReferencia`, `PorcentajeDisminucionPeso` sea `0m` y `AlertaDesnutricion` sea `false`, manteniendo las pruebas de `RN-12`.
- **Resultado de las pruebas:** Superadas: 91/91 pruebas unitarias (`AtencionMedicaServiceTests` y `MascotaServiceTests`) y 184/184 pruebas de integración y regresión en `API.Tests` — **275/275 pruebas exitosas**.

---

#### VAL-04
- **Código:** VAL-04
- **Problema:** `DuenoUpdateDTO` no posee ninguna anotación de validación y `DuenoService.ValidateUpdateDto` no valida el formato de `Email`, nulidad del DTO ni longitudes máximas (`MaxLength`).
- **Ubicación:** `Shared/DTOs/Dueno/DuenoUpdateDTO.cs` (líneas 1-31) y `BusinessLogic/Services/DuenoService.cs` (`ValidateUpdateDto` y `ValidateCamposComunes`).
- **Evidencia:** A diferencia de `DuenoCreateDTO`, `DuenoUpdateDTO` carecía de `[Required]`, `[MaxLength]` y `[EmailAddress]`. En `DuenoService.ValidateUpdateDto` se había omitido la validación `new EmailAddressAttribute().IsValid(dto.Email)` y tampoco se verificaban longitudes máximas.
- **Consecuencia:** Mediante `PUT /api/duenos/{id}` era posible actualizar un dueño con un correo electrónico inválido (ej. `"sin-arroba"`) y textos que superaban los límites configurados en `VeterinariaDbContext`.
- **Prioridad:** Alta
- **Solución propuesta:** Agregar las anotaciones `[Required]`, `[MaxLength]` y `[EmailAddress]` en `DuenoUpdateDTO` y unificar la lógica de validación en `DuenoService` para que tanto `CreateAsync` como `UpdateAsync` validen nulidad, campos obligatorios, formato de email, formato/longitud de DNI y teléfono, y longitudes máximas.
- **Decisión aprobada:** Aprobada — Incorporar las anotaciones de validación en `DuenoUpdateDTO` y unificar la validación de formato de `Email`, campos obligatorios y longitudes máximas (`Nombre <= 100`, `Apellido <= 100`, `Email <= 150`, `Direccion <= 200`) tanto en alta como en modificación, rechazando con `400 Bad Request` sin alterar datos.
- **Estado:** Completada
- **Solución implementada:**
  1. Se agregaron `[Required]`, `[MaxLength]`, `[RegularExpression]` y `[EmailAddress]` en `Shared/DTOs/Dueno/DuenoUpdateDTO.cs`.
  2. Se unificaron las validaciones de alta y modificación en `BusinessLogic/Services/DuenoService.cs` mediante `ValidateCamposComunes`.
  3. Se agregaron pruebas unitarias y de integración en `DuenoServiceTests` y `DuenosEndpointsTests`.
- **Pruebas necesarias:** Pruebas unitarias en `DuenoServiceTests` y de integración en `DuenosEndpointsTests` enviando en `PUT /api/duenos/{id}` un email inválido y campos vacíos o que excedan `MaxLength`, comprobando que devuelvan `400 Bad Request` sin modificar datos.
- **Resultado de las pruebas:** Superadas: 36/36 pruebas unitarias en `DuenoServiceTests` y 194/194 pruebas de integración y regresión en `API.Tests` — **230/230 pruebas exitosas**.

---

#### VAL-05
- **Código:** VAL-05
- **Problema:** Validación insuficiente de longitud y formato en `Dueno.Dni`, ausencia de validación de formato en `Dueno.Telefono` y falta de control de `MaxLength` en `DuenoService.ValidateCreateDto`.
- **Ubicación:** `Shared/DTOs/Dueno/DuenoCreateDTO.cs`, `Shared/DTOs/Dueno/DuenoUpdateDTO.cs` y `BusinessLogic/Services/DuenoService.cs` (`ValidateCamposComunes`).
- **Evidencia:** `ValidateCreateDto` solo verificaba dígitos numéricos aceptando un DNI de 1 solo dígito (`"1"`) o más de 10 dígitos, y no validaba ningún formato para `Telefono` (aceptaba `"abcdef"` o `"111"`). Tampoco validaba en el servicio las longitudes máximas de `Nombre` (100), `Apellido` (100), `Telefono` (30), `Email` (150) y `Direccion` (200).
- **Consecuencia:** Permitía persistir DNIs irreales de un dígito y teléfonos compuestos por letras o símbolos inválidos.
- **Prioridad:** Alta
- **Solución propuesta:** Validar que `Dni` contenga únicamente dígitos con una longitud entre 7 y 10 dígitos, que `Telefono` tenga máximo 30 caracteres, mínimo 6 dígitos numéricos y solo caracteres telefónicos válidos (dígitos, espacios, `+`, `-`, `()`), y verificar los `MaxLength` en `DuenoService`.
- **Decisión aprobada:** Aprobada — Exigir en alta y modificación que `Dni` sea numérico de 7 a 10 dígitos y que `Telefono` tenga máximo 30 caracteres, mínimo 6 dígitos numéricos y solo caracteres telefónicos válidos, además de validar `MaxLength` en todos los campos.
- **Estado:** Completada
- **Solución implementada:**
  1. Se actualizaron `DuenoCreateDTO.cs` y `DuenoUpdateDTO.cs` y se implementó en `DuenoService.ValidateCamposComunes` la validación de `Dni` (solo dígitos, longitud entre 7 y 10), `Telefono` (máx. 30 caracteres, solo dígitos/espacios/`+`/`-`/`()`, mín. 6 dígitos) y `MaxLength` de todos los campos.
  2. Se agregaron pruebas unitarias y de integración en `DuenoServiceTests` y `DuenosEndpointsTests`.
- **Pruebas necesarias:** Pruebas unitarias y de integración con `Dni` corto (`"1"`, `"123456"`), largo (`11` dígitos) o alfanumérico (`400`), `Telefono` corto (`"123"`) o con letras (`400`), exceso de `MaxLength` (`400`) y valores válidos (`201`/`200`).
- **Resultado de las pruebas:** Superadas: 36/36 pruebas unitarias en `DuenoServiceTests` y 194/194 pruebas de integración y regresión en `API.Tests` — **230/230 pruebas exitosas**.

---

#### VAL-06
- **Código:** VAL-06
- **Problema:** Falta validación de `FechaProximaDosis` futura en vacunaciones y validación de longitudes máximas (`MaxLength`) en `AtencionMedicaCreateDTO` y `AtencionMedicaService`.
- **Ubicación:** `Shared/DTOs/AtencionMedica/AtencionMedicaCreateDTO.cs` (líneas 14-24), `Shared/DTOs/AtencionMedica/VacunacionDTO.cs` (línea 17) y `BusinessLogic/Services/AtencionMedicaService.cs` (método `ValidateCreateDto`, líneas 377-520).
- **Evidencia:** `VacunacionCreateDTO.FechaProximaDosis` no se validaba en ningún punto, permitiendo registrar una fecha de próxima dosis en el pasado (ej. año `2000`) o igual al día actual. Además, `ValidateCreateDto` no verificaba los `MaxLength` definidos en `VeterinariaDbContext` ni rechazaba objetos `Vacunacion` sin nombre de vacuna.
- **Consecuencia:** Permitía programar próximas dosis de vacunas en fechas pasadas o actuales, guardar datos de vacuna incompletos e ingresar textos que excedían los límites del modelo de datos.
- **Prioridad:** Media
- **Solución propuesta:** En `AtencionMedicaService.ValidateCreateDto`, validar que si `FechaProximaDosis.HasValue`, sea estrictamente posterior a la fecha actual (`> DateTime.UtcNow.Date`), rechazar datos de vacunación incompletos, no guardar dosis/lote/próxima dosis si no se registra vacuna y validar las longitudes máximas de todos los campos de la atención médica y sus sub-entidades (`Vacunacion`, `EvaluacionAptitud`, `ProcedimientoQuirurgico`, `Prescripciones`, `Estudios`).
- **Decisión aprobada:** Mantener `FechaProximaDosis` opcional y exigir que, si se informa, sea estrictamente posterior al día actual (`> DateTime.UtcNow.Date`, devolviendo `400 Bad Request` en caso contrario); rechazar con `400` datos de vacunación incompletos y no guardar dosis, lote ni próxima dosis si no se registra vacuna; validar los `MaxLength` existentes de los campos clínicos y sus subobjetos sin cambiar los límites establecidos, conservando las reglas de aptitud para vacunación, inmutabilidad y contratos HTTP.
- **Estado:** Completada
- **Solución implementada:**
  1. En `Shared/DTOs/AtencionMedica/AtencionMedicaCreateDTO.cs` se agregó `[MaxLength(100)]` sobre `VacunaAplicada`.
  2. En `BusinessLogic/Services/AtencionMedicaService.cs` (`ValidateCreateDto` y `CreateAsync`), se validó que `FechaProximaDosis` (cuando tiene valor) tenga `.Value.Date > DateTime.UtcNow.Date`, que `dto.Vacunacion.Vacuna` no sea vacío ni espacios en blanco cuando se provee el subobjeto `Vacunacion`, que `VacunaDosis`, `VacunaLote` y `VacunaProximaDosis` solo se persistan cuando `solicitaVacuna == true`, y se incorporaron las validaciones de `MaxLength` para todos los campos clínicos y sus subobjetos (`MotivoConsulta <= 250`, `Anamnesis <= 2000`, `ExamenFisico <= 2000`, `Diagnostico <= 1000`, `Tratamiento <= 1000`, `Observaciones <= 500`, `MotivoNoApto <= 500`, `Vacuna <= 100`, `Dosis <= 50`, `Lote <= 50`, `ProcedimientoQuirurgico`, `Prescripciones`, `Estudios`).
  3. Se agregaron pruebas unitarias en `tests/BusinessLogic.Tests/AtencionMedicaServiceTests.cs` y de integración en `tests/API.Tests/AtencionesMedicasEndpointsTests.cs`.
- **Pruebas necesarias:** Pruebas unitarias en `AtencionMedicaServiceTests` y de integración en `AtencionesMedicasEndpointsTests` para fechas de próxima dosis válidas, inválidas (pasadas, hoy, `default`), ausencia de próxima dosis (`null`), datos de vacunación incompletos y exceso de longitudes máximas (`MaxLength`).
- **Resultado de las pruebas:** Superadas — `478/478` pruebas aprobadas en la suite completa (`259` unitarias en `BusinessLogic.Tests` + `219` de integración en `API.Tests`).

---

#### VAL-07
- **Código:** VAL-07
- **Problema:** El campo `Mascota.Sexo` acepta cualquier cadena de texto arbitraria de hasta 20 caracteres.
- **Ubicación:** `Shared/DTOs/Mascota/MascotaCreateDTO.cs` (línea 23), `Shared/DTOs/Mascota/MascotaUpdateDTO.cs` (línea 19) y `BusinessLogic/Services/MascotaService.cs` (métodos `CreateAsync`, `UpdateAsync`, `ValidateCreateDto`, `ValidateUpdateDto` y `NormalizeAndValidateSexo`).
- **Evidencia:** Solo se validaba `!string.IsNullOrWhiteSpace(dto.Sexo)` y `[MaxLength(20)]`, admitiendo valores sin sentido clínico como `"12345"` o `"Cualquiera"`.
- **Consecuencia:** Inconsistencia en los datos clínicos del paciente y en las agrupaciones estadísticas.
- **Prioridad:** Media
- **Solución propuesta:** Validar en `MascotaService` (en `CreateAsync` y `UpdateAsync`) que `Sexo` pertenezca al conjunto de valores permitidos definidos por negocio (`"Macho"` y `"Hembra"`), normalizando su formato al guardar.
- **Decisión aprobada:** Aceptar únicamente `"Macho"` y `"Hembra"` en el alta y modificación de mascotas (permitiendo diferencias de mayúsculas, minúsculas y espacios al inicio o final), normalizando siempre el valor guardado a `"Macho"` o `"Hembra"` y rechazando cualquier otro valor con `400 Bad Request` sin guardar modificaciones.
- **Estado:** Completada
- **Solución implementada:**
  1. En `BusinessLogic/Services/MascotaService.cs`, se implementó `NormalizeAndValidateSexo(string? sexo)` invocado tanto en el alta (`CreateAsync` / `ValidateCreateDto`) como en la modificación (`UpdateAsync` / `ValidateUpdateDto`), exigiendo que el valor (tras `.Trim()`, *case-insensitive*) sea `"Macho"` o `"Hembra"`, asignando el valor normalizado (`"Macho"` o `"Hembra"`) y lanzando `ValidationException` (`400 Bad Request`) ante cualquier otro valor.
  2. Se corrigió el valor de `peso` en `bruno/mascotas/5. Crear Mascota Exitosa (201).bru` (`12.50`) para respetar `VAL-02`.
  3. Se agregaron pruebas unitarias en `tests/BusinessLogic.Tests/MascotaServiceTests.cs` y de integración en `tests/API.Tests/MascotasEndpointsTests.cs` verificando valores válidos, normalización de mayúsculas/minúsculas/espacios y rechazo de valores fuera de catálogo sin persistir ni modificar datos.
- **Pruebas necesarias:** Pruebas unitarias en `MascotaServiceTests` y de integración en `MascotasEndpointsTests` enviando `Sexo` válido (con distintas combinaciones de mayúsculas/minúsculas y espacios) e inválido (`400 Bad Request`) tanto en `POST /api/mascotas` como en `PUT /api/mascotas/{id}`.
- **Resultado de las pruebas:** Superadas — `499/499` pruebas aprobadas en la suite completa (`279` unitarias en `BusinessLogic.Tests` + `220` de integración en `API.Tests`).

---

#### VAL-08
- **Código:** VAL-08
- **Problema:** `TurnoService.ConfigurarDisponibilidadVeterinarioAsync` no valida que `HoraInicio` y `HoraFin` estén dentro de las 24 horas del día, alineados a bloques de 30 minutos (`:00` o `:30`), que `DiaSemana` sea válido ni normaliza `Jornada`.
- **Ubicación:** `BusinessLogic/Services/TurnoService.cs` (método `ConfigurarDisponibilidadVeterinarioAsync`, líneas 307-390).
- **Evidencia:** Solo se verificaba `if (item.HoraInicio >= item.HoraFin)` y `item.Jornada.Trim()` sin proteger contra elementos nulos ni normalizar el valor guardado. En .NET, `TimeSpan` admite valores negativos (`-03:00:00`) o mayores a 24 horas (`30:00:00`), minutos no múltiplos de 30 (`08:17:00`) o segundos/milisegundos distintos de cero, y `DayOfWeek` admite enteros fuera del rango `0..6`.
- **Consecuencia:** Permitía guardar disponibilidades horarias fuera de las 24 horas del día, desalineadas respecto a la grilla de turnos de 30 minutos, con días de la semana inválidos o con jornadas sin normalizar.
- **Prioridad:** Media
- **Solución propuesta:** Validar en `ConfigurarDisponibilidadVeterinarioAsync` que `dto` y cada elemento no sean nulos, que `DiaSemana` pertenezca a `DayOfWeek`, que `Jornada` sea obligatoria y pertenezca a `"Manana"` o `"Tarde"` (normalizando el valor guardado), que `HoraInicio >= TimeSpan.Zero`, `HoraFin <= TimeSpan.FromHours(24)` y `HoraInicio < HoraFin`, y que ambos horarios estén alineados a bloques exactos de 30 minutos sin segundos ni fracciones, lanzando `DisponibilidadInvalidaException` (`409 Conflict`) sin alterar las disponibilidades anteriores.
- **Decisión aprobada:** Mantener `DisponibilidadInvalidaException` (`409 Conflict`) para las nuevas validaciones de disponibilidad respetando el contrato HTTP existente; validar `DiaSemana` válido y `Jornada` obligatoria aceptando únicamente `"Manana"` y `"Tarde"` (*case-insensitive* y sin espacios extremos) y guardando el valor normalizado; validar que `HoraInicio` y `HoraFin` estén dentro del día (`00:00` a `24:00`) con `HoraInicio < HoraFin` y alineados a bloques exactos de 30 minutos sin segundos ni fracciones; mantener la validación de superposiciones; rechazar DTO nulo o elementos nulos sin errores internos; no implementar límites rígidos entre jornadas y garantizar que ninguna validación fallida modifique las disponibilidades anteriores.
- **Estado:** Completada
- **Solución implementada:**
  1. En `BusinessLogic/Services/TurnoService.cs` (`ConfigurarDisponibilidadVeterinarioAsync`, `NormalizarYValidarJornadaDisponibilidad` y `EsBloqueHorarioDe30MinutosValido`), se validó que `dto` no sea nulo (`ValidationException` → `400 Bad Request`), y para cada elemento de `dto.Disponibilidades` se validó que no sea nulo, que `Enum.IsDefined(typeof(DayOfWeek), item.DiaSemana)` sea verdadero, que `item.Jornada` sea obligatorio y normalizado a `"Manana"` o `"Tarde"`, que `item.HoraInicio >= TimeSpan.Zero`, `item.HoraFin <= TimeSpan.FromHours(24)` e `item.HoraInicio < item.HoraFin`, que ambos horarios sean múltiplos exactos de 30 minutos (`horario.Ticks % TimeSpan.FromMinutes(30).Ticks == 0`) y que no existan superposiciones en el mismo día, lanzando `DisponibilidadInvalidaException` (`409 Conflict`) antes de persistir cualquier cambio.
  2. Se agregaron pruebas unitarias en `tests/BusinessLogic.Tests/TurnoServiceTests.cs` y de integración en `tests/API.Tests/TurnosEndpointsTests.cs` cubriendo todos los escenarios válidos e inválidos.
- **Pruebas necesarias:** Pruebas unitarias en `TurnoServiceTests` y de integración en `TurnosEndpointsTests` intentando configurar disponibilidad con DTO nulo, elementos nulos, `DiaSemana` inválido, `Jornada` vacía/inválida, normalización de `Jornada`, `TimeSpan` negativo o mayor a 24 h, `HoraInicio >= HoraFin`, minutos no múltiplos de 30 o con segundos/milisegundos (`409 Conflict` sin modificar datos previos) y casos válidos (`200 OK`).
- **Resultado de las pruebas:** Superadas — `522/522` pruebas aprobadas en la suite completa (`300` unitarias en `BusinessLogic.Tests` + `222` de integración en `API.Tests`).

---

#### VAL-09
- **Código:** VAL-09
- **Problema:** `ReporteService.GenerarReporteAsync` no valida que `TiposReporte` contenga valores del catálogo válido, no valida la longitud máxima de `Motivo` ni rechaza fechas `default(DateTime)` o futuras.
- **Ubicación:** `BusinessLogic/Services/ReporteService.cs` (método `GenerarReporteAsync`, líneas 46-104).
- **Evidencia:** El servicio comprobaba `if (dto.FechaDesde > dto.FechaHasta)` y luego evaluaba `tiposNormalizados.Any(t => t.Equals("Atenciones", ...))`, etc. Si se enviaba `TiposReporte = ["TipoInexistente"]` o `FechaDesde = 0001-01-01` / fecha futura, no lanzaba excepción y persistía un reporte vacío o incoherente en la base de datos.
- **Consecuencia:** Permitía almacenar reportes vacíos con tipos de reporte inexistentes o con rangos temporales irreales (`0001-01-01` o fechas futuras).
- **Prioridad:** Media
- **Solución propuesta:** Validar en `ReporteService.GenerarReporteAsync` que todos los elementos de `TiposReporte` pertenezcan al catálogo válido (`Atenciones`, `Vacunaciones`, `Mascotas`, `Duenos`, `Turnos`), que `Motivo` sea obligatorio y no supere los 200 caracteres tras `Trim()`, y que `FechaDesde` y `FechaHasta` no sean `default(DateTime)` ni posteriores a la fecha actual.
- **Decisión aprobada:** Aceptar únicamente los tipos de reporte `"Atenciones"`, `"Vacunaciones"`, `"Mascotas"`, `"Duenos"` y `"Turnos"` (permitiendo diferencias de mayúsculas, minúsculas y espacios extremos y guardando siempre los nombres normalizados); rechazar cualquier tipo inválido o elemento nulo/vacío con `ValidationException` (`400 Bad Request`); rechazar `FechaDesde` o `FechaHasta` con valor `default(DateTime)`, fechas futuras o rangos invertidos manteniendo `RangoFechasInvalidoException` (`400 Bad Request`) sin imponer el año 2000 como límite mínimo; validar que `Motivo` sea obligatorio y no supere los 200 caracteres después de `Trim()`; validar DTO nulo y lista nula o vacía; y garantizar que los errores se detecten antes de guardar cualquier reporte.
- **Estado:** Completada
- **Solución implementada:**
  1. En `BusinessLogic/Services/ReporteService.cs` (`GenerarReporteAsync`), se definió el catálogo `TiposReportePermitidos` (`"Atenciones"`, `"Vacunaciones"`, `"Mascotas"`, `"Duenos"`, `"Turnos"`) y se incorporaron al inicio del método las validaciones de nulidad de `dto`, obligatoriedad y longitud máxima de `Motivo` (`<= 200` caracteres tras `.Trim()`), validación y normalización canónica sin duplicados de cada elemento de `TiposReporte` (`ValidationException` → `400 Bad Request` ante elementos nulos, vacíos o fuera de catálogo), y validación de `FechaDesde` y `FechaHasta` contra `default(DateTime)`, rangos invertidos y fechas futuras (`> DateTime.UtcNow.Date`) mediante `RangoFechasInvalidoException` (`400 Bad Request`).
  2. Se agregaron pruebas unitarias en `tests/BusinessLogic.Tests/ReporteServiceTests.cs` y de integración en `tests/API.Tests/ReportesEndpointsTests.cs` cubriendo todos los escenarios válidos e inválidos.
- **Pruebas necesarias:** Pruebas unitarias en `ReporteServiceTests` y de integración en `ReportesEndpointsTests` con DTO nulo, `Motivo` mayor a 200 caracteres, tipos de reporte inválidos o vacíos, normalización de tipos con mayúsculas/minúsculas y espacios, y fechas `0001-01-01` o futuras, verificando que se devuelva `400 Bad Request` sin persistir el reporte.
- **Resultado de las pruebas:** Superadas — `535/535` pruebas aprobadas en la suite completa (`311` unitarias en `BusinessLogic.Tests` + `224` de integración en `API.Tests`).

---

#### VAL-10
- **Código:** VAL-10
- **Problema:** Ausencia de validación para evitar el registro duplicado accidental de la misma mascota para un mismo dueño.
- **Ubicación:** `BusinessLogic/Services/MascotaService.cs` (método `CreateAsync`, líneas 49-89) y `DataAccess/Repositories/MascotaRepository.cs`.
- **Evidencia:** `MascotaService.CreateAsync` verifica que el dueño exista, pero no comprueba si ese mismo dueño ya tiene una mascota activa con igual `Nombre` y `Especie`.
- **Consecuencia:** Un doble clic o reintento en recepción puede dar de alta dos fichas idénticas para la misma mascota del mismo dueño.
- **Prioridad:** Baja
- **Solución propuesta:** Evaluar si se incorpora una verificación en `MascotaRepository` / `MascotaService` que rechace con `409 Conflict` el alta de una mascota cuando el mismo `DuenoId` ya posee una mascota activa con igual `Nombre` y `Especie`.
- **Decisión aprobada:** Postergar como mejora opcional futura, dado que no constituye una regla de negocio especificada en `CU-07` ni en el catálogo `RN-01`..`RN-12`, y en escenarios reales un mismo dueño puede registrar dos mascotas rescatadas o sin nombre definitivo bajo igual denominación inicial.
- **Estado:** Postergada (Mejora opcional no requerida en RN)
- **Solución implementada:** No se aplica restricción adicional en esta entrega para conservar el contrato actual de `POST /api/mascotas`.
- **Pruebas necesarias:** Prueba unitaria y de integración intentando registrar dos veces la misma mascota (`Nombre` + `Especie`) bajo el mismo `DuenoId`.
- **Resultado de las pruebas:** Postergada

---

### 2.4. Manejo de Errores (`ERR`)

#### ERR-01
- **Código:** ERR-01
- **Problema:** Ausencia de un mecanismo centralizado de manejo de excepciones (`IExceptionHandler` / `UseExceptionHandler`), provocando que excepciones no capturadas expongan detalles internos.
- **Ubicación:** `API/Program.cs` y los 8 controladores en `API/Controllers/`.
- **Evidencia:** `Program.cs` no configuraba `AddExceptionHandler` ni `app.UseExceptionHandler()`. Cada controlador implementaba bloques `try/catch` manuales solo para ciertas excepciones previstas; si ocurría una `DbUpdateException`, `SqliteException` o `InvalidOperationException` inesperada, la excepción escapaba sin control.
- **Consecuencia:** Ante un fallo inesperado de base de datos o una excepción no listada en un controlador, la API podía devolver el *stack trace* completo, nombres de tablas/columnas SQL o respuestas sin formato JSON estándar.
- **Prioridad:** Alta
- **Solución propuesta:** Agregar `app.UseExceptionHandler` en `API/Program.cs` como respaldo global para registrar internamente cualquier excepción no controlada mediante `ILogger` y devolver `500 Internal Server Error` con JSON genérico (`{ "message": "Ocurrió un error interno al procesar la solicitud." }`) sin filtrar detalles técnicos ni *stack traces* (incluso en `Development`), conservando los `try/catch` específicos en los controladores.
- **Decisión aprobada:** Aprobada — Incorporar `app.UseExceptionHandler` en `API/Program.cs` como respaldo global, registrando el error internamente con `ILogger<Program>` y devolviendo `500 Internal Server Error` con `{ "message": "Ocurrió un error interno al procesar la solicitud." }` sin exponer detalles técnicos ni *stack trace* en ningún entorno, conservando los contratos HTTP y `try/catch` específicos existentes.
- **Estado:** Completada
- **Solución implementada:**
  1. Se configuró `app.UseExceptionHandler(...)` al inicio del pipeline HTTP en `API/Program.cs`, registrando la excepción no controlada en `ILogger<Program>` y respondiendo `500 Internal Server Error` (`application/json`) con `{ "message": "Ocurrió un error interno al procesar la solicitud." }`.
  2. Se agregó la prueba de integración `UnhandledInvalidOperationException_InUsuariosEndpoints_Returns500GenericJsonWithoutLeakingInternalDetails` en `tests/API.Tests/UsuariosEndpointsTests.cs`.
- **Pruebas necesarias:** Pruebas de integración simulando una excepción no controlada en capa de persistencia/servicio y verificando que la respuesta sea `500 Internal Server Error` con mensaje genérico y sin exponer `StackTrace` ni el mensaje interno.
- **Resultado de las pruebas:** Superadas: 217/217 pruebas unitarias en `BusinessLogic.Tests` y 208/208 pruebas de integración en `API.Tests` — **425/425 pruebas exitosas**.

---

#### ERR-02
- **Código:** ERR-02
- **Problema:** Los mensajes de las excepciones en `Shared/Exceptions/` exponen identificadores internos, códigos técnicos de reglas de negocio y detalles internos de cuentas de usuario.
- **Ubicación:** `Shared/Exceptions/` (`VeterinarioNotFoundException.cs`, `AtencionInmutableException.cs`, `DuenoConMascotasException.cs`, `MascotaInactivaException.cs`, `MascotaYaInactivaException.cs`, `PacienteNoAptoVacunacionException.cs`, `SeguimientoCronicoDuplicadoException.cs`, `TurnoFinalizadoException.cs`, `TurnoYaCanceladoException.cs`, `DuenoNotFoundException.cs`, `MascotaNotFoundException.cs`, `AtencionMedicaNotFoundException.cs`, `TurnoNotFoundException.cs`, `ReporteNotFoundException.cs`, `UsuarioNotFoundException.cs`).
- **Evidencia:**
  1. `VeterinarioNotFoundException.cs` devolvía: `$"El profesional veterinario con ID '{id}' no fue encontrado, no se encuentra activo o no posee rol de Veterinario."`, revelando detalles de cuentas internas.
  2. Excepciones como `TurnoFinalizadoException` y `SeguimientoCronicoDuplicadoException` incluían códigos internos `(RN-09)`, `(RN-11)` y los IDs internos de base de datos en el texto destinado al usuario final.
- **Consecuencia:** Las respuestas de error devolvían información técnica e interna innecesaria al cliente, tal como observó el profesor.
- **Prioridad:** Alta
- **Solución propuesta:** Depurar los mensajes por defecto de las clases en `Shared/Exceptions/` para que devuelvan mensajes claros orientados al usuario y alineados con los Casos de Uso, sin exponer IDs internos, códigos `(RN-XX)` ni estados internos de cuentas de usuario.
- **Decisión aprobada:** Aprobada — Limpiar los mensajes por defecto de las excepciones de dominio en `Shared/Exceptions/` eliminando `Guid`, códigos `RN-XX` e información interna de roles/estados de cuenta, conservando los constructores y contratos HTTP existentes.
- **Estado:** Completada
- **Solución implementada:**
  1. Se actualizaron las 15 clases de excepción en `Shared/Exceptions/` (`VeterinarioNotFoundException`, `AtencionInmutableException`, `DuenoConMascotasException`, `MascotaInactivaException`, `MascotaYaInactivaException`, `PacienteNoAptoVacunacionException`, `SeguimientoCronicoDuplicadoException`, `TurnoFinalizadoException`, `TurnoYaCanceladoException`, `DuenoNotFoundException`, `MascotaNotFoundException`, `AtencionMedicaNotFoundException`, `TurnoNotFoundException`, `ReporteNotFoundException`, `UsuarioNotFoundException`) eliminando la interpolación de `Guid`, los códigos `(RN-09)` / `(RN-11)` y los detalles internos de roles/estados.
  2. Se agregaron las pruebas `DomainExceptions_DefaultMessages_DoNotExposeInternalGuidsRuleCodesOrRoleDetails` en `tests/BusinessLogic.Tests/UsuarioServiceTests.cs` y `DomainErrorResponses_DoNotExposeInternalGuidOrRuleCodesInHttpResponse` en `tests/API.Tests/UsuariosEndpointsTests.cs`.
- **Pruebas necesarias:** Pruebas unitarias y de integración verificando que las respuestas `404` y `409` contengan únicamente el mensaje funcional limpio sin exponer detalles técnicos.
- **Resultado de las pruebas:** Superadas: 217/217 pruebas unitarias en `BusinessLogic.Tests` y 208/208 pruebas de integración en `API.Tests` — **425/425 pruebas exitosas**.

---

#### ERR-03
- **Código:** ERR-03
- **Problema:** Captura genérica de `InvalidOperationException` exponiendo `ex.Message` al cliente en `UsuariosController`.
- **Ubicación:** `API/Controllers/UsuariosController.cs` (métodos `Desbloquear` y `RestablecerPassword`) y `BusinessLogic/Services/UsuarioService.cs` (`DesbloquearCuentaAsync`).
- **Evidencia:** `UsuarioService.DesbloquearCuentaAsync` lanzaba `new InvalidOperationException("La cuenta no se encuentra bloqueada.")` y `UsuariosController.Desbloquear` y `RestablecerPassword` capturaban cualquier `InvalidOperationException` devolviendo `BadRequest(new { message = ex.Message })`.
- **Consecuencia:** Si EF Core o el runtime de .NET lanzaban una `InvalidOperationException` interna (por ejemplo, error de conexión o de mapeo), `UsuariosController` la devolvía como `400 Bad Request` filtrando el mensaje interno del framework al cliente.
- **Prioridad:** Alta
- **Solución propuesta:** Reemplazar `InvalidOperationException` en `UsuarioService.DesbloquearCuentaAsync` por una excepción de dominio específica (`CuentaNoBloqueadaException`), mantener `400 Bad Request` para esa situación y eliminar las capturas genéricas de `InvalidOperationException` en `UsuariosController`.
- **Decisión aprobada:** Aprobada — Crear `CuentaNoBloqueadaException`, lanzarla en `UsuarioService.DesbloquearCuentaAsync`, capturarla en `UsuariosController.Desbloquear` manteniendo `400 Bad Request` y eliminar los bloques `catch (InvalidOperationException)` de `Desbloquear` y `RestablecerPassword`.
- **Estado:** Completada
- **Solución implementada:**
  1. Se creó `Shared/Exceptions/CuentaNoBloqueadaException.cs`.
  2. Se actualizó `BusinessLogic/Services/UsuarioService.cs` (`DesbloquearCuentaAsync`) para lanzar `CuentaNoBloqueadaException`.
  3. En `API/Controllers/UsuariosController.cs`, se reemplazó `catch (InvalidOperationException ex)` por `catch (CuentaNoBloqueadaException ex)` en `Desbloquear` y se eliminó `catch (InvalidOperationException ex)` de `RestablecerPassword`.
  4. Se actualizaron y agregaron pruebas unitarias y de integración en `UsuarioServiceTests` y `UsuariosEndpointsTests`.
- **Pruebas necesarias:** Pruebas unitarias y de integración en `UsuarioServiceTests` y `UsuariosEndpointsTests` verificando el manejo de `CuentaNoBloqueadaException` (`400 Bad Request`) cuando la cuenta no está bloqueada y que un `InvalidOperationException` inesperado de base de datos resulte en `500` seguro y no en `400`.
- **Resultado de las pruebas:** Superadas: 217/217 pruebas unitarias en `BusinessLogic.Tests` y 208/208 pruebas de integración en `API.Tests` — **425/425 pruebas exitosas**.

---

#### ERR-04
- **Código:** ERR-04
- **Problema:** Diferencias en la estructura JSON de las respuestas de error y confirmación entre controladores y validaciones automáticas de modelo.
- **Ubicación:** `API/Controllers/AuthController.cs` (líneas 94 y 101), resto de controladores en `API/Controllers/` y configuración de `[ApiController]` en `API/Program.cs`.
- **Evidencia:**
  1. Todos los controladores devuelven errores con la propiedad `{ "message": "..." }`, mientras que `AuthController.Logout` y `VerificarSesion` usan `{ "mensaje": "..." }` (y `LoginResponseDTO` también usa `Mensaje`).
  2. Cuando falla una validación de DataAnnotations en un DTO antes de entrar al controlador, `[ApiController]` devuelve un objeto `ValidationProblemDetails` (`{ "type", "title", "status", "errors", "traceId" }`), distinto al `{ "message": "..." }` devuelto cuando `ValidationException` se lanza en la capa de negocio.
- **Consecuencia:** El cliente HTTP recibe estructuras JSON distintas para errores `400 Bad Request` dependiendo de si la validación se disparó en el DTO o en el servicio.
- **Prioridad:** Media
- **Solución propuesta:** Estandarizar el formato de respuesta de error en toda la API mediante el manejador centralizado y configurar `InvalidModelStateResponseFactory` en `Program.cs` para que los errores de validación de modelo devuelvan un formato consistente y predecible sin exponer `traceId` innecesarios.
- **Decisión aprobada:** Postergar para no romper la compatibilidad con los contratos JSON esperados actualmente por las pruebas de integración automatizadas y por la colección de requests en `bruno/`, habiéndose resuelto ya la seguridad y limpieza de mensajes de error mediante `ERR-01`, `ERR-02` y `ERR-03`.
- **Estado:** Postergada (Preservación de contratos HTTP / Bruno)
- **Solución implementada:** Se conservan los contratos de respuesta actuales de `[ApiController]` y `AuthController` junto con el manejador global seguro implementado en `ERR-01`.
- **Pruebas necesarias:** Pruebas de integración verificando que tanto las fallas de validación de DTO como las `ValidationException` de negocio devuelvan la misma estructura JSON de error `400 Bad Request`.
- **Resultado de las pruebas:** Postergada

---

### 2.5. Bugs Funcionales y Coherencia entre Documentación, Casos de Uso y Código (`BUG` / `DOC`)

#### BUG-01
- **Código:** BUG-01
- **Problema:** `ReporteService` ignora el rango de fechas `[FechaDesde, FechaHasta]` al calcular las métricas de `Mascotas`, devolviendo siempre todas las mascotas históricas.
- **Ubicación:** `BusinessLogic/Services/ReporteService.cs` (métodos `GenerarReporteAsync` línea 62 y `ConstruirMetricasMascotasAsync` líneas 127-144).
- **Evidencia:** Mientras que `ConstruirMetricasAtencionesAsync`, `ConstruirMetricasVacunacionesAsync`, `ConstruirMetricasDuenosAsync` y `ConstruirMetricasTurnosAsync` reciben `(desde, hasta)` y filtran por dicho período, `ConstruirMetricasMascotasAsync()` no recibe parámetros de fecha y llama a `_mascotaRepository.GetAllAsync()`.
- **Consecuencia:** Cualquier reporte generado para el tipo `"Mascotas"` (según `CU-11`) arroja números erróneos porque contabiliza todas las mascotas de la historia del sistema en lugar de las registradas en el período `[FechaDesde, FechaHasta]`.
- **Prioridad:** Alta
- **Solución propuesta:** Una vez incorporado el campo `FechaRegistro` en la entidad `Mascota` (`BD-02`), actualizar `ConstruirMetricasMascotasAsync` (y el método correspondiente en `IMascotaRepository`) para filtrar las mascotas cuya `FechaRegistro` se encuentre dentro de `[desde, hasta]`.
- **Decisión aprobada:** Postergar junto con `BD-02`, dado que la entidad `Mascota` no posee actualmente columna `FechaRegistro` en la base de datos SQLite y su incorporación requiere una migración de esquema de EF Core.
- **Estado:** Postergada (Dependiente de migración `BD-02`)
- **Solución implementada:** No se modifica el cálculo de `ConstruirMetricasMascotasAsync` en esta entrega por depender directamente de la incorporación de `Mascota.FechaRegistro` en base de datos (`BD-02`).
- **Pruebas necesarias:** Prueba unitaria en `ReporteServiceTests` y de integración en `ReportesEndpointsTests` con mascotas registradas dentro y fuera del rango `[FechaDesde, FechaHasta]`, comprobando que solo se contabilicen las del período solicitado.
- **Resultado de las pruebas:** Postergada

---

#### BUG-02
- **Código:** BUG-02
- **Problema:** `PUT /api/duenos/{id}` no permite actualizar el `Dni` del dueño ni verifica la regla `RN-05` (`DniDuplicadoException`) en la modificación, incumpliendo `CU-08`.
- **Ubicación:** `Shared/DTOs/Dueno/DuenoUpdateDTO.cs`, `BusinessLogic/Services/DuenoService.cs` (`UpdateAsync`) y `API/Controllers/DuenosController.cs` (`Update`).
- **Evidencia:** El documento `Documentos/Casos_de_uso/Casos_de_uso_tp/cu-08-gestionar-duenos.md` (Paso 1, Paso 3 y Flujo alternativo `3b`) especifica que `PUT /api/duenos/{id}` incluye el campo `dni` y que si el nuevo DNI ya pertenece a otro dueño debe lanzar `DniDuplicadoException` (`409 Conflict`). Sin embargo, `DuenoUpdateDTO` no tenía la propiedad `Dni`.
- **Consecuencia:** Si un recepcionista cometía un error al tipear el DNI de un dueño durante el alta, el sistema no permitía corregirlo nunca. Además, el código divergía de lo especificado en `CU-08`.
- **Prioridad:** Alta
- **Solución propuesta:** Agregar la propiedad `Dni` a `DuenoUpdateDTO`, validar en `DuenoService.UpdateAsync` que si el DNI ya pertenece a otro dueño (`duenoConMismoDni.Id != id`) se lance `DniDuplicadoException` (`409 Conflict`), y actualizar `dueno.Dni`.
- **Decisión aprobada:** Aprobada — Hacer `Dni` obligatorio en `DuenoUpdateDTO`, permitir actualizar el DNI en `PUT /api/duenos/{id}` conservando el propio o asignando uno libre, y rechazar con `409 Conflict` (`DniDuplicadoException`) si pertenece a otro dueño sin modificar datos.
- **Estado:** Completada
- **Solución implementada:**
  1. Se incorporó `Dni` obligatorio en `Shared/DTOs/Dueno/DuenoUpdateDTO.cs`.
  2. En `BusinessLogic/Services/DuenoService.cs` (`UpdateAsync`), se verificó mediante `_duenoRepository.GetByDniAsync(dniNormalizado)` que no exista otro dueño con `duenoConMismoDni.Id != id` (lanzando `DniDuplicadoException` en caso contrario) y se asignó `dueno.Dni = dniNormalizado`.
  3. En `API/Controllers/DuenosController.cs` (`Update`), se agregó el bloque `catch (DniDuplicadoException ex)` que retorna `409 Conflict`.
  4. Se agregaron pruebas unitarias y de integración en `DuenoServiceTests` y `DuenosEndpointsTests`.
- **Pruebas necesarias:** Pruebas unitarias en `DuenoServiceTests` y de integración en `DuenosEndpointsTests`: actualización manteniendo el mismo DNI (`200 OK`), cambiando a un DNI libre (`200 OK`) y cambiando al DNI de otro dueño existente (`409 Conflict` sin modificar datos).
- **Resultado de las pruebas:** Superadas: 36/36 pruebas unitarias en `DuenoServiceTests` y 194/194 pruebas de integración y regresión en `API.Tests` — **230/230 pruebas exitosas**.

---

#### BUG-03
- **Código:** BUG-03
- **Problema:** `AtencionMedicaCreateDTO` y `AtencionMedicaService` permiten omitir `Anamnesis` y `ExamenFisico` e inventan textos clínicos por defecto en lugar de rechazar la solicitud como exige `CU-04`.
- **Ubicación:** `Shared/DTOs/AtencionMedica/AtencionMedicaCreateDTO.cs` (líneas 14-20) y `BusinessLogic/Services/AtencionMedicaService.cs` (líneas 168-169 y 383-386).
- **Evidencia:** `cu-04-registrar-atencion-medica.md` (Paso 2 y Flujo alternativo `2a`) establece que `anamnesis` y `examenFisico` son campos obligatorios y su ausencia debe devolver `400 Bad Request`. En el código, ambos eran `string?` anulables y las líneas 168-169 asignaban `"Evaluación general del paciente."` y `"Constantes vitales dentro de parámetros normales."` cuando eran `null`.
- **Consecuencia:** Si el cliente omitía el examen físico o la anamnesis, el sistema falsificaba automáticamente información clínica afirmando que las constantes vitales eran normales en un registro médico que es inmutable (`RN-01`).
- **Prioridad:** Crítica
- **Solución propuesta:** Hacer que `Anamnesis` y `ExamenFisico` sean obligatorios (`[Required]`, `[MaxLength(2000)]`) en `AtencionMedicaCreateDTO` y validar en `AtencionMedicaService.ValidateCreateDto` que no sean nulos ni vacíos (`!string.IsNullOrWhiteSpace`), eliminando los textos ficticios por defecto.
- **Decisión aprobada:** Opción A aprobada: hacer `Anamnesis` y `ExamenFisico` obligatorios en `AtencionMedicaCreateDTO` y `AtencionMedicaService` conforme a `CU-04` (flujo `2a`), eliminando los textos clínicos generados automáticamente y devolviendo `400 Bad Request` si faltan, están vacíos o contienen solo espacios en blanco, sin persistir la atención.
- **Estado:** Completada
- **Solución implementada:**
  1. En `Shared/DTOs/AtencionMedica/AtencionMedicaCreateDTO.cs`, se cambiaron `Anamnesis` y `ExamenFisico` a `string` no anulables inicializados en `string.Empty` con `[Required]` y `[MaxLength(2000)]`.
  2. En `BusinessLogic/Services/AtencionMedicaService.cs`, se eliminaron los textos ficticios (`"Evaluación general del paciente."` y `"Constantes vitales dentro de parámetros normales."`) asignando `dto.Anamnesis.Trim()` y `dto.ExamenFisico.Trim()`, y se actualizó `ValidateCreateDto` para lanzar `ValidationException` (`400 Bad Request`) cuando `string.IsNullOrWhiteSpace(dto.Anamnesis)` o `string.IsNullOrWhiteSpace(dto.ExamenFisico)`.
  3. Se actualizaron los datos de prueba en `tests/BusinessLogic.Tests/AtencionMedicaServiceTests.cs` y `tests/API.Tests/AtencionesMedicasEndpointsTests.cs` y se agregaron pruebas específicas para valores nulos, vacíos y solo espacios en blanco.
- **Pruebas necesarias:** Pruebas unitarias en `AtencionMedicaServiceTests` y de integración en `AtencionesMedicasEndpointsTests` verificando que omitir `Anamnesis` o `ExamenFisico` (o enviarlos vacíos/con espacios) devuelva `400 Bad Request` sin guardar en base de datos, y que se persistan los valores exactos ingresados sin textos inventados.
- **Resultado de las pruebas:** Superadas: 37/37 pruebas unitarias en `AtencionMedicaServiceTests` y 151/151 pruebas de integración y regresión de seguridad (`AtencionesMedicasEndpointsTests`, `HistoriaClinicaEndpointsTests`, `DuenosEndpointsTests`, `AuthEndpointsTests`, `UsuariosEndpointsTests` y `TurnosEndpointsTests`) — **188/188 pruebas exitosas**.

---

#### BUG-04
- **Código:** BUG-04
- **Problema:** `TurnoService.GetAgendaAsync` no valida la existencia del `veterinarioId` filtrado (`CU-05` flujo `3b`) ni que el filtro `estado` sea un estado válido (`CU-05` flujo `1a`).
- **Ubicación:** `BusinessLogic/Services/TurnoService.cs` (método `GetAgendaAsync`) y `API/Controllers/TurnosController.cs` (método `GetAgenda`).
- **Evidencia:** `cu-05-consultar-agenda-de-turnos.md` especifica en el flujo alternativo `1a` que si `estado` no pertenece a `{Pendiente, Confirmado, EnAtencion, Finalizado, Cancelado}` debe devolver `400 Bad Request`, y en el flujo `3b` que si `veterinarioId` no corresponde a un veterinario registrado debe lanzar `VeterinarioNotFoundException` (`404 Not Found`). Actualmente `GetAgendaAsync` no validaba ninguno de los dos y devolvía `200 OK` con lista vacía `[]`.
- **Consecuencia:** Incumplimiento directo de los flujos alternativos `1a` y `3b` del Caso de Uso `CU-05`.
- **Prioridad:** Media
- **Solución propuesta:** En `TurnoService.GetAgendaAsync`: (1) si `!string.IsNullOrWhiteSpace(filtro.Estado)`, validar que sea uno de los 5 estados válidos (lanzando `ValidationException` → `400` si no lo es); (2) si `filtro.VeterinarioId.HasValue`, rechazar `Guid.Empty` con `ValidationException` (`400`) y verificar mediante `_usuarioRepository.GetByIdAsync` que exista, esté activo y tenga rol `Veterinario` (lanzando `VeterinarioNotFoundException` → `404` en caso contrario); (3) rechazar `MascotaId == Guid.Empty` con `ValidationException` (`400`).
- **Decisión aprobada:** Aprobada — Validar en `GetAgendaAsync` los estados permitidos (`Pendiente`, `Confirmado`, `EnAtencion`, `Finalizado`, `Cancelado`, rechazando valores inválidos con `400 Bad Request`), validar que el veterinario filtrado exista, esté activo y tenga rol `Veterinario` (`404 Not Found` en caso contrario), y rechazar `Guid.Empty` con `400 Bad Request` si se proporciona como filtro.
- **Estado:** Completada
- **Solución implementada:**
  1. En `BusinessLogic/Services/TurnoService.cs` (`GetAgendaAsync`), se incorporó el catálogo `EstadosTurnoValidos`, la validación de `filtro.Estado` (`ValidationException`), el rechazo de `Guid.Empty` en `VeterinarioId` y `MascotaId` (`ValidationException`) y la verificación de existencia, estado activo y rol `"Veterinario"` (`VeterinarioNotFoundException`).
  2. En `API/Controllers/TurnosController.cs` (`GetAgenda`), se agregaron los bloques `catch (ValidationException)` (`400 Bad Request`) y `catch (VeterinarioNotFoundException)` (`404 Not Found`).
  3. Se agregaron pruebas unitarias en `tests/BusinessLogic.Tests/TurnoServiceTests.cs` y de integración en `tests/API.Tests/TurnosEndpointsTests.cs`.
- **Pruebas necesarias:** Pruebas unitarias en `TurnoServiceTests` y de integración en `TurnosEndpointsTests` para estados válidos (`200`), `GET /api/turnos?estado=Invalido` (`400 Bad Request`), `GET /api/turnos?veterinarioId={inexistente|noVeterinario}` (`404 Not Found`) y `Guid.Empty` (`400 Bad Request`).
- **Resultado de las pruebas:** Superadas: 230/230 pruebas unitarias en `BusinessLogic.Tests` y 213/213 pruebas de integración en `API.Tests` — **443/443 pruebas exitosas**.

---

#### BUG-05
- **Código:** BUG-05
- **Problema:** `TurnoService.UpdateAsync` permite reprogramar turnos pertenecientes a mascotas inactivas, incumpliendo `RN-07` y `CU-06` (flujo `3e`).
- **Ubicación:** `BusinessLogic/Services/TurnoService.cs` (método `UpdateAsync`) y `API/Controllers/TurnosController.cs` (método `Update`).
- **Evidencia:** `cu-06-gestionar-turnos.md` (flujo alternativo `3e`) establece que tanto en el alta como en la reprogramación de un turno, si la mascota se encuentra inactiva se debe rechazar con `409 Conflict` (`MascotaInactivaException`). `CreateManualAsync` lo verificaba, pero `UpdateAsync` no comprobaba la existencia y estado activo de la mascota asociada.
- **Consecuencia:** Permitía reprogramar y mantener activos turnos para mascotas que ya habían sido dadas de baja lógica.
- **Prioridad:** Media
- **Solución propuesta:** En `TurnoService.UpdateAsync`, verificar que la mascota asociada al turno exista (`MascotaNotFoundException` → `404 Not Found` si no existe) y se encuentre activa (`MascotaInactivaException` → `409 Conflict` si está inactiva), y mapear ambas excepciones en `TurnosController.Update`.
- **Decisión aprobada:** Aprobada — Impedir reprogramar turnos de mascotas inactivas (`409 Conflict` via `MascotaInactivaException`), devolver `404 Not Found` (`MascotaNotFoundException`) si la mascota asociada no existe, y garantizar que el turno permanezca sin modificaciones.
- **Estado:** Completada
- **Solución implementada:**
  1. En `BusinessLogic/Services/TurnoService.cs` (`UpdateAsync`), se agregó la validación de la mascota asociada (`var mascota = turno.Mascota ?? await _mascotaRepository.GetByIdAsync(turno.MascotaId);`), lanzando `MascotaNotFoundException` si es `null` y `MascotaInactivaException` si `!mascota.Activa` antes de aplicar cualquier cambio al turno.
  2. En `API/Controllers/TurnosController.cs` (`Update`), se agregaron los bloques `catch (MascotaNotFoundException)` (`404 Not Found`) y `catch (MascotaInactivaException)` (`409 Conflict`).
  3. Se agregaron pruebas unitarias en `tests/BusinessLogic.Tests/TurnoServiceTests.cs` y de integración en `tests/API.Tests/TurnosEndpointsTests.cs`.
- **Pruebas necesarias:** Pruebas unitarias en `TurnoServiceTests` y de integración en `TurnosEndpointsTests` intentando reprogramar (`PUT /api/turnos/{id}`) un turno cuya mascota fue desactivada (`409 Conflict`) o no existe (`404 Not Found`), verificando que el turno permanezca sin modificaciones.
- **Resultado de las pruebas:** Superadas: 230/230 pruebas unitarias en `BusinessLogic.Tests` y 213/213 pruebas de integración en `API.Tests` — **443/443 pruebas exitosas**.

---

#### BUG-06
- **Código:** BUG-06
- **Problema:** La propiedad `EsEnfermedadCronica` de `AtencionMedicaCreateDTO` es ignorada por `AtencionMedicaService`.
- **Ubicación:** `Shared/DTOs/AtencionMedica/AtencionMedicaCreateDTO.cs` (línea 53) y `BusinessLogic/Services/AtencionMedicaService.cs` (`CreateAsync` y `ValidateCreateDto`, líneas 258-266 y 506-532).
- **Evidencia:** `AtencionMedicaCreateDTO` exponía `public bool EsEnfermedadCronica { get; set; }`, pero `AtencionMedicaService` nunca leía dicha propiedad: si un cliente enviaba `EsEnfermedadCronica = true` sin objeto `SeguimientoCronico`, no validaba error; y si enviaba explícitamente `EsEnfermedadCronica = false` con objeto `SeguimientoCronico`, generaba igualmente los turnos crónicos.
- **Consecuencia:** Comportamiento ambiguo e inconsistente del contrato del DTO al registrar atenciones médicas con seguimiento crónico (`RN-11` / `CU-04` flujo `2d`).
- **Prioridad:** Media
- **Solución propuesta:** Cambiar `EsEnfermedadCronica` a `bool?` en `AtencionMedicaCreateDTO` y validar en `AtencionMedicaService.ValidateCreateDto` que `dto.EsEnfermedadCronica == true && dto.SeguimientoCronico == null` o `dto.EsEnfermedadCronica == false && dto.SeguimientoCronico != null` lancen `ValidationException` (`400 Bad Request`), ignorando espacios extremos en `Frecuencia` y `JornadaPreferida`.
- **Decisión aprobada:** Cambiar `EsEnfermedadCronica` de `bool` a `bool?` en `AtencionMedicaCreateDTO`; rechazar con `ValidationException` (`400 Bad Request`) cuando `EsEnfermedadCronica == true && SeguimientoCronico == null` o cuando `EsEnfermedadCronica == false && SeguimientoCronico != null`; mantener la compatibilidad con solicitudes donde `EsEnfermedadCronica` es `null` o `true` con `SeguimientoCronico` válido, o `false`/`null` sin seguimiento; validar `Frecuencia` y `JornadaPreferida` ignorando espacios extremos; y rechazar solicitudes inválidas antes de persistir cualquier dato.
- **Estado:** Completada
- **Solución implementada:**
  1. En `Shared/DTOs/AtencionMedica/AtencionMedicaCreateDTO.cs`, se cambió `EsEnfermedadCronica` a `bool?`.
  2. En `BusinessLogic/Services/AtencionMedicaService.cs` (`ValidateCreateDto` y `CreateAsync`), se validó que `dto.EsEnfermedadCronica == true && dto.SeguimientoCronico == null` y `dto.EsEnfermedadCronica == false && dto.SeguimientoCronico != null` lancen `ValidationException` (`400 Bad Request`) antes de guardar datos, y se aplicó `.Trim()` sobre `Frecuencia` y `JornadaPreferida` tanto al validar como al invocar el generador de turnos crónicos.
  3. Se agregaron pruebas unitarias en `tests/BusinessLogic.Tests/AtencionMedicaServiceTests.cs` y de integración en `tests/API.Tests/AtencionesMedicasEndpointsTests.cs`.
- **Pruebas necesarias:** Pruebas unitarias en `AtencionMedicaServiceTests` y de integración en `AtencionesMedicasEndpointsTests` verificando que `EsEnfermedadCronica = true` sin `SeguimientoCronico` y `EsEnfermedadCronica = false` con `SeguimientoCronico` arrojen `ValidationException` (`400 Bad Request`) sin persistir datos, y que los casos válidos (`true` o `null` con `SeguimientoCronico`) funcionen normalmente.
- **Resultado de las pruebas:** Superadas — `539/539` pruebas aprobadas en la suite completa (`314` unitarias en `BusinessLogic.Tests` + `225` de integración en `API.Tests`).

---

#### DOC-01
- **Código:** DOC-01
- **Problema:** `MascotaUpdateDTO` y `MascotaService.UpdateAsync` no permiten modificar `Especie` ni `FechaNacimiento`, a diferencia de lo especificado en `CU-07`.
- **Ubicación:** `Shared/DTOs/Mascota/MascotaUpdateDTO.cs` (líneas 5-24), `BusinessLogic/Services/MascotaService.cs` (método `UpdateAsync`, líneas 91-109) y `Documentos/Casos_de_uso/Casos_de_uso_tp/cu-07-gestionar-mascotas.md` (Paso 6, líneas 73-79).
- **Evidencia:** `CU-07` establece que la modificación de una mascota permite actualizar *"nombre, especie, raza, sexo, edad/fecha de nacimiento, peso, observaciones"*, pero `MascotaUpdateDTO` solo incluye `Nombre`, `Raza`, `Sexo`, `Peso` y `Observaciones`.
- **Consecuencia:** Si al registrar una mascota se cometió un error en la fecha de nacimiento o en la especie, no es posible corregirlo mediante `PUT /api/mascotas/{id}`.
- **Prioridad:** Media
- **Solución propuesta:** Incorporar `Especie` y `FechaNacimiento` en `MascotaUpdateDTO` y `MascotaService.UpdateAsync` aplicando las mismas validaciones de dominio que en `CreateAsync`.
- **Decisión aprobada:** Agregar `Especie` y `FechaNacimiento` como campos obligatorios en `MascotaUpdateDTO` y permitir su actualización en `MascotaService.UpdateAsync`, aplicando las mismas validaciones del alta (`Especie` obligatoria con máximo 50 caracteres y `FechaNacimiento` obligatoria, no futura ni superior a 30 años de antigüedad según `VAL-01`), conservando todas las validaciones existentes (incluido `VAL-02`) y sin modificar `DuenoId`, `Activa` ni el historial clínico.
- **Estado:** Completada
- **Solución implementada:** Se agregaron las propiedades `Especie` (`[Required]`, `[MaxLength(50)]`) y `FechaNacimiento` (`[Required]`) en `MascotaUpdateDTO`. En `MascotaService.UpdateAsync` se incorporó la asignación de `Especie` (normalizada con `.Trim()`) y `FechaNacimiento`, y en `ValidateUpdateDto` se aplicaron las mismas reglas de validación del alta (`Especie` obligatoria y hasta 50 caracteres; `FechaNacimiento != default`, no futura y con antigüedad máxima de 30 años), manteniendo intactos `DuenoId`, `Activa` y `AtencionesMedicas`. Además, se actualizaron los payloads `PUT` existentes en las pruebas y en la colección Bruno (`8. Actualizar Mascota Exitosa (200 OK).bru`).
- **Pruebas necesarias:** Pruebas unitarias en `MascotaServiceTests` y de integración en `MascotasEndpointsTests` verificando la actualización de `Especie` y `FechaNacimiento` en `PUT /api/mascotas/{id}` y el rechazo sin modificación de datos ante valores inválidos.
- **Resultado de las pruebas:** Superadas — `448/448` pruebas aprobadas en la suite completa (`234` unitarias en `BusinessLogic.Tests` + `214` de integración en `API.Tests`), incluyendo las nuevas pruebas unitarias y de integración para `DOC-01`.

---

#### DOC-02
- **Código:** DOC-02
- **Problema:** Discrepancia entre `CU-01` (flujo A8.7) y `AuthService.CambiarPasswordTemporalAsync` respecto a la emisión inmediata del token JWT tras cambiar la contraseña temporal.
- **Ubicación:** `Documentos/Casos_de_uso/Casos_de_uso_tp/cu-01-iniciar-sesion.md` (líneas 74-83) y `BusinessLogic/Services/AuthService.cs` (líneas 189-198).
- **Evidencia:** El paso A8.7 de `CU-01` indica que luego de cambiar la contraseña temporal *"El usuario puede ahora iniciar sesión con su nueva contraseña definitiva y obtener su token de acceso"*, mientras que `AuthService.CambiarPasswordTemporalAsync` generaba y devolvía un token JWT válido en la misma respuesta `200 OK` (`Mensaje = "Contraseña actualizada exitosamente. Acceso concedido."`).
- **Consecuencia:** Diferencia documental entre el flujo alternativo A8 de `CU-01` y el comportamiento implementado en el endpoint.
- **Prioridad:** Baja
- **Solución propuesta:** Alinear la redacción del paso A8.7 en `cu-01-iniciar-sesion.md` y devolver `Token = null` en `/api/auth/cambiar-password-temporal` para requerir el inicio de sesión posterior mediante `/api/auth/login`.
- **Relación con SEC-03:** En la implementación de `SEC-03` (Opción B aprobada) se modificó `AuthService.CambiarPasswordTemporalAsync` para devolver `Token = null` y se actualizó el flujo A8 de `cu-01-iniciar-sesion.md`.
- **Decisión aprobada:** Aprobada junto con `SEC-03` — Retornar `Token = null` y `RequiereCambioPassword = false` en `/api/auth/cambiar-password-temporal` y reflejar este contrato en el paso A8 de `cu-01-iniciar-sesion.md`.
- **Estado:** Completada
- **Solución implementada:** Se verificó que `AuthService.CambiarPasswordTemporalAsync` devuelve `200 OK` con `Token = null`, `RequiereCambioPassword = false` y `IntentosFallidos = 0`, y que `Documentos/Casos_de_uso/Casos_de_uso_tp/cu-01-iniciar-sesion.md` (pasos A8.5 a A8.7) documenta exactamente dicho contrato y el inicio de sesión posterior mediante `POST /api/auth/login`.
- **Pruebas necesarias:** Verificación de coherencia entre la especificación de `CU-01` y las pruebas de `CambiarPasswordTemporal` en `AuthServiceTests` / `AuthEndpointsTests`.
- **Resultado de las pruebas:** Superadas — Verificado en las 80 pruebas de `SEC-03` y en la suite completa (`539/539`).

---

#### DOC-03
- **Código:** DOC-03
- **Problema:** Discrepancias entre `CU-02` / `CU-08` y el código en el nombre del campo `domicilio` vs `Direccion`, la mención de `email` en el flujo 2a de `CU-02` y la sub-variación de búsqueda `GET /api/duenos?criterio={texto}` en `CU-08`.
- **Ubicación:** `Documentos/Casos_de_uso/Casos_de_uso_tp/cu-02-registrar-dueno.md` (líneas 27, 46 y 87), `Documentos/Casos_de_uso/Casos_de_uso_tp/cu-08-gestionar-duenos.md` (líneas 27, 36, 48 y 72-86), `Shared/DTOs/Dueno/DuenoCreateDTO.cs` y `API/Controllers/DuenosController.cs` (línea 26).
- **Evidencia:** Los documentos `CU-02` y `CU-08` denominan `domicilio` al campo que en C# se llama `Direccion`; el flujo 2a de `CU-02` omite mencionar `email` en la lista de campos obligatorios; y la sub-variación 1 de `CU-08` menciona el parámetro opcional `GET /api/duenos?criterio={texto}`, el cual no está implementado en `DuenosController.GetAll()`.
- **Consecuencia:** Inconsistencia entre la especificación de los casos de uso de dueños y los contratos de la API.
- **Prioridad:** Baja
- **Solución propuesta:** Alinear exclusivamente en la documentación (`cu-02-registrar-dueno.md` y `cu-08-gestionar-duenos.md`) el nombre del campo `direccion`, la obligatoriedad y validaciones de `email`, `dni` y `telefono`, el nombre del DTO `DuenoResponseDTO` y la especificación de la consulta `GET /api/duenos` con la implementación real del backend, sin agregar el filtro opcional `?criterio=` en el código.
- **Decisión aprobada:** Corregir `DOC-03` únicamente en la documentación, alineando los nombres de campos (`direccion`, `email`) y las funcionalidades documentadas (`GET /api/duenos` y `GET /api/duenos/{id}`) con la implementación real sin agregar el filtro opcional de dueños en el código.
- **Estado:** Completada
- **Solución implementada:**
  1. En `Documentos/Casos_de_uso/Casos_de_uso_tp/cu-02-registrar-dueno.md`, se reemplazó `domicilio` por `direccion`, se incluyó `email` en la lista de campos obligatorios del flujo alternativo `2a` y se alineó la nota de validaciones técnicas (`dni` de 7 a 10 dígitos, `telefono` válido, `email` obligatorio con formato válido y longitudes máximas).
  2. En `Documentos/Casos_de_uso/Casos_de_uso_tp/cu-08-gestionar-duenos.md`, se reemplazó `domicilio` por `direccion`, se incluyó `email` en el flujo alternativo `2a`, se corrigió `OwnerResponseDTO` por `DuenoResponseDTO` y se alineó la Sub-variación 1 con la implementación real (`GET /api/duenos` devuelve el listado completo de dueños registrados con `200 OK` y `GET /api/duenos/{id}` consulta el detalle por identificador).
- **Pruebas necesarias:** Revisión documental frente a `DuenoCreateDTO`, `DuenoUpdateDTO`, `DuenoResponseDTO`, `DuenoService` y `DuenosController`.
- **Resultado de las pruebas:** Completada (cierre documental verificado; `539/539` pruebas de la solución en verde).

---

#### DOC-04
- **Código:** DOC-04
- **Problema:** Mención residual al rol `Administrador` en las precondiciones de `CU-05`, nombre de DTO desactualizado (`AppointmentResponseDTO`) y nota desactualizada sobre reportes (`MEJ-02`) en `INFORME-INCONSISTENCIAS-DOCUMENTACION-CASOS-DE-USO.md`.
- **Ubicación:** `Documentos/Casos_de_uso/Casos_de_uso_tp/cu-05-consultar-agenda-de-turnos.md` (líneas 23 y 38), `INFORME-INCONSISTENCIAS-DOCUMENTACION-CASOS-DE-USO.md` (líneas 86-99 y 135) y `Documentos/INFORME-INCONSISTENCIAS-DOCUMENTACION-CASOS-DE-USO.md` (líneas 86-99 y 135).
- **Evidencia:** `CU-05` ya restringía los roles a `Recepcionista` y `Veterinario` tras `SEC-02`, pero en el Paso 6 conservaba el nombre en inglés `IEnumerable<AppointmentResponseDTO>` en lugar de `IEnumerable<TurnoResponseDTO>`. Asimismo, ambos archivos `INFORME-INCONSISTENCIAS-DOCUMENTACION-CASOS-DE-USO.md` indicaban en `MEJ-02` (`☐` pendiente) que `CU-10` y `CU-11` no se implementarían, cuando en realidad ya están implementados y documentados (`CU-10` a `CU-14`).
- **Consecuencia:** Confusión al contrastar la documentación de casos de uso e informes de inconsistencias con los roles, DTOs y módulos reales del sistema.
- **Prioridad:** Baja
- **Solución propuesta:** Verificar que `cu-05-consultar-agenda-de-turnos.md` mantenga únicamente `Recepcionista` y `Veterinario`, actualizar `IEnumerable<AppointmentResponseDTO>` a `IEnumerable<TurnoResponseDTO>` en el Paso 6 de `CU-05`, y actualizar `MEJ-02` como resuelto (`☑`) en ambos archivos `INFORME-INCONSISTENCIAS-DOCUMENTACION-CASOS-DE-USO.md`.
- **Decisión aprobada:** Aprobada — Alinear `CU-05` y los informes de inconsistencias con los roles (`Recepcionista` y `Veterinario`), DTOs (`TurnoResponseDTO`) y casos de uso (`CU-01` a `CU-14`) actualmente implementados.
- **Estado:** Completada
- **Solución implementada:**
  1. En `Documentos/Casos_de_uso/Casos_de_uso_tp/cu-05-consultar-agenda-de-turnos.md`, se verificó que los únicos roles autorizados en actores y precondiciones sean `Recepcionista` y `Veterinario`, y se actualizó el Paso 6 a `IEnumerable<TurnoResponseDTO>`.
  2. En `INFORME-INCONSISTENCIAS-DOCUMENTACION-CASOS-DE-USO.md` y `Documentos/INFORME-INCONSISTENCIAS-DOCUMENTACION-CASOS-DE-USO.md`, se actualizó la sección `MEJ-02` y el checklist de verificación (`☑`) reflejando que `CU-10` y `CU-11` se encuentran implementados en el backend (`UsuariosController`, `ReportesController`) y documentados en la colección oficial (`CU-01` a `CU-14`).
- **Pruebas necesarias:** Revisión documental y verificación del test existente de `403 Forbidden` para `Administrador` en `TurnosEndpointsTests`.
- **Resultado de las pruebas:** Completada (cierre documental verificado; `539/539` pruebas de la solución en verde).

---

#### DOC-05
- **Código:** DOC-05
- **Problema:** Diferencia de nombre en la propiedad de alerta de desnutrición entre `CU-04` (`mensajeAlerta`) y `AtencionMedicaResponseDTO` (`MensajeAlertaDesnutricion`).
- **Ubicación:** `Documentos/Casos_de_uso/Casos_de_uso_tp/cu-04-registrar-atencion-medica.md` (Paso 5, línea 42) y `Shared/DTOs/AtencionMedica/AtencionMedicaResponseDTO.cs` (línea 20).
- **Evidencia:** `CU-04` refiere al campo de respuesta como `mensajeAlerta`, mientras que el DTO en C# lo expone como `MensajeAlertaDesnutricion` (`mensajeAlertaDesnutricion` en JSON).
- **Consecuencia:** Discrepancia menor de nomenclatura entre el documento de `CU-04` y el DTO implementado.
- **Prioridad:** Baja
- **Solución propuesta:** Actualizar la referencia en `cu-04-registrar-atencion-medica.md` a `mensajeAlertaDesnutricion` para reflejar el nombre descriptivo real del DTO.
- **Decisión aprobada:** Aprobada — Alinear el Paso 5 de `cu-04-registrar-atencion-medica.md` con la propiedad `mensajeAlertaDesnutricion` de `AtencionMedicaResponseDTO`.
- **Estado:** Completada
- **Solución implementada:** Se actualizó el Paso 5 de `Documentos/Casos_de_uso/Casos_de_uso_tp/cu-04-registrar-atencion-medica.md` reemplazando `mensajeAlerta` por `mensajeAlertaDesnutricion` en coincidencia exacta con `AtencionMedicaResponseDTO`.
- **Pruebas necesarias:** Verificación de que las pruebas en `AtencionMedicaServiceTests` y `AtencionesMedicasEndpointsTests` validen `MensajeAlertaDesnutricion`.
- **Resultado de las pruebas:** Completada (cierre documental verificado; `539/539` pruebas de la solución en verde).

---

### 2.6. Calidad de Código, Arquitectura y Pruebas (`SMELL` / `TEST`)

#### SMELL-01
- **Código:** SMELL-01
- **Problema:** Uso de consultas y filtrado LINQ en memoria dentro de los servicios de `BusinessLogic`, infringiendo la convención arquitectónica de `AGENTS.md`.
- **Ubicación:**
  - `BusinessLogic/Services/TurnoService.cs` (líneas 50-55: filtra `MascotaId` en memoria en `GetAgendaAsync`; líneas 238, 278, 367).
  - `BusinessLogic/Services/ReporteService.cs` (líneas 56-192: carga colecciones completas en memoria y aplica `.Where`, `.GroupBy`, `.Count` en el servicio).
  - `BusinessLogic/Services/HistoriaClinicaService.cs` (líneas 40, 55, 58).
- **Evidencia:** `AGENTS.md` (Sección 3.2) establece que toda consulta de filtrado o agregación debe resolverse mediante métodos descriptivos en los repositorios de `DataAccess`. En `TurnoService.GetAgendaAsync`, por ejemplo, se llama a `_turnoRepository.GetAgendaAsync(filtro.Fecha, filtro.VeterinarioId, filtro.Estado)` y luego se aplica `.Where(t => t.MascotaId == filtro.MascotaId.Value)` en memoria.
- **Consecuencia:** Ineficiencia al traer registros innecesarios desde SQLite a memoria y acoplamiento de lógica de consulta en la capa de servicios.
- **Prioridad:** Baja
- **Solución propuesta:** Trasladar el filtro opcional `mascotaId` al método `ITurnoRepository.GetAgendaAsync` en `DataAccess` y delegar los filtrados de datos en métodos descriptivos de los repositorios correspondientes.
- **Decisión aprobada:** Postergar como mejora interna de refactorización para no alterar las firmas de las interfaces `I...Repository` ni los mocks configurados en los tests unitarios antes de la entrega.
- **Estado:** Postergada (Refactor interno sin impacto funcional)
- **Solución implementada:** Se mantiene la implementación actual priorizando la estabilidad de los servicios y de las `539` pruebas automatizadas.
- **Pruebas necesarias:** Ejecución de las pruebas de regresión de `TurnoServiceTests` y `ReporteServiceTests`.
- **Resultado de las pruebas:** Postergada

---

#### SMELL-02
- **Código:** SMELL-02
- **Problema:** Duplicación exacta del algoritmo `CalcularFechasIdeales` y de métodos de mapeo de entidades a DTOs en múltiples servicios.
- **Ubicación:** `BusinessLogic/Services/AtencionMedicaService.cs` (líneas 440-643), `BusinessLogic/Services/TurnoService.cs` (líneas 475-531) y `BusinessLogic/Services/HistoriaClinicaService.cs` (líneas 155-286).
- **Evidencia:** El método privado `CalcularFechasIdeales` (56 líneas) está copiado de forma idéntica en `AtencionMedicaService` y en `TurnoService`. Asimismo, el mapeo de `AtencionMedica` a `AtencionMedicaResponseDTO` está duplicado entre `AtencionMedicaService` e `HistoriaClinicaService`.
- **Consecuencia:** Dificulta el mantenimiento: cualquier cambio en el cálculo de fechas de seguimiento crónico (`RN-11`) o en la estructura de `AtencionMedicaResponseDTO` obliga a editar múltiples servicios con riesgo de divergencia.
- **Prioridad:** Baja
- **Solución propuesta:** Centralizar el cálculo de fechas ideales de seguimiento crónico en `TurnoService` (o en un helper interno de `BusinessLogic`) y reutilizar la lógica de mapeo manual sin duplicar código.
- **Decisión aprobada:** Postergar como mejora interna de mantenibilidad, dado que no tiene impacto funcional externo y los algoritmos se encuentran alineados y cubiertos por pruebas.
- **Estado:** Postergada (Refactor interno de mantenibilidad)
- **Solución implementada:** No se modifica la estructura interna de métodos privados en esta entrega para evitar riesgos de regresión.
- **Pruebas necesarias:** Pruebas existentes de seguimiento crónico en `AtencionMedicaServiceTests` y `TurnoServiceTests`.
- **Resultado de las pruebas:** Postergada

---

#### SMELL-03
- **Código:** SMELL-03
- **Problema:** El enum `RolUsuario` definido en `Shared/Enums/RolUsuario.cs` no se utiliza, empleándose literales de texto (*magic strings*) en todo el proyecto.
- **Ubicación:** `Shared/Enums/RolUsuario.cs`, controladores en `API/Controllers/`, servicios en `BusinessLogic/Services/` y `DataAccess/DbInitializer.cs`.
- **Evidencia:** `RolUsuario.cs` define `Administrador`, `Veterinario` y `Recepcionista`, pero no tiene ninguna referencia en el resto de la solución.
- **Consecuencia:** Código muerto o propenso a errores de tipeo en literales de rol.
- **Prioridad:** Baja
- **Solución propuesta:** Utilizar `nameof(RolUsuario.Administrador)`, `nameof(RolUsuario.Veterinario)` y `nameof(RolUsuario.Recepcionista)` (o constantes basadas en `RolUsuario`) en el proyecto.
- **Decisión aprobada:** Postergar como mejora cosmética de código, ya que todos los atributos `[Authorize(Roles = ...)]` y validaciones de rol se encuentran cubiertos y verificados por las pruebas de integración de seguridad (`SEC-01` a `SEC-06`).
- **Estado:** Postergada (Mejora cosmética sin impacto funcional)
- **Solución implementada:** No se realizan cambios sobre las constantes de rol para la entrega actual.
- **Pruebas necesarias:** Compilación de la solución y ejecución de las pruebas de autorización por roles.
- **Resultado de las pruebas:** Postergada

---

#### TEST-01
- **Código:** TEST-01
- **Problema:** Las pruebas de integración de `DuenosEndpointsTests` y `AtencionesMedicasEndpointsTests` operan sin enviar token JWT en `GET`, `PUT` y `DELETE`, y faltan pruebas automatizadas para las nuevas validaciones y reglas de seguridad.
- **Ubicación:** `tests/API.Tests/DuenosEndpointsTests.cs`, `tests/API.Tests/AtencionesMedicasEndpointsTests.cs` y el resto de archivos en `tests/BusinessLogic.Tests/` y `tests/API.Tests/`.
- **Evidencia:** Originalmente, los métodos de prueba para `GET`, `PUT` y `DELETE` en `DuenosEndpointsTests` y `AtencionesMedicasEndpointsTests` utilizaban el `_client` por defecto sin cabecera `Authorization: Bearer`, y no existían pruebas para las nuevas reglas de seguridad (`SEC-01` a `SEC-06`), validaciones (`VAL-01` a `VAL-09`), manejo de errores (`ERR-01` a `ERR-03`), bugs funcionales (`BUG-02` a `BUG-06`) y `DOC-01`.
- **Consecuencia:** Era imprescindible actualizar los clientes HTTP con tokens JWT válidos según cada rol y ampliar la cobertura unitaria y de integración para verificar todas las correcciones implementadas.
- **Prioridad:** Alta
- **Solución propuesta:** Autenticar los clientes HTTP en `DuenosEndpointsTests`, `AtencionesMedicasEndpointsTests`, `MascotasEndpointsTests`, `HistoriaClinicaEndpointsTests`, `TurnosEndpointsTests`, `UsuariosEndpointsTests`, `AuthEndpointsTests` y `ReportesEndpointsTests`, y agregar las pruebas unitarias y de integración para cada hallazgo implementado (`SEC-01`..`SEC-06`, `VAL-01`..`VAL-09`, `ERR-01`..`ERR-03`, `BUG-02`..`BUG-06` y `DOC-01`).
- **Decisión aprobada:** Aprobada e implementada de forma incremental en cada corrección aprobada, manteniendo los identificadores `Guid` conforme a la postergación de `BD-01`.
- **Estado:** Completada
- **Solución implementada:**
  1. Se autenticaron todos los clientes HTTP de integración en `tests/API.Tests/` (`DuenosEndpointsTests`, `AtencionesMedicasEndpointsTests`, `MascotasEndpointsTests`, `HistoriaClinicaEndpointsTests`, `TurnosEndpointsTests`, `UsuariosEndpointsTests`, `AuthEndpointsTests` y `ReportesEndpointsTests`) y se incorporaron pruebas de autorización (`401 Unauthorized` sin token, `403 Forbidden` para roles no autorizados y `200`/`201`/`204` para roles permitidos).
  2. Se amplió la suite desde las 146 pruebas iniciales hasta **539 pruebas automatizadas** (`314` unitarias en `tests/BusinessLogic.Tests/` y `225` de integración en `tests/API.Tests/`), cubriendo todos los escenarios válidos, inválidos, de normalización y valores límite de las correcciones implementadas, y se actualizaron los requests afectados en `bruno/`.
- **Pruebas necesarias:** Ejecución exitosa del 100% de la suite de pruebas (`dotnet test SistemaVeterinaria.slnx`) incluyendo los nuevos casos válidos, inválidos y valores límite.
- **Resultado de las pruebas:** Superadas — `539/539` pruebas aprobadas en la suite completa (`314` unitarias en `BusinessLogic.Tests` + `225` de integración en `API.Tests`, `0` fallidas, `0` advertencias).

---

## 3. Seguimiento de avances

| Código | Problema | Prioridad | Estado | Pruebas |
|---|---|---|---|---|
| **SEC-01** | Endpoints `GET`, `PUT` y `DELETE` de `DuenosController` sin `[Authorize]` | Crítica | Completada | Superadas (27/27) |
| **SEC-02** | Endpoints `GET`, `PUT` y `DELETE` de `AtencionesMedicasController` sin `[Authorize]` y ajuste de rol en `HistoriaClinicaController` | Crítica | Completada | Superadas (49/49) |
| **SEC-03** | Bypass de bloqueo, falta de validación `RequiereCambioPassword` y enumeración en `/cambiar-password-temporal` | Crítica | Completada | Superadas (80/80) |
| **SEC-04** | Escalación horizontal de privilegios en `PUT` / `POST /api/turnos/disponibilidad/{veterinarioId}` | Alta | Completada | Superadas (44/44) |
| **SEC-05** | Clave JWT en `appsettings.json` y fallbacks discrepantes entre `Program.cs` y `JwtTokenGenerator.cs` | Media | Completada | Superadas (209/209) |
| **SEC-06** | Tokens JWT continúan válidos tras bloqueo, baja o restablecimiento de contraseña del usuario | Media | Completada | Superadas (146/146) |
| **BD-01** | Uso de `Guid` en lugar de identificadores `int` autoincrementales en entidades, FKs, DTOs y endpoints | Crítica *(Mejora)* | Postergada (Riesgo técnico) | Postergada |
| **BD-02** | Falta registro de fecha de creación y usuario creador en `Mascota`, `Dueno`, `Turno` y `Usuario` | Alta | Postergada (Requiere migración SQLite) | Postergada |
| **BD-03** | Falta de transacción explícita entre alta de `AtencionMedica`, `FechaUltimaVisita` y turnos crónicos | Media | Postergada (Estabilidad transaccional) | Postergada |
| **VAL-01** | `Mascota.FechaNacimiento` acepta fechas irreales (`1900-01-01`, `0001-01-01`) | Alta | Completada | Superadas (198/198) |
| **VAL-02** | Pesos poco razonables (`0.01` a `1000 kg`), topes inconsistentes (`1000` vs `500`) y sin control de 2 decimales | Alta | Completada | Superadas (271/271) |
| **VAL-03** | `PorcentajeDisminucionPeso` guarda porcentajes negativos y desborda precisión cuando la mascota sube de peso | Media | Completada | Superadas (275/275) |
| **VAL-04** | `DuenoUpdateDTO` sin DataAnnotations y `DuenoService.ValidateUpdateDto` sin validar formato de `Email` ni `MaxLength` | Alta | Completada | Superadas (230/230) |
| **VAL-05** | Validación insuficiente de `Dueno.Dni` (acepta 1 dígito), `Dueno.Telefono` (acepta letras) y `MaxLength` en alta | Alta | Completada | Superadas (230/230) |
| **VAL-06** | Falta validar que `FechaProximaDosis` de vacunación sea futura y validar `MaxLength` en `AtencionMedicaService` | Media | Completada | Superadas (478/478) |
| **VAL-07** | `Mascota.Sexo` acepta cualquier texto arbitrario sin validar catálogo permitido | Media | Completada | Superadas (499/499) |
| **VAL-08** | `ConfigurarDisponibilidadVeterinarioAsync` no valida rango `[00:00, 24:00]`, intervalos de 30 min, `DiaSemana` ni normaliza `Jornada` | Media | Completada | Superadas (522/522) |
| **VAL-09** | `ReporteService` no valida catálogo de `TiposReporte`, longitud de `Motivo` ni rechaza fechas `0001-01-01` o futuras | Media | Completada | Superadas (535/535) |
| **VAL-10** | Ausencia de control de duplicidad al registrar mascotas con igual `DuenoId`, `Nombre` y `Especie` | Baja | Postergada (Mejora opcional no en RN) | Postergada |
| **ERR-01** | Falta manejador centralizado de excepciones (`IExceptionHandler` / middleware global) en `Program.cs` | Alta | Completada | Superadas (425/425) |
| **ERR-02** | Exposición de IDs internos, códigos `(RN-XX)` y detalles de cuenta en mensajes de `Shared/Exceptions/` | Alta | Completada | Superadas (425/425) |
| **ERR-03** | Captura genérica de `InvalidOperationException` exponiendo mensajes internos en `UsuariosController` | Alta | Completada | Superadas (425/425) |
| **ERR-04** | Inconsistencia en formato de respuestas JSON (`message` vs `mensaje` vs `ValidationProblemDetails`) | Media | Postergada (Preservación de contratos) | Postergada |
| **BUG-01** | `ReporteService` ignora el rango `[FechaDesde, FechaHasta]` al calcular métricas de `Mascotas` (`CU-11`) | Alta | Postergada (Depende de `BD-02`) | Postergada |
| **BUG-02** | `PUT /api/duenos/{id}` no permite actualizar `Dni` ni valida `RN-05` (`DniDuplicadoException`), incumpliendo `CU-08` | Alta | Completada | Superadas (230/230) |
| **BUG-03** | `AtencionMedicaService` inventa textos clínicos cuando faltan `Anamnesis` o `ExamenFisico`, incumpliendo `CU-04` | Crítica | Completada | Superadas (188/188) |
| **BUG-04** | `TurnoService.GetAgendaAsync` no valida existencia de `veterinarioId` (`404`) ni validez de `estado` (`400`) (`CU-05`) | Media | Completada | Superadas (443/443) |
| **BUG-05** | `TurnoService.UpdateAsync` permite reprogramar turnos de mascotas inactivas, incumpliendo `RN-07` y `CU-06` (flujo 3e) | Media | Completada | Superadas (443/443) |
| **BUG-06** | Propiedad `EsEnfermedadCronica` en `AtencionMedicaCreateDTO` es ignorada en `AtencionMedicaService` | Media | Completada | Superadas (539/539) |
| **DOC-01** | `MascotaUpdateDTO` y `MascotaService.UpdateAsync` no permiten editar `Especie` ni `FechaNacimiento` (`CU-07`) | Media | Completada | Superadas (448/448) |
| **DOC-02** | Discrepancia entre `CU-01` (flujo A8.7) y `AuthService.CambiarPasswordTemporalAsync` sobre entrega de token | Baja | Completada | Superadas (539/539) |
| **DOC-03** | Discrepancia en campo `domicilio` vs `direccion`, `email` obligatorio y sub-variación de consulta en `CU-02` / `CU-08` | Baja | Completada | Documental (539/539) |
| **DOC-04** | Alineación de roles, `TurnoResponseDTO` en `CU-05` y estado de `MEJ-02` en informes de inconsistencias | Baja | Completada | Documental (539/539) |
| **DOC-05** | Diferencia de nombre `mensajeAlerta` (`CU-04`) vs `mensajeAlertaDesnutricion` (`AtencionMedicaResponseDTO`) | Baja | Completada | Documental (539/539) |
| **SMELL-01** | Consultas y filtrados LINQ en memoria dentro de servicios en lugar de repositorios (`AGENTS.md`) | Baja | Postergada (Refactor interno) | Postergada |
| **SMELL-02** | Duplicación exacta de `CalcularFechasIdeales` y mapeos de DTOs entre múltiples servicios | Baja | Postergada (Refactor interno) | Postergada |
| **SMELL-03** | Enum `RolUsuario` sin utilizar y uso de literales de texto (*magic strings*) para roles | Baja | Postergada (Mejora cosmética) | Postergada |
| **TEST-01** | Autenticación JWT en tests de integración y cobertura integral de validaciones, errores y seguridad | Alta | Completada | Superadas (539/539) |

### 3.1. Balance final de hallazgos

- **Hallazgos técnicos relevados:** **38**
  - **Completadas:** **29 / 38** (`SEC-01`, `SEC-02`, `SEC-03`, `SEC-04`, `SEC-05`, `SEC-06`, `VAL-01`, `VAL-02`, `VAL-04`, `VAL-05`, `VAL-06`, `VAL-07`, `VAL-08`, `VAL-09`, `ERR-01`, `ERR-02`, `ERR-03`, `BUG-02`, `BUG-03`, `BUG-04`, `BUG-05`, `BUG-06`, `DOC-01`, `DOC-02`, `DOC-03`, `DOC-04`, `DOC-05`, `TEST-01`, `VAL-03`).
  - **Postergadas:** **9 / 38** (`BD-01`, `BD-02`, `BD-03`, `VAL-10`, `ERR-04`, `BUG-01`, `SMELL-01`, `SMELL-02`, `SMELL-03`).
- **Resultado final de la suite de pruebas automatizadas del backend (`dotnet test SistemaVeterinaria.slnx`):** **539 / 539 pruebas aprobadas** (`314` unitarias en `BusinessLogic.Tests` + `225` de integración en `API.Tests`, ejecutadas y verificadas con éxito sin errores ni advertencias).

---

### 3.2. Mejoras identificadas para futuras versiones

A partir de la auditoría técnica integral, se consolidaron 9 observaciones catalogadas como mejoras o refactorizaciones postergadas, cuya postergación fue debidamente justificada para preservar la estabilidad de entrega del sistema:

1. **`BD-01` (Identificadores autoincrementales):** Postergada por riesgo técnico transversal (la arquitectura N-Tier actual y los contratos REST operan de manera homogénea con GUIDs en todas las capas).
2. **`BD-02` (Auditoría de creación):** Postergada por requerir alteración de esquema relacional y migraciones SQLite en entidades principales (`Mascota`, `Dueno`, `Turno`, `Usuario`).
3. **`BD-03` (Atomicidad transaccional explícita):** Postergada por estabilidad del flujo clínico existente, que persiste atenciones e invoca los servicios satélite correspondientes.
4. **`VAL-10` (Control de duplicidad de nombres de mascotas por dueño):** Postergada por ser una restricción no solicitada explícitamente en las reglas de negocio ni en los casos de uso.
5. **`ERR-04` (Unificación de formato de errores JSON):** Postergada para preservar los contratos de integración existentes (`message` / `ValidationProblemDetails`) consumidos por los clientes HTTP y Bruno.
6. **`BUG-01` (Filtro por rango de fechas en reporte de mascotas):** Postergada por dependencia técnica directa con la propiedad de auditoría `FechaCreacion` de `BD-02`.
7. **`SMELL-01` (Consultas y filtrados LINQ en servicios):** Postergada por tratarse de refactorización interna sin impacto funcional visible ni defectos clínicos.
8. **`SMELL-02` (Duplicación de lógica auxiliar entre servicios):** Postergada por refactorización interna cosmética de métodos privados auxiliares (`CalcularFechasIdeales`).
9. **`SMELL-03` (Uso de enum `RolUsuario` vs constantes):** Postergada por tratarse de un ajuste estético que no compromete la seguridad ni las validaciones basadas en roles.

---

### 3.3. Actualización de Bruno y Documentación para la Entrega

Durante la etapa de preparación final para la entrega técnica se realizaron las siguientes adecuaciones sobre la colección de pruebas HTTP en `bruno/` y los documentos del repositorio:

1. **Colección Bruno (`bruno/`):**
   * **Entorno y autenticación:** Se eliminaron tokens JWT fijos y GUIDs hardcodeados en `Local.bru` y en todas las carpetas (`auth/`, `duenos/`, `mascotas/`, `atenciones-medicas/`, `historia-clinica/`, `usuarios/`, `turnos/` y `reportes/`).
   * **Variables dinámicas:** Se incorporaron scripts `post-response` y `pre-request` para extraer tokens y IDs reales sembrados por `DbInitializer`, calcular fechas futuras válidas para turnos (bloques de 30 minutos a futuro dentro de la agenda configurada) y generar datos no colisionables (DNI aleatorio en pre-request de alta de dueños).
   * **Solicitudes preparatorias:** Se agregaron solicitudes dedicadas de preparación (`atenciones-medicas/0` para control de peso y `turnos/0` para disponibilidad horaria semanal).
   * **Alineación de seguridad:** Se corrigieron los métodos y códigos esperados según los permisos reales (e.g. `historia-clinica/3` a `403 Forbidden` para Administrador).
   * **Estado de ejecución de Bruno:** Las solicitudes de Bruno fueron **revisadas y corregidas estructuralmente** para compatibilidad total con los DTOs, rutas y validaciones del backend. A diferencia de la suite `dotnet test` (que cuenta con ejecución automatizada verificada al 100%), la colección de Bruno permanece como artefacto listo para demostración manual e interactiva por el equipo docente o evaluador.

2. **Documentación Principal (`README.md`):**
   * Alineación de puertos reales (`http://localhost:5274` y `https://localhost:7207`).
   * Especificación OpenAPI nativa en `/openapi/v1.json` (removiendo menciones a herramientas no configuradas).
   * Guía completa de usuarios y credenciales de desarrollo.
   * Catálogo de reglas de negocio actualizadas y guía paso a paso para importar y ejecutar Bruno respetando su orden de preparación.

---

## 4. Resolución de Observaciones Docentes (Entrega Final)

### 4.1. Observación Docente: CU-03 / CU-07 — La Historia Clínica no existe como entidad persistida ni como excepción tipada

- **Observación formulada:** La Historia Clínica no existe como entidad persistida en base de datos ni existe la excepción tipada `HistoriaClinicaNotFoundException` mencionada en la documentación de casos de uso.
- **Decisión técnica y funcional adoptada:**
  - Se ratifica la decisión arquitectónica de mantener la **Historia Clínica como una proyección lógica derivada** de la mascota (`Mascota`) y de sus atenciones médicas (`AtencionMedica`), identificable unívocamente a través de la identidad de la propia mascota (`MascotaId`).
  - No se crea una entidad ni tabla física `HistoriaClinica` en base de datos ni migraciones de EF Core.
  - Al registrar una nueva mascota (`CU-07`), esta queda en estado `Activa` y su historia clínica pasa a estar inmediatamente disponible para consulta sin requerir la creación de un registro físico previo.
  - La historia clínica puede consultarse aunque la mascota aún no posea atenciones médicas registradas (`CU-03` Flujo 3b), retornando código HTTP `200 OK` con `UltimaVisitaVisualizada = "Sin visitas registradas"`, colecciones clínicas vacías y mensaje informativo `"Sin atenciones médicas registradas a la fecha."`.
  - Toda nueva atención médica persistida se refleja de manera automática e inmediata en la historia clínica proyectada.
  - Ante una consulta con identificador inexistente, la Capa de Negocio lanza `MascotaNotFoundException` (excepción tipada real del dominio), y el controlador la captura retornando HTTP `404 Not Found` con `{ "message": "No se encontró la historia clínica para la mascota solicitada." }`. Se elimina toda mención a la hipotética `HistoriaClinicaNotFoundException`.
- **Documentos actualizados:**
  1. `Documentos/Casos_de_uso/Casos_de_uso_tp/cu-07-gestionar-mascotas.md`: Se corrigieron el Paso 5 del Flujo Principal, la Postcondición de Alta, la matriz de códigos HTTP (`201 Created`) y la matriz de trazabilidad a tests para documentar que la historia clínica es una proyección lógica derivada y no un registro físico generado al persistir la mascota.
  2. `Documentos/Casos_de_uso/Casos_de_uso_tp/cu-03-consultar-historia-clinica.md`: Se eliminó la precondición de "registro físico previo de historia clínica", se alineó el flujo principal (`GetHistoriaClinicaByMascotaIdAsync`), se corrigió el flujo 3a eliminando la mención a `HistoriaClinicaNotFoundException` e indicando el uso exclusivo de `MascotaNotFoundException`, se clarificó el flujo 3b (mascota sin visitas con `200 OK`), y se actualizaron las matrices de códigos HTTP y de trazabilidad a tests.
  3. `Documentos/Bussines/DOCUMENTACION PROYECTO-SistemaVeterinaria.md`: Se ajustó el requerimiento funcional de registro de mascotas para especificar que la historia clínica es una proyección lógica derivada accesible mediante `MascotaId` y no una entidad generada físicamente.
- **Estado:** Completada (Resolución aprobada por la cátedra mediante actualización documental y alineación formal con el comportamiento real del código).

---

### 4.2. Observación Docente: RN-09 / CU-06 — Ciclo de vida de turnos e inalcanzabilidad de Finalizado

- **Observación formulada:** El estado `Finalizado` de los turnos resultaba inalcanzable en el diseño preliminar y la cátedra propuso incorporar el estado intermedio `EnAtencion`.
- **Decisión técnica y funcional adoptada:**
  - Se mantiene una máquina de estados finita de cuatro (4) estados exclusivos: **`Pendiente`**, **`Confirmado`**, **`Cancelado`** y **`Finalizado`**.
  - **Transiciones formalizadas y permitidas:**
    1. `Pendiente → Confirmado`: Confirmación manual por Recepcionista o Veterinario (`POST /api/turnos/{id}/confirmar`), validando agenda configurada (**RN-10**), no solapamiento (**RN-08**) y paciente activo (**RN-07**).
    2. `Pendiente → Cancelado`: Cancelación directa con motivo obligatorio (`POST /api/turnos/{id}/cancelar`).
    3. `Confirmado → Cancelado`: Cancelación de turnos confirmados con motivo obligatorio, liberando de inmediato el bloque de 30 minutos en la agenda (**RN-08**).
    4. `Confirmado → Finalizado`: Finalización manual explícita (`POST /api/turnos/{id}/finalizar`), autorizada para `Recepcionista` y `Veterinario`.
  - **Inmutabilidad estricta de Finalizado (RN-09):** Una vez que un turno pasa a `Finalizado`, no puede ser cancelado, reprogramado, modificado ni vuelto a transicionar.
  - **Independencia absoluta de procesos:** Los turnos y las atenciones médicas son procesos funcional y técnicamente desacoplados. El registro de una atención médica no finaliza automáticamente un turno ni se alteran turnos por el simple transcurso del tiempo; la finalización es una acción manual autónoma.
  - **Justificación de diseño:** Se resolvió la inalcanzabilidad del estado `Finalizado` mediante esta máquina de estados alternativa de 4 estados con finalización manual explícita, sin implementar `EnAtencion`. Se evitó incorporar un estado intermedio efímero que complejiza la concurrencia y no responde a la operativa real de la clínica.
  - **Verificación de cancelación:** Se comprobó y verificó en código que la transición `Confirmado → Cancelado` está plenamente habilitada en `TurnoService.CancelarAsync`, rechazando únicamente turnos en estado `Finalizado` o ya `Cancelado`.
- **Documentos actualizados:**
  1. `Documentos/Casos_de_uso/Casos_de_uso_tp/cu-06-gestionar-turnos.md`: Se formalizaron los Flujos 3.5 (Confirmar) y 3.6 (Finalizar), los flujos alternativos 3i, 3j, 3k, las sub-variaciones 5 y 6, las postcondiciones, la matriz de transiciones del ciclo de vida y la matriz de trazabilidad a tests. Se removieron menciones obsoletas a `EnAtencion`.
  2. `Documentos/Casos_de_uso/Casos_de_uso_tp/cu-05-consultar-agenda-de-turnos.md`: Se eliminó la mención de `EnAtencion` del catálogo de estados filtrables.
  3. `Documentos/Bussines/DOCUMENTACION PROYECTO-SistemaVeterinaria.md`: Se actualizaron los requerimientos funcionales de turnos y la especificación formal de la regla de negocio `RN-09`.
- **Pruebas asociadas:**
  - Unitarias (`TurnoServiceTests`): `FinalizarAsync_TurnoConfirmado_CambiaEstadoAFinalizadoYRetornaTurnoResponseDTO`, `FinalizarAsync_TurnoNoEstaEnEstadoConfirmado_LanzaTurnoNoConfirmadoException`, `ConfirmarAsync_TurnoExistenteEnEstadoPendiente_ConfirmaTurnoYRetornaTurnoResponseDTO`.
  - Integración HTTP (`TurnosEndpointsTests`): `Finalizar_ConRolRecepcionista_TurnoConfirmado_Retorna200OKYEstadoFinalizado`, `Finalizar_ConRolVeterinario_TurnoConfirmado_Retorna200OKYEstadoFinalizado`, `Finalizar_TurnoEnEstadoPendiente_Retorna409Conflict`, `Confirmar_ConRolRecepcionista_TurnoPendienteValido_Retorna200OKYEstadoConfirmado`.
- **Estado:** Completada.

---

### 4.3. Observación Docente: CU-04 / RN-11 — Atomicidad transaccional de atenciones médicas y turnos de seguimiento

- **Observación formulada:** Riesgo de escrituras parciales o inconsistencias al persistir una atención médica antes de concluir la generación de los turnos de seguimiento crónico; ante fallos en estos últimos, la atención quedaba registrada y un reintento posterior podía provocar duplicaciones.
- **Decisión técnica y funcional adoptada:**
  - **Atomicidad transaccional total:** Se implementó una transacción explícita de Entity Framework Core (`IDbContextTransaction`) sobre el mismo `VeterinariaDbContext` coordinado entre `AtencionMedicaRepository` y `TurnoRepository` (`BeginTransactionAsync`, `CommitTransactionAsync`, `RollbackTransactionAsync`).
  - **Escrituras incluidas en la transacción indivisible:**
    1. Registro de la entidad `AtencionMedica` inmutable (**RN-01**).
    2. Guardado en cascada de sus entidades hijas clínicas: prescripciones (`Prescripcion`), estudios (`EstudioComplementario`) y procedimientos quirúrgicos (`ProcedimientoQuirurgico`) (**RN-02**).
    3. Actualización de la fecha de última visita en `Mascota` respetando la regla estricta de no retroceso (**INF-07**).
    4. Generación y almacenamiento de la serie de turnos de seguimiento crónico (`Turno`) en estado inicial `Pendiente` a lo largo de seis meses (**RN-11**), asociados a la atención mediante `AtencionMedicaId`.
  - **Comportamiento ante fallos (Rollback total):** Si ocurre cualquier excepción, timeout o error en base de datos durante cualquiera de las etapas (incluyendo fallos en la persistencia de turnos), se aborta la transacción y se ejecuta un rollback integral. No se confirman atenciones huérfanas ni escrituras parciales.
  - **Independencia de turnos de consulta:** Las atenciones médicas no requieren turnos previos, no poseen `TurnoId` ni finalizan turnos automáticamente. La propiedad `AtencionMedicaId` en los turnos de seguimiento crónico funciona exclusivamente como referencia de origen.
  - **Limitación técnica documentada:** El sistema no dispone de una clave de idempotencia por solicitud HTTP. En consecuencia, dos peticiones idénticas enviadas y completadas con éxito generarán dos atenciones independientes con sus respectivas series de turnos.
- **Documentos actualizados:**
  1. `Documentos/Casos_de_uso/Casos_de_uso_tp/cu-04-registrar-atencion-medica.md`: Se actualizaron el Flujo Principal (Paso 4), el Flujo Alternativo 4a (rollback integral), la sección de seguimiento crónico en el anexo, la descripción de atomicidad y la matriz de trazabilidad a tests.
  2. `Documentos/Bussines/DOCUMENTACION PROYECTO-SistemaVeterinaria.md`: Se ajustaron los requerimientos funcionales de atenciones médicas y turnos crónicos, y se reformuló formalmente la regla `RN-11`.
- **Pruebas asociadas:**
  - Unitarias (`AtencionMedicaServiceTests`): `CreateAsync_SeguimientoCronico_FallaTecnicaEnPersistenciaDeTurnos_RevierteTransaccionYPropagaExcepcion`, `CreateAsync_SeguimientoCronico_FallaEnTurnos_RevierteTransaccionYNoPersisteParcialmente`, `CreateAsync_SeguimientoCronico_TimeoutException_RevierteTransaccionYPropagaExcepcion`.
  - Integración SQLite (`AtencionesMedicasEndpointsTests`): `Create_FalloEnGeneracionSeguimiento_RevierteTransaccionYNoDejaEscriturasParcialesEnSQLite`, `Create_AtencionSinSeguimientoCronico_PersisteCorrectamenteEnTransaccion`, `Create_ReintentoTrasFalloEnSeguimiento_PermiteRegistroCompletoSinDuplicadosParciales`.
- **Estado:** Completada.





