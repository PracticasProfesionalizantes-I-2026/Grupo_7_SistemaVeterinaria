namespace DataAccess.Entities;

public class Mascota
{
    public Guid Id { get; set; }
    public Guid DuenoId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Especie { get; set; } = string.Empty;
    public string Raza { get; set; } = string.Empty;
    public string Sexo { get; set; } = string.Empty;
    public DateTime FechaNacimiento { get; set; }
    public decimal Peso { get; set; }
    public bool Activa { get; set; } = true;
    public DateTime? FechaUltimaVisita { get; set; }

    /// <summary>
    /// Fecha de alta en el sistema (UTC). Es inmutable y se asigna explícitamente al registrar.
    /// NULL en las mascotas históricas anteriores a esta columna: su alta es desconocida
    /// y no se contabilizan en reportes de altas por período.
    /// </summary>
    public DateTime? FechaRegistro { get; set; }
    public string? Observaciones { get; set; }

    // Navegación
    public Dueno Dueno { get; set; } = null!;
    public ICollection<AtencionMedica> AtencionesMedicas { get; set; } = new List<AtencionMedica>();
    public ICollection<HistorialPropietario> HistorialesPropietarios { get; set; } = new List<HistorialPropietario>();
}

