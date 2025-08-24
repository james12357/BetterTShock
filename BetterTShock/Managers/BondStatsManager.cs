using System.Timers;
using Microsoft.Xna.Framework;
using Timer = System.Timers.Timer;
// ReSharper disable InconsistentNaming

namespace BetterTShock.Managers;

public class BondStatsManager
{
    // 封装给 BondManager.cs 而不是 Plugin.cs
    private BondManager _bondManager;
    private Plugin _plugin;
    private readonly Timer _updateTimer;

    public BondStatsManager(Plugin plugin, BondManager bondManager)
    {
        _plugin = plugin;
        _bondManager = bondManager;
        // 创建一个定时器，每10秒检查一次
        _updateTimer = new Timer(10000); 
        _updateTimer.Elapsed += UpdateCoopTimes; // 绑定处理方法
        _updateTimer.AutoReset = true; // 确保它会重复触发
        _updateTimer.Start(); // 启动定时器
    }

    private void UpdateCoopTimes(Object? source, ElapsedEventArgs e)
    {
        foreach (var plr in TShock.Players.Where(p => p != null && p.Active))
        {
            if (!plr.GetData<bool>("Bonded")) continue;
            int targetUserID = plr.GetData<int>("BondedWithUserID");
            if (targetUserID <= 0) continue;
            var targetPlayer = TShock.Players.FirstOrDefault(p => p != null 
                                                                  && p.Active && p.Account.ID == targetUserID);
            if (targetPlayer == null) continue;
            
            
            string bondTimeKey = $"BondedTimeInSecondsWithUserID:{targetUserID}";
            string currentTimeStrInSeconds = _plugin.Db.ReadData(bondTimeKey) ?? "0"; // In case it doesn't exist
            
            int currentTime = int.Parse(currentTimeStrInSeconds);
            currentTime += 10;
            currentTimeStrInSeconds = currentTime.ToString();
            
            _plugin.Db.SaveData(bondTimeKey, currentTimeStrInSeconds);

        }
    }

    public void ShowBondStats(CommandArgs args)
    {
        TSPlayer plr = args.Player;
        if (plr == TSPlayer.Server) plr.SendErrorMessage("服务器控制台不能执行此操作。");
        if (!plr.GetData<bool>("Bonded"))
        {
            plr.SendErrorMessage("你没有绑定到玩家！");
            return;
        }
        int targetUserID = plr.GetData<int>("BondedWithUserID");
        if (targetUserID <= 0)
        {
            plr.SendErrorMessage("不太对劲...你可以试试重新绑定。");
            return;
        }
        var targetPlayer = TShock.Players.FirstOrDefault(p => p != null 
                                                              && p.Active && p.Account.ID == targetUserID);
        if (targetPlayer == null)
        {
            plr.SendErrorMessage("绑定的玩家当前不在线。");
            return;
        }
        
        // Found player with his companion online
        string bondTimeKey = $"BondedTimeInSecondsWithUserID:{targetUserID}";
        string currentTimeStrInSeconds = _plugin.Db.ReadData(bondTimeKey) ?? "0";
        int totalTimeInSeconds = int.Parse(currentTimeStrInSeconds);
        
        TimeSpan duration = TimeSpan.FromSeconds(totalTimeInSeconds);
        string resultStr = $"你已陪伴了 {targetPlayer.Name} {Snippets.FormatDuration(duration)}！";
        
        plr.SendMessage(resultStr, Color.White);
    }
    
    
}