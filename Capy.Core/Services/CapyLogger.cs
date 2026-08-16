using Capy.Core.API;

namespace Capy.Core.Services;

/// <summary>
/// Реализация ICapyLogger, оборачивающая Exiled.API.Features.Log с цветным префиксом модуля.
/// </summary>
public class CapyLogger : ICapyLogger
{
    private readonly string _prefix;
    private bool _debug;

    public CapyLogger(string prefix, bool debug = false)
    {
        _prefix = prefix;
        _debug = debug;
    }

    public void SetDebug(bool debug) => _debug = debug;

    public void Info(string message) => Log.Info($"[CapyLib:{_prefix}] {message}");

    public void Debug(string message)
    {
        if (_debug)
        {
            Log.Debug($"[CapyLib:{_prefix}:DEBUG] {message}");
        }
    }

    public void Warn(string message) => Log.Warn($"[CapyLib:{_prefix}:WARN] {message}");

    public void Error(string message) => Log.Error($"[CapyLib:{_prefix}:ERROR] {message}");
}
