namespace Capy.Core.API.Attributes;

/// <summary>
/// Помечает класс кастомного предмета, роли или обработчика для автоматической регистрации через рефлексию.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public class AutoRegisterAttribute : Attribute
{
    public bool AutoEnable { get; set; } = true;

    public AutoRegisterAttribute(bool autoEnable = true)
    {
        AutoEnable = autoEnable;
    }
}
