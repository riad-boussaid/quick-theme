namespace QuickTheme.App;

internal sealed class SingleInstance : IDisposable
{
    private const string MutexName = "QuickTheme.SingleInstance";
    private const string ShowEventName = "QuickTheme.ShowPanel";

    private readonly Mutex _mutex;
    private bool _owned;

    public SingleInstance()
    {
        _mutex = new Mutex(initiallyOwned: true, MutexName, out bool createdNew);
        _owned = createdNew;
        IsFirstInstance = createdNew;
    }

    public bool IsFirstInstance { get; }

    public EventWaitHandle CreateShowEvent() =>
        new(initialState: false, EventResetMode.AutoReset, ShowEventName);

    public void SignalFirstInstance()
    {
        try
        {
            using var handle = EventWaitHandle.OpenExisting(ShowEventName);
            handle.Set();
        }
        catch (Exception ex) when (ex is WaitHandleCannotBeOpenedException or UnauthorizedAccessException)
        {
            // The first instance is not ready yet; there is nothing else to do.
        }
    }

    public void Dispose()
    {
        if (_owned)
        {
            try
            {
                _mutex.ReleaseMutex();
            }
            catch (ApplicationException)
            {
                // The mutex is already released.
            }

            _owned = false;
        }

        _mutex.Dispose();
    }
}
