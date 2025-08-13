namespace MyPlugin1;

public class NpcDamageManager
{
    Plugin _plugin;
    
    public NpcDamageManager(Plugin plugin)
    {
        _plugin = plugin;
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