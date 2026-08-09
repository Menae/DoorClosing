using System;

public static class GameEvents
{
    public static event Action<BeatState> OnBeatStateChanged;
    public static event Action<PlayerAction> OnPlayerCommitted;
    public static event Action<BeatOutcome> OnBeatResolved;
    public static event Action<int, int> OnLightCountChanged;

    public static void RaiseBeatStateChanged(BeatState newState)
    {
        OnBeatStateChanged?.Invoke(newState);
    }

    public static void RaisePlayerCommitted(PlayerAction action)
    {
        OnPlayerCommitted?.Invoke(action);
    }

    public static void RaiseBeatResolved(BeatOutcome outcome)
    {
        OnBeatResolved?.Invoke(outcome);
    }

    public static void RaiseLightCountChanged(int current, int max)
    {
        OnLightCountChanged?.Invoke(current, max);
    }
}
