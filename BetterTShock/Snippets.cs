namespace BetterTShock;

public static class Snippets
{
    public static string FormatDuration(TimeSpan duration)
    {
        var parts = new List<string>();
        if (duration.Seconds == 0) return "0 秒";
        
        if (duration.Days > 0)
        {
            parts.Add($"{duration.Days} 天");
        }
        if (duration.Hours > 0)
        {
            parts.Add($"{duration.Hours} 小时");
        }
        if (duration.Minutes > 0)
        {
            parts.Add($"{duration.Minutes} 分钟");
        }
        // 即使前面有小时或分钟，也总是显示秒，或者当总时间小于1分钟时显示
        if (duration.Seconds > 0 || parts.Count == 0)
        {
            parts.Add($"{duration.Seconds} 秒");
        }

        return string.Join(" ", parts);
    }
    
    /// <summary>
    /// 代表泰拉瑞亚游戏中的货币组合。
    /// </summary>
    public struct Coinage
    {
        public int Platinum { get; set; }
        public int Gold { get; set; }
        public int Silver { get; set; }
        public int Copper { get; set; }

        public override string ToString()
        {
            return $"{Platinum} 铂金币, {Gold} 金币, {Silver} 银币, {Copper} 铜币";
        }
    }

    /// <summary>
    /// 将总铜币数转换为最简化的铂金、金、银、铜币组合。
    /// </summary>
    /// <param name="totalCopper">要转换的铜币总数。</param>
    /// <returns>一个包含最简化货币组合的 Coinage 对象。</returns>
    public static Coinage ConvertCopperToCoinage(long totalCopper)
    {
        if (totalCopper < 0)
        {
            // 对于负数输入，返回一个空的Coinage对象
            // 更好的做法是抛出异常： throw new ArgumentOutOfRangeException(nameof(totalCopper), "铜币数不能为负。");
            return new Coinage(); 
        }

        // 定义转换率常量
        // ReSharper disable InconsistentNaming
        const int COPPER_PER_SILVER = 100;
        const int SILVER_PER_GOLD = 100;
        const int GOLD_PER_PLATINUM = 100;

        const long COPPER_PER_GOLD = COPPER_PER_SILVER * SILVER_PER_GOLD; // 10,000
        const long COPPER_PER_PLATINUM = COPPER_PER_GOLD * GOLD_PER_PLATINUM; // 1,000,000

        Coinage result = new Coinage();

        result.Platinum = (int)(totalCopper / COPPER_PER_PLATINUM);
        long remainingCopper = totalCopper % COPPER_PER_PLATINUM;

        result.Gold = (int)(remainingCopper / COPPER_PER_GOLD);
        remainingCopper %= COPPER_PER_GOLD;

        result.Silver = (int)(remainingCopper / COPPER_PER_SILVER);
        result.Copper = (int)(remainingCopper % COPPER_PER_SILVER);

        return result;
    }
}