namespace Ramstack.FileSystem.Amazon.Utilities;

/// <summary>
/// Provides a mechanism to temporarily set the <see cref="SynchronizationContext"/> to <see langword="null"/>.
/// </summary>
internal static class NullSynchronizationContext
{
    /// <summary>
    /// Sets the current <see cref="SynchronizationContext"/> to <see langword="null"/>
    /// and returns a disposable scope that restores the original context when disposed.
    /// </summary>
    /// <returns>
    /// A <see cref="ContextScope"/> struct that restores the original <see cref="SynchronizationContext"/> when disposed.
    /// </returns>
    public static ContextScope CreateScope()
    {
        var context = SynchronizationContext.Current;
        SynchronizationContext.SetSynchronizationContext(null);
        return new ContextScope(context);
    }

    /// <summary>
    /// A disposable struct that restores the original <see cref="SynchronizationContext"/> when disposed.
    /// </summary>
    public readonly struct ContextScope(SynchronizationContext? context) : IDisposable
    {
        /// <summary>
        /// Restores the original <see cref="SynchronizationContext"/> that was present when the scope was created.
        /// </summary>
        public void Dispose() =>
            SynchronizationContext.SetSynchronizationContext(context);
    }
}
