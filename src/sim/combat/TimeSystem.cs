using Godot; // For Mathf only - no Node/UI types
using System;

namespace FringeTactics;

public partial class TimeSystem
{
    public const int TicksPerSecond = 20;
    public const float TickDuration = 1.0f / TicksPerSecond;

    public int CurrentTick { get; private set; } = 0;
    public bool IsPaused { get; private set; } = true;
    public float TimeScale { get; private set; } = 1.0f;
    private float accumulator = 0.0f;

    // C# Events
    public event Action<int> TickAdvanced;
    public event Action<bool> PauseChanged;
    public event Action<float> TimeScaleChanged;

    public TimeSystem()
    {
        CurrentTick = 0;
        IsPaused = true;
        accumulator = 0.0f;
    }

    public int Update(float dt)
    {
        // Process time and return number of ticks advanced.
        if (IsPaused)
        {
            return 0;
        }

        accumulator += dt * TimeScale;
        var ticksAdvanced = 0;

        while (accumulator >= TickDuration)
        {
            accumulator -= TickDuration;
            CurrentTick += 1;
            ticksAdvanced += 1;
            TickAdvanced?.Invoke(CurrentTick);
        }

        return ticksAdvanced;
    }

    public void Pause()
    {
        IsPaused = true;
        PauseChanged?.Invoke(true);
    }

    public void Resume()
    {
        IsPaused = false;
        SimLog.Log("TimeSystem: RESUMED");
        PauseChanged?.Invoke(false);
    }

    public void TogglePause()
    {
        if (IsPaused)
        {
            Resume();
        }
        else
        {
            Pause();
        }
    }

    public void SetTimeScale(float scale)
    {
        var newScale = Mathf.Clamp(scale, 0.1f, 4.0f);
        if (newScale != TimeScale)
        {
            TimeScale = newScale;
            TimeScaleChanged?.Invoke(TimeScale);
        }
    }

    public float GetCurrentTime()
    {
        return (float)CurrentTick / TicksPerSecond;
    }
}
