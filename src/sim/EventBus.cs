using System;
using System.Collections.Generic;
using System.Linq;

namespace FringeTactics;

/// <summary>
/// Minimal typed event bus for cross-domain communication.
/// Domains publish events, adapters and other domains subscribe.
/// </summary>
public class EventBus
{
    private readonly Dictionary<Type, List<Delegate>> subscribers = new();

    /// <summary>
    /// Subscribe to events of type TEvent.
    /// </summary>
    public void Subscribe<TEvent>(Action<TEvent> handler) where TEvent : struct
    {
        var type = typeof(TEvent);
        if (!subscribers.TryGetValue(type, out var handlers))
        {
            handlers = new List<Delegate>();
            subscribers[type] = handlers;
        }

        if (!handlers.Contains(handler))
        {
            handlers.Add(handler);
        }
    }

    /// <summary>
    /// Unsubscribe from events of type TEvent.
    /// </summary>
    public void Unsubscribe<TEvent>(Action<TEvent> handler) where TEvent : struct
    {
        var type = typeof(TEvent);
        if (subscribers.TryGetValue(type, out var handlers))
        {
            handlers.Remove(handler);
        }
    }

    /// <summary>
    /// Publish an event to all subscribers.
    /// </summary>
    public void Publish<TEvent>(TEvent evt) where TEvent : struct
    {
        var type = typeof(TEvent);
        if (!subscribers.TryGetValue(type, out var handlers))
        {
            return;
        }

        foreach (var handler in handlers.ToArray())
        {
            try
            {
                ((Action<TEvent>)handler)(evt);
            }
            catch (Exception ex)
            {
                SimLog.Log($"[EventBus] Error handling {type.Name}: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Clear all subscribers. Used for cleanup/testing.
    /// </summary>
    public void Clear()
    {
        subscribers.Clear();
    }

    /// <summary>
    /// Get subscriber count for a specific event type. For testing/debugging.
    /// </summary>
    public int GetSubscriberCount<TEvent>() where TEvent : struct
    {
        var type = typeof(TEvent);
        return subscribers.TryGetValue(type, out var handlers) ? handlers.Count : 0;
    }
}
