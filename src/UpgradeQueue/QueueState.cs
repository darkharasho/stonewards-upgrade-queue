using System;

namespace UpgradeQueue
{
    /// <summary>
    /// Number of level-up upgrades waiting to be picked. Choices are rolled when a queued
    /// upgrade is first opened, so only the count is stored here; choices of an upgrade put back
    /// without picking are kept by QueuedUpgradeSession.
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

        /// <summary>Sets the count after rejoining a run.</summary>
        public static void Restore(int count)
        {
            PendingCount = Math.Max(0, count);
            Changed?.Invoke(PendingCount);
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
