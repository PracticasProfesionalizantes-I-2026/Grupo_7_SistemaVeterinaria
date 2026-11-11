using DataAccess.Context;
using DataAccess.Entities;
using Microsoft.EntityFrameworkCore;
using Shared.Exceptions;

namespace DataAccess.Repositories;

public class TurnoRepository : ITurnoRepository
{
    private readonly VeterinariaDbContext _context;

    public TurnoRepository(VeterinariaDbContext context)
    {
        _context = context;
    }

    public async Task<Turno?> GetByIdAsync(Guid id)
    {
        return await _context.Turnos
            .AsNoTracking()
            .Include(t => t.Mascota)
                .ThenInclude(m => m.Dueno)
            .Include(t => t.Veterinario)
            .FirstOrDefaultAsync(t => t.Id == id);
    }

    public async Task<Turno?> GetByIdTrackedAsync(Guid id)
    {
        return await _context.Turnos
            .Include(t => t.Mascota)
                .ThenInclude(m => m.Dueno)
            .Include(t => t.Veterinario)
            .FirstOrDefaultAsync(t => t.Id == id);
    }

    public async Task<IEnumerable<Turno>> GetAgendaAsync(DateTime? fechaDesde, DateTime? fechaHasta, Guid? veterinarioId, string? estado)
    {
        var query = _context.Turnos
            .AsNoTracking()
            .Include(t => t.Mascota)
                .ThenInclude(m => m.Dueno)
            .Include(t => t.Veterinario)
            .AsQueryable();

        if (fechaDesde.HasValue)
        {
            query = query.Where(t => t.FechaHora >= fechaDesde.Value);
        }

        if (fechaHasta.HasValue)
        {
            query = query.Where(t => t.FechaHora <= fechaHasta.Value);
        }

        if (veterinarioId.HasValue && veterinarioId.Value != Guid.Empty)
        {
            query = query.Where(t => t.VeterinarioId == veterinarioId.Value);
        }

        if (!string.IsNullOrWhiteSpace(estado))
        {
            var estadoNormalizado = estado.Trim();
            query = query.Where(t => t.Estado == estadoNormalizado);
        }

        return await query.OrderBy(t => t.FechaHora).ToListAsync();
    }

    public async Task<bool> ExisteSuperposicionAsync(Guid veterinarioId, DateTime fechaHora, Guid? turnoIdExcluir = null)
    {
        // En bloques de 30 minutos: intervalo [fechaHora, fechaHora + 30m).
        // Se superpone únicamente con turnos que bloquean el horario: Confirmado o Finalizado.
        // Los turnos Pendiente no bloquean el horario; los turnos Cancelado liberan el horario.
        var inicioVentana = fechaHora.AddMinutes(-30);
        var finVentana = fechaHora.AddMinutes(30);

        var query = _context.Turnos
            .AsNoTracking()
            .Where(t => t.VeterinarioId == veterinarioId && (t.Estado == "Confirmado" || t.Estado == "Finalizado"));

        if (turnoIdExcluir.HasValue && turnoIdExcluir.Value != Guid.Empty)
        {
            query = query.Where(t => t.Id != turnoIdExcluir.Value);
        }

        return await query.AnyAsync(t => t.FechaHora > inicioVentana && t.FechaHora < finVentana);
    }

    public async Task<bool> ExisteSeguimientoParaAtencionAsync(Guid atencionMedicaId)
    {
        return await _context.Turnos
            .AsNoTracking()
            .AnyAsync(t => t.AtencionMedicaId == atencionMedicaId && t.Estado != "Cancelado");
    }

    public async Task<Turno> CreateAsync(Turno turno)
    {
        if (turno.Id == Guid.Empty)
        {
            turno.Id = Guid.NewGuid();
        }
        turno.FechaCreacion = DateTime.UtcNow;

        _context.Turnos.Add(turno);
        await _context.SaveChangesAsync();
        return turno;
    }

    public async Task<Turno> UpdateAsync(Turno turno)
    {
        _context.Turnos.Update(turno);
        await _context.SaveChangesAsync();
        return turno;
    }

    public async Task CreateRangeAsync(IEnumerable<Turno> turnos)
    {
        foreach (var turno in turnos)
        {
            if (turno.Id == Guid.Empty)
            {
                turno.Id = Guid.NewGuid();
            }
            turno.FechaCreacion = DateTime.UtcNow;
            turno.Mascota = null!;
            turno.Veterinario = null!;
            turno.AtencionMedica = null;
        }

        await _context.Turnos.AddRangeAsync(turnos);
        await _context.SaveChangesAsync();
    }

    public async Task ReloadAsync(Turno turno)
    {
        var entry = _context.Entry(turno);
        if (entry.State == EntityState.Detached)
        {
            _context.Turnos.Attach(turno);
        }

        await entry.ReloadAsync();

        if (entry.State == EntityState.Detached)
        {
            throw new TurnoNotFoundException(turno.Id);
        }

        if (turno.Mascota == null && turno.MascotaId != Guid.Empty)
        {
            await entry.Reference(t => t.Mascota).LoadAsync();
        }

        if (turno.Mascota != null && turno.Mascota.Dueno == null && turno.Mascota.DuenoId != Guid.Empty)
        {
            await _context.Entry(turno.Mascota).Reference(m => m.Dueno).LoadAsync();
        }

        if (turno.Veterinario == null && turno.VeterinarioId != Guid.Empty)
        {
            await entry.Reference(t => t.Veterinario).LoadAsync();
        }
    }
}
