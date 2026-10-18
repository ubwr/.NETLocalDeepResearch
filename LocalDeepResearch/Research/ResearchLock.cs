namespace LocalDeepResearch.Research;

/// <summary>
/// Lock to ensure there is only ever 1 research run at once.
/// </summary>
public class ResearchLock
{
    /// <summary>
    /// Only ever allow 1 research run at once.
    /// </summary>
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    
    /// <summary>
    /// Check if there is currently an active research run.
    /// If there are no active research runs, then claim the available slot.
    /// </summary>
    /// <returns> If a research run can fire </returns>
    public bool TryAcquire()
    {
        return _semaphore.Wait(0);
    }
    /// <summary>
    /// Frees the lock so that another research run can fire.
    /// </summary>
    public void Release()
    {
        _semaphore.Release();
    }
}
