using System;
using DeathcareTune.Core;

internal static class Program
{
    private static int s_Failed;

    private static void Check(string name, bool ok, string detail = "")
    {
        if (ok) { Console.WriteLine("  PASS  " + name); return; }
        s_Failed++;
        Console.WriteLine("  FAIL  " + name + (detail.Length > 0 ? "  -> " + detail : ""));
    }

    private static int Main()
    {
        Console.WriteLine("ScaleRate");

        Check("1x 不改变任何值", ScaleMath.ScaleRate(4f, 1) == 4f, ScaleMath.ScaleRate(4f, 1).ToString());
        Check("基线 0 保持 0（墓地不火化）", ScaleMath.ScaleRate(0f, 10) == 0f);
        Check("负基线保持原值", ScaleMath.ScaleRate(-2f, 5) == -2f);
        Check("NaN 原样返回", float.IsNaN(ScaleMath.ScaleRate(float.NaN, 5)));
        Check("Infinity 原样返回", float.IsPositiveInfinity(ScaleMath.ScaleRate(float.PositiveInfinity, 5)));
        Check("倍率生效 2.5x4=10", Math.Abs(ScaleMath.ScaleRate(2.5f, 4) - 10f) < 1e-4f,
            ScaleMath.ScaleRate(2.5f, 4).ToString());
        Check("上限截断到 MaxRate", ScaleMath.ScaleRate(ScaleMath.MaxRate, 4) == ScaleMath.MaxRate);
        Check("截断标记正确", ScaleMath.WasRateCapped(ScaleMath.MaxRate, 4));
        Check("未截断时标记为 false", !ScaleMath.WasRateCapped(4f, 4));

        Console.WriteLine("ScaleCount");

        Check("1x 不改变数量", ScaleMath.ScaleCount(3, 1) == 3);
        Check("基线 0 保持 0", ScaleMath.ScaleCount(0, 10) == 0);
        Check("负基线保持原值", ScaleMath.ScaleCount(-1, 5) == -1);
        Check("倍率生效 3x3=9", ScaleMath.ScaleCount(3, 3) == 9);
        Check("上限截断", ScaleMath.ScaleCount(ScaleMath.MaxCount, 10) == ScaleMath.MaxCount);
        Check("截断不会溢出成负数", ScaleMath.ScaleCount(int.MaxValue, 10) == ScaleMath.MaxCount);
        Check("截断标记正确", ScaleMath.WasCountCapped(int.MaxValue, 10));
        Check("未截断时标记为 false", !ScaleMath.WasCountCapped(3, 3));

        Console.WriteLine(s_Failed == 0 ? "\n全部通过" : $"\n失败 {s_Failed} 项");
        return s_Failed == 0 ? 0 : 1;
    }
}
