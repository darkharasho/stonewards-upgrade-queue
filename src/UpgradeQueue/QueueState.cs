using System;

namespace UpgradeQueue
{
    /// <summary>
    /// Number of level-up upgrades waiting to be picked. Choices are rolled when a queued
    /// upgrade is opened, so only the count is stored.
    /// </summary>
    internal static class QueueState
    {
        public static int PendingCount { get; private set; }

        public static event Action<int> Changed;

        public static void Enqueue()
        {
            PendingCount++;
            Changed?.Invoke(PendingCount);
        }

        public static bool TryDequeue()
        {
            if (PendingCount == 0)
                return false;
            PendingCount--;
            Changed?.Invoke(PendingCount);
            return true;
        }

        public static void Clear()
        {
            if (PendingCount == 0)
                return;
            PendingCount = 0;
            Changed?.Invoke(PendingCount);
        }
    }
}
