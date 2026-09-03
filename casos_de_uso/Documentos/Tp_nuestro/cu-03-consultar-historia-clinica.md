# Caso de Uso: Consultar Historia Clínica

> Especificación elaborada siguiendo la guía `GUIA-Especificacion-Casos-de-Uso.md` (sección 3).
> Implementación del acceso de solo lectura al expediente médico con regla de inmutabilidad histórica (**RN-01**) y consolidación modular de atenciones, vacunaciones, prescripciones y estudios (**RN-02**).

| Campo | Valor |
| --- | --- |
| **ID del Caso de Uso** | CU-03 |
| **Nombre** | Consultar Historia Clínica |
| **Actor Principal** | Veterinario/a |
| **Alcance / Nivel** | Sistema; meta de usuario |
| **Stakeholders e intereses** | Veterinario/a → consultar el historial clínico completo y antecedentes del paciente para formular diagnósticos certeros; Dueño de la Mascota → garantizar que la atención veterinaria considere la evolución y tratamientos previos del animal; Clínica Veterinaria → asegurar la integridad, inmutabilidad y disponibilidad del expediente médico |
| **Disparador (Trigger)** | El veterinario selecciona la opción "Historia Clínica" desde el menú principal o busca a un paciente por ID, nombre o datos de su dueño |
| **Prioridad / Frecuencia** | Alta; muy alta frecuencia (en cada consulta médica o procedimiento clínico) |
| **Reglas de negocio relacionadas** | RN-01 (inmutabilidad histórica de atenciones y registros clínicos); RN-02 (consolidación integral de atenciones, vacunas, prescripciones y estudios) |

---

### 1. BREVE DESCRIPCIÓN
Permite al veterinario consultar la historia clínica completa de una mascota registrada en el sistema, visualizando sus datos generales, resumen clínico, antecedentes, atenciones médicas previas, registro de vacunas aplicadas, prescripciones farmacológicas y estudios complementarios asociados.

### 2. PRECONDICIONES
- El veterinario debe haber iniciado sesión y poseer un Token JWT válido con rol de `Veterinario` o `Administrador`.
- La mascota debe estar registrada en el sistema y asociada a un dueño.
- Debe existir el registro de historia clínica vinculado a la mascota.

### 3. FLUJO PRINCIPAL (Camino Feliz - HTTP 200)
1. El Actor envía una petición al endpoint `GET /api/mascotas/{mascotaId}/historia-clinica` con el identificador de la mascota como parámetro de ruta.
2. La **Capa de Presentación** (`HistoriaClinicaController.GetHistoriaClinicaByMascotaId`) valida que el parámetro `{mascotaId}` tenga un formato de identificador válido.
3. La **Capa de Negocio** (`HistoriaClinicaService.GetHistoriaClinicaAsync`) verifica la existencia de la mascota y recupera su historia clínica junto con las secciones correspondientes (**RN-02**): datos generales, atenciones médicas ordenadas cronológicamente (**RN-01**), historial de vacunas, prescripciones y estudios.
4. La **Capa de Persistencia** ejecuta una consulta optimizada de solo lectura (`AsNoTracking()`) proyectando las entidades a `HistoriaClinicaResponseDTO`.
5. El Sistema devuelve un código **200 OK** con la información detallada de la historia clínica.

### 4. FLUJOS ALTERNATIVOS (Caminos Tristes / Excepciones)

* **1a. Identificador de mascota con formato inválido (HTTP 400 Bad Request):**
  1. Si en el Paso 1 el parámetro `{mascotaId}` no cumple con el formato esperado (ej. valor no numérico o GUID mal formado).
  2. La Capa de Presentación rechaza la petición por error de ruta.
  3. El Sistema devuelve un código **400 Bad Request**. Fin del caso de uso.

* **3a. Mascota o Historia Clínica no encontrada (HTTP 404 Not Found):**
  1. Si en el Paso 3 el identificador proporcionado no corresponde a ninguna mascota o historia clínica existente en la base de datos.
  2. La Capa de Negocio lanza la excepción `MascotaNotFoundException` o `HistoriaClinicaNotFoundException`.
  3. El Sistema devuelve un código **404 Not Found** con el mensaje: `"No se encontró la historia clínica para la mascota solicitada."`. Fin del caso de uso.

* **3b. Mascota sin atenciones previas registradas (HTTP 200 OK):**
  1. Si en el Paso 3 la mascota existe y posee historia clínica pero aún no cuenta con atenciones médicas, vacunas ni prescripciones cargadas.
  2. La Capa de Negocio construye el DTO con los datos de filiación de la mascota y listas vacías para las atenciones y tratamientos.
  3. El Sistema devuelve un código **200 OK** con la estructura básica y el mensaje informativo `"Sin atenciones médicas registradas a la fecha."`.

* **4a. Error de conexión con la base de datos (HTTP 500 Internal Server Error):**
  1. Si en el Paso 4 ocurre un error técnico de persistencia o timeout de la base de datos.
  2. El Sistema intercepta la excepción no controlada en el middleware global.
  3. El Sistema devuelve un código **500 Internal Server Error**. Fin del caso de uso.

### 5. SUB-VARIACIONES (opcional)
1. El veterinario puede buscar la historia clínica a través del endpoint de búsqueda `GET /api/historias-clinicas?criterio={texto}` (por DNI del dueño o nombre de la mascota) antes de solicitar el detalle por ID.
2. La consulta puede ser realizada desde la vista de agenda de turnos haciendo clic directo en el paciente asignado al turno.

### 6. POSTCONDICIONES
- La historia clínica y sus módulos vinculados quedan expuestos para visualización por parte del profesional.
- No se altera ningún dato ni estado en el sistema (operación segura y de solo lectura).

---

## Anexo: matrices de referencia

### Códigos HTTP usados

| Código HTTP | Nombre Técnico | Contexto de Aplicación en el Caso de Uso |
| --- | --- | --- |
| `200` | OK | Retorno exitoso de los datos completos de la historia clínica. |
| `400` | Bad Request | Formato de identificador o parámetros de búsqueda inválidos. |
| `404` | Not Found | Mascota o historia clínica inexistente en el sistema. |
| `500` | Internal Server Error | Error no controlado en la consulta a la base de datos. |

### Nota: Validación vs. Verificación aplicada

- **Validación (Presentación, → 400):** Comprobación de formato del identificador de ruta y sanitización de parámetros de búsqueda en `HistoriaClinicaController`.
- **Verificación (Negocio, → 404):** Validación de existencia de la entidad en la base de datos (`HistoriaClinicaService`), ordenamiento cronológico inmutable de atenciones (**RN-01**) y agregación de historial médico (**RN-02**).

### Matriz de trazabilidad CU-03 → Test

| Paso del CU | Excepción / Código | Test unitario (BusinessLogic) | Test integración (HTTP) |
| --- | --- | --- | --- |
| Flujo principal | `200 OK` | `GetHistoriaClinicaAsync_WithValidMascotaId_ReturnsHistoriaClinicaDTO` | `GetHistoriaClinica_WithValidMascotaId_Returns200OK` |
| 1a. ID inválido | `400 Bad Request` | — (validación de routing en ASP.NET Core) | `GetHistoriaClinica_WithInvalidIdFormat_Returns400BadRequest` |
| 3a. Mascota no encontrada | `404 Not Found` | `GetHistoriaClinicaAsync_WhenMascotaNotExists_ThrowsMascotaNotFoundException` | `GetHistoriaClinica_WhenMascotaNotExists_Returns404NotFound` |
| 3b. Mascota sin atenciones | `200 OK` | `GetHistoriaClinicaAsync_WhenNoAtenciones_ReturnsDTOWithEmptyCollections` | `GetHistoriaClinica_WhenNoAtenciones_Returns200OKWithEmptyList` |

> Regla de oro: cada flujo del caso de uso debe tener al menos un test. En los flujos resueltos en la Capa de Presentación el test aplicable es el de integración HTTP. Los tests se ejecutan con `dotnet test SistemaVeterinaria.slnx`.
