namespace Capy.Core.API;

/// <summary>
/// Абстракция логирования с изолированным префиксом модуля.
/// </summary>
public interface ICapyLogger
{
    void Info(string message);
    void Debug(string message);
    void Warn(string message);
    void Error(string message);
}
