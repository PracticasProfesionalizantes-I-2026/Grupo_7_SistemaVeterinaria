using System.Text.Json;
using BusinessLogic.Interfaces;
using DataAccess.Entities;
using DataAccess.Repositories;
using Shared.DTOs.Reporte;
using Shared.Exceptions;

namespace BusinessLogic.Services;

public class ReporteService : IReporteService
{
    private readonly IReporteRepository _reporteRepository;
    private readonly IAtencionMedicaRepository _atencionMedicaRepository;
    private readonly IMascotaRepository _mascotaRepository;
    private readonly IDuenoRepository _duenoRepository;
    private readonly ITurnoRepository _turnoRepository;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public ReporteService(
        IReporteRepository reporteRepository,
        IAtencionMedicaRepository atencionMedicaRepository,
        IMascotaRepository mascotaRepository,
        IDuenoRepository duenoRepository,
        ITurnoRepository turnoRepository)
    {
        _reporteRepository = reporteRepository;
        _atencionMedicaRepository = atencionMedicaRepository;
        _mascotaRepository = mascotaRepository;
        _duenoRepository = duenoRepository;
        _turnoRepository = turnoRepository;
    }

    public async Task<ReporteResponseDTO> GenerarReporteAsync(GenerarReporteRequestDTO dto, Guid usuarioId, string username)
    {
        // 1. Validaciones de campos obligatorios
        if (string.IsNullOrWhiteSpace(dto.Motivo) || dto.TiposReporte == null || !dto.TiposReporte.Any())
        {
            throw new ValidationException("Debe especificar el motivo y al menos un tipo de reporte a generar.");
        }

        // 2. Validación lógica de rango de fechas
        if (dto.FechaDesde > dto.FechaHasta)
        {
            throw new RangoFechasInvalidoException("La fecha inicial no puede ser posterior a la fecha final.");
        }

        var desde = dto.FechaDesde;
        var hasta = dto.FechaHasta.TimeOfDay == TimeSpan.Zero
            ? dto.FechaHasta.Date.AddDays(1).AddTicks(-1)
            : dto.FechaHasta;

        var tiposNormalizados = dto.TiposReporte
            .Select(t => t.Trim())
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (!tiposNormalizados.Any())
        {
            throw new ValidationException("Debe especificar el motivo y al menos un tipo de reporte a generar.");
        }

        var detalle = new ReporteDetalleDTO();
        int totalAtenciones = 0;
        int totalVacunaciones = 0;
        int totalMascotas = 0;
        int totalDuenos = 0;
        int totalTurnos = 0;

        // Atenciones y Vacunaciones comparten origen si ambos o cualquiera fue solicitado
        var incluyeAtenciones = tiposNormalizados.Any(t => t.Equals("Atenciones", StringComparison.OrdinalIgnoreCase));
        var incluyeVacunaciones = tiposNormalizados.Any(t => t.Equals("Vacunaciones", StringComparison.OrdinalIgnoreCase));

        List<AtencionMedica>? atencionesEnPeriodo = null;
        if (incluyeAtenciones || incluyeVacunaciones)
        {
            atencionesEnPeriodo = (await _atencionMedicaRepository.GetByRangoFechasAsync(desde, hasta)).ToList();
        }

        // 3. Cálculo módulo Atenciones
        if (incluyeAtenciones && atencionesEnPeriodo != null)
        {
            totalAtenciones = atencionesEnPeriodo.Count;
            detalle.Atenciones = new MetricasAtencionesDTO
            {
                TotalAtenciones = totalAtenciones,
                TotalCirugias = atencionesEnPeriodo.Count(a => a.ProcedimientoQuirurgico != null),
                TotalAlertasDesnutricion = atencionesEnPeriodo.Count(a => a.AlertaDesnutricion),
                TotalConPrescripciones = atencionesEnPeriodo.Count(a => a.Prescripciones != null && a.Prescripciones.Any()),
                TotalConEstudios = atencionesEnPeriodo.Count(a => a.EstudiosComplementarios != null && a.EstudiosComplementarios.Any()),
                DiagnosticosFrecuentes = atencionesEnPeriodo
                    .Where(a => !string.IsNullOrWhiteSpace(a.Diagnostico))
                    .GroupBy(a => a.Diagnostico.Trim())
                    .OrderByDescending(g => g.Count())
                    .Take(10)
                    .ToDictionary(g => g.Key, g => g.Count()),
                AtencionesPorVeterinario = atencionesEnPeriodo
                    .GroupBy(a => a.Veterinario?.Username ?? "Sin asignar")
                    .ToDictionary(g => g.Key, g => g.Count())
            };
        }

        // 4. Cálculo módulo Vacunaciones
        if (incluyeVacunaciones && atencionesEnPeriodo != null)
        {
            var atencionesConVacuna = atencionesEnPeriodo
                .Where(a => !string.IsNullOrWhiteSpace(a.VacunaAplicada))
                .ToList();

            totalVacunaciones = atencionesConVacuna.Count;
            detalle.Vacunaciones = new MetricasVacunacionesDTO
            {
                TotalVacunasAplicadas = totalVacunaciones,
                TotalEvaluacionesApto = atencionesEnPeriodo.Count(a => a.EsAptoVacunacion),
                TotalEvaluacionesNoApto = atencionesEnPeriodo.Count(a => !a.EsAptoVacunacion && !string.IsNullOrWhiteSpace(a.MotivoNoAptoVacunacion)),
                VacunasPorTipo = atencionesConVacuna
                    .GroupBy(a => a.VacunaAplicada!.Trim())
                    .ToDictionary(g => g.Key, g => g.Count())
            };
        }

        // 5. Cálculo módulo Mascotas
        if (tiposNormalizados.Any(t => t.Equals("Mascotas", StringComparison.OrdinalIgnoreCase)))
        {
            var mascotas = (await _mascotaRepository.GetAllAsync()).ToList();
            totalMascotas = mascotas.Count;

            detalle.Mascotas = new MetricasMascotasDTO
            {
                TotalMascotasRegistradas = totalMascotas,
                TotalActivas = mascotas.Count(m => m.Activa),
                TotalInactivas = mascotas.Count(m => !m.Activa),
                DistribucionPorEspecie = mascotas
                    .GroupBy(m => string.IsNullOrWhiteSpace(m.Especie) ? "Sin especificar" : m.Especie.Trim())
                    .ToDictionary(g => g.Key, g => g.Count()),
                DistribucionPorSexo = mascotas
                    .GroupBy(m => string.IsNullOrWhiteSpace(m.Sexo) ? "Sin especificar" : m.Sexo.Trim())
                    .ToDictionary(g => g.Key, g => g.Count())
            };
        }

        // 6. Cálculo módulo Dueños
        if (tiposNormalizados.Any(t => t.Equals("Duenos", StringComparison.OrdinalIgnoreCase)))
        {
            var duenos = (await _duenoRepository.GetByRangoFechasAsync(desde, hasta)).ToList();
            totalDuenos = duenos.Count;

            detalle.Duenos = new MetricasDuenosDTO
            {
                TotalDuenosRegistrados = totalDuenos,
                DuenosConMascotas = duenos.Count(d => d.Mascotas != null && d.Mascotas.Any()),
                DuenosSinMascotas = duenos.Count(d => d.Mascotas == null || !d.Mascotas.Any())
            };
        }

        // 7. Cálculo módulo Turnos
        if (tiposNormalizados.Any(t => t.Equals("Turnos", StringComparison.OrdinalIgnoreCase)))
        {
            var turnos = (await _turnoRepository.GetAgendaAsync(desde, hasta, null, null)).ToList();
            totalTurnos = turnos.Count;

            var turnosCancelados = turnos.Where(t => t.Estado == "Cancelado").ToList();

            detalle.Turnos = new MetricasTurnosDTO
            {
                TotalTurnos = totalTurnos,
                TurnosConfirmados = turnos.Count(t => t.Estado == "Confirmado"),
                TurnosFinalizados = turnos.Count(t => t.Estado == "Finalizado"),
                TurnosPendientes = turnos.Count(t => t.Estado == "Pendiente"),
                TurnosCancelados = turnosCancelados.Count,
                TurnosAutomaticosCronicos = turnos.Count(t => t.EsAutomatico || t.TipoOrigen == "SeguimientoCronico"),
                TurnosManuales = turnos.Count(t => !t.EsAutomatico && t.TipoOrigen != "SeguimientoCronico"),
                CancelacionesPorMotivo = turnosCancelados
                    .Where(t => !string.IsNullOrWhiteSpace(t.MotivoCancelacion))
                    .GroupBy(t => t.MotivoCancelacion!.Trim())
                    .ToDictionary(g => g.Key, g => g.Count()),
                DetalleCancelaciones = turnosCancelados.Select(t => new TurnoCanceladoAuditoriaDTO
                {
                    TurnoId = t.Id,
                    FechaHoraOriginal = t.FechaHora,
                    MascotaNombre = t.Mascota?.Nombre ?? string.Empty,
                    DuenoNombre = t.Mascota?.Dueno != null ? $"{t.Mascota.Dueno.Nombre} {t.Mascota.Dueno.Apellido}".Trim() : string.Empty,
                    VeterinarioNombre = t.Veterinario?.Username ?? string.Empty,
                    MotivoCancelacion = t.MotivoCancelacion ?? string.Empty,
                    FechaCancelacion = t.FechaCancelacion,
                    UsuarioCancelacionUsername = t.UsuarioCancelacionUsername
                }).ToList()
            };
        }

        // 8. Persistencia histórica inmutable del reporte consolidado
        var nuevoReporte = new Reporte
        {
            Motivo = dto.Motivo.Trim(),
            FechaDesde = dto.FechaDesde,
            FechaHasta = dto.FechaHasta,
            TiposReporteJson = JsonSerializer.Serialize(tiposNormalizados),
            FechaGeneracion = DateTime.UtcNow,
            UsuarioGeneradorId = usuarioId,
            UsuarioGeneradorUsername = username,
            TotalAtenciones = totalAtenciones,
            TotalVacunaciones = totalVacunaciones,
            TotalMascotas = totalMascotas,
            TotalDuenos = totalDuenos,
            TotalTurnos = totalTurnos,
            DatosConsolidadosJson = JsonSerializer.Serialize(detalle)
        };

        var reporteGuardado = await _reporteRepository.CreateAsync(nuevoReporte);

        return new ReporteResponseDTO
        {
            Id = reporteGuardado.Id,
            Motivo = reporteGuardado.Motivo,
            FechaDesde = reporteGuardado.FechaDesde,
            FechaHasta = reporteGuardado.FechaHasta,
            TiposReporte = tiposNormalizados,
            FechaGeneracion = reporteGuardado.FechaGeneracion,
            UsuarioGeneradorId = reporteGuardado.UsuarioGeneradorId,
            UsuarioGeneradorUsername = reporteGuardado.UsuarioGeneradorUsername,
            TotalAtenciones = reporteGuardado.TotalAtenciones,
            TotalVacunaciones = reporteGuardado.TotalVacunaciones,
            TotalMascotas = reporteGuardado.TotalMascotas,
            TotalDuenos = reporteGuardado.TotalDuenos,
            TotalTurnos = reporteGuardado.TotalTurnos,
            DatosReporte = detalle
        };
    }

    public async Task<IEnumerable<ReporteResumenDTO>> GetAllReportesAsync()
    {
        var reportes = await _reporteRepository.GetAllAsync();
        return reportes.Select(r => new ReporteResumenDTO
        {
            Id = r.Id,
            Motivo = r.Motivo,
            FechaDesde = r.FechaDesde,
            FechaHasta = r.FechaHasta,
            TiposReporte = DeserializarTipos(r.TiposReporteJson),
            FechaGeneracion = r.FechaGeneracion,
            UsuarioGeneradorId = r.UsuarioGeneradorId,
            UsuarioGeneradorUsername = r.UsuarioGeneradorUsername,
            TotalAtenciones = r.TotalAtenciones,
            TotalVacunaciones = r.TotalVacunaciones,
            TotalMascotas = r.TotalMascotas,
            TotalDuenos = r.TotalDuenos,
            TotalTurnos = r.TotalTurnos
        }).ToList();
    }

    public async Task<ReporteResponseDTO> GetReporteByIdAsync(Guid id)
    {
        var reporte = await _reporteRepository.GetByIdAsync(id);
        if (reporte == null)
        {
            throw new ReporteNotFoundException(id);
        }

        var detalle = string.IsNullOrWhiteSpace(reporte.DatosConsolidadosJson)
            ? new ReporteDetalleDTO()
            : JsonSerializer.Deserialize<ReporteDetalleDTO>(reporte.DatosConsolidadosJson, JsonOptions) ?? new ReporteDetalleDTO();

        return new ReporteResponseDTO
        {
            Id = reporte.Id,
            Motivo = reporte.Motivo,
            FechaDesde = reporte.FechaDesde,
            FechaHasta = reporte.FechaHasta,
            TiposReporte = DeserializarTipos(reporte.TiposReporteJson),
            FechaGeneracion = reporte.FechaGeneracion,
            UsuarioGeneradorId = reporte.UsuarioGeneradorId,
            UsuarioGeneradorUsername = reporte.UsuarioGeneradorUsername,
            TotalAtenciones = reporte.TotalAtenciones,
            TotalVacunaciones = reporte.TotalVacunaciones,
            TotalMascotas = reporte.TotalMascotas,
            TotalDuenos = reporte.TotalDuenos,
            TotalTurnos = reporte.TotalTurnos,
            DatosReporte = detalle
        };
    }

    private static List<string> DeserializarTipos(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return new List<string>();
        }

        try
        {
            return JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();
        }
        catch
        {
            return new List<string>();
        }
    }
}
