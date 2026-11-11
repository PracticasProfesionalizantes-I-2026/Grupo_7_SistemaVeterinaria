# Caso de Uso: Consultar Historia Clínica

> Especificación elaborada siguiendo la guía `GUIA-Especificacion-Casos-de-Uso.md` (sección 3).
> Implementación del acceso de solo lectura al expediente médico con regla de inmutabilidad de atenciones médicas (**RN-01**) e integración de registros clínicos (**RN-02**).
> Incorpora la consulta histórica de procedimientos quirúrgicos registrados en las atenciones médicas (INF-06).
> Incorpora la visualización de la fecha de última visita en la información general de la mascota (INF-07).
> La Historia Clínica no constituye una entidad persistida ni tabla física independiente en base de datos, sino una proyección lógica derivada e identificable a través de la propia identidad de la mascota (`MascotaId`), construida dinámicamente a partir de los datos del paciente y sus atenciones médicas. Ante un identificador inexistente, el sistema gestiona el error mediante la excepción tipada `MascotaNotFoundException` (sin requerir una excepción tipada separada de historia clínica).

| Campo | Valor |
| --- | --- |
| **ID del Caso de Uso** | CU-03 |
| **Nombre** | Consultar Historia Clínica |
| **Actor Principal** | Veterinario/a |
| **Actores Secundarios** | Ninguno |
| **Alcance / Nivel** | Sistema; meta de usuario |
| **Stakeholders e intereses** | Veterinario/a → consultar el historial clínico completo, intervenciones quirúrgicas y antecedentes del paciente para formular diagnósticos certeros; Dueño de la Mascota → garantizar que la atención veterinaria considere la evolución, cirugías y tratamientos previos del animal; Clínica Veterinaria → asegurar la integridad, inmutabilidad y disponibilidad del expediente médico |
| **Disparador (Trigger)** | El veterinario selecciona la opción "Historia Clínica" desde el menú principal o busca a un paciente por ID, nombre o datos de su dueño |
| **Prioridad / Frecuencia** | Alta; muy alta frecuencia (en cada consulta médica o procedimiento clínico) |
| **Reglas de negocio relacionadas** | RN-01 (inmutabilidad de las atenciones médicas); RN-02 (integración de registros clínicos a la historia clínica) |

---

### 1. BREVE DESCRIPCIÓN
Permite al veterinario consultar la historia clínica completa de una mascota registrada en el sistema. La historia clínica constituye una proyección lógica derivada e identificable a través de la propia identidad de la mascota (`MascotaId`), integrando en un expediente unificado los datos generales del paciente —incluyendo la fecha de última visita correspondiente a la atención médica más reciente registrada o «Sin visitas registradas» si aún no posee atenciones previas—, resumen clínico, antecedentes, atenciones médicas previas (incluyendo los procedimientos quirúrgicos realizados, si los hubiere), registro de vacunas aplicadas, prescripciones farmacológicas y estudios complementarios asociados. La información se presenta de forma integrada en el expediente del paciente, sin secciones ni tablas quirúrgicas o de historia clínica independientes y con campos estrictamente de solo lectura. Las nuevas atenciones médicas se reflejan de forma automática en la proyección de la historia clínica.

### 2. PRECONDICIONES
- El veterinario debe haber iniciado sesión y poseer un Token JWT válido con rol exclusivo de `Veterinario` (los roles `Recepcionista` y `Administrador` no poseen permisos para consultar historias clínicas ni atenciones médicas).
- La mascota debe encontrarse registrada en el sistema (en estado activa o inactiva, **RN-07**) y asociada a un dueño.
- *Nota arquitectónica:* No se requiere la existencia de un registro físico previo de historia clínica en base de datos; la historia clínica se construye de manera dinámica y derivada a partir de la identidad de la mascota y sus atenciones registradas.

### 3. FLUJO PRINCIPAL (Camino Feliz - HTTP 200)
1. El Actor envía una petición al endpoint `GET /api/mascotas/{mascotaId}/historia-clinica` con el identificador de la mascota como parámetro de ruta (o alternativamente a `GET /api/historias-clinicas/{mascotaId}`).
2. La **Capa de Presentación** (`HistoriaClinicaController.GetHistoriaClinicaByMascotaId`) valida que el parámetro `{mascotaId}` tenga un formato de identificador válido (GUID).
3. La **Capa de Negocio** (`HistoriaClinicaService.GetHistoriaClinicaByMascotaIdAsync`) verifica la existencia de la mascota en el repositorio y recupera sus atenciones médicas ordenadas cronológicamente (**RN-01**, **RN-02**), construyendo la proyección derivada de la historia clínica: datos generales de la mascota —incluyendo el campo de solo lectura con la fecha de última visita calculada (la cual refleja la atención médica más reciente guardada o «Sin visitas registradas» si no registra atenciones previas)—, atenciones médicas ordenadas cronológicamente —incluyendo el detalle de los procedimientos quirúrgicos realizados en cada atención (tipo de procedimiento, fecha de realización, descripción de la intervención, veterinario responsable y observaciones o complicaciones)—, historial de vacunas, prescripciones y estudios, junto con las métricas de resumen clínico.
4. La **Capa de Persistencia** ejecuta consultas optimizadas de solo lectura (`AsNoTracking()`) sobre las tablas de mascotas y atenciones médicas, y el servicio proyecta las entidades a `HistoriaClinicaResponseDTO`.
5. El Sistema devuelve un código **200 OK** con la información detallada de la historia clínica.

### 4. FLUJOS ALTERNATIVOS (Caminos Tristes / Excepciones)

* **1a. Identificador de mascota con formato inválido (HTTP 400 Bad Request):**
  1. Si en el Paso 1 el parámetro `{mascotaId}` no cumple con el formato esperado (ej. valor no numérico o GUID mal formado).
  2. La Capa de Presentación rechaza la petición por error de ruta.
  3. El Sistema devuelve un código **400 Bad Request**. Fin del caso de uso.

* **3a. Mascota no encontrada (HTTP 404 Not Found):**
  1. Si en el Paso 3 el identificador `{mascotaId}` proporcionado no corresponde a ninguna mascota existente en la base de datos.
  2. La Capa de Negocio lanza la excepción tipada `MascotaNotFoundException`.
  3. La Capa de Presentación captura la excepción y devuelve un código **404 Not Found** con el mensaje: `{"message": "No se encontró la historia clínica para la mascota solicitada."}`. Fin del caso de uso.

* **3b. Mascota sin atenciones previas registradas (HTTP 200 OK):**
  1. Si en el Paso 3 la mascota existe en el sistema pero aún no cuenta con atenciones médicas, vacunas, prescripciones ni cirugías cargadas.
  2. La Capa de Negocio construye la proyección derivada con los datos de filiación y datos generales de la mascota, exponiendo el campo `UltimaVisitaVisualizada` con el valor «Sin visitas registradas», colecciones vacías para las atenciones y tratamientos (`TotalAtenciones: 0`, `TotalCirugias: 0`, `TotalVacunasAplicadas: 0`), y el mensaje informativo `"Sin atenciones médicas registradas a la fecha."`.
  3. El Sistema devuelve un código **200 OK** con la estructura básica de la historia clínica proyectada y la indicación de «Sin visitas registradas» en los datos generales.

* **4a. Error de conexión con la base de datos (HTTP 500 Internal Server Error):**
  1. Si en el Paso 4 ocurre un error técnico de persistencia o timeout de la base de datos.
  2. El Sistema intercepta la excepción no controlada en el middleware global.
  3. El Sistema devuelve un código **500 Internal Server Error**. Fin del caso de uso.

### 5. SUB-VARIACIONES (opcional)
1. El veterinario puede buscar la historia clínica a través del endpoint de búsqueda `GET /api/historias-clinicas?criterio={texto}` (por DNI del dueño o nombre de la mascota) antes de solicitar el detalle por ID.
2. La consulta puede ser realizada desde la vista de agenda de turnos haciendo clic directo en el paciente asignado al turno.
3. **Consulta de detalle de atención con procedimiento quirúrgico:** Al seleccionar una atención médica que contiene un procedimiento quirúrgico registrado, el sistema muestra dentro del detalle de esa atención los datos de la intervención (tipo de procedimiento, fecha de realización, descripción detallada, veterinario responsable y observaciones o complicaciones), integrados en la misma vista de la Historia Clínica sin pestañas ni pantallas separadas.

### 6. POSTCONDICIONES
- La historia clínica proyectada y sus secciones vinculadas (incluyendo atenciones y procedimientos quirúrgicos históricos) quedan expuestas para visualización por parte del profesional.
- Las nuevas atenciones médicas guardadas se reflejan de forma automática e inmediata en las consultas subsecuentes de la historia clínica derivada.
- La fecha de última visita visualizada en los datos generales es de solo lectura y corresponde a la atención médica más reciente registrada (o «Sin visitas registradas»), sin posibilidad de edición manual.
- No se altera ningún dato ni estado en el sistema (operación de solo lectura). Los procedimientos quirúrgicos registrados permanecen inmutables (**RN-01**), sin posibilidad de ser modificados ni eliminados desde la consulta.

---

## Anexo: matrices de referencia

### Códigos HTTP usados

| Código HTTP | Nombre Técnico | Contexto de Aplicación en el Caso de Uso |
| --- | --- | --- |
| `200` | OK | Retorno exitoso de los datos completos de la historia clínica proyectada. |
| `400` | Bad Request | Formato de identificador o parámetros de búsqueda inválidos. |
| `401` | Unauthorized | Falta de autenticación o token JWT inválido/expirado. |
| `403` | Forbidden | Acceso denegado: rol insuficiente (`Recepcionista` o `Administrador`; requiere rol `Veterinario`). |
| `404` | Not Found | Mascota inexistente en el sistema (`MascotaNotFoundException`). Al ser la historia clínica una proyección derivada de la mascota, no se encontró expediente para el identificador solicitado. |
| `500` | Internal Server Error | Error no controlado en la consulta a la base de datos. |

### Visualización de la Fecha de Última Visita (INF-07)

- **Ubicación en la vista:** Se exhibe dentro del bloque de información general de la mascota en la Historia Clínica, sin requerir pestañas ni vistas adicionales.
- **Valor visualizado:** Muestra la fecha correspondiente a la atención médica más reciente guardada exitosamente en el sistema. En caso de mascotas que aún no tengan ninguna atención registrada, el campo muestra «Sin visitas registradas».
- **Naturaleza de solo lectura:** La fecha de última visita es un dato puramente informativo derivado del historial clínico; el sistema no permite su modificación manual ni intervención directa por parte de los usuarios.

### Visualización de Procedimientos Quirúrgicos y Alcance Clínico

- **Integración sin módulos independientes:** Los procedimientos quirúrgicos se consultan exclusivamente como un componente de las atenciones médicas que los originaron, dentro del flujo general de la Historia Clínica. No existe una sección ni un módulo independiente de cirugías.
- **Campos expuestos en la consulta:** Tipo de procedimiento, fecha de realización, descripción de la intervención, veterinario responsable y observaciones o complicaciones registradas. El diagnóstico y tratamiento corresponden a los campos generales de la atención médica.
- **Inmutabilidad y permisos:** La consulta es estrictamente de solo lectura y respeta **RN-01** (inmutabilidad de atenciones) y **RN-02** (trazabilidad de registros clínicos). No se permite edición ni eliminación de los antecedentes quirúrgicos consultados.

### Nota: Validación vs. Verificación aplicada

- **Validación (Presentación, → 400):** Comprobación de formato del identificador de ruta y sanitización de parámetros de búsqueda en `HistoriaClinicaController`.
- **Verificación (Negocio, → 404):** Validación de existencia de la mascota en la base de datos (`HistoriaClinicaService` arrojando `MascotaNotFoundException`), ordenamiento cronológico inmutable de atenciones (**RN-01**) e integración de registros clínicos (**RN-02**) para construir la proyección derivada del expediente médico.

### Matriz de trazabilidad CU-03 → Test

| Paso del CU | Excepción / Código | Test unitario (BusinessLogic) | Test integración (HTTP) |
| --- | --- | --- | --- |
| Flujo principal | `200 OK` | `GetHistoriaClinicaByMascotaIdAsync_MascotaValidaConAtenciones_RetornaHistoriaClinicaCompleta` | `GetHistoriaClinica_ConVeterinario_Retorna200OkYHistoriaCompleta` |
| Flujo principal (con Cirugía) | `200 OK` | `GetHistoriaClinicaByMascotaIdAsync_ConProcedimientoQuirurgico_MapeaDetallesQuirurgicos` | `GetHistoriaClinica_ConVeterinario_Retorna200OkYHistoriaCompleta` |
| 1a. ID inválido | `400 Bad Request` | — (validación de routing en ASP.NET Core) | `GetHistoriaClinica_MascotaInexistente_Retorna404NotFound` |
| 3a. Mascota no encontrada | `404 Not Found` | `GetHistoriaClinicaByMascotaIdAsync_MascotaInexistente_LanzaMascotaNotFoundException` | `GetHistoriaClinica_MascotaInexistente_Retorna404NotFound` |
| 3b. Mascota sin atenciones | `200 OK` | `GetHistoriaClinicaByMascotaIdAsync_MascotaSinAtenciones_RetornaSinVisitasRegistradasYMensajeInformativo` | `GetHistoriaClinica_MascotaSinAtenciones_Retorna200OkConSinVisitasRegistradas` |

> Regla de oro: cada flujo del caso de uso debe tener al menos un test. En los flujos resueltos en la Capa de Presentación el test aplicable es el de integración HTTP. Los tests se ejecutan con `dotnet test SistemaVeterinaria.slnx`.

