using System;

namespace DeathcareTune.Core
{
    /// <summary>
    /// 纯数值换算，不依赖任何游戏类型，便于独立回归测试。
    /// 输入输出都是「原版基线值 -> 倍率后的目标值」，不做任何 ECS 访问。
    /// </summary>
    public static class ScaleMath
    {
        /// <summary>数量类字段的安全上限。超过上限时按上限写入并在日志里告警。</summary>
        public const int MaxCount = 100000;

        /// <summary>速率类字段的安全上限（具/次更新）。</summary>
        public const float MaxRate = 100000f;

        public static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }

        /// <summary>
        /// 速率倍率。基线为 0（例如墓地不做火化）时保持 0：0 乘任何数仍是 0，
        /// 强制改成非零会把「不处理」变成「处理」，属于改变语义，必须避免。
        /// </summary>
        public static float ScaleRate(float baseline, int multiplier)
        {
            if (!IsFinite(baseline) || baseline <= 0f || multiplier <= 1) return baseline;
            double scaled = (double)baseline * multiplier;
            if (scaled >= MaxRate) return MaxRate;
            return (float)scaled;
        }

        /// <summary>
        /// 数量倍率。基线为 0 或负数时原值返回，理由同上：0 容量有明确语义（不接收），
        /// 不能被倍率「造」出容量来。
        /// </summary>
        public static int ScaleCount(int baseline, int multiplier)
        {
            if (baseline <= 0 || multiplier <= 1) return baseline;
            long scaled = (long)baseline * multiplier;
            if (scaled > MaxCount) return MaxCount;
            return (int)scaled;
        }

        /// <summary>结果是否被上限截断，用于日志告警。</summary>
        public static bool WasRateCapped(float baseline, int multiplier)
        {
            if (!IsFinite(baseline) || baseline <= 0f || multiplier <= 1) return false;
            return (double)baseline * multiplier >= MaxRate;
        }

        /// <summary>结果是否被上限截断，用于日志告警。</summary>
        public static bool WasCountCapped(int baseline, int multiplier)
        {
            if (baseline <= 0 || multiplier <= 1) return false;
            return (long)baseline * multiplier > MaxCount;
        }
    }
}
