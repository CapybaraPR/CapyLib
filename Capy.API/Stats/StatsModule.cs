using Capy.Core.API;
using Exiled.Events.Handlers;

namespace Capy.API.Stats;

/// <summary>
/// Модуль сбора игровой статистики и отправки логов на внешний API.
/// </summary>
public class StatsModule : BaseModule<StatsConfig>
{
    public override string Name => "StatsTracker";
    public override string Author => "CapybaraPR";
    public override Version Version => new(1, 0, 0);

    private StatsEventHandlers? _handlers;

    public override void OnEnabled()
    {
        _handlers = new StatsEventHandlers(Log, Config);

        Exiled.Events.Handlers.Player.Verified += _handlers.OnVerified;
        Exiled.Events.Handlers.Player.Died += _handlers.OnDied;
        Exiled.Events.Handlers.Player.Left += _handlers.OnLeft;
        Exiled.Events.Handlers.Server.RoundEnded += _handlers.OnRoundEnded;

        Log.Debug("Модуль сбора статистики успешно активирован.");
    }

    public override void OnDisabled()
    {
        if (_handlers != null)
        {
            Exiled.Events.Handlers.Player.Verified -= _handlers.OnVerified;
            Exiled.Events.Handlers.Player.Died -= _handlers.OnDied;
            Exiled.Events.Handlers.Player.Left -= _handlers.OnLeft;
            Exiled.Events.Handlers.Server.RoundEnded -= _handlers.OnRoundEnded;

            _handlers.Clear();
            _handlers = null;
        }

        Log.Debug("Модуль сбора статистики отключен.");
    }
}
