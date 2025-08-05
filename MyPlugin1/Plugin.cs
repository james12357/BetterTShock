global using Terraria;
global using TShockAPI;
global using TerrariaApi.Server;
global using System.Reflection;
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
    private NewPlayerManager _newPlayerManager;
    private EventDispatcher _eventDispatcher;
    private PlayerDropManager _playerDropManager;
    


    public override void Initialize()
    {
        _newPlayerManager = new NewPlayerManager(this);
        _bondManager = new BondManager(this);
        _playerDropManager = new PlayerDropManager(this);
        _eventDispatcher = new EventDispatcher(this, _bondManager, _newPlayerManager, _playerDropManager);
        Commands.ChatCommands.Add(new Command("tshock.account.logout", ChangeImmediateRespawn, "toggleresp", "tsp"));
        Commands.ChatCommands.Add(new Command("tshock.account.logout", _bondManager.HandleChangeBond, "bond", "b"));
        Commands.ChatCommands.Add(new Command("tshock.account.logout", _bondManager.HandleGiftCommand, "gift", "g"));

    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            var asm = Assembly.GetExecutingAssembly();
            Commands.ChatCommands.RemoveAll(c => c.CommandDelegate.Method?.DeclaringType?.Assembly == asm);
            _eventDispatcher.Dispose();

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

    

    // private void OnJoin(JoinEventArgs args)
    // {
    //     if (IsFirstJoin)
    //     {
    //         LastPlayer = TShock.Players[args.Who];
    //         LastJoinedTime = DateTime.Now;
    //         IsFirstJoin = false;
    //         LastPlayer.SendSuccessMessage("欢迎！您是第一位加入服务器的玩家。");
    //     }
    //     else
    //     {
    //         TShock.Players[args.Who].SendSuccessMessage("欢迎！上个加入的玩家是："  + LastPlayer.Name + 
    //                                                     "，其加入的时间为：" + LastJoinedTime.ToLongTimeString() + 
    //                                                     "，在 " + LastJoinedTime.ToLongDateString());
    //         LastPlayer = TShock.Players[args.Who];
    //         LastJoinedTime = DateTime.Now;
    //         
    //     }
    //     // 无论怎样到了这里 LastPlayer 都是当前玩家。
    //     LastPlayer.SendSuccessMessage("此服务器提供立即重生功能，输入 /toggleresp 或者 /tsp 就可以切换了！");
    //     
    // }
    


    
}