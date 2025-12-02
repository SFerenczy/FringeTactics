using System;

namespace FringeTactics;

/// <summary>
/// Simple logging abstraction for the sim layer.
/// Allows the adapter layer to subscribe to log messages without coupling sim to Godot.
/// </summary>
public static class SimLog
{
    /// <summary>
    /// Called when a log message is emitted. Adapter layer should subscribe to this.
    /// </summary>
    public static event Action<string> OnLog;

    /// <summary>
    /// Log a message. Does nothing if no handler is subscribed.
    /// </summary>
    public static void Log(string message)
    {
        OnLog?.Invoke(message);
    }
}
