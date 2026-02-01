using UnityEngine;

namespace AvatarInfection.Utilities;

// https://github.com/Lakatrazz/BONELAB-Fusion/blob/main/LabFusion/src/Utilities/Internal/TimeUtilities.cs
internal static class TimeUtilities
{
    public static float DeltaTime
    { get => _deltaTime; }

    public static float FixedDeltaTime
    { get => _fixedDeltaTime; }

    public static float TimeSinceStartup
    { get => _timeSinceStartup; }

    public static float TimeScale
    { get => _timeScale; }

    public static int FrameCount
    { get => _frameCount; }

    private static float _deltaTime = 1f;
    private static float _fixedDeltaTime = 0.02f;
    private static float _timeSinceStartup = 0f;

    private static float _timeScale = 1f;

    private static int _frameCount = 0;

    internal static void OnEarlyUpdate()
    {
        _timeScale = Time.timeScale;

        _deltaTime = Time.deltaTime;
        _timeSinceStartup += _deltaTime;

        _frameCount++;
    }

    internal static void OnEarlyFixedUpdate()
        => _fixedDeltaTime = Time.fixedDeltaTime;

    public static bool IsMatchingFrame(int interval)
        => FrameCount % interval == 0;

    public static bool IsMatchingFrame(int interval, int offset)
        => (FrameCount + offset) % interval == 0;
}