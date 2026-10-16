using BusinessLogic.Interfaces;
using DataAccess.Entities;
using DataAccess.Repositories;
using Shared.DTOs.AtencionMedica;
using Shared.DTOs.HistoriaClinica;
using Shared.DTOs.Turno;
using Shared.Exceptions;

namespace BusinessLogic.Services;

public class HistoriaClinicaService : IHistoriaClinicaService
{
    private readonly IMascotaRepository _mascotaRepository;
    private readonly IAtencionMedicaRepository _atencionMedicaRepository;

    public HistoriaClinicaService(
        IMascotaRepository mascotaRepository,
        IAtencionMedicaRepository atencionMedicaRepository)
    {
        _mascotaRepository = mascotaRepository;
        _atencionMedicaRepository = atencionMedicaRepository;
    }

    public async Task<HistoriaClinicaResponseDTO> GetHistoriaClinicaByMascotaIdAsync(Guid mascotaId)
    {
        // 1. Verificar existencia de la mascota (RN-07: Inactiva igual se consulta)
        var mascota = await _mascotaRepository.GetByIdAsync(mascotaId);
        if (mascota == null)
        {
            throw new MascotaNotFoundException(mascotaId);
        }

        // 2. Obtener todas las atenciones médicas ordenadas cronológicamente (RN-01, RN-02)
        var atenciones = (await _atencionMedicaRepository.GetByMascotaIdAsync(mascotaId)).ToList();

        // 3. Determinar la fecha de última visita efectivamente más reciente (INF-07)
        DateTime? fechaUltimaVisita = null;
        if (atenciones.Any())
        {
            var fechaAtencionMasReciente = atenciones.Max(a => a.Fecha);
            fechaUltimaVisita = mascota.FechaUltimaVisita.HasValue && mascota.FechaUltimaVisita.Value > fechaAtencionMasReciente
                ? mascota.FechaUltimaVisita.Value
                : fechaAtencionMasReciente;
        }
        else if (mascota.FechaUltimaVisita.HasValue)
        {
            fechaUltimaVisita = mascota.FechaUltimaVisita.Value;
        }

        var ultimaVisitaVisualizada = fechaUltimaVisita.HasValue
            ? fechaUltimaVisita.Value.ToString("yyyy-MM-dd HH:mm")
            : "Sin visitas registradas";

        // 4. Mapear atenciones con sus secciones clínicas integradas (cirugías, prescripciones, estudios, vacunas)
        var atencionesDto = atenciones.Select(MapAtencionToResponseDTO).ToList();

        // 5. Métricas y resumen clínico
        var totalCirugias = atenciones.Count(a => a.ProcedimientoQuirurgico != null);
        var totalVacunas = atenciones.Count(a => !string.IsNullOrWhiteSpace(a.VacunaAplicada));

        // 6. Mensaje informativo (Flujo 3b si no tiene atenciones previas)
        string? mensajeInformativo = atenciones.Any()
            ? null
            : "Sin atenciones médicas registradas a la fecha.";

        return new HistoriaClinicaResponseDTO
        {
            MascotaId = mascota.Id,
            NombreMascota = mascota.Nombre,
            Especie = mascota.Especie,
            Raza = mascota.Raza,
            Sexo = mascota.Sexo,
            FechaNacimiento = mascota.FechaNacimiento,
            Edad = CalcularEdad(mascota.FechaNacimiento),
            PesoActual = mascota.Peso,
            Activa = mascota.Activa,
            ObservacionesMascota = mascota.Observaciones,

            FechaUltimaVisita = fechaUltimaVisita,
            UltimaVisitaVisualizada = ultimaVisitaVisualizada,

            DuenoId = mascota.DuenoId,
            DuenoNombreCompleto = mascota.Dueno != null ? $"{mascota.Dueno.Nombre} {mascota.Dueno.Apellido}".Trim() : string.Empty,
            DuenoDni = mascota.Dueno?.Dni ?? string.Empty,
            DuenoTelefono = mascota.Dueno?.Telefono ?? string.Empty,
            DuenoEmail = mascota.Dueno?.Email ?? string.Empty,

            AtencionesMedicas = atencionesDto,
            TotalAtenciones = atenciones.Count,
            TotalCirugias = totalCirugias,
            TotalVacunasAplicadas = totalVacunas,
            MensajeInformativo = mensajeInformativo
        };
    }

    public async Task<IEnumerable<HistoriaClinicaBusquedaDTO>> BuscarHistoriasClinicasAsync(string? criterio)
    {
        var mascotas = await _mascotaRepository.BuscarAsync(criterio?.Trim() ?? string.Empty);

        var resultados = new List<HistoriaClinicaBusquedaDTO>();
        foreach (var m in mascotas)
        {
            var ultimaVisita = m.FechaUltimaVisita.HasValue
                ? m.FechaUltimaVisita.Value.ToString("yyyy-MM-dd HH:mm")
                : "Sin visitas registradas";

            var totalAtenciones = m.AtencionesMedicas != null ? m.AtencionesMedicas.Count : 0;

            resultados.Add(new HistoriaClinicaBusquedaDTO
            {
                MascotaId = m.Id,
                NombreMascota = m.Nombre,
                Especie = m.Especie,
                Raza = m.Raza,
                Activa = m.Activa,
                DuenoId = m.DuenoId,
                DuenoNombreCompleto = m.Dueno != null ? $"{m.Dueno.Nombre} {m.Dueno.Apellido}".Trim() : string.Empty,
                DuenoDni = m.Dueno?.Dni ?? string.Empty,
                FechaUltimaVisita = m.FechaUltimaVisita,
                UltimaVisitaVisualizada = ultimaVisita,
                TotalAtenciones = totalAtenciones
            });
        }

        return resultados;
    }

    private static string CalcularEdad(DateTime fechaNacimiento)
    {
        var hoy = DateTime.UtcNow.Date;
        var anios = hoy.Year - fechaNacimiento.Year;
        var meses = hoy.Month - fechaNacimiento.Month;

        if (hoy.Day < fechaNacimiento.Day)
        {
            meses--;
        }

        if (meses < 0)
        {
            anios--;
            meses += 12;
        }

        if (anios > 0)
        {
            return meses > 0
                ? $"{anios} {(anios == 1 ? "año" : "años")} y {meses} {(meses == 1 ? "mes" : "meses")}"
                : $"{anios} {(anios == 1 ? "año" : "años")}";
        }

        return $"{meses} {(meses == 1 ? "mes" : "meses")}";
    }

    private static AtencionMedicaResponseDTO MapAtencionToResponseDTO(AtencionMedica atencion)
    {
        var response = new AtencionMedicaResponseDTO
        {
            Id = atencion.Id,
            MascotaId = atencion.MascotaId,
            NombreMascota = atencion.Mascota?.Nombre ?? string.Empty,
            VeterinarioId = atencion.VeterinarioId ?? Guid.Empty,
            NombreVeterinario = atencion.Veterinario?.Username ?? string.Empty,
            Fecha = atencion.Fecha,
            MotivoConsulta = atencion.MotivoConsulta,
            Anamnesis = atencion.Anamnesis,
            ExamenFisico = atencion.ExamenFisico,
            Diagnostico = atencion.Diagnostico,
            Tratamiento = atencion.Tratamiento,
            PesoRegistrado = atencion.PesoRegistrado,
            Observaciones = atencion.Observaciones,

            // Alerta de desnutrición (RN-12)
            AlertaDesnutricion = atencion.AlertaDesnutricion,
            PorcentajeDisminucionPeso = atencion.PorcentajeDisminucionPeso,
            PesoReferenciaUtilizado = atencion.PesoReferenciaUtilizado,

            // Aptitud y vacunación (RN-03, RN-04)
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

        // Procedimiento quirúrgico (INF-06)
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

        // Prescripciones farmacológicas (RN-02)
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

        // Estudios complementarios (RN-02)
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

        // Turnos de seguimiento crónico si existen
        if (atencion.TurnosSeguimiento != null && atencion.TurnosSeguimiento.Any())
        {
            foreach (var t in atencion.TurnosSeguimiento)
            {
                response.TurnosSeguimiento.Add(new TurnoResponseDTO
                {
                    Id = t.Id,
                    MascotaId = t.MascotaId,
                    MascotaNombre = atencion.Mascota?.Nombre ?? string.Empty,
                    DuenoId = atencion.Mascota?.DuenoId ?? Guid.Empty,
                    DuenoNombreCompleto = atencion.Mascota?.Dueno != null ? $"{atencion.Mascota.Dueno.Nombre} {atencion.Mascota.Dueno.Apellido}".Trim() : string.Empty,
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

        return response;
    }
}
