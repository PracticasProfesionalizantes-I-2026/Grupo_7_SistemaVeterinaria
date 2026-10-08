# 1. Requerimientos del Negocio

## 1.1 Situación actual o Propósito

La clínica veterinaria “Patitas” es un centro de atención de salud animal que, debido a su crecimiento en cantidad de pacientes y servicios, incrementó la complejidad de su gestión clínica y administrativa.

Actualmente, la organización trabaja con una combinación de registros manuales y herramientas digitales no integradas entre sí, lo que genera fragmentación de la información y dificultades en su actualización y seguimiento. Esta situación impacta directamente en la calidad del servicio, especialmente en el control de historias clínicas y planes de vacunación.

Entre los principales problemas se identifican la falta de validaciones automáticas en procesos críticos, la dificultad para detectar alertas de salud a partir de datos históricos, y la ausencia de controles efectivos sobre roles y permisos del personal. Asimismo, se observan limitaciones en el seguimiento de tratamientos crónicos, en el control de vacunación y en el registro adecuado de consentimientos en procedimientos de mayor complejidad.

Estos inconvenientes evidencian la necesidad de contar con un sistema informático que permita centralizar la información, automatizar procesos clave y garantizar el cumplimiento de las reglas de negocio, mejorando la eficiencia operativa y la calidad de atención.

## 1.2 Oportunidad del negocio

La implementación de un sistema informático en la clínica veterinaria Patitas permitirá superar las limitaciones actuales mediante la centralización de la información en una única plataforma, asegurando consistencia y actualización automática de los datos clínicos.

El sistema incorporará reglas de negocio automatizadas, garantizando el cumplimiento de condiciones clave, como la evaluación de aptitud clínica para vacunación y la actualización de la última visita. Además, permitirá el monitoreo y detección temprana de alertas de salud, mejorando el seguimiento de los pacientes.

Se optimizará la gestión mediante la automatización de turnos para tratamientos crónicos, junto con un control de roles y permisos, restringiendo acciones críticas y asegurando el cumplimiento normativo, incluyendo la validación de consentimientos digitales en cirugías.

El sistema operará en un entorno digital centralizado, accesible para todo el personal, mejorando la coordinación interna. Las soluciones actuales del mercado no se adaptan completamente a estas necesidades, debido a su baja flexibilidad y limitada capacidad para implementar reglas específicas, lo que justifica el desarrollo de una solución a medida.

## 1.3 Riesgos

### HUMANO

Riesgo 1: Resistencia al cambio del personal (Severidad: Alta)
Mitigación: Capacitaciones iniciales, acompañamiento en el uso del sistema y diseño de una interfaz intuitiva que facilite su adopción.

Riesgo 2: Errores en el uso del sistema (Severidad: Media)
Mitigación: Se implementarán validaciones automáticas dentro del sistema (por ejemplo, la evaluación de aptitud clínica para vacunación) y se darán guías de uso claras para minimizar errores.

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
Mitigación: El sistema bloqueará el registro de la vacunación cuando el paciente no haya sido evaluado como APTO para vacunación durante la atención médica actual mediante la checklist clínica.

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
Mitigación: En la versión inicial (MVP), los procedimientos quirúrgicos se registran dentro de la atención médica para garantizar la trazabilidad clínica y control en la Historia Clínica; la digitalización del consentimiento y la firma del dueño se reservan para una etapa futura.

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

8- Módulo de Gestión de Procedimientos Quirúrgicos y Consentimientos
Permite registrar las intervenciones quirúrgicas dentro de las atenciones médicas garantizando trazabilidad e inmutabilidad en la Historia Clínica; la captura y validación de firma digital del dueño en cirugías se prevé para una etapa futura.

# 3. Contexto del Negocio

## 3.1 Perfil de los interesados (Stakeholders)

| **Stakeholder** | **Beneficio y valor percibido** | **Actitudes** | **Funciones de interés mayor** | **Restricciones** |
| --- | --- | --- | --- | --- |
| Administrador (dueño de la clínica) | Mejora organización, aumento de ingresos, información centralizada y control total del negocio | Interesado en el sistema y exigentes con el resultado, pero preocupado por costos | Reportes de ingresos y productividad, control de turnos y gestión general | Presupuesto limitado, que el sistema no cumpla con las expectativas y que sea difícil de utilizar |
| Veterinarios | Acceso rápido al historial clínico, menos errores de seguimiento, mejor atención y seguimiento de pacientes. | Interesados, pero pueden resistirse al cambio. Buscan rapidez y simplicidad. | Historias clínicas, registro de atenciones, vacunaciones y agenda diaria. | Falta de tiempo, sistema lento o complejo y miedo a depender mucho del sistema, |
| Recepcionista | Reducción de errores administrativos, organización más clara y agilidad en atención al cliente. | Miedo al cambio y necesidad de capacitación | Gestión de turnos, registro de pacientes y agenda diaria | Poco conocimiento tecnológico, necesidad de un sistema simple e intuitivo y dependencia de que el sistema funcione más rápido. |
| Clientes (dueños de mascotas) | Atención más rápida, mejor seguimiento de sus mascotas y menos errores en turnos | Alta expectativa de rapidez y exigentes con el servicio | Turnos organizados, historial claro de su mascota y información confiable | Demoras en atención, errores en turnos y falta de comunicación |

# 4. Alcance y limitaciones

## 4.1 Alcance inicial (MVP - Minimum Viable Product)

La versión 1.0 del sistema para la clínica veterinaria “Patitas” se enfocará en cubrir las necesidades básicas de organización y registro de información, priorizando simplicidad y funcionalidad.

En esta primera versión, el sistema permitirá la gestión de pacientes y fichas de mascotas (registro y actualización de información de mascotas y dueños, visualizando la fecha de última visita de solo lectura o «Sin visitas registradas»), el registro básico de atenciones clínicas (carga y consulta inicial del historial clínico, con actualización automática de la fecha de última visita al guardar la atención) y la gestión de turnos y agenda clínica (asignación, modificación y cancelación de turnos con validación de disponibilidad horaria, incluyendo el registro de la consulta).

Además, se incorporará el control y registro de la evaluación de aptitud clínica (checklist APTO/NO APTO) para la aplicación de vacunas durante la atención médica y el acceso mediante login para el personal autorizado (autenticación de usuarios).

Asimismo, dentro del registro de atenciones médicas se incluye la posibilidad de registrar de manera opcional procedimientos quirúrgicos (tipo de procedimiento, fecha, descripción, veterinario responsable y observaciones/complicaciones), quedando disponibles para su posterior consulta histórica desde la Historia Clínica.

## 4.2 Limitaciones y exclusiones (Out of Scope)

La clínica veterinaria Patitas ha planteado la necesidad de incorporar funcionalidades avanzadas, pero en esta primera versión del sistema se ha definido un alcance acotado y realista. En consecuencia, quedarán fuera de esta etapa el sistema de alertas y monitoreo automático de salud (Sistema de Alertas y Monitoreo de Salud), la generación automática de turnos para tratamientos crónicos (Gestión de Turnos y Agenda Clínica), y la gestión avanzada de permisos para prescripción de medicamentos psicotrópicos (Gestión de Usuarios, Roles y Permisos / Gestión de Medicación y Prescripciones).

Asimismo, no se implementará la administración completa de medicamentos (Gestión de Medicación y Prescripciones). En relación con los procedimientos quirúrgicos, quedan expresamente excluidos de esta primera entrega la captura, almacenamiento y validación de consentimiento digital del dueño, así como su firma digital o electrónica y la gestión de autorizaciones mediante dispositivos externos (previstos para una etapa futura), sin que esta exclusión de software exima de las responsabilidades profesionales y legales correspondientes al acto médico. Tampoco se contemplan módulos independientes de gestión de quirófanos, anestesia, insumos, costos ni programación quirúrgica separada.

Tampoco se implementarán sistemas de guardado automático de borradores ni recuperación automática de sesiones ante eventos externos o no controlados por la aplicación (tales como el cierre intempestivo de la ventana o pestaña del navegador, la recarga forzada de página o la pérdida imprevista de conectividad), quedando señaladas estas limitaciones técnicas para una futura evaluación.

Estas funcionalidades podrán incorporarse en futuras iteraciones del sistema, una vez validada la base operativa de la solución.

# 5. Requerimientos

## 5.1 Requerimientos Funcionales

- El sistema deberá permitir registrar, consultar y modificar dueños, manteniendo sus datos de contacto y responsabilidad sobre las mascotas asociadas.
- El sistema deberá permitir registrar, consultar, modificar y desactivar mascotas/pacientes asociadas a un dueño previamente registrado. La desactivación será de carácter lógico y conservará la historia clínica y los registros históricos asociados a la mascota. Las mascotas inactivas podrán ser consultadas, pero no podrán recibir nuevos turnos ni nuevas atenciones médicas.
- El sistema deberá permitir reasignar una mascota a otro dueño en casos de cambio de titularidad o actualización de responsabilidad sobre el paciente, bajo las siguientes condiciones: la mascota deberá encontrarse en estado Activa; el nuevo dueño deberá estar previamente registrado en el sistema; no se permitirá seleccionar como nuevo dueño al mismo propietario actual; el sistema solicitará confirmación antes de realizar la reasignación. La mascota conservará su identidad, su Historia Clínica completa y todos sus registros históricos. El sistema mantendrá un historial de propietarios con la mascota afectada, el dueño anterior, el nuevo dueño, la fecha y hora de la reasignación y el usuario que la realizó. Los turnos futuros de la mascota se conservarán sin modificar fechas ni horarios, mostrando al nuevo dueño como responsable actual. Las mascotas inactivas no podrán ser reasignadas.
- El sistema deberá permitir buscar un dueño y visualizar las mascotas vinculadas a su registro.
- El sistema deberá permitir registrar atenciones médicas asociadas a un paciente, almacenando fecha, motivo de consulta y observaciones.
- El sistema deberá actualizar automáticamente la fecha de última visita de la mascota al guardar correctamente una atención médica, asegurando que corresponda a la atención más reciente registrada. Dicha fecha se mostrará como dato de solo lectura en la información general de la mascota y en su Historia Clínica (o indicando «Sin visitas registradas» si la mascota aún no posee atenciones médicas). La actualización se realizará sin requerir intervención manual y no se aplicará ante errores de guardado ni al abandonar el formulario sin guardar.
- El sistema deberá permitir consultar el historial de atenciones de un paciente.
- El sistema deberá permitir crear turnos indicando paciente, fecha, horario y motivo, organizando la agenda de la clínica.
- El sistema deberá validar la disponibilidad horaria al asignar turnos, asegurando la no superposición de citas para un mismo veterinario (RN-08). El control de disponibilidad considerará únicamente los turnos que ocupen activamente el horario según su estado vigente; los turnos cancelados no computarán como ocupados y liberarán el espacio en la agenda, permitiendo nuevas reservas en ese horario con registros e identificaciones independientes sin generar conflictos de superposición.
- El sistema deberá permitir modificar y cancelar turnos. La cancelación podrá ser realizada por los roles Recepcionista y Veterinario; requerirá el ingreso obligatorio de un motivo y confirmación previa del usuario. Al confirmarse, el turno cambiará al estado Cancelado de manera irreversible (no pudiendo reactivarse). Los turnos finalizados no podrán cancelarse ni reprogramarse (RN-09). La cancelación no modificará los registros históricos de atenciones médicas ni alterará la historia clínica del paciente (RN-01).
- El sistema deberá permitir el registro y autenticación de usuarios mediante login.
- El sistema deberá validar que el paciente haya sido evaluado como APTO para vacunación mediante la checklist clínica durante la atención médica actual antes de permitir el registro de una vacuna.
- El sistema deberá controlar los intentos fallidos de inicio de sesión, permitiendo hasta cinco (5) intentos fallidos consecutivos por cuenta de usuario. Cada intento de autenticación con contraseña incorrecta para una cuenta existente y no bloqueada incrementará el contador de intentos fallidos. Al alcanzar el quinto intento fallido consecutivo, la cuenta quedará bloqueada impidiendo el acceso, incluso si posteriormente se ingresa la contraseña correcta. El bloqueo se mantendrá hasta que el Administrador realice el desbloqueo de la cuenta (no existiendo desbloqueo automático por tiempo). Tanto el inicio de sesión exitoso antes de alcanzar el límite como el desbloqueo administrativo restablecerán el contador de intentos fallidos a cero.
- El sistema deberá proveer al Administrador una sección de **Gestión de Usuarios** con acceso exclusivo para dicho rol, desde la cual podrá: (a) consultar el listado de usuarios registrados con su identificación, rol asignado y estado de cuenta; (b) desbloquear cuentas bloqueadas, restableciendo el contador de intentos fallidos a cero sin modificar la contraseña vigente; (c) restablecer la contraseña de un usuario mediante una contraseña temporal generada de forma segura. Desbloquear una cuenta y restablecer una contraseña son operaciones independientes: no será obligatorio restablecer la contraseña para desbloquear una cuenta, y restablecer la contraseña no desbloqueará automáticamente una cuenta bloqueada.
- El sistema deberá exigir al usuario que cambie obligatoriamente su contraseña temporal en su próximo inicio de sesión. Mientras ese cambio no se complete, el usuario no podrá utilizar las demás funcionalidades del sistema.
- El sistema deberá conservar un registro de las operaciones administrativas de desbloqueo de cuentas y restablecimiento de contraseñas, incluyendo el usuario afectado, el tipo de operación, el Administrador responsable y la fecha y hora de la operación. Este registro no deberá almacenar contraseñas ni información secreta.
- El sistema deberá mostrar al veterinario su agenda diaria de atenciones y procedimientos programados.
- El sistema deberá permitir registrar dueños y asociarles una o más mascotas. Al registrar una mascota, el sistema generará automáticamente su historia clínica.
- El sistema deberá registrar y conservar en el historial todos los turnos cancelados sin eliminarlos físicamente, registrando de forma automática la fecha y hora exacta de la cancelación y la identidad del usuario responsable, junto con el motivo obligatorio y los datos originales del turno (mascota, dueño, veterinario, fecha y horario), para su posterior consulta histórica en la agenda y en los reportes operativos del sistema.
- El sistema deberá generar reportes de atenciones médicas realizadas, vacunas aplicadas, mascotas registradas y dueños registradas.
- El sistema deberá advertir al Veterinario cuando intente abandonar el formulario de una atención médica habiendo ingresado o modificado información clínica o quirúrgica que aún no fue guardada, mediante acciones de navegación controladas por el sistema (regresar a la pantalla anterior, seleccionar otra sección, seleccionar otra mascota o historia clínica, cancelar el registro o cerrar el formulario). La advertencia se presentará mediante un mensaje de confirmación con el título "¿Desea salir sin guardar?" y el mensaje "Los datos ingresados se perderán si abandona esta atención.", ofreciendo las opciones: (a) "Continuar editando": cierra la advertencia, mantiene al Veterinario en el formulario y conserva toda la información ingresada sin registrar ni descartar la atención; (b) "Salir sin guardar": descarta la información no guardada, abandona el formulario y no registra una nueva atención médica ni genera registros clínicos asociados. Si el Veterinario cierra la advertencia sin seleccionar una opción, permanecerá en el formulario conservando sus datos. Si el formulario no posee cambios pendientes, el sistema permitirá salir inmediatamente sin mostrar advertencia. La advertencia no guardará automáticamente una atención incompleta ni recuperará borradores. Ante errores durante el guardado, el sistema informará que la atención no pudo guardarse, conservará todos los datos ingresados en el formulario y permitirá corregir o reintentar el guardado, sin dar por registrada la atención y preservando la inmutabilidad de las atenciones ya registradas (RN-01).
- El sistema deberá registrar de forma inmutable las vacunas aplicadas, las prescripciones emitidas, los estudios adjuntados y los procedimientos quirúrgicos realizados durante una atención médica en la Historia Clínica del paciente, asociando tipo de procedimiento, fecha, descripción, Veterinario responsable y observaciones o complicaciones para su consulta histórica. Los estudios médicos se registran durante una atención médica y quedan asociados a la Historia Clínica de la mascota. Si los resultados se reciben después de guardar la atención original, podrán incorporarse mediante una nueva atención médica, sin modificar los registros anteriores.

## 5.2 Requerimientos No Funcionales

- El sistema deberá implementar autenticación mediante usuario y contraseña, garantizando que solo usuarios registrados puedan acceder.
- El sistema deberá aplicar control de acceso basado en roles, restringiendo las funcionalidades disponibles según el perfil del usuario: Recepcionista, Veterinario o Administrador.
- El sistema deberá garantizar la protección de los datos clínicos y personales mediante mecanismos que eviten accesos no autorizados.
- Las operaciones de consulta de pacientes, turnos e historias clínicas deberán responder en un tiempo máximo de 2 segundos en condiciones normales.
- El sistema deberá presentar la información de forma organizada, facilitando la lectura de historias clínicas y agendas.
- El sistema deberá estar desarrollado bajo una arquitectura en capas (por ejemplo, presentación, lógica de negocio y acceso a datos), facilitando su mantenimiento y escalabilidad.
- El sistema deberá utilizar una base de datos estructurada que garantice la integridad y consistencia de la información.
- El sistema deberá estar disponible durante el horario operativo de la clínica, minimizando interrupciones del servicio.
- El sistema deberá contar con mecanismos de respaldo de la información para evitar pérdida de datos.
- El sistema deberá estar diseñado de manera modular, permitiendo la incorporación de nuevas funcionalidades sin afectar las existentes.
- El sistema no deberá almacenar, mostrar ni exponer contraseñas en texto legible. Las contraseñas temporales generadas por el Administrador deberán almacenarse de forma segura (mediante hash) y su visualización para comunicación al usuario será única y no persistente en la interfaz.

# Reglas de negocio

RN-01. Inmutabilidad de las Atenciones Médicas: Una vez registrada una atención médica en la Historia Clínica, no podrá ser modificada ni eliminada y permanecerá como registro histórico.

RN-02. Integración de Registros Clínicos a la Historia Clínica: Los registros clínicos generados para una mascota, como prescripciones, estudios, procedimientos quirúrgicos y vacunaciones, deberán quedar asociados a su Historia Clínica, manteniendo la trazabilidad correspondiente con la atención médica cuando corresponda.

RN-03. Evaluación de Aptitud para Vacunación durante la Atención Médica: La evaluación de aptitud para vacunación solo podrá realizarse durante una atención médica mediante una checklist clínica. El resultado deberá quedar registrado como APTO o NO APTO junto con la atención.

RN-04. Vacunación condicionada a Aptitud Clínica: Una vacunación solo podrá registrarse cuando el paciente haya obtenido resultado APTO en la evaluación de aptitud realizada durante la atención médica. Un resultado NO APTO impedirá registrar la vacuna, pero no impedirá guardar y finalizar normalmente la atención médica.

RN-05. Unicidad del DNI del Dueño: El DNI de cada dueño deberá ser único en el sistema. No se permitirá registrar un nuevo dueño ni modificar uno existente utilizando un DNI que ya corresponda a otro dueño registrado.

RN-06. Asociación de Mascota a Dueño: Toda mascota deberá estar asociada a un dueño previamente registrado en el sistema.

RN-07. Baja Lógica y Preservación Histórica de Mascotas: La desactivación de una mascota será de carácter lógico y no implicará su eliminación física del sistema. La mascota conservará su Historia Clínica y todos sus registros históricos y continuará disponible para consulta. Mientras permanezcan inactiva no podrá recibir nuevos turnos ni nuevas atenciones médicas.

RN-08. No Solapamiento de Turnos: El sistema no permitirá asignar a un mismo veterinario turnos cuyos horarios se superpongan. El control de disponibilidad horaria considerará únicamente aquellos turnos que mantengan una reserva activa del horario según su estado vigente; los turnos en estado Cancelado liberan el espacio en la agenda y no generan conflicto de superposición ante nuevas reservas.

RN-09. Inmutabilidad de Turnos Finalizados: Los turnos que se encuentren en estado Finalizado no podrán ser cancelados ni reprogramados.
