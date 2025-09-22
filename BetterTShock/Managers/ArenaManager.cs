using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System.Linq; // [新增] 需要这个来使用 FirstOrDefault
using System.Timers;
using Terraria.DataStructures;
using Timer = System.Timers.Timer;

namespace BetterTShock.Managers
{
    public enum ArenaState
    {
        Idle,
        InProgress,
        Cooldown
        // (可选) 未来可以添加 Cooldown 状态
    }

    public class ArenaManager
    {
        private readonly Plugin _plugin;
        private ArenaState _currentState = ArenaState.Idle;
        private Timer _updateTimer = new Timer(2000); 
        
        // [新增] 用于追踪当前波数
        private int _currentWave = -1;
        private DateTime _cooldownStartTime;
        
        private Rectangle _arenaBounds;
        private readonly List<TSPlayer> _playersInArena = new();

        public ArenaManager(Plugin plugin)
        {
            _plugin = plugin;
        }
        
        public void Dispose()
        {
            _updateTimer?.Stop();
            _updateTimer?.Dispose();
        }

        public void HandleArenaCommand(CommandArgs args) // [命名修正] 我将您的方法名改回了 HandleArenaCommand
        {
            var plr = args.Player;
            var subCommand = args.Parameters.Count > 0 ? args.Parameters[0].ToLower() : "help";

            switch (subCommand)
            {
                case "start":
                    if (_currentState != ArenaState.Idle)
                    {
                        plr.SendErrorMessage("已经有一场挑战正在进行中！");
                        return;
                    }

                    int size = Plugin.Config.ArenaRadius * 2;
                    _arenaBounds = new Rectangle(plr.TileX - size / 2, plr.TileY - size / 2, size, size);

                    _playersInArena.Clear(); // [新增] 开始前清空上一场的玩家列表
                    _playersInArena.Add(plr);
                    
                    TShock.Utils.Broadcast($"{plr.Name} 在 ({plr.TileX}, {plr.TileY}) 开启了竞技场挑战！", Color.MediumPurple);
                    StartGame();
                    break;
                // ...
            }
        }
        
        private void StartGame()
        {
            // [新增] 在游戏开始时重置状态并绑定事件
            _currentWave = -1;
            _updateTimer.Elapsed += OnTimerElapsed;
            _updateTimer.AutoReset = true;
            _updateTimer.Start();
            StartNextWave();
        }

        private void OnTimerElapsed(object sender, ElapsedEventArgs e)
        {
            Main.QueueMainThreadAction(() => OnGameLoopCheck(sender, e));
        }

        private void StartNextWave()
        {
            _currentWave++;
            // 检查是否所有波数都已完成
            if (_currentWave >= Plugin.Config.Waves.Count)
            {
                EndGame(true); // 胜利！
                return;
            }
            _currentState = ArenaState.InProgress;

            var wave = Plugin.Config.Waves[_currentWave];
            TShock.Utils.Broadcast($"[竞技场] {wave.StartMessage}", Color.Cyan);

            foreach (var monsterToSpawn in wave.Monsters)
            {
                for (int i = 0; i < monsterToSpawn.SpawnCount; i++)
                {
                    if (FindSafeSpawnTile(out int x, out int y))
                    {
                        IEntitySource src = new EntitySource_SpawnNPC();
                        int idx = Terraria.NPC.NewNPC(src, x * 16, y * 16, monsterToSpawn.NpcId);
                        if (idx >= 0 && idx < Main.maxNPCs)
                        {
                            NetMessage.SendData((int)PacketTypes.NpcUpdate, -1, -1, null, idx);
                        }
                    }
                    else
                    {
                        TShock.Log.ConsoleInfo($"[竞技场] 无法为 {Lang.GetNPCNameValue(monsterToSpawn.NpcId)} 找到安全的刷新点。");
                    }
                }
            }
        }
        
        // 您的 FindSafeSpawnTile 方法，我完全没有改动
        private bool FindSafeSpawnTile(out int x, out int y)
        {
            x = 0;
            y = 0;
            for (int i = 0; i < 50; i++)
            {
                int randomX = Main.rand.Next(_arenaBounds.Left, _arenaBounds.Right);
                for (int currentY = _arenaBounds.Top; currentY < _arenaBounds.Bottom; currentY++)
                {
                    if (Main.tile[randomX, currentY].active())
                    {
                        if (!Main.tile[randomX, currentY - 1].active() &&
                            !Main.tile[randomX, currentY - 2].active() &&
                            !Main.tile[randomX, currentY - 3].active())
                        {
                            x = randomX;
                            y = currentY - 1;
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        // [新增] 补全了游戏循环检查的逻辑
        private void OnGameLoopCheck(object? o, ElapsedEventArgs e)
        {
            // --- 如果正在休息，则检查休息时间是否结束 ---
            if (_currentState == ArenaState.Cooldown)
            {
                // 休息5秒
                if ((DateTime.UtcNow - _cooldownStartTime).TotalSeconds >= 5)
                {
                    StartNextWave(); // 休息结束，开始下一波
                }
                return; // 在休息期间，不做其他检查
            }

            // --- 如果波数正在进行中，则检查胜负 ---
            if (_currentState != ArenaState.InProgress) return;

            // 1. 检查玩家是否全部阵亡（失败条件）
            for (int i = _playersInArena.Count - 1; i >= 0; i--)
            {
                var p = _playersInArena[i];
                if (p == null || !p.Active || p.Dead)
                {
                    _playersInArena.RemoveAt(i);
                }
            }

            if (_playersInArena.Count == 0)
            {
                EndGame(false); // 所有玩家阵亡，游戏失败
                return;
            }

            // 2. 检查怪物是否全部被消灭
            bool monstersAlive = false;
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                var npc = Main.npc[i];
                if (npc != null && npc.active && !npc.friendly && npc.life > 0 &&
                    _arenaBounds.Contains((int)(npc.position.X / 16), (int)(npc.position.Y / 16)))
                {
                    monstersAlive = true;
                    break;
                }
            }

            if (!monstersAlive)
            {
                // 所有怪物都被消灭！进入休息状态
                TShock.Utils.Broadcast($"[竞技场] 第 {_currentWave + 1} 波已肃清！", Color.Green);
                _currentState = ArenaState.Cooldown; // <--- [关键] 切换到休息状态
                _cooldownStartTime = DateTime.UtcNow; // <--- [关键] 记录休息开始时间
                TShock.Utils.Broadcast("[竞技场] 5秒后，下一波即将开始！", Color.Yellow);
            }
        }

        // [新增] 游戏结束处理方法
        private void EndGame(bool victory)
        {
            if (victory)
            {
                TShock.Utils.Broadcast("[竞技场] 恭喜！你们成功完成了所有挑战！", Color.Gold);
                foreach (var plr in _playersInArena)
                {
                    plr.GiveItem(Terraria.ID.ItemID.GoldCoin, 10);
                }
            }
            else
            {
                TShock.Utils.Broadcast("[竞技场] 挑战失败！", Color.Red);
            }
            
            _currentState = ArenaState.Idle; // <--- 确保状态重置为 Idle
            _currentWave = -1;
            _playersInArena.Clear();
            _updateTimer.Stop();
            _updateTimer.Elapsed -= OnGameLoopCheck;
        }
    }
}