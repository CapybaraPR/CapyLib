using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Exiled.API.Features;
using MEC;

namespace Capy.API.DiscordBridge;

public sealed class BridgeApiServer : IDisposable
{
    private const int MaximumRequestBodyBytes = 64 * 1024;
    private const int MaximumRoleSyncBodyBytes = 512 * 1024;
    private static readonly Regex HiddenServerMetadata = new(
        @"<color\s*=\s*#00000000>.*?</color>|<size\s*=\s*1>.*?</size>",
        RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);
    private static readonly Regex RichTextTag = new("<[^>]*>", RegexOptions.Compiled);
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = false
    };
    private static readonly object ProcessQuerySync = new();
    private static System.Reflection.MethodInfo? _processQueryMethod;

    private readonly DiscordBridgeConfig _config;
    private readonly BridgeEventStore _eventStore;
    private readonly BridgeEventLogger _eventLogger;
    private readonly DiscordLinkService _linkService;
    private readonly DiscordRoleController _roleController;
    private readonly BridgeAuth _auth;
    private readonly MainThreadDispatcher _dispatcher = new();
    private readonly CancellationTokenSource _lifetime = new();
    private readonly HttpListener _listener = new();
    private readonly SemaphoreSlim _requestSlots;
    private readonly DateTime _startTimeUtc = DateTime.UtcNow;
    private Task? _acceptLoop;
    private CoroutineHandle _statusRefreshHandle;
    private StatusResponse? _lastStatus;
    private int _stopped;

    public BridgeApiServer(
        DiscordBridgeConfig config,
        BridgeEventStore eventStore,
        BridgeEventLogger eventLogger,
        DiscordLinkService linkService,
        DiscordRoleController roleController,
        BridgeAuth auth)
    {
        _config = config;
        _eventStore = eventStore;
        _eventLogger = eventLogger;
        _linkService = linkService;
        _roleController = roleController;
        _auth = auth;
        _requestSlots = new SemaphoreSlim(Math.Max(1, Math.Min(config.MaxConcurrentRequests, 64)));
    }

    public void Start()
    {
        if (!ValidateConfiguration(out string error))
        {
            Log.Error($"[DiscordBridge.ApiServer] API не запущен: {error}");
            return;
        }

        try
        {
            _listener.Prefixes.Add(NormalizePrefix(_config.ListenPrefix));
            _listener.Start();
            _dispatcher.Start();
            _statusRefreshHandle = Timing.RunCoroutine(StatusSnapshotLoop());
            _acceptLoop = Task.Run(() => AcceptLoopAsync(_lifetime.Token));
            Log.Info($"[DiscordBridge.ApiServer] API успешно слушает {NormalizePrefix(_config.ListenPrefix)} (SSH подпись: {(_config.RequireSshSignature ? "включена" : "отключена")})");
        }
        catch (Exception ex)
        {
            Log.Error($"[DiscordBridge.ApiServer] Не удалось запустить HTTP API: {ex}");
        }
    }

    public void Stop()
    {
        if (Interlocked.Exchange(ref _stopped, 1) != 0)
            return;

        _lifetime.Cancel();
        try
        {
            _listener.Stop();
            _listener.Close();
        }
        catch
        {
        }

        if (_acceptLoop != null)
        {
            try
            {
                Task.WhenAny(_acceptLoop, Task.Delay(3000)).GetAwaiter().GetResult();
            }
            catch
            {
            }
        }

        if (_statusRefreshHandle.IsRunning)
            Timing.KillCoroutines(_statusRefreshHandle);

        _dispatcher.Stop();
        _requestSlots.Dispose();
        _lifetime.Dispose();
    }

    public void Dispose() => Stop();

    private async Task AcceptLoopAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested && _listener.IsListening)
        {
            try
            {
                HttpListenerContext context = await _listener.GetContextAsync().ConfigureAwait(false);
                _ = Task.Run(() => HandleIncomingRequestAsync(context, cancellationToken), cancellationToken);
            }
            catch (HttpListenerException) when (cancellationToken.IsCancellationRequested || _stopped == 1)
            {
                break;
            }
            catch (ObjectDisposedException)
            {
                break;
            }
            catch (Exception ex)
            {
                if (!cancellationToken.IsCancellationRequested && _stopped == 0)
                    Log.Warn($"[DiscordBridge.ApiServer] Ошибка accept loop: {ex.Message}");
            }
        }
    }

    private async Task HandleIncomingRequestAsync(HttpListenerContext context, CancellationToken cancellationToken)
    {
        if (!await _requestSlots.WaitAsync(TimeSpan.FromSeconds(5), cancellationToken).ConfigureAwait(false))
        {
            await RespondJsonAsync(context.Response, HttpStatusCode.ServiceUnavailable, new ErrorResponse { Error = "Сервер перегружен запросами." }).ConfigureAwait(false);
            return;
        }

        try
        {
            IPAddress? clientIp = context.Request.RemoteEndPoint?.Address;
            if (!IsIpAllowed(clientIp))
            {
                Log.Warn($"[DiscordBridge.ApiServer] Отклонен запрос с неразрешенного IP: {clientIp}");
                await RespondJsonAsync(context.Response, HttpStatusCode.Forbidden, new ErrorResponse { Error = "IP адрес клиента не входит в список разрешенных." }).ConfigureAwait(false);
                return;
            }

            string rawPath = context.Request.Url?.AbsolutePath ?? "/";
            string pathAndQuery = context.Request.Url?.PathAndQuery ?? "/";
            string method = context.Request.HttpMethod?.ToUpperInvariant() ?? "GET";

            byte[] bodyBytes = Array.Empty<byte>();
            if (context.Request.HasEntityBody)
            {
                int maxBytes = rawPath.Contains("/roles/sync") || rawPath.Contains("/links/sync")
                    ? MaximumRoleSyncBodyBytes
                    : MaximumRequestBodyBytes;

                using var ms = new MemoryStream();
                await context.Request.InputStream.CopyToAsync(ms).ConfigureAwait(false);
                bodyBytes = ms.ToArray();

                if (bodyBytes.Length > maxBytes)
                {
                    await RespondJsonAsync(context.Response, (HttpStatusCode)413, new ErrorResponse { Error = "Тело запроса превышает допустимый размер." }).ConfigureAwait(false);
                    return;
                }
            }

            string? sigHeader = context.Request.Headers["X-Aspect-Signature"];
            string? tsHeader = context.Request.Headers["X-Aspect-Timestamp"];
            string? keyHeader = context.Request.Headers["X-Aspect-Key"];

            if (!_auth.ValidateRequest(method, pathAndQuery, bodyBytes, sigHeader, tsHeader, keyHeader, out string authError))
            {
                if (_config.DebugMode)
                    Log.Debug($"[DiscordBridge.ApiServer] Ошибка аутентификации: {authError} (Method: {method}, Path: {pathAndQuery})");

                await RespondJsonAsync(context.Response, HttpStatusCode.Unauthorized, new ErrorResponse { Error = authError }).ConfigureAwait(false);
                return;
            }

            await RouteRequestAsync(context, rawPath, method, bodyBytes).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Log.Error($"[DiscordBridge.ApiServer] Необработанная ошибка обработки запроса: {ex}");
            try
            {
                await RespondJsonAsync(context.Response, HttpStatusCode.InternalServerError, new ErrorResponse { Error = "Внутренняя ошибка сервера: " + ex.Message }).ConfigureAwait(false);
            }
            catch
            {
            }
        }
        finally
        {
            _requestSlots.Release();
        }
    }

    private async Task RouteRequestAsync(HttpListenerContext context, string path, string method, byte[] bodyBytes)
    {
        string normalized = path.TrimEnd('/').ToLowerInvariant();
        if (normalized.StartsWith("/v1"))
            normalized = normalized.Substring(3);

        switch (normalized)
        {
            case "/health" when method == "GET":
                await HandleHealthAsync(context.Response).ConfigureAwait(false);
                break;

            case "/status" when method == "GET":
                await HandleStatusAsync(context.Response).ConfigureAwait(false);
                break;

            case "/players" when method == "GET":
                await HandlePlayersAsync(context.Response).ConfigureAwait(false);
                break;

            case "/groups" when method == "GET":
                await HandleGroupsAsync(context.Response).ConfigureAwait(false);
                break;

            case "/command" when method == "POST":
                await HandleCommandAsync(context.Response, bodyBytes).ConfigureAwait(false);
                break;

            case "/logs" when method == "GET":
                HandleLogs(context.Request, context.Response);
                break;

            case "/links/code" when method == "POST":
                await HandleCreateLinkCodeAsync(context.Response, bodyBytes).ConfigureAwait(false);
                break;

            case "/links" when method == "GET":
                HandleGetLinks(context.Response);
                break;

            case "/links/sync" when method == "POST":
                await HandleSyncLinkRolesAsync(context.Response, bodyBytes).ConfigureAwait(false);
                break;

            default:
                await RespondJsonAsync(context.Response, HttpStatusCode.NotFound, new ErrorResponse { Error = $"Эндпоинт {method} {path} не найден." }).ConfigureAwait(false);
                break;
        }
    }

    private async Task HandleHealthAsync(HttpListenerResponse response)
    {
        bool gameResponsive = false;
        try
        {
            gameResponsive = await _dispatcher.InvokeAsync(() => true, 2).ConfigureAwait(false);
        }
        catch
        {
        }

        var health = new HealthResponse
        {
            Status = gameResponsive ? "healthy" : "degraded",
            PluginName = "CapyLib",
            Version = typeof(BridgeApiServer).Assembly.GetName().Version?.ToString() ?? "1.1.0",
            UptimeSeconds = (DateTime.UtcNow - _startTimeUtc).TotalSeconds,
            GameThreadResponsive = gameResponsive,
            AuthMode = _config.RequireSshSignature ? "ssh_signature" : "api_key"
        };

        await RespondJsonAsync(response, HttpStatusCode.OK, health).ConfigureAwait(false);
    }

    private async Task HandleStatusAsync(HttpListenerResponse response)
    {
        StatusResponse? status = _lastStatus;
        if (status == null)
        {
            status = await _dispatcher.InvokeAsync(BuildStatusSnapshot, _config.GameThreadTimeoutSeconds).ConfigureAwait(false);
            _lastStatus = status;
        }

        await RespondJsonAsync(response, HttpStatusCode.OK, status).ConfigureAwait(false);
    }

    private async Task HandlePlayersAsync(HttpListenerResponse response)
    {
        PlayersResponse players = await _dispatcher.InvokeAsync(BuildPlayersSnapshot, _config.GameThreadTimeoutSeconds).ConfigureAwait(false);
        await RespondJsonAsync(response, HttpStatusCode.OK, players).ConfigureAwait(false);
    }

    private async Task HandleGroupsAsync(HttpListenerResponse response)
    {
        GroupsResponse groups = await _dispatcher.InvokeAsync(BuildGroupsSnapshot, _config.GameThreadTimeoutSeconds).ConfigureAwait(false);
        await RespondJsonAsync(response, HttpStatusCode.OK, groups).ConfigureAwait(false);
    }

    private async Task HandleCommandAsync(HttpListenerResponse response, byte[] bodyBytes)
    {
        if (bodyBytes.Length == 0)
        {
            await RespondJsonAsync(response, HttpStatusCode.BadRequest, new ErrorResponse { Error = "Тело запроса команды пусто." }).ConfigureAwait(false);
            return;
        }

        CommandRequest? req;
        try
        {
            req = JsonSerializer.Deserialize<CommandRequest>(bodyBytes, JsonOptions);
        }
        catch (Exception ex)
        {
            await RespondJsonAsync(response, HttpStatusCode.BadRequest, new ErrorResponse { Error = "Некорректный JSON команды: " + ex.Message }).ConfigureAwait(false);
            return;
        }

        if (req == null || string.IsNullOrWhiteSpace(req.Command))
        {
            await RespondJsonAsync(response, HttpStatusCode.BadRequest, new ErrorResponse { Error = "Поле command обязательно." }).ConfigureAwait(false);
            return;
        }

        string rawCmd = req.Command.Trim();
        if (rawCmd.Length > _config.MaxCommandLength)
        {
            await RespondJsonAsync(response, HttpStatusCode.BadRequest, new ErrorResponse { Error = $"Команда превышает лимит длины {_config.MaxCommandLength} символов." }).ConfigureAwait(false);
            return;
        }

        string access = (req.Access ?? "ra").Trim().ToLowerInvariant();
        string actorName = string.IsNullOrWhiteSpace(req.ActorName) ? "DiscordUser" : req.ActorName.Trim();
        ulong actorId = req.ActorId;

        string normalizedName = rawCmd.TrimStart('/', '.').Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? string.Empty;

        // Check Creator Blocked
        if ((_config.CreatorBlockedCommands ?? new List<string>()).Any(c => c.Equals(normalizedName, StringComparison.OrdinalIgnoreCase)))
        {
            _eventLogger.LogDiscordDeniedCommand(rawCmd, "Команда заблокирована для Discord в CreatorBlockedCommands.", $"{actorName} ({actorId})", access);
            await RespondJsonAsync(response, HttpStatusCode.Forbidden, new CommandResponse { Success = false, Response = "Данная команда заблокирована конфигурацией сервера." }).ConfigureAwait(false);
            return;
        }

        // Check RA Allowed if not creator
        if (access != "creator")
        {
            bool isAllowed = (_config.RaAllowedCommands ?? new List<string>()).Any(c => c.Equals(normalizedName, StringComparison.OrdinalIgnoreCase));
            if (!isAllowed)
            {
                _eventLogger.LogDiscordDeniedCommand(rawCmd, "Команда не разрешена для уровня RA.", $"{actorName} ({actorId})", access);
                await RespondJsonAsync(response, HttpStatusCode.Forbidden, new CommandResponse { Success = false, Response = $"Команда '{normalizedName}' не входит в список разрешенных для уровня RA." }).ConfigureAwait(false);
                return;
            }
        }

        var sw = Stopwatch.StartNew();
        BridgeCommandResult result = await _dispatcher.InvokeAsync(() => ExecuteServerCommand(rawCmd, actorId.ToString(), actorName), _config.GameThreadTimeoutSeconds).ConfigureAwait(false);
        sw.Stop();

        _eventLogger.LogDiscordCommand(rawCmd, result.Output, result.Success, $"{actorName} ({actorId})", access);

        await RespondJsonAsync(response, HttpStatusCode.OK, new CommandResponse
        {
            Success = result.Success,
            Response = result.Output,
            ExecutionTimeMs = sw.Elapsed.TotalMilliseconds
        }).ConfigureAwait(false);
    }

    private void HandleLogs(HttpListenerRequest request, HttpListenerResponse response)
    {
        long afterId = 0;
        int limit = 25;

        if (long.TryParse(request.QueryString["after_id"], out long parsedAfter))
            afterId = Math.Max(0, parsedAfter);

        if (int.TryParse(request.QueryString["limit"], out int parsedLimit))
            limit = Math.Max(1, Math.Min(parsedLimit, 100));

        LogEventBatchResponse batch = _eventStore.GetEvents(afterId, limit);
        RespondJson(response, HttpStatusCode.OK, batch);
    }

    private async Task HandleCreateLinkCodeAsync(HttpListenerResponse response, byte[] bodyBytes)
    {
        LinkCodeRequest? req;
        try
        {
            req = JsonSerializer.Deserialize<LinkCodeRequest>(bodyBytes, JsonOptions);
        }
        catch (Exception ex)
        {
            await RespondJsonAsync(response, HttpStatusCode.BadRequest, new ErrorResponse { Error = "Некорректный JSON: " + ex.Message }).ConfigureAwait(false);
            return;
        }

        if (req == null || req.DiscordUserId == 0)
        {
            await RespondJsonAsync(response, HttpStatusCode.BadRequest, new ErrorResponse { Error = "discord_user_id обязателен." }).ConfigureAwait(false);
            return;
        }

        try
        {
            LinkCodeResponse res = _linkService.CreateCode(req);
            await RespondJsonAsync(response, HttpStatusCode.OK, res).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            await RespondJsonAsync(response, HttpStatusCode.InternalServerError, new ErrorResponse { Error = ex.Message }).ConfigureAwait(false);
        }
    }

    private void HandleGetLinks(HttpListenerResponse response)
    {
        LinkedAccountsResponse accounts = _linkService.GetLinkedAccounts();
        RespondJson(response, HttpStatusCode.OK, accounts);
    }

    private async Task HandleSyncLinkRolesAsync(HttpListenerResponse response, byte[] bodyBytes)
    {
        LinkRoleSyncRequest? req;
        try
        {
            req = JsonSerializer.Deserialize<LinkRoleSyncRequest>(bodyBytes, JsonOptions);
        }
        catch (Exception ex)
        {
            await RespondJsonAsync(response, HttpStatusCode.BadRequest, new ErrorResponse { Error = "Некорректный JSON: " + ex.Message }).ConfigureAwait(false);
            return;
        }

        if (req == null || req.DiscordUserId == 0)
        {
            await RespondJsonAsync(response, HttpStatusCode.BadRequest, new ErrorResponse { Error = "discord_user_id обязателен." }).ConfigureAwait(false);
            return;
        }

        LinkRoleSyncResponse syncRes = _linkService.SyncRoles(req.DiscordUserId, req.Roles ?? new List<ulong>());
        if (syncRes.Success && !string.IsNullOrWhiteSpace(syncRes.GameUserId))
        {
            await _dispatcher.InvokeAsync(() =>
            {
                Player? p = Player.Get(syncRes.GameUserId);
                if (p != null)
                    _roleController.ApplyToPlayer(p);
            }, 5).ConfigureAwait(false);
        }

        await RespondJsonAsync(response, HttpStatusCode.OK, syncRes).ConfigureAwait(false);
    }

    private BridgeCommandResult ExecuteServerCommand(string command, string actorId, string actorName)
    {
        BridgeCommandExecutionContext.IsActive = true;
        try
        {
            string cleanCmd = command.Trim();
            if (cleanCmd.StartsWith("/", StringComparison.Ordinal) || cleanCmd.StartsWith(".", StringComparison.Ordinal))
                cleanCmd = cleanCmd.Substring(1);

            string cmdName = cleanCmd.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? string.Empty;
            var sender = new BridgeCommandSender(actorId, actorName, cmdName);

            string? reply = InvokeProcessQuery(cleanCmd, sender);
            return sender.Complete(reply);
        }
        catch (Exception ex)
        {
            return new BridgeCommandResult
            {
                Success = false,
                Output = "Исключение при выполнении команды: " + ex.Message
            };
        }
        finally
        {
            BridgeCommandExecutionContext.IsActive = false;
        }
    }

    private static string? InvokeProcessQuery(string command, CommandSender sender)
    {
        var processQuery = ResolveProcessQueryMethod();
        try
        {
            return processQuery.Invoke(null, new object?[] { command, sender }) as string;
        }
        catch (System.Reflection.TargetInvocationException ex) when (ex.InnerException != null)
        {
            throw new InvalidOperationException("RemoteAdmin не смог выполнить команду.", ex.InnerException);
        }
    }

    private static System.Reflection.MethodInfo ResolveProcessQueryMethod()
    {
        lock (ProcessQuerySync)
        {
            if (_processQueryMethod != null)
                return _processQueryMethod;

            _processQueryMethod = typeof(RemoteAdmin.CommandProcessor)
                .GetMethods(System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic)
                .Where(method => method.Name.Equals("ProcessQuery", StringComparison.Ordinal))
                .Where(method => method.ReturnType == typeof(string))
                .FirstOrDefault(method =>
                {
                    var parameters = method.GetParameters();
                    return parameters.Length == 2 &&
                           parameters[0].ParameterType == typeof(string) &&
                           parameters[1].ParameterType.IsAssignableFrom(typeof(CommandSender));
                });

            return _processQueryMethod ?? throw new MissingMethodException(
                typeof(RemoteAdmin.CommandProcessor).FullName,
                "ProcessQuery(string, CommandSender)");
        }
    }

    private StatusResponse BuildStatusSnapshot()
    {
        int scps = 0;
        int humans = 0;
        int spectators = 0;

        try
        {
            foreach (Player p in Player.List)
            {
                if (p == null || !p.IsConnected || p.IsHost) continue;
                if (p.Role == null) continue;

                if (p.IsDead || p.Role.Team == PlayerRoles.Team.Dead)
                    spectators++;
                else if (p.Role.Team == PlayerRoles.Team.SCPs)
                    scps++;
                else
                    humans++;
            }
        }
        catch
        {
        }

        string sName = "SCP: SL Server";
        try
        {
            sName = Server.Name ?? "SCP: SL Server";
            sName = HiddenServerMetadata.Replace(sName, string.Empty);
            sName = RichTextTag.Replace(sName, string.Empty).Trim();
        }
        catch
        {
        }

        string serverIp = "127.0.0.1";
        ushort serverPort = 7777;
        try { serverPort = Exiled.API.Features.Server.Port; } catch { }
        try { serverIp = Exiled.API.Features.Server.IpAddress ?? "127.0.0.1"; } catch { }

        int onlineCount = 0;
        try { onlineCount = Player.List.Count(p => p != null && p.IsConnected && !p.IsHost); } catch { }

        int maxPlayers = 20;
        try { maxPlayers = Server.MaxPlayerCount; } catch { }

        bool isRoundRunning = false;
        bool isRoundStarted = false;
        bool isRoundEnded = false;
        bool isWaiting = true;
        int durationSec = 0;
        try
        {
            isRoundStarted = Round.IsStarted;
            isRoundEnded = Round.IsEnded;
            isRoundRunning = isRoundStarted && !isRoundEnded;
            isWaiting = Round.IsLobby;
            durationSec = (int)Round.ElapsedTime.TotalSeconds;
        }
        catch
        {
        }

        bool warheadDet = false;
        bool warheadProg = false;
        try
        {
            warheadDet = Warhead.IsDetonated;
            warheadProg = Warhead.IsInProgress;
        }
        catch
        {
        }

        double tps = 60.0;
        try { tps = Server.Tps; } catch { }

        bool friendlyFire = false;
        try { friendlyFire = Server.FriendlyFire; } catch { }

        string roundState = isWaiting ? "lobby" : (isRoundRunning ? "in_progress" : "ended");
        string roundTime = $"{durationSec / 60:D2}:{durationSec % 60:D2}";
        string address = !string.IsNullOrWhiteSpace(_config.ServerAddress) ? _config.ServerAddress : $"{serverIp}:{serverPort}";

        var srvStatus = new ServerStatus
        {
            ServerName = string.IsNullOrWhiteSpace(sName) ? "SCP: SL Server" : sName,
            PublicAddress = address,
            Port = serverPort,
            PlayersCount = onlineCount,
            MaxPlayers = maxPlayers,
            IsRoundRunning = isRoundRunning,
            IsRoundStarted = isRoundStarted,
            IsRoundEnded = isRoundEnded,
            IsWaitingForPlayers = isWaiting,
            IsWarheadDetonated = warheadDet,
            IsWarheadInProgress = warheadProg,
            IsFriendlyFireEnabled = friendlyFire,
            RoundDurationSeconds = Math.Max(0, durationSec),
            Tps = tps
        };

        return new StatusResponse
        {
            Success = true,
            Online = onlineCount,
            Maximum = maxPlayers,
            Address = address,
            ServerName = srvStatus.ServerName,
            RoundState = roundState,
            RoundTime = roundTime,
            Tps = tps,
            Utc = DateTime.UtcNow.ToString("o"),
            Server = srvStatus,
            ScpsAlive = scps,
            HumansAlive = humans,
            SpectatorsCount = spectators
        };
    }

    private PlayersResponse BuildPlayersSnapshot()
    {
        var list = new List<PlayerItem>();
        foreach (Player p in Player.List)
        {
            if (p == null || !p.IsConnected || p.IsHost) continue;

            list.Add(new PlayerItem
            {
                Id = p.Id,
                Nickname = p.Nickname ?? "Unknown",
                UserId = _config.IncludePlayerUserIds ? p.UserId : null,
                Role = p.Role.Type.ToString(),
                Team = p.Role.Team.ToString(),
                IsAlive = p.IsAlive,
                IsCuffed = p.IsCuffed,
                IsGodmode = p.IsGodModeEnabled,
                Group = p.GroupName,
                Ping = (int)p.Ping
            });
        }

        return new PlayersResponse
        {
            Count = list.Count,
            Players = list
        };
    }

    private GroupsResponse BuildGroupsSnapshot()
    {
        var list = new List<GroupItem>();
        foreach (var pair in ServerStatic.PermissionsHandler.Groups)
        {
            list.Add(new GroupItem
            {
                Key = pair.Key,
                BadgeText = pair.Value.BadgeText,
                BadgeColor = pair.Value.BadgeColor,
                PermissionsBitmask = pair.Value.Permissions,
                Cover = pair.Value.Cover,
                Hidden = pair.Value.HiddenByDefault
            });
        }

        return new GroupsResponse { Groups = list };
    }

    private IEnumerator<float> StatusSnapshotLoop()
    {
        while (_stopped == 0)
        {
            try
            {
                _lastStatus = BuildStatusSnapshot();
            }
            catch
            {
            }

            yield return Timing.WaitForSeconds(5f);
        }
    }

    private bool IsIpAllowed(IPAddress? address)
    {
        if (address == null) return false;
        if (_config.AllowedClientIps == null || _config.AllowedClientIps.Count == 0)
            return true;

        string ipStr = address.ToString();
        if (address.IsIPv4MappedToIPv6)
            ipStr = address.MapToIPv4().ToString();

        foreach (string allowed in _config.AllowedClientIps)
        {
            if (string.IsNullOrWhiteSpace(allowed)) continue;
            if (allowed.Trim().Equals(ipStr, StringComparison.OrdinalIgnoreCase))
                return true;
            if (IPAddress.TryParse(allowed.Trim(), out var parsed) && parsed.Equals(address))
                return true;
        }

        return false;
    }

    private bool ValidateConfiguration(out string error)
    {
        error = string.Empty;
        if (string.IsNullOrWhiteSpace(_config.ListenPrefix))
        {
            error = "listen_prefix не заполнен в конфигурации";
            return false;
        }

        return true;
    }

    private static string NormalizePrefix(string prefix)
    {
        string p = (prefix ?? string.Empty).Trim();
        if (!p.EndsWith("/")) p += "/";
        return p;
    }

    private static async Task RespondJsonAsync<T>(HttpListenerResponse response, HttpStatusCode status, T data)
    {
        response.StatusCode = (int)status;
        response.ContentType = "application/json; charset=utf-8";
        byte[] bytes = JsonSerializer.SerializeToUtf8Bytes(data, JsonOptions);
        response.ContentLength64 = bytes.Length;
        await response.OutputStream.WriteAsync(bytes, 0, bytes.Length).ConfigureAwait(false);
        response.OutputStream.Close();
    }

    private static void RespondJson<T>(HttpListenerResponse response, HttpStatusCode status, T data)
    {
        response.StatusCode = (int)status;
        response.ContentType = "application/json; charset=utf-8";
        byte[] bytes = JsonSerializer.SerializeToUtf8Bytes(data, JsonOptions);
        response.ContentLength64 = bytes.Length;
        response.OutputStream.Write(bytes, 0, bytes.Length);
        response.OutputStream.Close();
    }
}
