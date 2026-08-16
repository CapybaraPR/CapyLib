using Exiled.API.Features.Items;

namespace Capy.Core.Extensions;

/// <summary>
/// Полезные методы расширения для предметов.
/// </summary>
public static class ItemExtensions
{
    public static bool IsCustomItem(this Item? item, out uint serial)
    {
        serial = 0;
        if (item == null) return false;
        serial = item.Serial;
        return serial > 0;
    }
}
