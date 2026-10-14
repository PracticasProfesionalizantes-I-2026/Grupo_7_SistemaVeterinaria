using DataAccess.Context;
using DataAccess.Entities;
using Microsoft.EntityFrameworkCore;

namespace DataAccess.Repositories;

public class DisponibilidadVeterinarioRepository : IDisponibilidadVeterinarioRepository
{
    private readonly VeterinariaDbContext _context;

    public DisponibilidadVeterinarioRepository(VeterinariaDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<DisponibilidadVeterinario>> GetByVeterinarioIdAsync(Guid veterinarioId)
    {
        var disponibilidades = await _context.DisponibilidadesVeterinarios
            .AsNoTracking()
            .Where(d => d.VeterinarioId == veterinarioId && d.Activo)
            .ToListAsync();

        return disponibilidades
            .OrderBy(d => d.DiaSemana)
            .ThenBy(d => d.Jornada)
            .ThenBy(d => d.HoraInicio)
            .ToList();
    }

    public async Task<IEnumerable<DisponibilidadVeterinario>> GetByVeterinarioAndDiaAsync(Guid veterinarioId, DayOfWeek diaSemana)
    {
        var disponibilidades = await _context.DisponibilidadesVeterinarios
            .AsNoTracking()
            .Where(d => d.VeterinarioId == veterinarioId && d.DiaSemana == diaSemana && d.Activo)
            .ToListAsync();

        return disponibilidades
            .OrderBy(d => d.Jornada)
            .ThenBy(d => d.HoraInicio)
            .ToList();
    }

    public async Task SetDisponibilidadesAsync(Guid veterinarioId, IEnumerable<DisponibilidadVeterinario> disponibilidades)
    {
        var existentes = await _context.DisponibilidadesVeterinarios
            .Where(d => d.VeterinarioId == veterinarioId)
            .ToListAsync();

        _context.DisponibilidadesVeterinarios.RemoveRange(existentes);

        foreach (var item in disponibilidades)
        {
            if (item.Id == Guid.Empty)
            {
                item.Id = Guid.NewGuid();
            }
            item.VeterinarioId = veterinarioId;
            item.Activo = true;
        }

        await _context.DisponibilidadesVeterinarios.AddRangeAsync(disponibilidades);
        await _context.SaveChangesAsync();
    }

    public async Task<bool> EstaDentroDeDisponibilidadAsync(Guid veterinarioId, DateTime fechaHora, int duracionMinutos = 30)
    {
        var diaSemana = fechaHora.DayOfWeek;
        var horaInicioTurno = fechaHora.TimeOfDay;
        var horaFinTurno = horaInicioTurno.Add(TimeSpan.FromMinutes(duracionMinutos));

        var franjas = await _context.DisponibilidadesVeterinarios
            .AsNoTracking()
            .Where(d => d.VeterinarioId == veterinarioId && d.DiaSemana == diaSemana && d.Activo)
            .ToListAsync();

        return franjas.Any(f => horaInicioTurno >= f.HoraInicio && horaFinTurno <= f.HoraFin);
    }
}
