using BusinessLogic.Interfaces;
using DataAccess.Entities;
using DataAccess.Repositories;
using Shared.DTOs.Turno;
using Shared.Exceptions;

namespace BusinessLogic.Services;

public class TurnoService : ITurnoService
{
    private static readonly long TicksBloque30Minutos = TimeSpan.FromMinutes(30).Ticks;

    private static readonly string[] EstadosTurnoValidos =
    [
        "Pendiente",
        "Confirmado",
        "Finalizado",
        "Cancelado"
    ];

    private readonly ITurnoRepository _turnoRepository;
    private readonly IDisponibilidadVeterinarioRepository _disponibilidadRepository;
    private readonly IMascotaRepository _mascotaRepository;
    private readonly IUsuarioRepository _usuarioRepository;

    public TurnoService(
        ITurnoRepository turnoRepository,
        IDisponibilidadVeterinarioRepository disponibilidadRepository,
        IMascotaRepository mascotaRepository,
        IUsuarioRepository usuarioRepository)
    {
        _turnoRepository = turnoRepository;
        _disponibilidadRepository = disponibilidadRepository;
        _mascotaRepository = mascotaRepository;
        _usuarioRepository = usuarioRepository;
    }

    public async Task<TurnoResponseDTO> GetByIdAsync(Guid id)
    {
        var turno = await _turnoRepository.GetByIdAsync(id);
        if (turno == null)
        {
            throw new TurnoNotFoundException(id);
        }

        return MapToResponseDTO(turno);
    }

    public async Task<IEnumerable<TurnoResponseDTO>> GetAgendaAsync(AgendaFilterDTO filtro)
    {
        if (filtro == null)
        {
            throw new ValidationException("Los parámetros de filtro de la agenda son inválidos.");
        }

        string? estadoNormalizado = null;
        if (!string.IsNullOrWhiteSpace(filtro.Estado))
        {
            var estadoBuscado = filtro.Estado.Trim();
            estadoNormalizado = EstadosTurnoValidos.FirstOrDefault(
                e => string.Equals(e, estadoBuscado, StringComparison.OrdinalIgnoreCase));

            if (estadoNormalizado == null)
            {
                throw new ValidationException("El estado filtrado no es válido. Los valores permitidos son: Pendiente, Confirmado, Finalizado, Cancelado.");
            }
        }

        if (filtro.VeterinarioId.HasValue)
        {
            if (filtro.VeterinarioId.Value == Guid.Empty)
            {
                throw new ValidationException("El identificador del veterinario filtrado no es válido.");
            }

            var veterinario = await _usuarioRepository.GetByIdAsync(filtro.VeterinarioId.Value);
            if (veterinario == null || veterinario.Rol != "Veterinario" || !veterinario.Activo)
            {
                throw new VeterinarioNotFoundException(filtro.VeterinarioId.Value);
            }
        }

        if (filtro.MascotaId.HasValue && filtro.MascotaId.Value == Guid.Empty)
        {
            throw new ValidationException("El identificador de la mascota filtrada no es válido.");
        }

        DateTime? fechaDesde = null;
        DateTime? fechaHasta = null;

        if (filtro.Fecha.HasValue)
        {
            fechaDesde = filtro.Fecha.Value.Date;
            fechaHasta = filtro.Fecha.Value.Date.AddDays(1).AddTicks(-1);
        }

        var turnos = await _turnoRepository.GetAgendaAsync(fechaDesde, fechaHasta, filtro.VeterinarioId, estadoNormalizado);

        if (filtro.MascotaId.HasValue)
        {
            turnos = turnos.Where(t => t.MascotaId == filtro.MascotaId.Value);
        }

        return turnos.Select(MapToResponseDTO).ToList();
    }

    public async Task<TurnoResponseDTO> CreateManualAsync(TurnoCreateDTO dto)
    {
        // 1. Validar Mascota existente y activa
        var mascota = await _mascotaRepository.GetByIdAsync(dto.MascotaId);
        if (mascota == null)
        {
            throw new MascotaNotFoundException(dto.MascotaId);
        }

        if (!mascota.Activa)
        {
            throw new MascotaInactivaException(dto.MascotaId);
        }

        // 2. Validar Veterinario habilitado
        var veterinario = await _usuarioRepository.GetByIdAsync(dto.VeterinarioId);
        if (veterinario == null || veterinario.Rol != "Veterinario" || !veterinario.Activo)
        {
            throw new VeterinarioNotFoundException(dto.VeterinarioId);
        }

        // 3. Validar duración y alineación a bloques de 30 minutos (:00 o :30)
        ValidarAlineacionHoraria(dto.FechaHora);

        // 4. Validar fecha no pasada
        if (dto.FechaHora < DateTime.UtcNow)
        {
            throw new ValidationException("No se pueden agendar turnos en fechas u horarios pasados.");
        }

        // 5. Validar disponibilidad semanal configurada del veterinario (RN-10)
        var dentroDeDisponibilidad = await _disponibilidadRepository.EstaDentroDeDisponibilidadAsync(dto.VeterinarioId, dto.FechaHora, 30);
        if (!dentroDeDisponibilidad)
        {
            throw new HorarioNoDisponibleException("El horario solicitado se encuentra fuera de la disponibilidad configurada del profesional.");
        }

        // 6. Validar que no haya superposición para el veterinario (RN-08) con sincronización atómica
        using (await VeterinarioLockManager.AdquirirBloqueoAsync(dto.VeterinarioId))
        {
            var haySuperposicion = await _turnoRepository.ExisteSuperposicionAsync(dto.VeterinarioId, dto.FechaHora);
            if (haySuperposicion)
            {
                throw new HorarioNoDisponibleException("El profesional ya cuenta con un turno activo en ese horario.");
            }

            // 7. Persistir turno manual
            var nuevoTurno = new Turno
            {
                Id = Guid.NewGuid(),
                MascotaId = dto.MascotaId,
                VeterinarioId = dto.VeterinarioId,
                FechaHora = dto.FechaHora,
                MotivoConsulta = dto.MotivoConsulta.Trim(),
                Estado = "Confirmado",
                EsAutomatico = false,
                TipoOrigen = "Manual",
                FechaCreacion = DateTime.UtcNow
            };

            var creado = await _turnoRepository.CreateAsync(nuevoTurno);

            // Recargar con navegación completa para respuesta
            var turnoCompleto = await _turnoRepository.GetByIdAsync(creado.Id);
            return MapToResponseDTO(turnoCompleto ?? creado);
        }
    }

    public async Task<TurnoResponseDTO> UpdateAsync(Guid id, TurnoUpdateDTO dto)
    {
        var turno = await _turnoRepository.GetByIdTrackedAsync(id);
        if (turno == null)
        {
            throw new TurnoNotFoundException(id);
        }

        var targetVetId = dto.VeterinarioId ?? turno.VeterinarioId;

        using (await VeterinarioLockManager.AdquirirBloqueoDobleAsync(turno.VeterinarioId, targetVetId))
        {
            // Recargar estado real desde la base de datos dentro del semáforo para garantizar
            // que cualquier cambio persistido por otra operación concurrente se refleje efectivamente
            // aunque la entidad ya estuviera rastreada en el ChangeTracker de EF Core.
            await _turnoRepository.ReloadAsync(turno);

            // RN-09: No modificar turno finalizado
            if (turno.Estado == "Finalizado")
            {
                throw new TurnoFinalizadoException("No se puede reprogramar ni modificar un turno finalizado.");
            }

            if (turno.Estado == "Cancelado")
            {
                throw new TurnoYaCanceladoException("No se puede reprogramar un turno cancelado.");
            }

            // RN-07 y CU-06 (flujo 3e): Validar que la mascota asociada exista y se encuentre activa
            var mascota = turno.Mascota ?? await _mascotaRepository.GetByIdAsync(turno.MascotaId);
            if (mascota == null)
            {
                throw new MascotaNotFoundException(turno.MascotaId);
            }

            if (!mascota.Activa)
            {
                throw new MascotaInactivaException(turno.MascotaId);
            }

            // Validar profesional si cambia
            if (dto.VeterinarioId.HasValue && dto.VeterinarioId.Value != turno.VeterinarioId)
            {
                var vet = await _usuarioRepository.GetByIdAsync(dto.VeterinarioId.Value);
                if (vet == null || vet.Rol != "Veterinario" || !vet.Activo)
                {
                    throw new VeterinarioNotFoundException(dto.VeterinarioId.Value);
                }
            }

            // Validar alineación y fecha
            ValidarAlineacionHoraria(dto.FechaHora);

            if (dto.FechaHora < DateTime.UtcNow)
            {
                throw new ValidationException("No se pueden agendar turnos en fechas u horarios pasados.");
            }

            // Validar disponibilidad
            var dentroDeDisponibilidad = await _disponibilidadRepository.EstaDentroDeDisponibilidadAsync(targetVetId, dto.FechaHora, 30);
            if (!dentroDeDisponibilidad)
            {
                throw new HorarioNoDisponibleException("El horario solicitado se encuentra fuera de la disponibilidad configurada del profesional.");
            }

            // Validar superposición excluyendo el turno actual (protegido contra concurrencia)
            var haySuperposicion = await _turnoRepository.ExisteSuperposicionAsync(targetVetId, dto.FechaHora, turno.Id);
            if (haySuperposicion)
            {
                throw new HorarioNoDisponibleException("El profesional ya cuenta con un turno activo en ese horario.");
            }

            // Aplicar cambios
            turno.FechaHora = dto.FechaHora;
            turno.VeterinarioId = targetVetId;
            if (!string.IsNullOrWhiteSpace(dto.MotivoConsulta))
            {
                turno.MotivoConsulta = dto.MotivoConsulta.Trim();
            }

            await _turnoRepository.UpdateAsync(turno);

            var turnoActualizado = await _turnoRepository.GetByIdAsync(id);
            return MapToResponseDTO(turnoActualizado ?? turno);
        }
    }

    public async Task<TurnoResponseDTO> ConfirmarAsync(Guid id, Guid usuarioId, string username)
    {
        // 1. Obtener la entidad para determinar el profesional y validar existencia inicial
        var turno = await _turnoRepository.GetByIdTrackedAsync(id);
        if (turno == null)
        {
            throw new TurnoNotFoundException(id);
        }

        using (await VeterinarioLockManager.AdquirirBloqueoAsync(turno.VeterinarioId))
        {
            // 2. Recargar estado real desde la base de datos dentro de la sección protegida,
            // garantizando que cualquier cambio persistido por otra operación concurrente
            // se refleje efectivamente aunque la entidad ya estuviera rastreada en el ChangeTracker de EF Core.
            await _turnoRepository.ReloadAsync(turno);

            // 3. Comprobar que esté en estado Pendiente
            if (!string.Equals(turno.Estado, "Pendiente", StringComparison.OrdinalIgnoreCase))
            {
                throw new TurnoNoPendienteException(id, turno.Estado);
            }

            // 4. Validar que la mascota se encuentre activa (RN-07)
            var mascota = turno.Mascota ?? await _mascotaRepository.GetByIdAsync(turno.MascotaId);
            if (mascota == null)
            {
                throw new MascotaNotFoundException(turno.MascotaId);
            }

            if (!mascota.Activa)
            {
                throw new MascotaInactivaException(mascota.Id);
            }

            // 5. Validar que el turno no esté en el pasado
            if (turno.FechaHora < DateTime.UtcNow)
            {
                throw new ValidationException("No se puede confirmar un turno cuya fecha y hora ya han pasado.");
            }

            // 6. Verificar disponibilidad semanal del veterinario (RN-10)
            var dentroDeDisponibilidad = await _disponibilidadRepository.EstaDentroDeDisponibilidadAsync(turno.VeterinarioId, turno.FechaHora, 30);
            if (!dentroDeDisponibilidad)
            {
                throw new HorarioNoDisponibleException("El horario del turno se encuentra fuera de la disponibilidad configurada del profesional.");
            }

            // 7. Comprobar que no exista otro turno Confirmado o Finalizado superpuesto (RN-08) con exclusión atómica
            var haySuperposicion = await _turnoRepository.ExisteSuperposicionAsync(turno.VeterinarioId, turno.FechaHora, turno.Id);
            if (haySuperposicion)
            {
                throw new HorarioNoDisponibleException("El profesional ya cuenta con un turno activo en ese horario.");
            }

            // 8. Cambiar el estado a Confirmado únicamente cuando todas las validaciones se cumplen
            turno.Estado = "Confirmado";

            await _turnoRepository.UpdateAsync(turno);

            var turnoActualizado = await _turnoRepository.GetByIdAsync(id);
            return MapToResponseDTO(turnoActualizado ?? turno);
        }
    }

    public async Task<TurnoResponseDTO> FinalizarAsync(Guid id, Guid usuarioId, string username)
    {
        // 1. Obtener la entidad para determinar el profesional y validar existencia inicial
        var turno = await _turnoRepository.GetByIdTrackedAsync(id);
        if (turno == null)
        {
            throw new TurnoNotFoundException(id);
        }

        using (await VeterinarioLockManager.AdquirirBloqueoAsync(turno.VeterinarioId))
        {
            // 2. Recargar estado real desde la base de datos dentro de la sección protegida
            await _turnoRepository.ReloadAsync(turno);

            // 3. Regla funcional: Permitir únicamente la transición Confirmado -> Finalizado
            if (!string.Equals(turno.Estado, "Confirmado", StringComparison.OrdinalIgnoreCase))
            {
                throw new TurnoNoConfirmadoException(id, turno.Estado);
            }

            // 4. Cambiar el estado a Finalizado
            turno.Estado = "Finalizado";

            await _turnoRepository.UpdateAsync(turno);

            var turnoActualizado = await _turnoRepository.GetByIdAsync(id);
            return MapToResponseDTO(turnoActualizado ?? turno);
        }
    }

    public async Task<TurnoResponseDTO> CancelarAsync(Guid id, TurnoCancelarDTO dto, Guid usuarioId, string username)
    {
        if (string.IsNullOrWhiteSpace(dto.MotivoCancelacion))
        {
            throw new ValidationException("Debe ingresar un motivo de cancelación obligatorio.");
        }

        var turno = await _turnoRepository.GetByIdTrackedAsync(id);
        if (turno == null)
        {
            throw new TurnoNotFoundException(id);
        }

        using (await VeterinarioLockManager.AdquirirBloqueoAsync(turno.VeterinarioId))
        {
            // Recargar estado real desde la base de datos dentro del semáforo
            await _turnoRepository.ReloadAsync(turno);

            // RN-09: No cancelar turno finalizado
            if (turno.Estado == "Finalizado")
            {
                throw new TurnoFinalizadoException("No se puede cancelar un turno finalizado.");
            }

            if (turno.Estado == "Cancelado")
            {
                throw new TurnoYaCanceladoException("El turno ya se encuentra cancelado.");
            }

            // Auditoría de cancelación e inmediata liberación del horario (RN-08)
            turno.Estado = "Cancelado";
            turno.MotivoCancelacion = dto.MotivoCancelacion.Trim();
            turno.FechaCancelacion = DateTime.UtcNow;
            turno.UsuarioCancelacionId = usuarioId;
            turno.UsuarioCancelacionUsername = username;

            await _turnoRepository.UpdateAsync(turno);

            var turnoActualizado = await _turnoRepository.GetByIdAsync(id);
            return MapToResponseDTO(turnoActualizado ?? turno);
        }
    }

    public async Task<IEnumerable<DisponibilidadVeterinarioDTO>> GetDisponibilidadVeterinarioAsync(Guid veterinarioId)
    {
        var veterinario = await _usuarioRepository.GetByIdAsync(veterinarioId);
        if (veterinario == null || veterinario.Rol != "Veterinario" || !veterinario.Activo)
        {
            throw new VeterinarioNotFoundException(veterinarioId);
        }

        var disponibilidades = await _disponibilidadRepository.GetByVeterinarioIdAsync(veterinarioId);
        return disponibilidades.Select(d => new DisponibilidadVeterinarioDTO
        {
            DiaSemana = d.DiaSemana,
            Jornada = d.Jornada,
            HoraInicio = d.HoraInicio,
            HoraFin = d.HoraFin
        }).ToList();
    }

    public async Task<IEnumerable<DisponibilidadVeterinarioDTO>> ConfigurarDisponibilidadVeterinarioAsync(Guid veterinarioId, ConfigurarDisponibilidadDTO dto)
    {
        var veterinario = await _usuarioRepository.GetByIdAsync(veterinarioId);
        if (veterinario == null || veterinario.Rol != "Veterinario" || !veterinario.Activo)
        {
            throw new VeterinarioNotFoundException(veterinarioId);
        }

        if (dto == null || dto.Disponibilidades == null || !dto.Disponibilidades.Any())
        {
            throw new ValidationException("Debe configurar al menos un horario de disponibilidad.");
        }

        // Validaciones de rangos y no superposición interna
        var entidades = new List<DisponibilidadVeterinario>();
        for (var i = 0; i < dto.Disponibilidades.Count; i++)
        {
            var item = dto.Disponibilidades[i];
            if (item == null)
            {
                throw new DisponibilidadInvalidaException("El bloque de disponibilidad horaria no puede ser nulo.");
            }

            if (!Enum.IsDefined(typeof(DayOfWeek), item.DiaSemana))
            {
                throw new DisponibilidadInvalidaException("El día de la semana especificado no es válido.");
            }

            var jornada = NormalizarYValidarJornadaDisponibilidad(item.Jornada);

            if (item.HoraInicio < TimeSpan.Zero || item.HoraFin > TimeSpan.FromHours(24))
            {
                throw new DisponibilidadInvalidaException("Los horarios de disponibilidad deben encontrarse dentro del día (entre 00:00 y 24:00).");
            }

            if (item.HoraInicio >= item.HoraFin)
            {
                throw new DisponibilidadInvalidaException("La hora de inicio debe ser anterior a la hora de fin.");
            }

            if (!EsBloqueHorarioDe30MinutosValido(item.HoraInicio) || !EsBloqueHorarioDe30MinutosValido(item.HoraFin))
            {
                throw new DisponibilidadInvalidaException("Los horarios de disponibilidad deben estar alineados a bloques exactos de 30 minutos (:00 o :30), sin segundos ni fracciones.");
            }

            // Chequeo de superposición dentro del mismo día
            var superpuesto = dto.Disponibilidades
                .Where((other, j) => j != i && other != null && other.DiaSemana == item.DiaSemana)
                .Any(other => item.HoraInicio < other.HoraFin && item.HoraFin > other.HoraInicio);

            if (superpuesto)
            {
                throw new DisponibilidadInvalidaException($"Existe una superposición de horarios para el día {item.DiaSemana}.");
            }

            entidades.Add(new DisponibilidadVeterinario
            {
                Id = Guid.NewGuid(),
                VeterinarioId = veterinarioId,
                DiaSemana = item.DiaSemana,
                Jornada = jornada,
                HoraInicio = item.HoraInicio,
                HoraFin = item.HoraFin,
                Activo = true
            });
        }

        await _disponibilidadRepository.SetDisponibilidadesAsync(veterinarioId, entidades);

        return entidades.Select(d => new DisponibilidadVeterinarioDTO
        {
            DiaSemana = d.DiaSemana,
            Jornada = d.Jornada,
            HoraInicio = d.HoraInicio,
            HoraFin = d.HoraFin
        }).ToList();
    }

    public async Task<GenerarTurnosCronicosResponseDTO> GenerarTurnosSeguimientoCronicoAsync(GenerarTurnosCronicosDTO dto)
    {
        // 1. Validar Mascota activa
        var mascota = await _mascotaRepository.GetByIdAsync(dto.MascotaId);
        if (mascota == null)
        {
            throw new MascotaNotFoundException(dto.MascotaId);
        }

        if (!mascota.Activa)
        {
            throw new MascotaInactivaException(dto.MascotaId);
        }

        // 2. Validar Veterinario
        var vet = await _usuarioRepository.GetByIdAsync(dto.VeterinarioId);
        if (vet == null || vet.Rol != "Veterinario" || !vet.Activo)
        {
            throw new VeterinarioNotFoundException(dto.VeterinarioId);
        }

        // 3. Control de duplicados si viene vinculado a una atención médica
        if (dto.AtencionMedicaId.HasValue && dto.AtencionMedicaId.Value != Guid.Empty)
        {
            var yaGenerado = await _turnoRepository.ExisteSeguimientoParaAtencionAsync(dto.AtencionMedicaId.Value);
            if (yaGenerado)
            {
                throw new SeguimientoCronicoDuplicadoException("Ya existe un plan de seguimiento generado para la atención médica indicada.");
            }
        }

        // 4. Calcular fechas ideales para un horizonte de 6 meses
        var fechasIdeales = CalcularFechasIdeales(dto.FechaAtencion, dto.Frecuencia);

        // 5. Cargar disponibilidad semanal del profesional
        var disponibilidades = (await _disponibilidadRepository.GetByVeterinarioIdAsync(dto.VeterinarioId)).ToList();
        if (!disponibilidades.Any())
        {
            throw new ValidationException("El veterinario no tiene horarios de disponibilidad configurados.");
        }

        var jornadaPreferida = dto.JornadaPreferida.Trim();
        var turnosParaCrear = new List<Turno>();
        var turnosAsignadosDTO = new List<TurnoResponseDTO>();
        var fechasSinCupo = new List<DateTime>();
        var horariosOcupadosEnBatch = new HashSet<DateTime>();

        // 6. Para cada fecha ideal, buscar bloque de 30m libre hasta 7 días posteriores dentro de la jornada preferida
        using (await VeterinarioLockManager.AdquirirBloqueoAsync(dto.VeterinarioId))
        {
            foreach (var fechaIdeal in fechasIdeales)
            {
                var asignado = false;

                for (var offset = 0; offset <= 7; offset++)
                {
                    var fechaCandidata = fechaIdeal.Date.AddDays(offset);
                    var diaSemana = fechaCandidata.DayOfWeek;

                    // Filtrar disponibilidad por día y jornada preferida (sin cambiar de turno si fue especificado)
                    var franjas = disponibilidades.Where(d => d.DiaSemana == diaSemana).ToList();
                    if (string.Equals(jornadaPreferida, "Manana", StringComparison.OrdinalIgnoreCase))
                    {
                        franjas = franjas.Where(f => string.Equals(f.Jornada, "Manana", StringComparison.OrdinalIgnoreCase)).ToList();
                    }
                    else if (string.Equals(jornadaPreferida, "Tarde", StringComparison.OrdinalIgnoreCase))
                    {
                        franjas = franjas.Where(f => string.Equals(f.Jornada, "Tarde", StringComparison.OrdinalIgnoreCase)).ToList();
                    }

                    foreach (var franja in franjas)
                    {
                        for (var hora = franja.HoraInicio; hora + TimeSpan.FromMinutes(30) <= franja.HoraFin; hora += TimeSpan.FromMinutes(30))
                        {
                            var slot = fechaCandidata.Add(hora);

                            if (slot <= DateTime.UtcNow)
                            {
                                continue;
                            }

                            if (horariosOcupadosEnBatch.Contains(slot))
                            {
                                continue;
                            }

                            var superposicion = await _turnoRepository.ExisteSuperposicionAsync(dto.VeterinarioId, slot);
                            if (!superposicion)
                            {
                                // Slot libre encontrado
                                horariosOcupadosEnBatch.Add(slot);

                                var nuevoTurno = new Turno
                                {
                                    Id = Guid.NewGuid(),
                                    MascotaId = dto.MascotaId,
                                    VeterinarioId = dto.VeterinarioId,
                                    AtencionMedicaId = dto.AtencionMedicaId,
                                    FechaHora = slot,
                                    MotivoConsulta = "Control y seguimiento de enfermedad crónica",
                                    Estado = "Pendiente",
                                    EsAutomatico = true,
                                    TipoOrigen = "SeguimientoCronico",
                                    FechaCreacion = DateTime.UtcNow
                                };

                                turnosParaCrear.Add(nuevoTurno);

                                var responseDto = new TurnoResponseDTO
                                {
                                    Id = nuevoTurno.Id,
                                    MascotaId = nuevoTurno.MascotaId,
                                    MascotaNombre = mascota.Nombre,
                                    DuenoId = mascota.DuenoId,
                                    DuenoNombreCompleto = mascota.Dueno != null ? $"{mascota.Dueno.Nombre} {mascota.Dueno.Apellido}".Trim() : string.Empty,
                                    VeterinarioId = nuevoTurno.VeterinarioId,
                                    VeterinarioNombreCompleto = vet.Username,
                                    FechaHora = nuevoTurno.FechaHora,
                                    MotivoConsulta = nuevoTurno.MotivoConsulta,
                                    Estado = nuevoTurno.Estado,
                                    EsAutomatico = nuevoTurno.EsAutomatico,
                                    TipoOrigen = nuevoTurno.TipoOrigen,
                                    AtencionMedicaId = nuevoTurno.AtencionMedicaId,
                                    FechaCreacion = nuevoTurno.FechaCreacion
                                };
                                turnosAsignadosDTO.Add(responseDto);

                                asignado = true;
                                break;
                            }
                        }

                        if (asignado)
                        {
                            break;
                        }
                    }

                    if (asignado)
                    {
                        break;
                    }
                }

                if (!asignado)
                {
                    fechasSinCupo.Add(fechaIdeal);
                }
            }

            // 7. Persistir asignaciones parciales o totales
            if (turnosParaCrear.Any())
            {
                await _turnoRepository.CreateRangeAsync(turnosParaCrear);
            }
        }

        var mensaje = fechasSinCupo.Any()
            ? $"Se generaron {turnosAsignadosDTO.Count} turnos de seguimiento. {fechasSinCupo.Count} fecha(s) no contaron con cupo disponible dentro del horizonte tolerado."
            : $"Se generaron exitosamente los {turnosAsignadosDTO.Count} turnos de seguimiento para los próximos 6 meses.";

        return new GenerarTurnosCronicosResponseDTO
        {
            TurnosGenerados = turnosAsignadosDTO,
            FechasPendientes = fechasSinCupo,
            Mensaje = mensaje
        };
    }

    private static List<DateTime> CalcularFechasIdeales(DateTime fechaInicio, string frecuencia)
    {
        var fechas = new List<DateTime>();
        var fechaLimite = fechaInicio.AddMonths(6);
        var actual = fechaInicio;

        var normalizada = frecuencia.Trim();
        switch (normalizada.ToLowerInvariant())
        {
            case "quincenal":
                actual = actual.AddDays(14);
                while (actual <= fechaLimite)
                {
                    fechas.Add(actual);
                    actual = actual.AddDays(14);
                }
                break;

            case "mensual":
                for (var i = 1; i <= 6; i++)
                {
                    var proxima = fechaInicio.AddMonths(i);
                    if (proxima <= fechaLimite)
                    {
                        fechas.Add(proxima);
                    }
                }
                break;

            case "bimestral":
                for (var i = 2; i <= 6; i += 2)
                {
                    var proxima = fechaInicio.AddMonths(i);
                    if (proxima <= fechaLimite)
                    {
                        fechas.Add(proxima);
                    }
                }
                break;

            case "trimestral":
                for (var i = 3; i <= 6; i += 3)
                {
                    var proxima = fechaInicio.AddMonths(i);
                    if (proxima <= fechaLimite)
                    {
                        fechas.Add(proxima);
                    }
                }
                break;

            default:
                throw new ValidationException("Frecuencia no válida. Valores permitidos: Quincenal, Mensual, Bimestral, Trimestral.");
        }

        return fechas;
    }

    private static void ValidarAlineacionHoraria(DateTime fechaHora)
    {
        if (fechaHora.Minute % 30 != 0 || fechaHora.Second != 0 || fechaHora.Millisecond != 0)
        {
            throw new ValidationException("Los turnos deben agendarse en bloques fijos de 30 minutos (:00 o :30).");
        }
    }

    private static string NormalizarYValidarJornadaDisponibilidad(string? jornada)
    {
        if (string.IsNullOrWhiteSpace(jornada))
        {
            throw new DisponibilidadInvalidaException("La jornada es obligatoria y debe ser 'Manana' o 'Tarde'.");
        }

        var trimmed = jornada.Trim();
        if (string.Equals(trimmed, "Manana", StringComparison.OrdinalIgnoreCase))
        {
            return "Manana";
        }

        if (string.Equals(trimmed, "Tarde", StringComparison.OrdinalIgnoreCase))
        {
            return "Tarde";
        }

        throw new DisponibilidadInvalidaException("La jornada debe ser 'Manana' o 'Tarde'.");
    }

    private static bool EsBloqueHorarioDe30MinutosValido(TimeSpan horario)
    {
        return horario.Ticks % TicksBloque30Minutos == 0;
    }

    private static TurnoResponseDTO MapToResponseDTO(Turno turno)
    {
        var duenoNombreCompleto = string.Empty;
        if (turno.Mascota?.Dueno != null)
        {
            duenoNombreCompleto = $"{turno.Mascota.Dueno.Nombre} {turno.Mascota.Dueno.Apellido}".Trim();
        }

        return new TurnoResponseDTO
        {
            Id = turno.Id,
            MascotaId = turno.MascotaId,
            MascotaNombre = turno.Mascota?.Nombre ?? string.Empty,
            DuenoId = turno.Mascota?.DuenoId ?? Guid.Empty,
            DuenoNombreCompleto = duenoNombreCompleto,
            VeterinarioId = turno.VeterinarioId,
            VeterinarioNombreCompleto = turno.Veterinario?.Username ?? string.Empty,
            FechaHora = turno.FechaHora,
            MotivoConsulta = turno.MotivoConsulta,
            Estado = turno.Estado,
            EsAutomatico = turno.EsAutomatico,
            TipoOrigen = turno.TipoOrigen,
            AtencionMedicaId = turno.AtencionMedicaId,
            MotivoCancelacion = turno.MotivoCancelacion,
            FechaCancelacion = turno.FechaCancelacion,
            UsuarioCancelacionUsername = turno.UsuarioCancelacionUsername,
            FechaCreacion = turno.FechaCreacion
        };
    }
}
