using System;

namespace Hiking.Journey
{
    public enum FocusPhase { Idle, Running }

    // 番茄钟的计时状态（需求 DEV31）。
    // 用真实时间戳推进，所以不受模拟倍率、镜头预览暂停和帧率波动影响。
    // 与旅途状态完全独立：不绑定节点，取消不惩罚也不奖励。
    public sealed class FocusSession
    {
        public FocusPhase Phase { get; private set; } = FocusPhase.Idle;
        public double DurationSeconds { get; private set; }
        public double RemainingSeconds { get; private set; }
        public bool Running => Phase == FocusPhase.Running;
        double endTime;

        public bool Start(double now, double durationSeconds)
        {
            if (Phase == FocusPhase.Running || durationSeconds <= 0) return false;
            DurationSeconds = durationSeconds;
            RemainingSeconds = durationSeconds;
            endTime = now + durationSeconds;
            Phase = FocusPhase.Running;
            return true;
        }

        public bool Cancel()
        {
            if (Phase != FocusPhase.Running) return false;
            Phase = FocusPhase.Idle;
            RemainingSeconds = 0;
            return true;
        }

        // 返回本次调用是否刚好走完一轮。完成事件只会在归零的那一次返回 true。
        public bool Tick(double now)
        {
            if (Phase != FocusPhase.Running) return false;
            RemainingSeconds = Math.Max(0, endTime - now);
            if (RemainingSeconds > 0) return false;
            Phase = FocusPhase.Idle;
            return true;
        }
    }
}
