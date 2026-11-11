using System.Collections.Concurrent;

namespace BusinessLogic.Services;

/// <summary>
/// Coordinador de sincronización en memoria por profesional para operaciones críticas de agenda.
/// Garantiza exclusión mutua por veterinario en confirmación, creación manual,
/// reprogramación y cancelación de turnos, eliminando condiciones de carrera
/// ("Check-Then-Act") en entornos multihilo dentro de una misma instancia de proceso.
/// </summary>
public static class VeterinarioLockManager
{
    private static readonly ConcurrentDictionary<Guid, SemaphoreSlim> Locks = new();

    /// <summary>
    /// Adquiere el bloqueo exclusivo para el veterinario indicado.
    /// </summary>
    public static async Task<IDisposable> AdquirirBloqueoAsync(Guid veterinarioId)
    {
        var semaforo = Locks.GetOrAdd(veterinarioId, _ => new SemaphoreSlim(1, 1));
        await semaforo.WaitAsync();
        return new LockReleaser(semaforo);
    }

    /// <summary>
    /// Adquiere bloqueos exclusivos para dos veterinarios en orden canónico estricto,
    /// previniendo deadlocks cuando una operación (como reprogramación) transfiere
    /// un turno entre dos profesionales distintos.
    /// </summary>
    public static async Task<IDisposable> AdquirirBloqueoDobleAsync(Guid vetId1, Guid? vetId2)
    {
        if (!vetId2.HasValue || vetId2.Value == Guid.Empty || vetId1 == vetId2.Value)
        {
            return await AdquirirBloqueoAsync(vetId1);
        }

        // Orden canónico por comparación de GUID para prevenir interbloqueos
        var primero = vetId1.CompareTo(vetId2.Value) < 0 ? vetId1 : vetId2.Value;
        var segundo = primero == vetId1 ? vetId2.Value : vetId1;

        var sem1 = Locks.GetOrAdd(primero, _ => new SemaphoreSlim(1, 1));
        var sem2 = Locks.GetOrAdd(segundo, _ => new SemaphoreSlim(1, 1));

        await sem1.WaitAsync();
        try
        {
            await sem2.WaitAsync();
            return new DoubleLockReleaser(sem1, sem2);
        }
        catch
        {
            sem1.Release();
            throw;
        }
    }

    private sealed class LockReleaser : IDisposable
    {
        private readonly SemaphoreSlim _semaphore;
        private bool _disposed;

        public LockReleaser(SemaphoreSlim semaphore)
        {
            _semaphore = semaphore;
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _semaphore.Release();
                _disposed = true;
            }
        }
    }

    private sealed class DoubleLockReleaser : IDisposable
    {
        private readonly SemaphoreSlim _sem1;
        private readonly SemaphoreSlim _sem2;
        private bool _disposed;

        public DoubleLockReleaser(SemaphoreSlim sem1, SemaphoreSlim sem2)
        {
            _sem1 = sem1;
            _sem2 = sem2;
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                try
                {
                    _sem2.Release();
                }
                finally
                {
                    _sem1.Release();
                }
                _disposed = true;
            }
        }
    }
}

