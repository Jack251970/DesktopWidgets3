namespace CustomExtensions.WinUI.Models;

internal class DisposableObject(Action callback) : IDisposable
{
    private bool IsDisposed;
    private readonly Action Callback = callback;

    protected virtual void Dispose(bool disposing)
    {
        if (!IsDisposed)
        {
            if (disposing)
            {
                Callback();
            }
            IsDisposed = true;
        }
    }

    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
