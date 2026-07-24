using System.Configuration;
using BetterTShock.Features;
using BetterTShock.Managers;
using Terraria;
using TerrariaApi.Server;
using TShockAPI;

namespace BetterTShock
{
    public class EventDispatcher
    {
        private readonly Plugin _plugin;
        private readonly BondManager _bondManager;
        private readonly NewPlayerManager _newPlayerManager;
        private readonly PlayerDropManager _playerDropManager;
        private readonly NpcDamageManager _npcDamageManager;
        private readonly GiftManager _giftManager;
        private readonly AvengerManager _avengerManager;
        private readonly ArenaManager _arenaManager;
        private readonly Store _store;
        // private readonly RespawnManager _respawnManager; // 未来可以添加

        // 构造函数，接收所有需要它调度的“部门经理”
        public EventDispatcher(Plugin plugin, BondManager bondManager, NewPlayerManager newPlayerManager,
            PlayerDropManager playerDropManager, NpcDamageManager npcDamageManager, Store store,
            GiftManager giftManager, AvengerManager avengerManager, ArenaManager arenaManager)
        {
            _plugin = plugin;
            _bondManager = bondManager;
            // _respawnManager = respawnManager;
            _newPlayerManager = newPlayerManager;
            _playerDropManager = playerDropManager;
            _npcDamageManager = npcDamageManager;
            _giftManager = giftManager;
            _avengerManager = avengerManager;
            _arenaManager = arenaManager;
            _store = store;
            // 在这里，集中注册所有我们关心的钩子
            GetDataHandlers.PlayerDamage += OnPlayerDamage;
            GetDataHandlers.PlayerSpawn += OnPlayerSpawn;
            GetDataHandlers.ItemDrop += OnItemDrop;
            GetDataHandlers.NPCStrike += OnNPCStruck;
            ServerApi.Hooks.ServerJoin.Register(_plugin, OnJoin);

        }

        // 统一的清理方法
        public void Dispose()
        {
            GetDataHandlers.PlayerDamage -= OnPlayerDamage;
            GetDataHandlers.PlayerSpawn -= OnPlayerSpawn;
            GetDataHandlers.ItemDrop -= OnItemDrop;
            GetDataHandlers.NPCStrike -= OnNPCStruck;
            ServerApi.Hooks.ServerJoin.Deregister(_plugin, OnJoin);

        }

        // 这是总的伤害处理入口
        private void OnPlayerDamage(object? sender, GetDataHandlers.PlayerDamageEventArgs args)
        {
            TSPlayer plr = args.Player as TSPlayer;
            if (plr == null || !plr.IsLoggedIn) return;

            // 这里原先直接使用 args.Damage >= statLife 作为死亡判定，这不准确，因为还需要扣除护甲。
            // 真实判定玩家死亡应依靠 Terraria 的其它机制。不过为了保持原有逻辑，我们先将其视为“承受大量伤害”的阈值。
            // 实际上更好的办法是挂载特定的 PlayerDeath 钩子，但这里为了兼容您目前的 Manager 架构，
            // 修正为您提到的逻辑：“查找谁绑定了他，并分担伤害”。

            // 1. 如果该次伤害会导致致命后果，执行复仇Buff给予和重生判断。
            if (args.Damage >= plr.TPlayer.statLife)
            {
                _avengerManager.GiveBondBuff(args);

                // 【注意】这里不再使用 SendDeathMessage，因为真正的死亡处理应该在专门的死亡事件里，
                // 或者在下面我们重构后的 BondManager 里面统一处理死亡播报。

                if (plr.GetData<bool>("WantImmediateRespawn"))
                {
                    plr.Spawn(PlayerSpawnContext.ReviveFromDeath);
                    plr.SendSuccessMessage("已重生！");
                }
            }
            else
            { 
                // 2. 将非致命伤害交由 BondManager 处理（伤害分担等）
                _bondManager.HandlePlayerDamaged(sender, args);
            }
        }

        // 这是总的重生处理入口
        private void OnPlayerSpawn(object? sender, GetDataHandlers.SpawnEventArgs args)
        {
            _bondManager.HandlePlayerSpawn(sender, args);
        }

        private void OnJoin(JoinEventArgs args)
        {
            _newPlayerManager.OnJoin(args);
        }

        private void OnItemDrop(object? sender, GetDataHandlers.ItemDropEventArgs args)
        {
            TSPlayer plr = args.Player as TSPlayer;
            if (plr == null || !plr.IsLoggedIn) return;
            // if (plr.GetData<bool>("PendingSellItemToDrop"))
            // {
            //     _store.HandlePlayerDropForSell(sender, args);
            // }
            // else
            // {
            //     _giftManager.HandleGiftDrop(args);
            // }
            DropIntent intent = plr.GetData<DropIntent>("DropIntent");
            switch (intent)
            {
                case DropIntent.Gifting:
                    _giftManager.HandleGiftDrop(args);
                    break;
                case DropIntent.Selling:
                    _store.HandlePlayerDropForSell(sender, args);
                    break;
                default:
                    // 那当然就是正常掉落
                    break;
            }
            if (intent != DropIntent.None)
            {
                args.Player.SetData("DropIntent", DropIntent.None);
            }
        }

        private void OnNPCStruck(object? sender, GetDataHandlers.NPCStrikeEventArgs args)
        {
            _avengerManager.OnNPCStruck(sender, args);
        }
    }
}