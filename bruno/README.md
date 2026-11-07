# Colección de Requests Bruno — Sistema Veterinario "Patitas"

Colección oficial de solicitudes HTTP para [Bruno](https://www.usebruno.com/) alineada con la implementación actual del backend (`.NET 10`).

## 1. Configuración inicial y Autenticación (`environments/Local.bru`)

1. Seleccionar el entorno **`Local`** en Bruno (`baseUrl: http://localhost:5274`).
2. Ejecutar las solicitudes de inicio de sesión en `auth/` para poblar automáticamente los tokens JWT por rol en las variables de entorno:
   - **`auth/1. Login Exitoso - Administrador (200 OK)`** (`admin` / `Admin1234!`) $\rightarrow$ guarda `{{tokenAdministrador}}`.
   - **`auth/2. Login Exitoso - Veterinario (200 OK)`** (`vet.carlos` / `Vet1234!`) $\rightarrow$ guarda `{{tokenVeterinario}}` y `{{veterinarioId}}`.
   - **`auth/3. Login Exitoso - Recepcionista (200 OK)`** (`recep.ana` / `Recep1234!`) $\rightarrow$ guarda `{{tokenRecepcionista}}`.
3. Ejecutar el primer listado de cada carpeta (`1. Listar Dueños`, `1. Listar Mascotas`, `1. Listar Atenciones Médicas`, `1. Listar Usuarios`) actualiza automáticamente en el entorno los identificadores (`{{duenoId}}`, `{{mascotaId}}`, `{{mascotaInactivaId}}`, `{{atencionId}}`, `{{usuarioBloqueadoId}}`, etc.) correspondientes a la instancia actual de `veterinaria.db`.

## 2. Estructura de Carpetas

- **`auth/`**: Inicio de sesión por rol y email, cuenta temporal, cambio de contraseña temporal (`CU-01`), bloqueo al 5° intento, cierre de sesión idempotente (`CU-09`) y verificación de sesión.
- **`duenos/`**: Listado, alta (`201`), consulta por ID, actualización con DNI (`BUG-02`), conflicto por DNI duplicado (`RN-05`), restricción de baja con mascotas (`409`) y controles de acceso (`SEC-01`).
- **`mascotas/`**: Listado general y de activas, alta (`VAL-01`, `VAL-02`, `VAL-07`), modificación incluyendo especie y fecha de nacimiento (`DOC-01`), baja lógica (`RN-07` / `MEJ-03`) y reasignación de dueño (`CU-12`).
- **`turnos/`**: Agenda de turnos (`CU-05`), disponibilidad horaria profesional (`RN-10`, `SEC-04`, `VAL-08`), alta manual, reprogramación (`BUG-05`) y cancelación (`CU-06`).
- **`atenciones-medicas/`**: Registro clínico con anamnesis y examen físico obligatorios (`CU-04`, `BUG-03`), vacunación (`RN-03`, `RN-04`, `VAL-06`), alerta de desnutrición (`RN-12`), seguimiento crónico (`RN-11`, `BUG-06`), cirugía/prescripciones/estudios (`CU-13`, `CU-14`), inmutabilidad (`RN-01`) y autorización exclusiva para `Veterinario` (`SEC-02`).
- **`historia-clinica/`**: Consulta integral por mascota, búsqueda por criterio (nombre o DNI de dueño) y control de acceso exclusivo para `Veterinario` (`CU-03`, `SEC-02`).
- **`usuarios/`**: Administración de cuentas, filtros, desbloqueo y restablecimiento de contraseña temporal por el `Administrador` (`CU-10`, `ERR-03`).
- **`reportes/`**: Generación y consulta de reportes analíticos consolidados y modulares para el `Administrador` (`CU-11`, `VAL-09`).

