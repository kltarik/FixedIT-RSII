using System.Collections.Concurrent;
using FixedIT.Shared.Configuration;
using Microsoft.Extensions.Options;

namespace FixedIT.NotificationService.Consumers;

public sealed class ProcessedMessageCache(IOptions<RabbitMqOptions> options)
{
    private readonly ConcurrentDictionary<Guid, byte> _processed = new();
    private readonly ConcurrentQueue<Guid> _order = new();
    private readonly int _capacity = options.Value.ProcessedMessageCacheSize;

    public bool Contains(Guid messageId)
    {
        return _processed.ContainsKey(messageId);
    }

    public void Add(Guid messageId)
    {
        if (!_processed.TryAdd(messageId, 0))
        {
            return;
        }

        _order.Enqueue(messageId);
        while (_processed.Count > _capacity && _order.TryDequeue(out var oldest))
        {
            _processed.TryRemove(oldest, out _);
        }
    }
}
