using On.Terraria.GameContent;
using TShockAPI;
using Terraria;
using TerrariaApi.Server;

namespace BetterTShock;

public class NewPlayerManager
{
    private TimeZoneInfo targetZone;
    private Plugin _plugin;
    public NewPlayerManager(Plugin plugin)
    {
        _plugin = plugin;
        LastJoinedTime = DateTime.UtcNow;
        IsFirstJoin = true;
        targetZone = TimeZoneInfo.FindSystemTimeZoneById(Plugin.Config.TimeZoneString);
        LastJoinedTime = TimeZoneInfo.ConvertTimeFromUtc(LastJoinedTime, targetZone);

    }
    
    private bool IsFirstJoin
    {
        get;
        set;
    }
    private DateTime LastJoinedTime
    {
        get;
        set;
    }

    private TSPlayer? LastPlayer
    {
        get;
        set;
    }

    public void OnJoin(JoinEventArgs args)
    {
        if (IsFirstJoin)
        {
            LastPlayer = TShock.Players[args.Who];
            LastJoinedTime = DateTime.UtcNow;
            LastJoinedTime = TimeZoneInfo.ConvertTimeFromUtc(LastJoinedTime, targetZone);
            IsFirstJoin = false;
            LastPlayer.SendSuccessMessage("欢迎！您是第一位加入服务器的玩家。");
        }
        else
        {
            TShock.Players[args.Who].SendSuccessMessage("欢迎！上个加入的玩家是："  + LastPlayer.Name + 
                                                        "，其加入的时间为：" + LastJoinedTime.ToLongTimeString() + 
                                                        "，在 " + LastJoinedTime.ToLongDateString() + " (" + 
                                                        Plugin.Config.TimeZoneString + ")");
            LastPlayer = TShock.Players[args.Who];
            LastJoinedTime = DateTime.UtcNow;
            LastJoinedTime = TimeZoneInfo.ConvertTimeFromUtc(LastJoinedTime, targetZone);
            
        }
        // 无论怎样到了这里 LastPlayer 都是当前玩家。
        LastPlayer.SendSuccessMessage("此服务器提供立即重生功能，输入 /toggleresp 或者 /tsp 就可以切换了！");
    }
}