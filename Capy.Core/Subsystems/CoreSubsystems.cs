using System;
using System.IO;
using Exiled.API.Features;
using HarmonyLib;
using Capy.Core.API;
using Capy.Core.API.Attributes;
using Capy.Core.Database;
using Capy.Core.Loader;
using Capy.Core.Services;
using Capy.Engine;
using Capy.Engine.Audio;
using Capy.Engine.CustomItems.Base;
using Capy.Engine.CustomItems.Manager;
using Capy.Engine.CustomRoles.Base;
using Capy.Engine.CustomRoles.Manager;
using Capy.Engine.Hud;
using Capy.Engine.ServerSpecific;
using Capy.Engine.Studio.Core;
using Capy.Engine.Studio.ToolGun;

namespace Capy.Core.Subsystems;

public sealed class HarmonySubsystem : ICapySubsystem
{
    public string Name => "HarmonyPatches";
    public int Priority => -100;

    private Harmony? _harmony;

    public void Initialize()
    {
        _harmony = new Harmony($"capylib.patches.{DateTime.UtcNow.Ticks}");
        _harmony.PatchAll();
        Log.Debug("[HarmonySubsystem] Harmony-патчи успешно применены.");
    }

    public void Shutdown()
    {
        if (_harmony != null)
        {
            _harmony.UnpatchAll(_harmony.Id);
            _harmony = null;
            Log.Debug("[HarmonySubsystem] Harmony-патчи выгружены.");
        }
    }
}

public sealed class DatabaseSubsystem : ICapySubsystem
{
    public string Name => "Database";
    public int Priority => -50;

    public IDatabaseProvider? Database { get; private set; }

    public void Initialize()
    {
        var config = CapyPlugin.Instance?.Config;
        if (config != null && string.Equals(config.DatabaseType, "MongoDB", StringComparison.OrdinalIgnoreCase))
        {
            Database = new MongoDbProvider(config.MongoConnectionString, config.MongoDatabaseName);
        }
        else
        {
            Database = new LiteDbProvider();
        }

        Database.Initialize();
        Log.Debug($"[DatabaseSubsystem] База данных инициализирована ({Database.GetType().Name}).");
    }

    public void Shutdown()
    {
        try
        {
            Database?.Shutdown();
            Database = null;
        }
        catch (Exception ex)
        {
            Log.Error($"[DatabaseSubsystem] Ошибка при отключении БД: {ex.Message}");
        }
    }
}

public sealed class PlaceholdersSubsystem : ICapySubsystem
{
    public string Name => "Placeholders";
    public int Priority => -40;

    public void Initialize()
    {
        PlaceholderReplacer.RegisterDefaults();
        Log.Debug("[PlaceholdersSubsystem] Стандартные плейсхолдеры зарегистрированы.");
    }

    public void Shutdown()
    {
    }
}

public sealed class AudioRegistrySubsystem : ICapySubsystem
{
    public string Name => "AudioRegistry";
    public int Priority => -30;

    public void Initialize()
    {
        AudioRegistry.RegisterClips();
        Log.Debug("[AudioRegistrySubsystem] Аудиоклипы успешно зарегистрированы в AudioPlayerApi.");
    }

    public void Shutdown()
    {
        try
        {
            AudioRegistry.UnloadAll();
        }
        catch (Exception ex)
        {
            Log.Error($"[AudioRegistrySubsystem] Ошибка при выгрузке аудиоклипов: {ex.Message}");
        }
    }
}

public sealed class AssKeybindsSubsystem : ICapySubsystem
{
    public string Name => "AssKeybinds";
    public int Priority => 0;

    public void Initialize()
    {
        AssKeybinds.Initialize();
        Log.Debug("[AssKeybindsSubsystem] Серверные бинды ASS успешно инициализированы.");
    }

    public void Shutdown()
    {
        try
        {
            AssKeybinds.Unregister();
        }
        catch (Exception ex)
        {
            Log.Error($"[AssKeybindsSubsystem] Ошибка при выгрузке ASS биндов: {ex.Message}");
        }
    }
}

public sealed class HudSubsystem : ICapySubsystem
{
    public string Name => "HudModule";
    public int Priority => 10;

    public void Initialize()
    {
        HudModule.Enable();
        Log.Debug("[HudSubsystem] Dynamic HUD модуль запущен.");
    }

    public void Shutdown()
    {
        try
        {
            HudModule.Disable();
            Log.Debug("[HudSubsystem] HUD модуль отключен.");
        }
        catch (Exception ex)
        {
            Log.Error($"[HudSubsystem] Ошибка при отключении HUD: {ex.Message}");
        }
    }
}

public sealed class CapyStudioSubsystem : ICapySubsystem
{
    public string Name => "CapyStudio";
    public int Priority => 20;

    public void Initialize()
    {
        SchematicLoader.Initialize();
        MapManager.Initialize();
        CapyToolGun.Initialize();

        Exiled.Events.Handlers.Server.WaitingForPlayers += PrefabManager.Initialize;
        Exiled.Events.Handlers.Server.RoundStarted += MapManager.OnRoundStarted;
        Exiled.Events.Handlers.Server.RestartingRound += MapManager.OnRoundRestarted;

        Log.Debug("[CapyStudioSubsystem] Движок 3D-схематик CapyStudio активирован.");
    }

    public void Shutdown()
    {
        Exiled.Events.Handlers.Server.WaitingForPlayers -= PrefabManager.Initialize;
        Exiled.Events.Handlers.Server.RoundStarted -= MapManager.OnRoundStarted;
        Exiled.Events.Handlers.Server.RestartingRound -= MapManager.OnRoundRestarted;

        try
        {
            CapyToolGun.Unregister();
            MapManager.ClearCurrentMap();
            SchematicLoader.DestroyAll();
            PrefabManager.Reset();
            Log.Debug("[CapyStudioSubsystem] CapyStudio успешно выгружен.");
        }
        catch (Exception ex)
        {
            Log.Error($"[CapyStudioSubsystem] Ошибка при выгрузке CapyStudio: {ex.Message}");
        }
    }
}

public sealed class CustomEntitiesSubsystem : ICapySubsystem
{
    public string Name => "CustomEntities";
    public int Priority => 30;

    public void Initialize()
    {
        // Автоматический поиск через рефлексию всех CustomItem и CustomRole с атрибутом [AutoRegister]
        int itemsCount = 0;
        int rolesCount = 0;

        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            try
            {
                foreach (var type in assembly.GetTypes())
                {
                    if (type.IsAbstract || type.IsInterface) continue;

                    var autoAttr = (AutoRegisterAttribute?)Attribute.GetCustomAttribute(type, typeof(AutoRegisterAttribute));

                    if (typeof(CustomItem).IsAssignableFrom(type))
                    {
                        if (autoAttr != null || type.Assembly == typeof(CapyPlugin).Assembly)
                        {
                            try
                            {
                                if (Activator.CreateInstance(type) is CustomItem item)
                                {
                                    CustomItemsManager.Register(item);
                                    itemsCount++;
                                }
                            }
                            catch (Exception ex)
                            {
                                Log.Error($"[CustomEntitiesSubsystem] Ошибка регистрации предмета {type.Name}: {ex.Message}");
                            }
                        }
                    }
                    else if (typeof(CustomRole).IsAssignableFrom(type))
                    {
                        if (autoAttr != null || type.Assembly == typeof(CapyPlugin).Assembly)
                        {
                            try
                            {
                                if (Activator.CreateInstance(type) is CustomRole role)
                                {
                                    CustomRolesManager.Register(role);
                                    rolesCount++;
                                }
                            }
                            catch (Exception ex)
                            {
                                Log.Error($"[CustomEntitiesSubsystem] Ошибка регистрации роли {type.Name}: {ex.Message}");
                            }
                        }
                    }
                }
            }
            catch
            {
                // Пропускаем динамические или недоступные сборки
            }
        }

        if (itemsCount > 0 || rolesCount > 0)
        {
            Log.Debug($"[CustomEntitiesSubsystem] Рефлексией зарегистрировано: {itemsCount} предметов, {rolesCount} ролей.");
        }
    }

    public void Shutdown()
    {
        try
        {
            CustomItemsManager.UnregisterAll();
            CustomRolesManager.UnregisterAll();
        }
        catch (Exception ex)
        {
            Log.Error($"[CustomEntitiesSubsystem] Ошибка при отключении сущностей: {ex.Message}");
        }
    }
}

public sealed class EventRegistrarSubsystem : ICapySubsystem
{
    public string Name => "EventRegistrar";
    public int Priority => 40;

    public void Initialize()
    {
        // Автоматический поиск через рефлексию всех классов с [CapyEventHandler]
        var targetTypes = new List<Type>();

        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            try
            {
                foreach (var type in assembly.GetTypes())
                {
                    if (type.GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)
                        .Any(m => Attribute.IsDefined(m, typeof(CapyEventHandlerAttribute))))
                    {
                        targetTypes.Add(type);
                    }
                }
            }
            catch
            {
            }
        }

        if (targetTypes.Count > 0)
        {
            EventRegistrar.Register(targetTypes.ToArray());
            Log.Debug($"[EventRegistrarSubsystem] Автоматически подписано {targetTypes.Count} обработчиков событий.");
        }
    }

    public void Shutdown()
    {
        try
        {
            EventRegistrar.UnregisterAll();
            EventBus.Clear();
        }
        catch (Exception ex)
        {
            Log.Error($"[EventRegistrarSubsystem] Ошибка при отписке событий: {ex.Message}");
        }
    }
}

public sealed class ModuleLoaderSubsystem : ICapySubsystem
{
    public string Name => "ModuleLoader";
    public int Priority => 50;

    public ModuleLoader Loader { get; } = new();

    public void Initialize()
    {
        try
        {
            Loader.InitializeAsync().GetAwaiter().GetResult();
            Log.Debug($"[ModuleLoaderSubsystem] Загружено модулей: {Loader.Modules.Count}.");
        }
        catch (Exception ex)
        {
            Log.Error($"[ModuleLoaderSubsystem] Ошибка при инициализации модулей: {ex}");
        }
    }

    public void Shutdown()
    {
        try
        {
            Loader.ShutdownAsync().GetAwaiter().GetResult();
            ModuleManager.Clear();
        }
        catch (Exception ex)
        {
            Log.Error($"[ModuleLoaderSubsystem] Ошибка при остановке модулей: {ex.Message}");
        }
    }
}

public sealed class ServerBannerSubsystem : ICapySubsystem
{
    public string Name => "ServerBanner";
    public int Priority => 100;

    public void Initialize()
    {
        ServerBanner.Show();
    }

    public void Shutdown()
    {
    }
}
