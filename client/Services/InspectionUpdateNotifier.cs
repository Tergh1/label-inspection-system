using System.Collections.Concurrent;

namespace client.Services;

public sealed class InspectionUpdateNotifier
{
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<Guid, Func<InspectionUpdateNotification, Task>>> subscriptions = new();

    public IDisposable Subscribe(string ownerUserId, Func<InspectionUpdateNotification, Task> callback)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ownerUserId);
        ArgumentNullException.ThrowIfNull(callback);

        var subscriptionId = Guid.NewGuid();
        var ownerSubscriptions = subscriptions.GetOrAdd(ownerUserId, _ => new ConcurrentDictionary<Guid, Func<InspectionUpdateNotification, Task>>());
        ownerSubscriptions[subscriptionId] = callback;

        return new Subscription(() => Unsubscribe(ownerUserId, subscriptionId));
    }

    public async Task PublishAsync(string ownerUserId, Guid imageId)
    {
        if (!subscriptions.TryGetValue(ownerUserId, out var ownerSubscriptions))
        {
            return;
        }

        var notification = new InspectionUpdateNotification(ownerUserId, imageId);
        var callbacks = ownerSubscriptions.Values.ToArray();

        foreach (var callback in callbacks)
        {
            try
            {
                await callback(notification);
            }
            catch
            {
                // Ignore subscriber failures so webhook processing remains independent.
            }
        }
    }

    private void Unsubscribe(string ownerUserId, Guid subscriptionId)
    {
        if (!subscriptions.TryGetValue(ownerUserId, out var ownerSubscriptions))
        {
            return;
        }

        ownerSubscriptions.TryRemove(subscriptionId, out _);

        if (ownerSubscriptions.IsEmpty)
        {
            subscriptions.TryRemove(ownerUserId, out _);
        }
    }

    private sealed class Subscription(Action disposeAction) : IDisposable
    {
        private Action? disposeAction = disposeAction;

        public void Dispose()
        {
            Interlocked.Exchange(ref disposeAction, null)?.Invoke();
        }
    }
}

public sealed record InspectionUpdateNotification(string OwnerUserId, Guid ImageId);
