using System.Configuration;
using Terraria;
using TerrariaApi.Server;
using TShockAPI;

namespace MyPlugin1
{
    public class EventDispatcher
    {
        private readonly Plugin _plugin;
        private readonly BondManager _bondManager;
        private readonly NewPlayerManager _newPlayerManager;
        private readonly PlayerDropManager _playerDropManager;
        // private readonly RespawnManager _respawnManager; // 未来可以添加

        // 构造函数，接收所有需要它调度的“部门经理”
        public EventDispatcher(Plugin plugin, BondManager bondManager, NewPlayerManager newPlayerManager,
            PlayerDropManager playerDropManager/*, RespawnManager respawnManager*/)
        {
            _plugin = plugin;
            _bondManager = bondManager;
            // _respawnManager = respawnManager;
            _newPlayerManager = newPlayerManager;
            _playerDropManager = playerDropManager;
            
            // 在这里，集中注册所有我们关心的钩子
            GetDataHandlers.PlayerDamage += OnPlayerDamage;
            GetDataHandlers.PlayerSpawn += OnPlayerSpawn;
            GetDataHandlers.ItemDrop += OnItemDrop;
            ServerApi.Hooks.ServerJoin.Register(_plugin, OnJoin);

        }

        // 统一的清理方法
        public void Dispose()
        {
            GetDataHandlers.PlayerDamage -= OnPlayerDamage;
            GetDataHandlers.PlayerSpawn -= OnPlayerSpawn;
            GetDataHandlers.ItemDrop -= OnItemDrop;
            ServerApi.Hooks.ServerJoin.Deregister(_plugin, OnJoin);

        }

        // 这是总的伤害处理入口
        private void OnPlayerDamage(object? sender, GetDataHandlers.PlayerDamageEventArgs args)
        {
            // 在这里，我们“决定”谁来处理
            
            // 比如，未来可以先调用重生管理器的逻辑
            // bool handled = _respawnManager.HandleImmediateRespawn(args);
            // if (handled) return;

            // 如果没被处理，再调用绑定管理器的逻辑
            TSPlayer plr = args.Player as TSPlayer;
            if (plr.GetData<bool>("WantImmediateRespawn") && args.Damage >= plr.TPlayer.statLife)
            { 
                plr.Spawn(PlayerSpawnContext.ReviveFromDeath);
                plr.SendSuccessMessage("已重生！");
            }
            else
            { 
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
            _playerDropManager.OnDrop(args);
        }
    }
}