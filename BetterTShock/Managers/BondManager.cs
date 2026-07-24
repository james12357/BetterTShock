namespace BetterTShock.Managers;

public class BondManager
{
    // 私有字段，用来存放主插件的引用
    private readonly Plugin _plugin;
    private BondStatsManager _bondStatsManager;

    // 构造函数，当 BondManager 被创建时调用
    // 它需要一个 Plugin 类型的参数
    public BondManager(Plugin plugin)
    {
        // 将传入的 plugin 实例保存到自己的字段中
        _plugin = plugin;
        _bondStatsManager = new BondStatsManager(_plugin, this);
    }
        

    public void HandleChangeBond(CommandArgs args)
    {
        TSPlayer plr = args.Player;
        if (plr == null) return;

        if (!plr.IsLoggedIn)
        {
            plr.SendErrorMessage("你需要登录后才能使用绑定功能。");
            return;
        }

        // 使用 ! 来切换布尔值，非常简洁
        plr.SetData("Bonded", !plr.GetData<bool>("Bonded"));

        // 注意：这里的键名应该和你SetData时保持一致
        if (plr.GetData<bool>("Bonded"))
        {
            TSPlayer? dest = null; // 1. 初始化为 null，并使用可空类型 ?
            double minDistanceSquared = double.MaxValue;

            // 2. 遍历 TShock.Players 列表更安全高效
            foreach (var p in TShock.Players)
            {
                // 排除无效玩家和自己
                if (p == null || !p.Active || p == plr || !p.IsLoggedIn)
                {
                    continue;
                }

                // 计算距离的平方，避免开方运算
                double dx = p.X - plr.X;
                double dy = p.Y - plr.Y;
                double distanceSquared = (dx * dx + dy * dy);

                if (distanceSquared < minDistanceSquared)
                {
                    minDistanceSquared = distanceSquared;
                    dest = p; // 3. 直接赋值 TSPlayer 对象，而不是去查找
                }
            }

            // 4. 通过判断 dest 是否为 null 来确定是否找到了玩家
            if (dest == null)
            {
                plr.SendErrorMessage("附近没有其他玩家可以绑定。");
                // 绑定失败，把状态改回去
                plr.SetData("Bonded", false);
            }
            else
            {
                // 存储被绑定玩家的用户ID更可靠，因为Name可以改，ID是唯一的
                plr.SetData("BondedWithUserID", dest.Account.ID);
                plr.SendSuccessMessage($"已与最近的玩家 {dest.Name} 绑定！重生后会自动回到Ta身旁。");
            }
        }
        else
        {
            plr.SendSuccessMessage("已解除绑定！");
            // 解除绑定时，清掉绑定的对象ID
            plr.SetData<int>("BondedWithUserID", -1);
        }
    }

    public void HandlePlayerSpawn(object? sender, GetDataHandlers.SpawnEventArgs args)
    {
        TSPlayer plr = args.Player as TSPlayer;
        if (plr == null) return;

        if (plr.GetData<bool>("Bonded"))
        {
            int destIndex = plr.GetData<int>("BondedWithUserID");
            TSPlayer? targetPlayer =
                TShock.Players.FirstOrDefault(p => p != null && p.Active && p.IsLoggedIn && p.Account.ID == destIndex);
            if (targetPlayer != null)
            {
                plr.Teleport(targetPlayer.X, targetPlayer.Y);
            }
            else
            {
                plr.SendErrorMessage("绑定的玩家不在线，已解除绑定。");
                plr.SetData("Bonded", false);
                plr.SetData("BondedWithUserID", -1);
            }
        }
    }

    public void HandlePlayerDamaged(object? sender, GetDataHandlers.PlayerDamageEventArgs args)
    {
        TSPlayer plr = args.Player as TSPlayer;
        if (plr == null || !plr.IsLoggedIn) return;

        if (plr.GetData<bool>("OnDamageShare"))
        {
            args.Handled = true;
            plr.SetData("OnDamageShare", false);
            return;
        }
        // --- 优化：直接通过判断对方是否互相绑定来避免全服查找，
        // --- 但既然目前的逻辑是单向绑定（A绑定了B，A承受B的伤害），我们依然查找谁绑定了plr。
        // --- 这里使用 LINQ 进行简化，使得代码更加优雅。

        TSPlayer? targetPlayer = TShock.Players.FirstOrDefault(p =>
            p != null && p.Active && p.IsLoggedIn &&
            p.GetData<bool>("Bonded") &&
            p.GetData<int>("BondedWithUserID") == plr.Account.ID);

        if (targetPlayer != null)
        {
            if (targetPlayer.Dead) return;

            int originalDamage = args.Damage;
            int sharedDamage = (int)Math.Round(originalDamage * Plugin.Config.BondDamageSharingRatio);
            int finalDamage = originalDamage - sharedDamage;

            if (sharedDamage > 0)
            {
                targetPlayer.SetData("OnDamageShare", true);

                // 为了让分担的伤害看起来更自然，并且不破坏原有的死亡信息（DamagePlayer直接扣血如果是致命的不会有正常击杀提示）
                // 我们直接设置 statLife，而不是调用 DamagePlayer，或者仍然调用 DamagePlayer 但需确保不会因此导致意外错误。
                // 考虑到兼容性，保留 DamagePlayer，但我们要判断一下是否会致死。
                if (targetPlayer.TPlayer.statLife > sharedDamage)
                {
                    targetPlayer.DamagePlayer(sharedDamage);
                }
                else
                {
                    // 如果分担的伤害会导致绑定者死亡，则让他剩 1 滴血，避免尴尬的非正常死亡。
                    targetPlayer.DamagePlayer(targetPlayer.TPlayer.statLife - 1);
                }
            }
            args.Damage = (short)finalDamage;
        }
            
    }

    public void SendDeathMessage(GetDataHandlers.PlayerDamageEventArgs args)
    {
        TSPlayer plr = args.Player as TSPlayer; // 这里是已死的
        if (plr == null) return;

        int destIndex = plr.GetData<int>("BondedWithUserID");
        TSPlayer? targetPlayer =
            TShock.Players.FirstOrDefault(p => p != null && p.Active && p.IsLoggedIn && p.Account.ID == destIndex);
        if (targetPlayer == null) return;
        targetPlayer.SendErrorMessage("你把 " + plr.Name + " 害死了！");
    }

    public void HandleShowBondStats(CommandArgs args)
    {
        _bondStatsManager.ShowBondStats(args);
    }
        
}