namespace BetterTShock.Managers;

public class AvengerManager
{
    private readonly Plugin _plugin;

    public AvengerManager(Plugin plugin)
    {
        _plugin = plugin;
    }
    
    public void GiveBondBuff(GetDataHandlers.PlayerDamageEventArgs args) // args.Player是受伤害的人（玩家B）
    {
        if (args.Player == null) return;
        // A绑定B，B死之后给A添加buff
        TSPlayer plr = args.Player as TSPlayer;
        TSPlayer? targetPlayer =
            TShock.Players.FirstOrDefault(p => p != null && p.Active && p.GetData<bool>("Bonded") 
                                               && p.GetData<int>("BondedWithUserID") == plr.Account.ID);
        // 这里是玩家A
        if (targetPlayer == null) return;
        targetPlayer.SendErrorMessage(plr.Name + " 倒下了！你获得临时的伤害提升。");
        targetPlayer.SetData("DamageIncreasedByBond", true);
        // 找到了幸存者！为他施加Buff。
        int buffDurationSeconds = 10; // Buff持续10秒
            
        // 计算结束时间戳。DateTime.UtcNow 是全球标准时间，可以避免时区问题。
        // .Ticks 是一个非常精确的时间单位（1 Tick = 100纳秒）。
        long buffEndTimeTicks = DateTime.UtcNow.Ticks + TimeSpan.FromSeconds(buffDurationSeconds).Ticks;
        targetPlayer.SetData("DamageIncreasedUntil", buffEndTimeTicks);
            
    }
    
    public void OnNPCStruck(object? sender, GetDataHandlers.NPCStrikeEventArgs args)
    {
        if (args.Player == null) return;
        if (!args.Player.GetData<bool>("DamageIncreasedByBond")) return;
        long buffEndTime = args.Player.GetData<long>("DamageIncreasedUntil");

        // 2. 如果当前时间小于结束时间，说明Buff有效
        if (DateTime.UtcNow.Ticks < buffEndTime)
        {
            double damageMultiplier = 1.5; // 伤害提升50%
            int originalDamage = args.Damage;
        
            // 增加伤害
            args.Damage = (short)(originalDamage * damageMultiplier);
        }
        else
        {
            args.Player.SetData("DamageIncreasedByBond", false);
            args.Player.SetData("DamageIncreasedUntil", 0);
        }
        
    }
}