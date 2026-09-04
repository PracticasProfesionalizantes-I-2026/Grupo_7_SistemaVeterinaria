# Informe de Inconsistencias: Documentación General vs. Casos de Uso

**Proyecto:** Sistema de Gestión Veterinaria ("Patitas")  
**Fecha de Elaboración:** Septiembre 2026  
**Documento General de Referencia:** `Documentos/Bussines/DOCUMENTACION PROYECTO-SistemaVeterinaria.md`  
**Casos de Uso Analizados:** `Documentos/Casos_de_uso/Casos_de_uso_tp/` (`cu-01` a `cu-11`)  
**Estado:** Auditoría de Consistencia Inicial — Pendiente de Revisión  

---

## Resumen Ejecutivo

El presente informe consolida los resultados de la auditoría y análisis comparativo entre la **Documentación General del Proyecto** (`DOCUMENTACION PROYECTO-SistemaVeterinaria.md`) y los **Casos de Uso** actuales (`cu-01` a `cu-11`).

### Métricas de Hallazgos

| Tipo de Hallazgo | Cantidad |
| --- | :---: |
| **Inconsistencias Reales** | 4 |
| **Información Faltante** | 7 |
| **Ambigüedades** | 3 |
| **Posibles Mejoras** | 3 |
| **Total de Observaciones** | **17** |

---

## 1. Inconsistencias Reales

> Contradicciones directas entre lo establecido en la Documentación General y los Casos de Uso.

### INC-01: Desactivación / Baja Lógica vs. Eliminación Física de Mascotas
* **Identificador:** INC-01
* **Documento o Caso de Uso Involucrado:** `DOCUMENTACION PROYECTO-SistemaVeterinaria.md` (RF 5.1 ítem 2) vs. `cu-07-gestionar-mascotas.md` (Flujo principal, Flujo alternativo 3c, Matriz HTTP 204).
* **Qué establece la Documentación General:** El Requerimiento Funcional 5.1 ítem 2 establece: *"El sistema deberá permitir registrar, consultar, modificar y **desactivar** mascotas/pacientes, asociándolas a un dueño previamente registrado."*
* **Qué establece el Caso de Uso:** En CU-07 se define la operación de baja mediante eliminación física (`DELETE /api/mascotas/{id}`) que retorna `204 No Content`. El flujo alternativo 3c bloquea la eliminación con código `409 Conflict` (`MascotaConHistorialException`) si el paciente tiene consultas o turnos previos. La baja lógica se menciona únicamente como una sub-variación secundaria sin especificación de contrato.
* **Por qué existe una inconsistencia:** Hay una contradicción funcional y técnica: el requerimiento exige **desactivar** (baja lógica / inhabilitación) al paciente, lo cual debe ser siempre posible incluso si tiene historial clínico. El caso de uso prioriza la eliminación física (`DELETE`), lo que hace imposible dar de baja a cualquier mascota que haya recibido atención médica.
* **Nivel de Impacto:** **Alto**
* **Recomendación de Corrección:** Redefinir la operación de baja en CU-07 como **baja lógica / desactivación** (ej. `PATCH /api/mascotas/{id}/desactivar` o `PUT` con cambio de estado a inactivo), permitiendo preservar la integridad referencial de la historia clínica sin bloquear la acción administrativa.

---

### INC-02: Validación de "Alta Médica" Previa vs. "Evaluación de Aptitud Clínica In Situ"
* **Identificador:** INC-02
* **Documento o Caso de Uso Involucrado:** `DOCUMENTACION PROYECTO-SistemaVeterinaria.md` (Sección 1.2, Riesgo 8, Sección 4.1, RF 5.1 ítem 12) vs. `cu-04-registrar-atencion-medica.md` (Paso 3, Flujo alternativo 3b).
* **Qué establece la Documentación General:** El RF 5.1 ítem 12 y la Sección 4.1 establecen: *"El sistema deberá validar el estado de ‘Alta Médica’ antes de permitir la aplicación de vacunas"* y *"El sistema bloqueará automáticamente la vacunación cuando el paciente no posea ‘Alta Médica’ vigente."* Esto plantea el "Alta Médica" como un estado clínico preexistente o condición vigente en la ficha del paciente.
* **Qué establece el Caso de Uso:** En CU-04, la aptitud para vacunación se evalúa de manera simultánea durante la misma atención médica mediante un checklist in situ (sin fiebre, buen estado general, autorización veterinaria) que genera un resultado transitorio "APTO" (`RN-04`).
* **Por qué existe una inconsistencia:** La documentación general conceptualiza el "Alta Médica" como un estado o bandera previa del paciente requerida para vacunar, mientras que el caso de uso lo trata como una evaluación dinámica realizada dentro de la misma consulta.
* **Nivel de Impacto:** **Medio**
* **Recomendación de Corrección:** Alinear los conceptos: explicitar en la documentación general y en CU-04 si la evaluación clínica in situ otorga el estado de "Alta Médica" transaccionalmente, o si el paciente debe contar con un alta médica asentada previamente.

---

### INC-03: Colisión en la Identificación y Numeración de Reglas de Negocio (RN)
* **Identificador:** INC-03
* **Documento o Caso de Uso Involucrado:** `DOCUMENTACION PROYECTO-SistemaVeterinaria.md` (Sección Reglas de Negocio RN-01 a RN-04) vs. Todos los Casos de Uso (`cu-01` a `cu-11`).
* **Qué establece la Documentación General:** Define cuatro reglas de negocio globales y transversales al sistema:
  * `RN-01`: Inmutabilidad de atenciones médicas en historia clínica.
  * `RN-02`: Asociación automática de prescripciones, estudios y vacunas a la atención y a la historia clínica.
  * `RN-03`: Evaluación de aptitud para vacunación realizada únicamente durante atención médica.
  * `RN-04`: Registro de vacuna condicionado a paciente clínicamente apto.
* **Qué establece el Caso de Uso:** Cada caso de uso renumera localmente sus propias reglas de negocio comenzando desde `RN-01`, `RN-02`, etc. Por ejemplo, en CU-01 `RN-01` es "usuarios registrados y activos"; en CU-02 `RN-01` es "DNI único por dueño"; en CU-06 `RN-01` es "sin solapamiento de turnos"; en CU-11 `RN-01` es "autorización por rol Administrador".
* **Por qué existe una inconsistencia:** Existe colisión de identificadores. El mismo identificador (`RN-01`, `RN-02`) refiere a conceptos de negocio completamente diferentes según el documento que se consulte, rompiendo la trazabilidad del proyecto.
* **Nivel de Impacto:** **Medio**
* **Recomendación de Corrección:** Establecer una convención unívoca: crear un catálogo maestro global de Reglas de Negocio (`RN-01` a `RN-XX`) para todo el sistema, o prefijar las reglas locales en cada caso de uso según su ID (ej. `RN-CU01-01`, `RN-CU02-01`).

---

### INC-04: Definición de Perfiles y Roles en Requerimiento No Funcional vs. Casos de Uso
* **Identificador:** INC-04
* **Documento o Caso de Uso Involucrado:** `DOCUMENTACION PROYECTO-SistemaVeterinaria.md` (RNF 5.2 ítem 2) vs. `cu-01`, `cu-10`, `cu-11` y matriz general de roles.
* **Qué establece la Documentación General:** En RNF 5.2 ítem 2 se indica: *"El sistema deberá aplicar control de acceso basado en roles, restringiendo funcionalidades según el perfil del usuario (**recepción o veterinario**)."*
* **Qué establece el Caso de Uso:** En CU-01, CU-10 y CU-11 se formaliza y restringe el acceso al rol `Dueño de la Veterinaria (Administrador)` (`DuenoVeterinaria` / `Administrador`), quien posee permisos exclusivos sobre la generación y consulta de reportes gerenciales.
* **Por qué existe una inconsistencia:** El texto del RNF 5.2 omitió al Administrador/Dueño en la enumeración de perfiles, restringiéndolos taxativamente a "recepción o veterinario", a pesar de que la tabla de stakeholders y los casos de uso le otorgan funciones exclusivas.
* **Nivel de Impacto:** **Bajo**
* **Recomendación de Corrección:** Actualizar el RNF 5.2 ítem 2 de la documentación general para indicar explícitamente los tres roles del sistema: `Recepcionista`, `Veterinario` y `Administrador / Dueño de la Veterinaria`.

---

## 2. Información Faltante

> Requerimientos o especificaciones definidos en un documento que no están contemplados en el otro.

### INF-01: Lógica de Bloqueo por 5 Intentos Fallidos de Inicio de Sesión
* **Identificador:** INF-01
* **Documento o Caso de Uso Involucrado:** `DOCUMENTACION PROYECTO-SistemaVeterinaria.md` (RF 5.1 ítem 13) vs. `cu-01-iniciar-sesion.md`.
* **Qué establece la Documentación General:** *"El sistema deberá permitir hasta cinco (5) intentos fallidos de autenticación. Al superar dicho límite, la cuenta del usuario será bloqueada."*
* **Qué establece el Caso de Uso:** CU-01 describe en el flujo alternativo 3a el retorno de código `401 Unauthorized` por credenciales erróneas y en 3b el código `403 Forbidden` si la cuenta ya se encuentra inactiva/bloqueada. Sin embargo, **no especifica** el contador de intentos fallidos, el incremento en cada fallo ni el cambio de estado a "Bloqueado" al alcanzar el quinto intento.
* **Por qué existe una inconsistencia:** El requerimiento de seguridad está explícito en la documentación general pero falta su desarrollo procedural y técnico en el caso de uso.
* **Nivel de Impacto:** **Alto**
* **Recomendación de Corrección:** Agregar un flujo alternativo en CU-01 que gestione el incremento de intentos fallidos y el bloqueo automático de la cuenta al superar el umbral de 5 intentos.

---

### INF-02: Mecanismo de Desbloqueo de Usuarios y Gestión de Contraseñas
* **Identificador:** INF-02
* **Documento o Caso de Uso Involucrado:** `DOCUMENTACION PROYECTO-SistemaVeterinaria.md` (RF 5.1 ítem 13) vs. Especificación de Casos de Uso.
* **Qué establece la Documentación General:** Se define la política de bloqueo de usuarios por intentos fallidos.
* **Qué establece el Caso de Uso:** No existe ningún caso de uso ni flujo documentado para que un Administrador desbloquee una cuenta, ni para que un usuario pueda restablecer o cambiar su contraseña.
* **Por qué existe una inconsistencia:** Se diseñó el mecanismo de bloqueo pero no se previó el proceso funcional para la recuperación del acceso o desbloqueo administrativo.
* **Nivel de Impacto:** **Medio**
* **Recomendación de Corrección:** Diseñar un caso de uso administrativo complementario (o subflujo en gestión de usuarios) que permita al Administrador reactivar usuarios bloqueados y gestionar el reseteo de credenciales.

---

### INF-03: Reasignación de Mascota a otro Dueño (Cambio de Titularidad)
* **Identificador:** INF-03
* **Documento o Caso de Uso Involucrado:** `DOCUMENTACION PROYECTO-SistemaVeterinaria.md` (RF 5.1 ítem 3) vs. `cu-07-gestionar-mascotas.md`.
* **Qué establece la Documentación General:** RF 5.1 ítem 3: *"El sistema deberá permitir reasignar una mascota a otro dueño en casos de cambio de titularidad o actualización de responsabilidad sobre el paciente."*
* **Qué establece el Caso de Uso:** En CU-07, el flujo de modificación (`PUT /api/mascotas/{id}`) solo detalla la actualización de datos biométricos (peso, raza, observaciones). No contiene pasos ni reglas de negocio para la reasignación de titularidad hacia un nuevo `duenoId`.
* **Por qué existe una inconsistencia:** Un requerimiento funcional clave del negocio no fue incorporado en los flujos del caso de uso correspondiente.
* **Nivel de Impacto:** **Alto**
* **Recomendación de Corrección:** Incorporar en CU-07 una sub-variación o endpoint explícito (ej. `PATCH /api/mascotas/{id}/reasignar-dueno`) con la verificación de existencia del nuevo dueño y registro del cambio.

---

### INF-04: Advertencia por Abandono de Atención Médica con Cambios sin Guardar
* **Identificador:** INF-04
* **Documento o Caso de Uso Involucrado:** `DOCUMENTACION PROYECTO-SistemaVeterinaria.md` (RF 5.1 ítem 18) vs. `cu-04-registrar-atencion-medica.md`.
* **Qué establece la Documentación General:** RF 5.1 ítem 18: *"El sistema deberá advertir al usuario cuando intente abandonar una atención médica con información sin guardar, informando que los datos ingresados se perderán si no son registrados."*
* **Qué establece el Caso de Uso:** CU-04 está redactado exclusivamente como un contrato de API transaccional (`POST /api/atenciones`). No contempla interacción de interfaz, cancelación de atención ni advertencia de datos no guardados.
* **Por qué existe una inconsistencia:** Al estructurar el caso de uso con foco exclusivo en el backend, se omitió el requerimiento de control de navegación y confirmación en la interfaz de usuario.
* **Nivel de Impacto:** **Medio**
* **Recomendación de Corrección:** Agregar en CU-04 un flujo alternativo de cancelación/abandono de la consulta con mensaje de confirmación previo a descartar el borrador.

---

### INF-05: Registro Obligatorio de Fecha y Motivo de Cancelación de Turnos
* **Identificador:** INF-05
* **Documento o Caso de Uso Involucrado:** `DOCUMENTACION PROYECTO-SistemaVeterinaria.md` (RF 5.1 ítem 16) vs. `cu-06-gestionar-turnos.md` y `cu-11-generar-reportes.md`.
* **Qué establece la Documentación General:** RF 5.1 ítem 16: *"El sistema deberá registrar los turnos cancelados, indicando la fecha de cancelación, para su posterior consulta en los reportes del sistema."*
* **Qué establece el Caso de Uso:** En CU-06 (sub-variación 3 de cancelación `PATCH /api/turnos/{id}/cancelar`) únicamente se menciona que el turno pasa a estado `"Cancelado"`, sin detallar la captura de la marca temporal (`fechaCancelacion`) ni el motivo.
* **Por qué existe una inconsistencia:** Falta especificar el almacenamiento explícito de los metadatos de cancelación requeridos para los reportes de auditoría.
* **Nivel de Impacto:** **Medio**
* **Recomendación de Corrección:** Especificar en CU-06 que la operación de cancelación registra automáticamente la fecha y hora exacta del evento y un motivo opcional de cancelación.

---

### INF-06: Gestión y Documentación de Procedimientos Programados
* **Identificador:** INF-06
* **Documento o Caso de Uso Involucrado:** `DOCUMENTACION PROYECTO-SistemaVeterinaria.md` (Riesgo 12, Función 8, Sección 4.2, RF 5.1 ítem 14) vs. Especificación de Casos de Uso.
* **Qué establece la Documentación General:** La Sección 4.2 excluye la "validación de firma digital", pero el Riesgo 12, la Función 8 y el RF 14 mantienen la necesidad de mostrar al veterinario su agenda de "procedimientos programados" y almacenar la documentación/consentimiento asociado para asegurar trazabilidad.
* **Qué establece el Caso de Uso:** No existe ningún caso de uso para agendar o registrar intervenciones quirúrgicas/procedimientos, ni para adjuntar consentimientos informados físicos/escaneados.
* **Por qué existe una inconsistencia:** Existe un vacío funcional entre la exclusión de la firma digital y la necesidad operativa de registrar intervenciones complejas y sus respaldos documentales.
* **Nivel de Impacto:** **Medio**
* **Recomendación de Corrección:** Definir si los procedimientos se registran dentro de CU-04 (como atención especial con adjuntos) o si se requiere un caso de uso simplificado para registro de intervenciones y carga de documentos de consentimiento.

---

### INF-07: Actualización de Fecha de Última Visita en la Persistencia de CU-04
* **Identificador:** INF-07
* **Documento o Caso de Uso Involucrado:** `DOCUMENTACION PROYECTO-SistemaVeterinaria.md` (RF 5.1 ítem 6) vs. `cu-04-registrar-atencion-medica.md`.
* **Qué establece la Documentación General:** RF 5.1 ítem 6: *"El sistema deberá actualizar automáticamente la fecha de última visita del paciente al registrar una nueva atención."*
* **Qué establece el Caso de Uso:** CU-04 menciona la actualización en su sección de postcondiciones, pero en el Flujo Principal (Paso 3 y Paso 4 de persistencia) no detalla la actualización del campo `FechaUltimaVisita` en la tabla `Mascotas`.
* **Por qué existe una inconsistencia:** Falta precisión técnica en el paso a paso del flujo principal de la Capa de Negocio y Persistencia.
* **Nivel de Impacto:** **Bajo**
* **Recomendación de Corrección:** Incluir explícitamente en los Pasos 3 y 4 de CU-04 la actualización del campo `FechaUltimaVisita` en la entidad `Mascota` dentro de la misma transacción.

---

## 3. Ambigüedades

> Aspectos donde ambos documentos admiten más de una interpretación o generan dudas de implementación.

### AMB-01: Carga Diferida de Estudios Complementarios vs. Inmutabilidad de la Atención
* **Identificador:** AMB-01
* **Documento o Caso de Uso Involucrado:** `DOCUMENTACION PROYECTO-SistemaVeterinaria.md` (RN-01, RN-02) vs. `cu-03-consultar-historia-clinica.md` y `cu-04-registrar-atencion-medica.md`.
* **Qué establece la Documentación General:** `RN-01` establece que las atenciones médicas son inmutables (no pueden modificarse ni eliminarse). `RN-02` indica que los estudios quedan asociados a la atención y a la historia clínica.
* **Qué establece el Caso de Uso:** En CU-04 los estudios se envían en la colección `estudios[]` al crear la atención médica.
* **Por qué existe una ambigüedad:** Si un estudio complementario (ej. análisis de laboratorio o biopsia) se solicita en una consulta y sus resultados se obtienen días después, no es posible editar la atención médica original debido a la regla de inmutabilidad (`RN-01`). No queda especificado cómo se incorporan los resultados de estudios diferidos: ¿requieren una nueva atención médica o existe una vía de adjunto vinculada a la orden previa?
* **Nivel de Impacto:** **Alto**
* **Recomendación de Corrección:** Especificar formalmente el ciclo de vida de los estudios diagnósticos (Solicitud en Atención → Estado Pendiente → Carga de Informe/Resultados con trazabilidad a la orden original).

---

### AMB-02: Asimetría Estructural y de Permisos en la Gestión de Dueños vs. Mascotas
* **Identificador:** AMB-02
* **Documento o Caso de Uso Involucrado:** `cu-02-registrar-dueno.md`, `cu-08-gestionar-duenos.md` vs. `cu-07-gestionar-mascotas.md`.
* **Qué establece la Documentación General:** RF 1 y RF 2 presentan el ciclo de administración de Dueños y Mascotas con simetría funcional (registro, consulta, modificación).
* **Qué establece el Caso de Uso:** Para Dueños se crearon dos casos de uso separados: CU-02 (Alta exclusiva por Recepcionista) y CU-08 (Gestión/Consulta/Update/Delete por Recepcionista y Veterinario). Para Mascotas, todas las operaciones (Alta, Consulta, Modificación y Baja) se unificaron en CU-07.
* **Por qué existe una ambigüedad:** Se genera confusión sobre los permisos reales: ¿puede un veterinario dar de alta un dueño nuevo durante una urgencia (como sugiere CU-08 en sus actores) o le está impedido (como indica CU-02)?
* **Nivel de Impacto:** **Medio**
* **Recomendación de Corrección:** Unificar la política de permisos: permitir que el Veterinario también figure como actor de alta en CU-02, o fusionar el alta dentro de CU-08 para mantener simetría con CU-07.

---

### AMB-03: Modelado Monolítico de API vs. Relaciones UML `<<include>>` y `<<extend>>`
* **Identificador:** AMB-03
* **Documento o Caso de Uso Involucrado:** `CasosDeUso_SisVet.docx` vs. Archivos `.md` de Casos de Uso (`cu-03`, `cu-04`).
* **Qué establece la Documentación General / Antecedente:** En la matriz preliminar de casos de uso se definían relaciones formales UML:
  * `Registrar atención médica <<include>> Registrar diagnóstico`
  * `Registrar atención médica <<include>> Registrar tratamiento`
  * `Registrar atención médica <<extend>> Registrar vacunación`
  * `Registrar atención médica <<extend>> Registrar prescripción`
  * `Consultar historia clínica <<include>> Consultar historial de vacunas / atenciones / prescripciones / estudios`
* **Qué establece el Caso de Uso:** En los archivos `.md` actuales no se documentan estas relaciones formalmente, habiéndose consolidado todo en endpoints únicos (`POST /api/atenciones` y `GET /api/mascotas/{id}/historia-clinica`) con sub-variaciones opcionales.
* **Por qué existe una ambigüedad:** Existe discrepancia entre la representación formal de casos de uso UML y la especificación de endpoints RESTful.
* **Nivel de Impacto:** **Medio**
* **Recomendación de Corrección:** Incorporar en la sección de relaciones de CU-03 y CU-04 la aclaración de cómo se materializan las extensiones e inclusiones funcionales en la arquitectura del sistema.

---

## 4. Posibles Mejoras

> Oportunidades para incrementar la precisión, calidad y coherencia documental.

### MEJ-01: Incorporación de la Perspectiva del Usuario Frente a la Pantalla
* **Identificador:** MEJ-01
* **Documento o Caso de Uso Involucrado:** Casos de uso `cu-01` a `cu-11`.
* **Descripción:** Los casos de uso actuales poseen una excelente definición de arquitectura y backend (endpoints, DTOs, ASP.NET Core, EF Core, códigos HTTP), pero presentan escasa descripción de las acciones físicas del usuario en la interfaz gráfica (pantallas, botones, formularios).
* **Nivel de Impacto:** **Bajo**
* **Recomendación de Corrección:** Agregar en cada paso del flujo principal la acción del actor en la interfaz antes de la emisión del mensaje HTTP.

---

### MEJ-02: Especificación de Indicadores y Métricas en Reportes
* **Identificador:** MEJ-02
* **Documento o Caso de Uso Involucrado:** `cu-10-consultar-reportes.md` y `cu-11-generar-reportes.md`.
* **Descripción:** CU-11 menciona los tipos de reportes (`Atenciones`, `Vacunaciones`, `Mascotas`, `Duenos`, `Turnos`), pero no define qué indicadores concretos se consolidan en cada uno (ej. totales acumulados, promedios, distribución por especie, porcentaje de turnos cancelados).
* **Nivel de Impacto:** **Bajo**
* **Recomendación de Corrección:** Agregar una tabla anexa en CU-11 que describa los campos y métricas calculadas que conforma cada tipo de reporte.

---

### MEJ-03: Estandarización de Códigos HTTP en Operaciones de Baja Lógica
* **Identificador:** MEJ-03
* **Documento o Caso de Uso Involucrado:** `cu-07-gestionar-mascotas.md` y `cu-08-gestionar-duenos.md`.
* **Descripción:** Los casos de uso utilizan `204 No Content` propio de `DELETE` físico. Si la operación se estandariza como baja lógica (actualización de estado), la buena práctica REST recomienda responder `200 OK` con la entidad actualizada.
* **Nivel de Impacto:** **Bajo**
* **Recomendación de Corrección:** Ajustar los códigos de retorno en las matrices HTTP para reflejar `200 OK` en bajas lógicas.

---

## Matriz Resumen de Inconsistencias y Hallazgos

| ID | Tipo | Documentos Afectados | Título / Proceso | Nivel de Impacto |
| :---: | :---: | :--- | :--- | :---: |
| **INC-01** | Inconsistencia Real | Doc General / CU-07 | Desactivación vs. Eliminación física de Mascotas | **Alto** |
| **INC-02** | Inconsistencia Real | Doc General / CU-04 | Validación de Alta Médica vs. Evaluación in situ | **Medio** |
| **INC-03** | Inconsistencia Real | Doc General / CU-01..11 | Colisión en identificación de Reglas de Negocio (RN) | **Medio** |
| **INC-04** | Inconsistencia Real | Doc General / CU-01,10,11 | Roles definidos en RNF vs. Casos de Uso | **Bajo** |
| **INF-01** | Información Faltante | Doc General / CU-01 | Bloqueo por 5 intentos fallidos en login | **Alto** |
| **INF-02** | Información Faltante | Doc General / General | Desbloqueo de cuentas y gestión de contraseñas | **Medio** |
| **INF-03** | Información Faltante | Doc General / CU-07 | Reasignación de mascota a otro dueño | **Alto** |
| **INF-04** | Información Faltante | Doc General / CU-04 | Advertencia por cambios sin guardar en atención | **Medio** |
| **INF-05** | Información Faltante | Doc General / CU-06, CU-11 | Fecha y motivo de cancelación de turnos | **Medio** |
| **INF-06** | Información Faltante | Doc General / General | Procedimientos programados y consentimientos | **Medio** |
| **INF-07** | Información Faltante | Doc General / CU-04 | Actualización explícita de fecha de última visita | **Bajo** |
| **AMB-01** | Ambigüedad | Doc General / CU-03, CU-04 | Carga diferida de estudios vs. Inmutabilidad de atención | **Alto** |
| **AMB-02** | Ambigüedad | CU-02, CU-08 vs. CU-07 | Asimetría de casos de uso y permisos en Dueños vs Mascotas | **Medio** |
| **AMB-03** | Ambigüedad | Docx previo / CU-03, CU-04 | Relaciones `<<include>>` / `<<extend>>` vs modelo API | **Medio** |
| **MEJ-01** | Posible Mejora | CU-01 a CU-11 | Enfoque de interacción de usuario en UI | **Bajo** |
| **MEJ-02** | Posible Mejora | CU-10, CU-11 | Detalle de métricas en reportes | **Bajo** |
| **MEJ-03** | Posible Mejora | CU-07, CU-08 | Estandarización HTTP en bajas lógicas | **Bajo** |

---
*Fin del informe. Esperando revisión del equipo para proceder con las modificaciones acordadas.*
