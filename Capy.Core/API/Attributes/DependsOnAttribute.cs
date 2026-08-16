namespace Capy.Core.API.Attributes;

/// <summary>
/// Декларирует зависимость модуля от другого модуля для правильного порядка инициализации.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class DependsOnAttribute : Attribute
{
    public string TargetModuleName { get; }

    public DependsOnAttribute(string targetModuleName)
    {
        TargetModuleName = targetModuleName;
    }
}
