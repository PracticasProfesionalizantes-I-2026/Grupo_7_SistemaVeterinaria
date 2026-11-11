using System.Data.Common;
using BusinessLogic.Interfaces;
using DataAccess.Entities;
using DataAccess.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Shared.DTOs.AtencionMedica;
using Shared.DTOs.Turno;
using Shared.Exceptions;

namespace BusinessLogic.Services;

public class AtencionMedicaService : IAtencionMedicaService
{
    private readonly IAtencionMedicaRepository _atencionMedicaRepository;
    private readonly IMascotaRepository _mascotaRepository;
    private readonly ITurnoService? _turnoService;

    public AtencionMedicaService(
        IAtencionMedicaRepository atencionMedicaRepository, 
        IMascotaRepository mascotaRepository,
        ITurnoService? turnoService = null)
    {
        _atencionMedicaRepository = atencionMedicaRepository;
        _mascotaRepository = mascotaRepository;
        _turnoService = turnoService;
    }

    public async Task<IEnumerable<AtencionMedicaResponseDTO>> GetAllAsync()
    {
        var atenciones = await _atencionMedicaRepository.GetAllAsync();
        var result = new List<AtencionMedicaResponseDTO>();
        foreach (var a in atenciones)
        {
            result.Add(MapToResponseDTO(a));
        }
        return result;
    }

    public async Task<AtencionMedicaResponseDTO> GetByIdAsync(Guid id)
    {
        var atencion = await _atencionMedicaRepository.GetByIdAsync(id);
        if (atencion == null)
        {
            throw new AtencionMedicaNotFoundException(id);
        }

        return MapToResponseDTO(atencion);
    }

    public async Task<IEnumerable<AtencionMedicaResponseDTO>> GetByMascotaIdAsync(Guid mascotaId)
    {
        var mascota = await _mascotaRepository.GetByIdAsync(mascotaId);
        if (mascota == null)
        {
            throw new MascotaNotFoundException(mascotaId);
        }

        var atenciones = await _atencionMedicaRepository.GetByMascotaIdAsync(mascotaId);
        var result = new List<AtencionMedicaResponseDTO>();
        foreach (var a in atenciones)
        {
            result.Add(MapToResponseDTO(a));
        }
        return result;
    }

    public async Task<AtencionMedicaResponseDTO> CreateAsync(
        AtencionMedicaCreateDTO dto, 
        Guid? veterinarioId = null, 
        string? veterinarioNombre = null)
    {
        ValidateCreateDto(dto);

        // 1. Verificar existencia y estado de la mascota (RN-07)
        var mascota = await _mascotaRepository.GetByIdAsync(dto.MascotaId);
        if (mascota == null)
        {
            throw new MascotaNotFoundException(dto.MascotaId);
        }

        if (!mascota.Activa)
        {
            throw new MascotaInactivaException(dto.MascotaId);
        }

        // 2. Nivel 1: Alerta de Desnutrición (RN-12)
        bool alertaDesnutricion = false;
        decimal? porcentajeDisminucionPeso = null;
        decimal? pesoReferenciaUtilizado = null;
        string? mensajeAlertaDesnutricion = null;

        var ultimaAtencion = await _atencionMedicaRepository.GetUltimaAtencionByMascotaIdAsync(dto.MascotaId);
        if (ultimaAtencion != null && ultimaAtencion.PesoRegistrado > 0)
        {
            pesoReferenciaUtilizado = ultimaAtencion.PesoRegistrado;
        }
        else if (mascota.Peso > 0)
        {
            pesoReferenciaUtilizado = mascota.Peso;
        }

        if (pesoReferenciaUtilizado.HasValue && pesoReferenciaUtilizado.Value > 0)
        {
            var disminucion = pesoReferenciaUtilizado.Value - dto.PesoRegistrado;
            if (disminucion > 0)
            {
                var porcentaje = (disminucion / pesoReferenciaUtilizado.Value) * 100m;
                porcentajeDisminucionPeso = Math.Round(porcentaje, 2);

                // Activación estricta cuando la disminución es estrictamente superior al 10 %
                if (porcentaje > 10.0m)
                {
                    alertaDesnutricion = true;
                    mensajeAlertaDesnutricion = $"Alerta clínica de desnutrición: Disminución de peso del {porcentajeDisminucionPeso}% estrictamente superior al 10% respecto al peso de referencia de {pesoReferenciaUtilizado.Value:F2} kg.";
                }
            }
            else
            {
                porcentajeDisminucionPeso = 0m;
            }
        }

        // 3. Evaluación de aptitud para vacunación y vacunas aplicadas (RN-03, RN-04)
        bool esApto = false;
        string? aptitudResultado = null;
        string? motivoNoApto = null;
        bool? sinFiebre = null;
        bool? buenEstado = null;
        bool? sinInfeccion = null;
        bool? autorizacion = null;

        if (dto.EvaluacionAptitud != null)
        {
            sinFiebre = dto.EvaluacionAptitud.SinFiebre;
            buenEstado = dto.EvaluacionAptitud.BuenEstadoGeneral;
            sinInfeccion = dto.EvaluacionAptitud.SinEnfermedadInfecciosaAguda;
            autorizacion = dto.EvaluacionAptitud.AutorizacionVeterinaria;
            esApto = dto.EvaluacionAptitud.EsApto;
            aptitudResultado = dto.EvaluacionAptitud.Resultado;
            motivoNoApto = dto.EvaluacionAptitud.MotivoNoApto;
        }
        else if (dto.EsAptoVacunacion.HasValue)
        {
            esApto = dto.EsAptoVacunacion.Value;
            aptitudResultado = esApto ? "APTO" : "NO_APTO";
            sinFiebre = esApto;
            buenEstado = esApto;
            sinInfeccion = esApto;
            autorizacion = esApto;
        }

        bool solicitaVacuna = (dto.Vacunacion != null && !string.IsNullOrWhiteSpace(dto.Vacunacion.Vacuna))
            || !string.IsNullOrWhiteSpace(dto.VacunaAplicada);

        string? vacunaNombre = dto.Vacunacion != null && !string.IsNullOrWhiteSpace(dto.Vacunacion.Vacuna)
            ? dto.Vacunacion.Vacuna.Trim()
            : (string.IsNullOrWhiteSpace(dto.VacunaAplicada) ? null : dto.VacunaAplicada.Trim());

        if (solicitaVacuna)
        {
            // RN-03 / RN-04: Solo se permite registrar vacuna si el paciente fue declarado APTO en la misma consulta médica
            if (!esApto || aptitudResultado != "APTO")
            {
                throw new PacienteNoAptoVacunacionException(dto.MascotaId);
            }
        }

        // 4. Construcción de la entidad AtencionMedica
        var nuevaAtencion = new AtencionMedica
        {
            MascotaId = dto.MascotaId,
            VeterinarioId = veterinarioId,
            Fecha = DateTime.UtcNow,
            MotivoConsulta = dto.MotivoConsulta.Trim(),
            Anamnesis = dto.Anamnesis.Trim(),
            ExamenFisico = dto.ExamenFisico.Trim(),
            Diagnostico = dto.Diagnostico.Trim(),
            Tratamiento = dto.Tratamiento.Trim(),
            PesoRegistrado = dto.PesoRegistrado,
            Observaciones = string.IsNullOrWhiteSpace(dto.Observaciones) ? null : dto.Observaciones.Trim(),

            // Nivel 1: Alerta Desnutrición
            AlertaDesnutricion = alertaDesnutricion,
            PorcentajeDisminucionPeso = porcentajeDisminucionPeso,
            PesoReferenciaUtilizado = pesoReferenciaUtilizado,

            // Vacunación y Aptitud Clínica
            EsAptoVacunacion = esApto,
            AptitudVacunacionResultado = aptitudResultado,
            MotivoNoAptoVacunacion = string.IsNullOrWhiteSpace(motivoNoApto) ? null : motivoNoApto.Trim(),
            SinFiebre = sinFiebre,
            BuenEstadoGeneral = buenEstado,
            SinEnfermedadInfecciosaAguda = sinInfeccion,
            AutorizacionVeterinaria = autorizacion,
            VacunaAplicada = solicitaVacuna ? vacunaNombre : null,
            VacunaDosis = solicitaVacuna && !string.IsNullOrWhiteSpace(dto.Vacunacion?.Dosis) ? dto.Vacunacion.Dosis.Trim() : null,
            VacunaLote = solicitaVacuna && !string.IsNullOrWhiteSpace(dto.Vacunacion?.Lote) ? dto.Vacunacion.Lote.Trim() : null,
            VacunaProximaDosis = solicitaVacuna ? dto.Vacunacion?.FechaProximaDosis : null
        };

        // Procedimiento quirúrgico (INF-06)
        if (dto.ProcedimientoQuirurgico != null)
        {
            nuevaAtencion.ProcedimientoQuirurgico = new ProcedimientoQuirurgico
            {
                TipoProcedimiento = dto.ProcedimientoQuirurgico.TipoProcedimiento.Trim(),
                Descripcion = dto.ProcedimientoQuirurgico.Descripcion.Trim(),
                Fecha = nuevaAtencion.Fecha,
                VeterinarioId = veterinarioId ?? Guid.Empty,
                ObservacionesComplicaciones = string.IsNullOrWhiteSpace(dto.ProcedimientoQuirurgico.ObservacionesComplicaciones)
                    ? null
                    : dto.ProcedimientoQuirurgico.ObservacionesComplicaciones.Trim()
            };
        }

        // Prescripciones farmacológicas (RN-02)
        if (dto.Prescripciones != null && dto.Prescripciones.Any())
        {
            foreach (var p in dto.Prescripciones)
            {
                nuevaAtencion.Prescripciones.Add(new PrescripcionMedica
                {
                    Medicamento = p.Medicamento.Trim(),
                    Dosis = p.Dosis.Trim(),
                    Frecuencia = p.Frecuencia.Trim(),
                    Duracion = p.Duracion.Trim()
                });
            }
        }

        // Estudios complementarios (RN-02, AMB-01)
        if (dto.Estudios != null && dto.Estudios.Any())
        {
            foreach (var e in dto.Estudios)
            {
                nuevaAtencion.EstudiosComplementarios.Add(new EstudioComplementario
                {
                    Nombre = e.Nombre.Trim(),
                    Resultado = string.IsNullOrWhiteSpace(e.Resultado) ? null : e.Resultado.Trim()
                });
            }
        }

        // 5. Persistencia atómica de la atención, última visita y seguimiento crónico en una única transacción de EF Core
        var transaction = await _atencionMedicaRepository.BeginTransactionAsync();
        try
        {
            var atencionCreada = await _atencionMedicaRepository.CreateAsync(nuevaAtencion);
            atencionCreada.Mascota ??= mascota;

            // 6. Nivel 3: Seguimiento de enfermedades crónicas (RN-11 / CU-06)
            List<TurnoResponseDTO> turnosSeguimiento = new();
            List<DateTime> fechasPendientesManual = new();
            string? mensajeSeguimiento = null;

            if (dto.SeguimientoCronico != null && _turnoService != null)
            {
                bool yaTieneTurnos = await _atencionMedicaRepository.HasTurnosSeguimientoAsync(atencionCreada.Id);
                if (!yaTieneTurnos)
                {
                    var cronicosDTO = new GenerarTurnosCronicosDTO
                    {
                        MascotaId = atencionCreada.MascotaId,
                        VeterinarioId = veterinarioId ?? atencionCreada.VeterinarioId ?? Guid.Empty,
                        AtencionMedicaId = atencionCreada.Id,
                        FechaAtencion = atencionCreada.Fecha,
                        Frecuencia = dto.SeguimientoCronico.Frecuencia.Trim(),
                        JornadaPreferida = dto.SeguimientoCronico.JornadaPreferida.Trim()
                    };

                    // Verificación de disponibilidad configurada antes de generar turnos
                    var disponibilidades = await _turnoService.GetDisponibilidadVeterinarioAsync(cronicosDTO.VeterinarioId);
                    if (disponibilidades != null && !disponibilidades.Any())
                    {
                        // Falta total de disponibilidad configurada: se marcan todas las fechas ideales para coordinación manual sin bloquear 201 Created
                        var fechasIdeales = CalcularFechasIdeales(atencionCreada.Fecha, dto.SeguimientoCronico.Frecuencia);
                        fechasPendientesManual = fechasIdeales;
                        mensajeSeguimiento = $"Advertencia: La atención médica fue registrada con éxito (Id: {atencionCreada.Id}). El profesional no cuenta con horarios de disponibilidad configurados en la agenda. Se marcaron {fechasPendientesManual.Count} fecha(s) para coordinación manual con recepción.";
                    }
                    else
                    {
                        var respCronicos = await _turnoService.GenerarTurnosSeguimientoCronicoAsync(cronicosDTO);
                        turnosSeguimiento = respCronicos.TurnosGenerados;
                        fechasPendientesManual = respCronicos.FechasPendientes;
                        mensajeSeguimiento = respCronicos.Mensaje;
                    }
                }
                else
                {
                    // Si la serie ya fue generada previamente para esta atención, recuperamos los turnos existentes para evitar duplicación
                    var atencionExistente = await _atencionMedicaRepository.GetByIdAsync(atencionCreada.Id);
                    if (atencionExistente?.TurnosSeguimiento != null && atencionExistente.TurnosSeguimiento.Any())
                    {
                        turnosSeguimiento = atencionExistente.TurnosSeguimiento.Select(t => new TurnoResponseDTO
                        {
                            Id = t.Id,
                            MascotaId = t.MascotaId,
                            MascotaNombre = atencionCreada.Mascota?.Nombre ?? string.Empty,
                            DuenoId = atencionCreada.Mascota?.DuenoId ?? Guid.Empty,
                            DuenoNombreCompleto = atencionCreada.Mascota?.Dueno != null ? $"{atencionCreada.Mascota.Dueno.Nombre} {atencionCreada.Mascota.Dueno.Apellido}".Trim() : string.Empty,
                            VeterinarioId = t.VeterinarioId,
                            VeterinarioNombreCompleto = t.Veterinario?.Username ?? atencionCreada.Veterinario?.Username ?? string.Empty,
                            AtencionMedicaId = t.AtencionMedicaId,
                            FechaHora = t.FechaHora,
                            MotivoConsulta = t.MotivoConsulta,
                            Estado = t.Estado,
                            EsAutomatico = t.EsAutomatico,
                            TipoOrigen = t.TipoOrigen,
                            FechaCreacion = t.FechaCreacion
                        }).ToList();
                    }
                    mensajeSeguimiento = "La atención médica ya contaba con una serie de turnos de seguimiento crónico registrada previamente.";
                }
            }

            await _atencionMedicaRepository.CommitTransactionAsync(transaction);

            return MapToResponseDTO(
                atencionCreada, 
                turnosSeguimiento, 
                fechasPendientesManual, 
                mensajeSeguimiento, 
                mensajeAlertaDesnutricion,
                veterinarioNombre);
        }
        catch
        {
            await _atencionMedicaRepository.RollbackTransactionAsync(transaction);
            throw;
        }
        finally
        {
            if (transaction != null)
            {
                await transaction.DisposeAsync();
            }
        }
    }

    // RN-01: Inmutabilidad de las atenciones médicas (no modificable)
    public Task UpdateAsync(Guid id)
    {
        throw new AtencionInmutableException(id);
    }

    // RN-01: Inmutabilidad de las atenciones médicas (no eliminable)
    public Task DeleteAsync(Guid id)
    {
        throw new AtencionInmutableException(id);
    }

    private static void ValidateCreateDto(AtencionMedicaCreateDTO dto)
    {
        if (dto.MascotaId == Guid.Empty)
            throw new ValidationException("El identificador de la mascota es obligatorio.");
        if (string.IsNullOrWhiteSpace(dto.MotivoConsulta))
            throw new ValidationException("El motivo de consulta es obligatorio.");
        if (dto.MotivoConsulta.Trim().Length > 250)
            throw new ValidationException("El motivo de consulta no puede superar los 250 caracteres.");

        if (string.IsNullOrWhiteSpace(dto.Diagnostico))
            throw new ValidationException("El diagnóstico médico es obligatorio.");
        if (dto.Diagnostico.Trim().Length > 1000)
            throw new ValidationException("El diagnóstico no puede superar los 1000 caracteres.");

        if (string.IsNullOrWhiteSpace(dto.Tratamiento))
            throw new ValidationException("El tratamiento médico es obligatorio.");
        if (dto.Tratamiento.Trim().Length > 1000)
            throw new ValidationException("El tratamiento no puede superar los 1000 caracteres.");

        if (dto.PesoRegistrado < 0.05m || dto.PesoRegistrado > 200.00m)
            throw new ValidationException("El peso registrado de la mascota debe estar entre 0.05 kg y 200 kg.");
        if (decimal.Round(dto.PesoRegistrado, 2) != dto.PesoRegistrado)
            throw new ValidationException("El peso registrado de la mascota admite como máximo 2 decimales.");

        if (string.IsNullOrWhiteSpace(dto.Anamnesis))
            throw new ValidationException("La anamnesis clínica es obligatoria.");
        if (dto.Anamnesis.Trim().Length > 2000)
            throw new ValidationException("La anamnesis no puede superar los 2000 caracteres.");

        if (string.IsNullOrWhiteSpace(dto.ExamenFisico))
            throw new ValidationException("El examen físico es obligatorio.");
        if (dto.ExamenFisico.Trim().Length > 2000)
            throw new ValidationException("El examen físico no puede superar los 2000 caracteres.");

        if (!string.IsNullOrWhiteSpace(dto.Observaciones) && dto.Observaciones.Trim().Length > 500)
            throw new ValidationException("Las observaciones no pueden superar los 500 caracteres.");

        // Validar evaluación de aptitud si fue provista
        if (dto.EvaluacionAptitud != null &&
            !string.IsNullOrWhiteSpace(dto.EvaluacionAptitud.MotivoNoApto) &&
            dto.EvaluacionAptitud.MotivoNoApto.Trim().Length > 500)
        {
            throw new ValidationException("El motivo de no aptitud no puede superar los 500 caracteres.");
        }

        // Validar vacunación si fue provista (VAL-06)
        if (!string.IsNullOrWhiteSpace(dto.VacunaAplicada) && dto.VacunaAplicada.Trim().Length > 100)
            throw new ValidationException("El nombre de la vacuna no puede superar los 100 caracteres.");

        if (dto.Vacunacion != null)
        {
            if (string.IsNullOrWhiteSpace(dto.Vacunacion.Vacuna))
                throw new ValidationException("El nombre de la vacuna es obligatorio.");

            if (dto.Vacunacion.Vacuna.Trim().Length > 100)
                throw new ValidationException("El nombre de la vacuna no puede superar los 100 caracteres.");

            if (!string.IsNullOrWhiteSpace(dto.Vacunacion.Dosis) && dto.Vacunacion.Dosis.Trim().Length > 50)
                throw new ValidationException("La dosis no puede superar los 50 caracteres.");

            if (!string.IsNullOrWhiteSpace(dto.Vacunacion.Lote) && dto.Vacunacion.Lote.Trim().Length > 50)
                throw new ValidationException("El lote no puede superar los 50 caracteres.");

            if (dto.Vacunacion.FechaProximaDosis.HasValue &&
                dto.Vacunacion.FechaProximaDosis.Value.Date <= DateTime.UtcNow.Date)
            {
                throw new ValidationException("La fecha de próxima dosis debe ser posterior a la fecha actual.");
            }
        }

        // Validar procedimiento quirúrgico si fue provisto
        if (dto.ProcedimientoQuirurgico != null)
        {
            if (string.IsNullOrWhiteSpace(dto.ProcedimientoQuirurgico.TipoProcedimiento))
                throw new ValidationException("El tipo de procedimiento quirúrgico y la descripción de la intervención son obligatorios.");
            if (string.IsNullOrWhiteSpace(dto.ProcedimientoQuirurgico.Descripcion))
                throw new ValidationException("El tipo de procedimiento quirúrgico y la descripción de la intervención son obligatorios.");
            if (dto.ProcedimientoQuirurgico.TipoProcedimiento.Trim().Length > 100)
                throw new ValidationException("El tipo de procedimiento no puede superar los 100 caracteres.");
            if (dto.ProcedimientoQuirurgico.Descripcion.Trim().Length > 1000)
                throw new ValidationException("La descripción no puede superar los 1000 caracteres.");
            if (!string.IsNullOrWhiteSpace(dto.ProcedimientoQuirurgico.ObservacionesComplicaciones) &&
                dto.ProcedimientoQuirurgico.ObservacionesComplicaciones.Trim().Length > 500)
            {
                throw new ValidationException("Las observaciones o complicaciones no pueden superar los 500 caracteres.");
            }
        }

        // Validar prescripciones si fueron provistas
        if (dto.Prescripciones != null && dto.Prescripciones.Any())
        {
            foreach (var p in dto.Prescripciones)
            {
                if (string.IsNullOrWhiteSpace(p.Medicamento) ||
                    string.IsNullOrWhiteSpace(p.Dosis) ||
                    string.IsNullOrWhiteSpace(p.Frecuencia) ||
                    string.IsNullOrWhiteSpace(p.Duracion))
                {
                    throw new ValidationException("Los campos de la prescripción médica son obligatorios.");
                }

                if (p.Medicamento.Trim().Length > 150)
                    throw new ValidationException("El medicamento no puede superar los 150 caracteres.");
                if (p.Dosis.Trim().Length > 100)
                    throw new ValidationException("La dosis no puede superar los 100 caracteres.");
                if (p.Frecuencia.Trim().Length > 100)
                    throw new ValidationException("La frecuencia no puede superar los 100 caracteres.");
                if (p.Duracion.Trim().Length > 100)
                    throw new ValidationException("La duración no puede superar los 100 caracteres.");
            }
        }

        // Validar estudios si fueron provistos
        if (dto.Estudios != null && dto.Estudios.Any())
        {
            foreach (var e in dto.Estudios)
            {
                if (string.IsNullOrWhiteSpace(e.Nombre))
                {
                    throw new ValidationException("El nombre del estudio complementario es obligatorio.");
                }

                if (e.Nombre.Trim().Length > 150)
                    throw new ValidationException("El nombre del estudio no puede superar los 150 caracteres.");
                if (!string.IsNullOrWhiteSpace(e.Resultado) && e.Resultado.Trim().Length > 1000)
                    throw new ValidationException("El resultado no puede superar los 1000 caracteres.");
            }
        }

        // Validar coherencia de seguimiento crónico (BUG-06 / CU-04 flujo 2d / RN-11)
        if (dto.EsEnfermedadCronica == true && dto.SeguimientoCronico == null)
        {
            throw new ValidationException("La frecuencia de seguimiento y la jornada preferida son obligatorias para el seguimiento de enfermedad crónica.");
        }

        if (dto.EsEnfermedadCronica == false && dto.SeguimientoCronico != null)
        {
            throw new ValidationException("No se puede especificar un plan de seguimiento crónico cuando se indica que la atención no corresponde a una enfermedad crónica.");
        }

        // Validar seguimiento crónico si fue provisto
        if (dto.SeguimientoCronico != null)
        {
            var frecuenciasValidas = new[] { "Quincenal", "Mensual", "Bimestral", "Trimestral" };
            var jornadasValidas = new[] { "Manana", "Tarde", "Indistinta" };

            var frecuenciaTrimmed = dto.SeguimientoCronico.Frecuencia?.Trim();
            var jornadaTrimmed = dto.SeguimientoCronico.JornadaPreferida?.Trim();

            if (string.IsNullOrWhiteSpace(frecuenciaTrimmed) ||
                !frecuenciasValidas.Contains(frecuenciaTrimmed, StringComparer.OrdinalIgnoreCase) ||
                string.IsNullOrWhiteSpace(jornadaTrimmed) ||
                !jornadasValidas.Contains(jornadaTrimmed, StringComparer.OrdinalIgnoreCase))
            {
                throw new ValidationException("La frecuencia de seguimiento y la jornada preferida son obligatorias para el seguimiento de enfermedad crónica.");
            }
        }
    }

    private AtencionMedicaResponseDTO MapToResponseDTO(
        AtencionMedica atencion,
        List<TurnoResponseDTO>? turnosSeguimiento = null,
        List<DateTime>? fechasPendientesManual = null,
        string? mensajeSeguimiento = null,
        string? mensajeAlertaDesnutricion = null,
        string? veterinarioNombreParam = null)
    {
        var response = new AtencionMedicaResponseDTO
        {
            Id = atencion.Id,
            MascotaId = atencion.MascotaId,
            NombreMascota = atencion.Mascota != null ? atencion.Mascota.Nombre : string.Empty,
            VeterinarioId = atencion.VeterinarioId ?? Guid.Empty,
            NombreVeterinario = veterinarioNombreParam ?? (atencion.Veterinario != null ? atencion.Veterinario.Username : string.Empty),
            Fecha = atencion.Fecha,
            MotivoConsulta = atencion.MotivoConsulta,
            Anamnesis = atencion.Anamnesis,
            ExamenFisico = atencion.ExamenFisico,
            Diagnostico = atencion.Diagnostico,
            Tratamiento = atencion.Tratamiento,
            PesoRegistrado = atencion.PesoRegistrado,
            Observaciones = atencion.Observaciones,

            // Alerta de desnutrición
            AlertaDesnutricion = atencion.AlertaDesnutricion,
            PorcentajeDisminucionPeso = atencion.PorcentajeDisminucionPeso,
            PesoReferenciaUtilizado = atencion.PesoReferenciaUtilizado,
            MensajeAlertaDesnutricion = mensajeAlertaDesnutricion,

            // Vacunación y Aptitud
            EsAptoVacunacion = atencion.EsAptoVacunacion,
            AptitudVacunacionResultado = atencion.AptitudVacunacionResultado,
            MotivoNoAptoVacunacion = atencion.MotivoNoAptoVacunacion,
            VacunaAplicada = atencion.VacunaAplicada,

            // Última visita
            FechaUltimaVisitaMascota = atencion.Mascota?.FechaUltimaVisita
        };

        if (atencion.SinFiebre.HasValue || atencion.BuenEstadoGeneral.HasValue)
        {
            response.EvaluacionAptitud = new EvaluacionAptitudVacunacionResponseDTO
            {
                SinFiebre = atencion.SinFiebre ?? false,
                BuenEstadoGeneral = atencion.BuenEstadoGeneral ?? false,
                SinEnfermedadInfecciosaAguda = atencion.SinEnfermedadInfecciosaAguda ?? false,
                AutorizacionVeterinaria = atencion.AutorizacionVeterinaria ?? false,
                Resultado = atencion.AptitudVacunacionResultado ?? (atencion.EsAptoVacunacion ? "APTO" : "NO_APTO"),
                MotivoNoApto = atencion.MotivoNoAptoVacunacion
            };
        }

        if (!string.IsNullOrWhiteSpace(atencion.VacunaAplicada))
        {
            response.Vacunacion = new VacunacionResponseDTO
            {
                Vacuna = atencion.VacunaAplicada,
                Dosis = atencion.VacunaDosis,
                Lote = atencion.VacunaLote,
                FechaProximaDosis = atencion.VacunaProximaDosis
            };
        }

        if (atencion.ProcedimientoQuirurgico != null)
        {
            response.ProcedimientoQuirurgico = new ProcedimientoQuirurgicoResponseDTO
            {
                Id = atencion.ProcedimientoQuirurgico.Id,
                AtencionMedicaId = atencion.ProcedimientoQuirurgico.AtencionMedicaId,
                TipoProcedimiento = atencion.ProcedimientoQuirurgico.TipoProcedimiento,
                Descripcion = atencion.ProcedimientoQuirurgico.Descripcion,
                Fecha = atencion.ProcedimientoQuirurgico.Fecha,
                VeterinarioId = atencion.ProcedimientoQuirurgico.VeterinarioId,
                VeterinarioNombre = atencion.ProcedimientoQuirurgico.Veterinario?.Username ?? response.NombreVeterinario,
                ObservacionesComplicaciones = atencion.ProcedimientoQuirurgico.ObservacionesComplicaciones
            };
        }

        if (atencion.Prescripciones != null && atencion.Prescripciones.Any())
        {
            foreach (var p in atencion.Prescripciones)
            {
                response.Prescripciones.Add(new PrescripcionResponseDTO
                {
                    Id = p.Id,
                    AtencionMedicaId = p.AtencionMedicaId,
                    Medicamento = p.Medicamento,
                    Dosis = p.Dosis,
                    Frecuencia = p.Frecuencia,
                    Duracion = p.Duracion
                });
            }
        }

        if (atencion.EstudiosComplementarios != null && atencion.EstudiosComplementarios.Any())
        {
            foreach (var e in atencion.EstudiosComplementarios)
            {
                response.Estudios.Add(new EstudioComplementarioResponseDTO
                {
                    Id = e.Id,
                    AtencionMedicaId = e.AtencionMedicaId,
                    Nombre = e.Nombre,
                    Resultado = e.Resultado
                });
            }
        }

        if (turnosSeguimiento != null && turnosSeguimiento.Any())
        {
            response.TurnosSeguimiento = turnosSeguimiento;
        }
        else if (atencion.TurnosSeguimiento != null && atencion.TurnosSeguimiento.Any())
        {
            foreach (var t in atencion.TurnosSeguimiento)
            {
                response.TurnosSeguimiento.Add(new TurnoResponseDTO
                {
                    Id = t.Id,
                    MascotaId = t.MascotaId,
                    MascotaNombre = atencion.Mascota?.Nombre ?? string.Empty,
                    DuenoId = atencion.Mascota?.DuenoId ?? Guid.Empty,
                    DuenoNombreCompleto = atencion.Mascota?.Dueno != null ? $"{atencion.Mascota.Dueno.Nombre} {atencion.Mascota.Dueno.Apellido}" : string.Empty,
                    VeterinarioId = t.VeterinarioId,
                    VeterinarioNombreCompleto = t.Veterinario?.Username ?? atencion.Veterinario?.Username ?? string.Empty,
                    AtencionMedicaId = t.AtencionMedicaId,
                    FechaHora = t.FechaHora,
                    MotivoConsulta = t.MotivoConsulta,
                    Estado = t.Estado,
                    EsAutomatico = t.EsAutomatico,
                    TipoOrigen = t.TipoOrigen,
                    FechaCreacion = t.FechaCreacion
                });
            }
        }

        if (fechasPendientesManual != null)
        {
            response.FechasPendientesManual = fechasPendientesManual;
        }

        response.MensajeSeguimientoCronico = mensajeSeguimiento;

        return response;
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
                break;
        }

        return fechas;
    }
}
