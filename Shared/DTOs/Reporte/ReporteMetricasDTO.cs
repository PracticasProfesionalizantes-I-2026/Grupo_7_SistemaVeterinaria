namespace Shared.DTOs.Reporte;

public class ReporteDetalleDTO
{
    public MetricasAtencionesDTO? Atenciones { get; set; }
    public MetricasVacunacionesDTO? Vacunaciones { get; set; }
    public MetricasMascotasDTO? Mascotas { get; set; }
    public MetricasDuenosDTO? Duenos { get; set; }
    public MetricasTurnosDTO? Turnos { get; set; }
}

public class MetricasAtencionesDTO
{
    public int TotalAtenciones { get; set; }
    public int TotalCirugias { get; set; }
    public int TotalAlertasDesnutricion { get; set; }
    public int TotalConPrescripciones { get; set; }
    public int TotalConEstudios { get; set; }
    public Dictionary<string, int> DiagnosticosFrecuentes { get; set; } = new();
    public Dictionary<string, int> AtencionesPorVeterinario { get; set; } = new();
}

public class MetricasVacunacionesDTO
{
    public int TotalVacunasAplicadas { get; set; }
    public int TotalEvaluacionesApto { get; set; }
    public int TotalEvaluacionesNoApto { get; set; }
    public Dictionary<string, int> VacunasPorTipo { get; set; } = new();
}

public class MetricasMascotasDTO
{
    public int TotalMascotasRegistradas { get; set; }
    public int TotalActivas { get; set; }
    public int TotalInactivas { get; set; }
    public Dictionary<string, int> DistribucionPorEspecie { get; set; } = new();
    public Dictionary<string, int> DistribucionPorSexo { get; set; } = new();
}

public class MetricasDuenosDTO
{
    public int TotalDuenosRegistrados { get; set; }
    public int DuenosConMascotas { get; set; }
    public int DuenosSinMascotas { get; set; }
}

public class MetricasTurnosDTO
{
    public int TotalTurnos { get; set; }
    public int TurnosConfirmados { get; set; }
    public int TurnosFinalizados { get; set; }
    public int TurnosPendientes { get; set; }
    public int TurnosCancelados { get; set; }
    public int TurnosAutomaticosCronicos { get; set; }
    public int TurnosManuales { get; set; }
    public Dictionary<string, int> CancelacionesPorMotivo { get; set; } = new();
    public List<TurnoCanceladoAuditoriaDTO> DetalleCancelaciones { get; set; } = new();
}

public class TurnoCanceladoAuditoriaDTO
{
    public Guid TurnoId { get; set; }
    public DateTime FechaHoraOriginal { get; set; }
    public string MascotaNombre { get; set; } = string.Empty;
    public string DuenoNombre { get; set; } = string.Empty;
    public string VeterinarioNombre { get; set; } = string.Empty;
    public string MotivoCancelacion { get; set; } = string.Empty;
    public DateTime? FechaCancelacion { get; set; }
    public string? UsuarioCancelacionUsername { get; set; }
}
