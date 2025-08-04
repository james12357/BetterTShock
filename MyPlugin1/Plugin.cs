using System.Reflection;
using Microsoft.Xna.Framework;
using Terraria;
using TerrariaApi.Server;
using TShockAPI;
namespace MyPlugin1;

[ApiVersion(2, 1)]
public class Plugin : TerrariaPlugin
{
    public Plugin(Main game) : base(game)
    {
    }

    public override string Name => "BetterTShock";
    public override string Author => "Junxi Cai";
    public override string Description => "None";
    public override Version Version => new Version(1, 0);
    private BondManager _bondManager;

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
    


    public override void Initialize()
    {
        _bondManager = new BondManager(this);
        Commands.ChatCommands.Add(new Command("tshock.account.logout", ChangeImmediateRespawn, "toggleresp", "tsp"));
        Commands.ChatCommands.Add(new Command("tshock.account.logout", _bondManager.HandleChangeBond, "bond", "b"));
        ServerApi.Hooks.ServerJoin.Register(this, OnJoin);
        TShockAPI.GetDataHandlers.PlayerDamage += OnPlayerDamaged;
        LastJoinedTime = DateTime.Now;
        IsFirstJoin = true;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            var asm = Assembly.GetExecutingAssembly();
            Commands.ChatCommands.RemoveAll(c => c.CommandDelegate.Method?.DeclaringType?.Assembly == asm);
            ServerApi.Hooks.ServerJoin.Deregister(this, OnJoin);
            TShockAPI.GetDataHandlers.PlayerDamage -= OnPlayerDamaged;
            _bondManager.Dispose();

        }
        base.Dispose(disposing);
    }
    
    private void ChangeImmediateRespawn(CommandArgs args)
    {
        TSPlayer plr = args.Player as TSPlayer;
        // plr.SetData("WantImmediateRespawn", plr.GetData<bool>("WantImmediateRespawn") == false);
        // 天才的写法！ true 会返回 false，反之亦然。
        plr.SetData("WantImmediateRespawn", !plr.GetData<bool>("WantImmediateRespawn"));
        if (plr.GetData<bool>("WantImmediateRespawn"))
        {
            plr.SendSuccessMessage("立即重生设置成功！");
        }
        else
        {
            plr.SendSuccessMessage("已关闭立即重生！");
        }
    }

    

    private void OnJoin(JoinEventArgs args)
    {
        if (IsFirstJoin)
        {
            LastPlayer = TShock.Players[args.Who];
            LastJoinedTime = DateTime.Now;
            IsFirstJoin = false;
            LastPlayer.SendSuccessMessage("欢迎！您是第一位加入服务器的玩家。");
        }
        else
        {
            TShock.Players[args.Who].SendSuccessMessage("欢迎！上个加入的玩家是："  + LastPlayer.Name + 
                                                        "，其加入的时间为：" + LastJoinedTime.ToLongTimeString() + 
                                                        "，在 " + LastJoinedTime.ToLongDateString());
            LastPlayer = TShock.Players[args.Who];
            LastJoinedTime = DateTime.Now;
            
        }
        // 无论怎样到了这里 LastPlayer 都是当前玩家。
        LastPlayer.SendSuccessMessage("此服务器提供立即重生功能，输入 /toggleresp 或者 /tsp 就可以切换了！");
        
    }
    
    private void OnPlayerDamaged(object? sender, GetDataHandlers.PlayerDamageEventArgs? args)
    {
        TSPlayer plr = args.Player as TSPlayer;
        if (plr.GetData<bool>("WantImmediateRespawn") && args.Damage >= plr.TPlayer.statLife)
        { 
            plr.Spawn(PlayerSpawnContext.ReviveFromDeath);
            plr.SendSuccessMessage("已重生！");
        }
    }

    
}