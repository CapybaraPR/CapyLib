using System;
using System.Collections.Generic;
using System.Linq;

namespace Capy.API.DiscordBridge;

public sealed class BridgeEventStore
{
    private readonly object _lock = new();
    private readonly DiscordBridgeConfig _config;
    private readonly List<BridgeLogEvent> _events = new();
    private long _nextId = 1;

    public BridgeEventStore(DiscordBridgeConfig config)
    {
        _config = config;
    }

    public void Append(
        BridgeLogCategory category,
        string eventType,
        string title,
        string description,
        string level,
        List<BridgeLogField>? fields = null)
    {
        lock (_lock)
        {
            long id = _nextId++;
            var ev = new BridgeLogEvent
            {
                Id = id,
                Timestamp = DateTime.UtcNow.ToString("o"),
                Category = category.ToString().ToLowerInvariant(),
                EventType = eventType,
                Title = title,
                Description = description,
                Level = level,
                Fields = fields ?? new List<BridgeLogField>()
            };

            _events.Add(ev);

            int maxBuffer = Math.Max(100, Math.Min(_config.MaxBufferedLogEvents, 10000));
            while (_events.Count > maxBuffer)
            {
                _events.RemoveAt(0);
            }
        }
    }

    public LogEventBatchResponse GetEvents(long afterId, int limit)
    {
        limit = Math.Max(1, Math.Min(limit, 100));

        lock (_lock)
        {
            List<BridgeLogEvent> matching = _events
                .Where(e => e.Id > afterId)
                .Take(limit)
                .ToList();

            long latest = _events.Count > 0 ? _events[_events.Count - 1].Id : 0;
            long oldest = _events.Count > 0 ? _events[0].Id : 0;
            bool hasMore = matching.Count == limit && matching.Count > 0 && matching[matching.Count - 1].Id < latest;

            return new LogEventBatchResponse
            {
                Events = matching,
                LatestId = latest,
                OldestId = oldest,
                HasMore = hasMore
            };
        }
    }
}
