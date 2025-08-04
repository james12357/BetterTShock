using TShockAPI;

namespace MyPlugin1
{
    public class BondManager
    {
        // 私有字段，用来存放主插件的引用
        private readonly Plugin _plugin;

        // 构造函数，当 BondManager 被创建时调用
        // 它需要一个 Plugin 类型的参数
        public BondManager(Plugin plugin)
        {
            // 将传入的 plugin 实例保存到自己的字段中
            _plugin = plugin;
            TShockAPI.GetDataHandlers.PlayerSpawn += HandlePlayerSpawn;
            TShockAPI.GetDataHandlers.PlayerDamage += HandlePlayerDamaged;
        }

        public void Dispose()
        {
            TShockAPI.GetDataHandlers.PlayerSpawn -= HandlePlayerSpawn;
            TShockAPI.GetDataHandlers.PlayerDamage -= HandlePlayerDamaged;
        }

        public void HandleChangeBond(CommandArgs args)
        {
            TSPlayer plr = args.Player;
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
                    if (p == null || !p.Active || p == plr)
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
            if (plr.GetData<bool>("Bonded"))
            {
                int destIndex = plr.GetData<int>("BondedWithUserID");
                TSPlayer? targetPlayer =
                    TShock.Players.FirstOrDefault(p => p != null && p.Active && p.Account.ID == destIndex);
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

        public void HandlePlayerDamaged(object? sender, GetDataHandlers.PlayerDamageEventArgs? args)
        {
            TSPlayer plr = args.Player as TSPlayer;
            if (plr.GetData<bool>("OnDamageShare"))
            {
                plr.SetData("OnDamageShare", false);
                return;
            }
            // --- 开始查找谁绑定了他 ---
            TSPlayer? targetPlayer = null; // 用 dest 来存储找到的“绑定者”

            foreach (TSPlayer p in TShock.Players)
            {
                // 排除无效玩家
                if (p == null || !p.Active)
                {
                    continue;
                }

                // 检查玩家 p 是否绑定了受伤的玩家 plr
                if (p.GetData<bool>("Bonded") && p.GetData<int>("BondedWithUserID") == plr.Account.ID)
                {
                    // 找到了！p 就是那个绑定者
                    targetPlayer = p;
                    break; // 既然已经找到，就跳出循环，提高效率
                }
            }

            if (targetPlayer != null)
            {
                // 在 if (targetPlayer != null) 内部
                if (targetPlayer.Dead) return;
                int originalDamage = args.Damage;
                int sharedDamage = (int)Math.Round(originalDamage * 0.1);
                int finalDamage = originalDamage - sharedDamage;
                if (sharedDamage > 0)
                {
                    targetPlayer.SetData("OnDamageShare", true);
                    targetPlayer.DamagePlayer(sharedDamage);
                    targetPlayer.SetData("OnDamageShare", false); // 造成伤害后立刻重置标记
                }
                args.Damage = (short)finalDamage;
            }
            
        }
    }
}