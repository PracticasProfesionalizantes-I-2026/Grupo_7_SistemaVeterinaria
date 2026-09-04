# 1. Requerimientos del Negocio

## 1.1 Situación actual o Propósito

La clínica veterinaria “Patitas” es un centro de atención de salud animal que, debido a su crecimiento en cantidad de pacientes y servicios, incrementó la complejidad de su gestión clínica y administrativa.

Actualmente, la organización trabaja con una combinación de registros manuales y herramientas digitales no integradas entre sí, lo que genera fragmentación de la información y dificultades en su actualización y seguimiento. Esta situación impacta directamente en la calidad del servicio, especialmente en el control de historias clínicas y planes de vacunación.

Entre los principales problemas se identifican la falta de validaciones automáticas en procesos críticos, la dificultad para detectar alertas de salud a partir de datos históricos, y la ausencia de controles efectivos sobre roles y permisos del personal. Asimismo, se observan limitaciones en el seguimiento de tratamientos crónicos, en el control de vacunación y en el registro adecuado de consentimientos en procedimientos de mayor complejidad.

Estos inconvenientes evidencian la necesidad de contar con un sistema informático que permita centralizar la información, automatizar procesos clave y garantizar el cumplimiento de las reglas de negocio, mejorando la eficiencia operativa y la calidad de atención.

## 1.2 Oportunidad del negocio

La implementación de un sistema informático en la clínica veterinaria Patitas permitirá superar las limitaciones actuales mediante la centralización de la información en una única plataforma, asegurando consistencia y actualización automática de los datos clínicos.

El sistema incorporará reglas de negocio automatizadas, garantizando el cumplimiento de condiciones clave, como la validación de “Alta Médica” para vacunación y la actualización de la última visita. Además, permitirá el monitoreo y detección temprana de alertas de salud, mejorando el seguimiento de los pacientes.

Se optimizará la gestión mediante la automatización de turnos para tratamientos crónicos, junto con un control de roles y permisos, restringiendo acciones críticas y asegurando el cumplimiento normativo, incluyendo la validación de consentimientos digitales en cirugías.

El sistema operará en un entorno digital centralizado, accesible para todo el personal, mejorando la coordinación interna. Las soluciones actuales del mercado no se adaptan completamente a estas necesidades, debido a su baja flexibilidad y limitada capacidad para implementar reglas específicas, lo que justifica el desarrollo de una solución a medida.

## 1.3 Riesgos

### HUMANO

Riesgo 1: Resistencia al cambio del personal (Severidad: Alta)
Mitigación: Capacitaciones iniciales, acompañamiento en el uso del sistema y diseño de una interfaz intuitiva que facilite su adopción.

Riesgo 2: Errores en el uso del sistema (Severidad: Media)
Mitigación: Se implementarán validaciones automáticas dentro del sistema (por ejemplo, el alta médica) y se darán guías de uso claras para minimizar errores.

### TÉCNICO

Riesgo 3: Problemas técnicos o fallas del sistema (Severidad: Alta)
Mitigación: Se realizarán pruebas previas a la implementación (testing) y mantenimiento periódico del sistema para asegurar su correcto funcionamiento.

### GESTIÓN

Riesgo 4: Retrasos en el desarrollo (Severidad: Media)
Mitigación: Se planificará el proyecto en etapas (MVP), estableciendo tiempos realistas y prioridades claras para cumplir con los objetivos.

### NEGOCIO

Riesgo 5: El sistema no cubra todas las necesidades (Severidad: Alta)
Mitigación: Se realizará un relevamiento previo de requerimientos y se mantendrá comunicación constante con el cliente para ajustar el sistema según sus necesidades.

Riesgo 6: Registro incorrecto de información clínica del paciente (Severidad: Alta)
Mitigación: Se implementarán validaciones obligatorias en los formularios y revisiones de consistencia en los registros clínicos.

Riesgo 7: Pérdida de seguimiento en tratamientos crónicos (Severidad: Alta)
Mitigación: El sistema incorporará recordatorios y controles de agenda para facilitar el seguimiento periódico de los pacientes.

Riesgo 8: Aplicación de vacunas sin validación del estado clínico del animal (Severidad: Alta)
Mitigación: El sistema bloqueará automáticamente la vacunación cuando el paciente no posea “Alta Médica” vigente.

### EXTERNO

Riesgo 9: Problemas de conectividad a internet (Severidad: Media)
Mitigación: Se procurará que el sistema sea liviano y se evaluará la posibilidad de contar con acceso alternativo o respaldo de conexión.

Riesgo 10: Fallas en la infraestructura (Severidad: Media)
Mitigación: Se utilizarán servicios confiables y se realizarán copias de seguridad periódicas para evitar pérdida de información.

### OPERATIVO

Riesgo 11: Superposición de turnos por errores en la agenda (Severidad: Media)   
Mitigación: El sistema validará la disponibilidad horaria antes de confirmar un turno.

### LEGAL/NORMATIVO

Riesgo 12: Falta de registro adecuado de consentimientos en procedimientos complejos (Severidad: Media)
Mitigación: El sistema almacenará la documentación asociada a cada procedimiento para asegurar trazabilidad y control.

### SEGURIDAD

Riesgo 13: Acceso indebido a funcionalidades restringidas (Severidad: Alta)
Mitigación: Control de permisos según rol y autenticación segura.

# 2. Visión de la Solución

## Funciones principales

1- Gestión de Pacientes y Fichas de Mascotas
Permite el registro, consulta y actualización de la información de cada mascota, centralizando datos generales, estado de salud y vínculo con su dueño.

2- Módulo de Historias Clínicas y Registro de Atenciones
Facilita el registro de consultas médicas, evolución del paciente y seguimiento clínico, asegurando la trazabilidad de cada atención.

3- Gestión de Vacunación y Control Sanitario
Administra los planes de vacunación, validando condiciones necesarias antes de su aplicación y garantizando el cumplimiento de normativas de salud animal.

4- Gestión de Turnos y Agenda Clínica
Permite la asignación, seguimiento y organización de turnos, incluyendo la generación automática de citas para tratamientos crónicos y vacunaciones.

5- Sistema de Alertas y Monitoreo de Salud
Detecta automáticamente situaciones relevantes a partir de los datos clínicos (por ejemplo, variaciones de peso), permitiendo una atención preventiva.

6- Módulo de Gestión de Usuarios, Roles y Permisos
Controla el acceso al sistema y restringe acciones según el perfil del usuario, asegurando el cumplimiento de políticas internas.

7- Gestión de Medicación y Prescripciones
Administra la prescripción de medicamentos, aplicando restricciones según su categoría y el rol del profesional.

8- Módulo de Gestión de Procedimientos y Consentimientos
Permite registrar y validar la documentación necesaria para intervenciones, incluyendo la firma digital del dueño en cirugías.

# 3. Contexto del Negocio

## 3.1 Perfil de los interesados (Stakeholders)

| **Stakeholder** | **Beneficio y valor percibido** | **Actitudes** | **Funciones de interés mayor** | **Restricciones** |
| --- | --- | --- | --- | --- |
| Dueño de la clínica | Mejora organización, aumento de ingresos, información centralizada y control total del negocio | Interesado en el sistema y exigentes con el resultado, pero preocupado por costos | Reportes de ingresos y productividad, control de turnos y gestión general | Presupuesto limitado, que el sistema no cumpla con las expectativas y que sea difícil de utilizar |
| Veterinarios | Acceso rápido al historial clínico, menos errores de seguimiento, mejor atención y seguimiento de pacientes. | Interesados, pero pueden resistirse al cambio. Buscan rapidez y simplicidad. | Historias clínicas, registro de atenciones, vacunaciones y agenda diaria. | Falta de tiempo, sistema lento o complejo y miedo a depender mucho del sistema, |
| Recepcionista | Reducción de errores administrativos, organización más clara y agilidad en atención al cliente. | Miedo al cambio y necesidad de capacitación | Gestión de turnos, registro de pacientes y agenda diaria | Poco conocimiento tecnológico, necesidad de un sistema simple e intuitivo y dependencia de que el sistema funcione más rápido. |
| Clientes (dueños de mascotas) | Atención más rápida, mejor seguimiento de sus mascotas y menos errores en turnos | Alta expectativa de rapidez y exigentes con el servicio | Turnos organizados, historial claro de su mascota y información confiable | Demoras en atención, errores en turnos y falta de comunicación |

# 4. Alcance y limitaciones

## 4.1 Alcance inicial (MVP - Minimum Viable Product)

La versión 1.0 del sistema para la clínica veterinaria “Patitas” se enfocará en cubrir las necesidades básicas de organización y registro de información, priorizando simplicidad y funcionalidad.

En esta primera versión, el sistema permitirá la gestión de pacientes y fichas de mascotas (registro y actualización de información de mascotas y dueños), el registro básico de atenciones clínicas (carga y consulta inicial del historial clínico) y la gestión de turnos y agenda clínica (asignación, modificación y cancelación de turnos con validación de disponibilidad horaria, incluyendo el registro de la consulta).

Además, se incorporará el control de “Alta Médica” para la aplicación de vacunas (validación clínica básica) y el acceso mediante login para el personal autorizado (autenticación de usuarios).

## 4.2 Limitaciones y exclusiones (Out of Scope)

La clínica veterinaria Patitas ha planteado la necesidad de incorporar funcionalidades avanzadas, pero en esta primera versión del sistema se ha definido un alcance acotado y realista. En consecuencia, quedarán fuera de esta etapa el sistema de alertas y monitoreo automático de salud (Sistema de Alertas y Monitoreo de Salud), la generación automática de turnos para tratamientos crónicos (Gestión de Turnos y Agenda Clínica), y la gestión avanzada de permisos para prescripción de medicamentos psicotrópicos (Gestión de Usuarios, Roles y Permisos / Gestión de Medicación y Prescripciones).

Asimismo, no se implementará la administración completa de medicamentos (Gestión de Medicación y Prescripciones), ni la validación de firma digital para procedimientos quirúrgicos (Gestión de Procedimientos y Consentimientos).

Estas funcionalidades podrán incorporarse en futuras iteraciones del sistema, una vez validada la base operativa de la solución.

# 5. Requerimientos

## 5.1 Requerimientos Funcionales

- El sistema deberá permitir registrar, consultar y modificar dueños, manteniendo sus datos de contacto y responsabilidad sobre las mascotas asociadas.
- El sistema deberá permitir registrar, consultar, modificar y desactivar mascotas/pacientes, asociándolas a un dueño previamente registrado.
- El sistema deberá permitir reasignar una mascota a otro dueño en casos de cambio de titularidad o actualización de responsabilidad sobre el paciente.
- El sistema deberá permitir buscar un dueño y visualizar las mascotas vinculadas a su registro.
- El sistema deberá permitir registrar atenciones médicas asociadas a un paciente, almacenando fecha, motivo de consulta y observaciones.
- El sistema deberá actualizar automáticamente la fecha de última visita del paciente al registrar una nueva atención.
- El sistema deberá permitir consultar el historial de atenciones de un paciente.
- El sistema deberá permitir crear turnos indicando paciente, fecha, horario y motivo, organizando la agenda de la clínica.
- El sistema deberá validar la disponibilidad horaria al asignar turnos.
- El sistema deberá permitir modificar y cancelar turnos.
- El sistema deberá permitir el registro y autenticación de usuarios mediante login.
- El sistema deberá validar el estado de ‘Alta Médica’ antes de permitir la aplicación de vacunas.
- El sistema deberá permitir hasta cinco (5) intentos fallidos de autenticación. Al superar dicho límite, la cuenta del usuario será bloqueada.
- El sistema deberá mostrar al veterinario su agenda diaria de atenciones y procedimientos programados.
- El sistema deberá permitir registrar dueños y asociarles una o más mascotas. Al registrar una mascota, el sistema generará automáticamente su historia clínica.
- El sistema deberá registrar los turnos cancelados, indicando la fecha de cancelación, para su posterior consulta en los reportes del sistema.
- El sistema deberá generar reportes de atenciones médicas realizadas, vacunas aplicadas, mascotas registradas y dueños registrados.
- El sistema deberá advertir al usuario cuando intente abandonar una atención médica con información sin guardar, informando que los datos ingresados se perderán si no son registrados.
- El sistema deberá registrar automáticamente las vacunas aplicadas, las prescripciones emitidas y los estudios adjuntados durante una atención médica en sus respectivos historiales dentro de la historia clínica del paciente.

## 5.2 Requerimientos No Funcionales

- El sistema deberá implementar autenticación mediante usuario y contraseña, garantizando que solo usuarios registrados puedan acceder.
- El sistema deberá aplicar control de acceso basado en roles, restringiendo funcionalidades según el perfil del usuario (recepción o veterinario).
- El sistema deberá garantizar la protección de los datos clínicos y personales mediante mecanismos que eviten accesos no autorizados.
- Las operaciones de consulta de pacientes, turnos e historias clínicas deberán responder en un tiempo máximo de 2 segundos en condiciones normales.
- El sistema deberá presentar la información de forma organizada, facilitando la lectura de historias clínicas y agendas.
- El sistema deberá estar desarrollado bajo una arquitectura en capas (por ejemplo, presentación, lógica de negocio y acceso a datos), facilitando su mantenimiento y escalabilidad.
- El sistema deberá utilizar una base de datos estructurada que garantice la integridad y consistencia de la información.
- El sistema deberá estar disponible durante el horario operativo de la clínica, minimizando interrupciones del servicio.
- El sistema deberá contar con mecanismos de respaldo de la información para evitar pérdida de datos.
- El sistema deberá estar diseñado de manera modular, permitiendo la incorporación de nuevas funcionalidades sin afectar las existentes.

# Reglas de negocio

RN-01: Las atenciones médicas registradas en la historia clínica no podrán ser modificadas ni eliminadas, ya que constituyen un registro histórico de la atención brindada al paciente.

RN-02. Las prescripciones, estudios y vacunaciones registrados durante una atención médica quedarán asociados automáticamente a dicha atención y a la historia clínica del paciente.

RN-03. La evaluación de aptitud para vacunación solo podrá realizarse durante el registro de una atención médica.

RN-04. Una vacuna sólo podrá registrarse si el paciente fue evaluado como apto para la vacunación.
