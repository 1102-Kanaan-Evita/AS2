// Assets/Scripts/GameState.cs
using System.Collections.Generic;

public static class GameState
{
    public enum Preset
    {
        RandomCircles20,
        RandomCircles30,
        RandomCircles100,
        RandomRects20,
        RandomRects30,
        RandomRects100,
        AStarShowcase,
        Office,
        Difficult
    }

    // selected preset for the next PlayScene
    public static Preset SelectedPreset = Preset.RandomCircles20;

    // set of presets that have been completed
    private static HashSet<Preset> _completed = new HashSet<Preset>();
    public static bool IsCompleted(Preset p) => _completed.Contains(p);
    public static void MarkCompleted(Preset p) { if (!_completed.Contains(p)) _completed.Add(p); }
    public static void ResetAll() { _completed.Clear(); }
}
