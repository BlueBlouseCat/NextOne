using System;

public static class GlobalInteractionLock
{
    private static int _lockCount;

    public static bool IsLocked => _lockCount > 0;

    public static event Action<bool> StateChanged;

    public static void Acquire()
    {
        bool wasLocked = IsLocked;
        _lockCount++;

        if (!wasLocked && IsLocked)
            StateChanged?.Invoke(true);
    }

    public static void Release()
    {
        if (_lockCount <= 0)
            return;

        _lockCount--;

        if (_lockCount == 0)
            StateChanged?.Invoke(false);
    }

    public static void Reset()
    {
        if (_lockCount == 0)
            return;

        _lockCount = 0;
        StateChanged?.Invoke(false);
    }
}
