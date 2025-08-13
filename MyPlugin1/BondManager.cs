using TShockAPI;
using Utils = Terraria.Utils;

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

        public void HandlePlayerDamaged(object? sender, GetDataHandlers.PlayerDamageEventArgs args)
        {
            TSPlayer plr = args.Player as TSPlayer;
            if (plr.GetData<bool>("OnDamageShare"))
            {
                args.Handled = true;
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
                int sharedDamage = (int)Math.Round(originalDamage * 0.2);
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

        public void HandleGiftCommand(CommandArgs args)
        {
            TSPlayer plr = args.Player as TSPlayer;
            if (!plr.GetData<bool>("Bonded"))
            {
                plr.SendErrorMessage("你没有绑定到玩家！");
                return;
            }
            int destIndex = plr.GetData<int>("BondedWithUserID");
            TSPlayer? targetPlayer =
                TShock.Players.FirstOrDefault(p => p != null && p.Active && p.Account.ID == destIndex);
            if (targetPlayer == null)
            {
                plr.SendErrorMessage("绑定的玩家当前不在线。");
                return;
            }

            // Item itemOnHand = plr.TPlayer.inventory[plr.TPlayer.selectedItem];
            // if (itemOnHand == null || itemOnHand.type == 0 || itemOnHand.stack == 0)
            // {
            //     plr.SendErrorMessage("此物品不合法，或是你未持有物品。");
            //     return;
            // }

            if (!targetPlayer.InventorySlotAvailable)
            {
                plr.SendErrorMessage("对方背包已满！");
                // targetPlayer.SendErrorMessage(plr.Name + "尝试向你发送" + TShock.Utils.ItemTag(itemOnHand) + "，但是你的背包满了。");
                return;
            }
            // 前置判断完成
            // targetPlayer.GiveItem(itemOnHand.type, itemOnHand.stack, itemOnHand.prefix);
            // targetPlayer.SendSuccessMessage(plr.Name + "向你发送了 " + (itemOnHand.stack == 1 ? "" : (itemOnHand.stack + " 个")) + TShock.Utils.ItemTag(itemOnHand) + "。");
            // plr.TPlayer.inventory[plr.TPlayer.selectedItem].SetDefaults(0);
            // NetMessage.SendData((int)PacketTypes.PlayerSlot, -1, -1, null, plr.Index, plr.TPlayer.selectedItem, 0); 
            // 上述方法只在开启SSC的时候有效。换一种方法：激活命令之后玩家第一次丢出物品就发送此物品。
            plr.SetData("PendingItemDrop", true);
            plr.SendSuccessMessage("成功激活给 " + targetPlayer.Name + " 的发送通道！将物品丢出即可发送。");
            targetPlayer.SendSuccessMessage(plr.Name + " 想给你送个礼物。");
        }

        public void SendDeathMessage(GetDataHandlers.PlayerDamageEventArgs args)
        {
            TSPlayer plr = args.Player as TSPlayer;
            int destIndex = plr.GetData<int>("BondedWithUserID");
            TSPlayer? targetPlayer =
                TShock.Players.FirstOrDefault(p => p != null && p.Active && p.Account.ID == destIndex);
            if (targetPlayer == null) return;
            targetPlayer.SendErrorMessage("你把 " + plr.Name + " 害死了！");
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
            targetPlayer.SetData("DamageIncreasedByBond", true);
            // 找到了幸存者！为他施加Buff。
            int buffDurationSeconds = 10; // Buff持续10秒
            
            // 计算结束时间戳。DateTime.UtcNow 是全球标准时间，可以避免时区问题。
            // .Ticks 是一个非常精确的时间单位（1 Tick = 100纳秒）。
            long buffEndTimeTicks = DateTime.UtcNow.Ticks + TimeSpan.FromSeconds(buffDurationSeconds).Ticks;
            targetPlayer.SetData("DamageIncreasedUntil", buffEndTimeTicks);
            
        }
    }
}