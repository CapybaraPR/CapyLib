using System;
using Capy.Core.API;
using Exiled.API.Features;

namespace Capy.API.DiscordBridge;

public sealed class DiscordBridgeModule : BaseModule<DiscordBridgeConfig>
{
    public override string Name => "DiscordBridge";
    public override string Author => "CapybaraPR";
    public override Version Version => new(1, 1, 0);

    private static DiscordBridgeModule? _instance;
    private BridgeApiServer? _apiServer;
    private BridgeEventStore? _eventStore;
    private BridgeEventLogger? _eventLogger;
    private DiscordLinkService? _linkService;
    private DiscordRoleController? _roleController;
    private BridgeAuth? _auth;

    public static DiscordBridgeModule? Instance => _instance;
    public static DiscordLinkService? LinkService => _instance?._linkService;
    public static DiscordRoleController? RoleController => _instance?._roleController;
    public static BridgeEventStore? EventStore => _instance?._eventStore;
    public static bool IsRunning => _instance?._apiServer != null;

    public override void OnEnabled()
    {
        _instance = this;
        if (!Config.IsEnabled)
        {
            Log.Info("Модуль DiscordBridge выключен в конфигурации.");
            return;
        }

        try
        {
            _auth = new BridgeAuth(Config);
            _eventStore = new BridgeEventStore(Config);
            _eventLogger = new BridgeEventLogger(Config, _eventStore);
            _eventLogger.Subscribe();

            _linkService = new DiscordLinkService(Config);
            _roleController = new DiscordRoleController(Config, _linkService);
            _roleController.Start();

            _apiServer = new BridgeApiServer(Config, _eventStore, _eventLogger, _linkService, _roleController, _auth);
            _apiServer.Start();

            Log.Info("Модуль интеграции DiscordBridge успешно запущен.");
        }
        catch (Exception ex)
        {
            Log.Error($"Ошибка при запуске DiscordBridge: {ex}");
        }
    }

    public override void OnDisabled()
    {
        try
        {
            _apiServer?.Stop();
            _apiServer = null;

            _roleController?.Stop();
            _roleController = null;

            _linkService?.Dispose();
            _linkService = null;

            _eventLogger?.Unsubscribe();
            _eventLogger = null;

            _eventStore = null;

            _auth?.Dispose();
            _auth = null;

            Log.Info("Модуль DiscordBridge остановлен.");
        }
        catch (Exception ex)
        {
            Log.Error($"Ошибка при остановке DiscordBridge: {ex}");
        }
        finally
        {
            _instance = null;
        }
    }
}
