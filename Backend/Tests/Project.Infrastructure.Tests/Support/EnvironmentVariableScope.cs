namespace Project.Infrastructure.Tests.Support;

public sealed class EnvironmentVariableScope : IDisposable
{
    private static readonly object Gate = new();
    private readonly Dictionary<string, string?> _original;
    private bool _disposed;

    public EnvironmentVariableScope(params string[] names)
    {
        Monitor.Enter(Gate);
        _original = names.ToDictionary(name => name, Environment.GetEnvironmentVariable);
        foreach (var name in names)
        {
            Environment.SetEnvironmentVariable(name, null);
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        foreach (var (name, value) in _original)
        {
            Environment.SetEnvironmentVariable(name, value);
        }

        _disposed = true;
        Monitor.Exit(Gate);
    }
}
