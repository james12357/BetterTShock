using Microsoft.Xna.Framework;
using Terraria.GameContent.ItemDropRules;

namespace MyPlugin1;

public class PlayerDropManager
{
    Plugin _plugin;
    public PlayerDropManager(Plugin plugin)
    {
        _plugin = plugin;
    }

    public void OnDrop(GetDataHandlers.ItemDropEventArgs args)
    {
        
        if (args.Player == null) return;
        TSPlayer plr = args.Player;
        if (!plr.GetData<bool>("PendingItemDrop")) return;
        if (!plr.GetData<bool>("Bonded")) return;
        int destId = plr.GetData<int>("BondedWithUserID");
        TSPlayer? targetPlayer =
            TShock.Players.FirstOrDefault(p => p != null && p.Active && p.Account.ID == destId);
        if (targetPlayer == null)
        {
            plr.SendErrorMessage("绑定的玩家当前不在线。");
            return;
        }
        args.Handled = true;
        Item itemToTransfer = new Item();
        itemToTransfer.SetDefaults(args.Type);
        itemToTransfer.prefix = args.Prefix;
        itemToTransfer.stack = args.Stacks;
        targetPlayer.GiveItem((int)args.Type, (int)args.Stacks, (int)args.Prefix);
        plr.SendSuccessMessage("已向 " + targetPlayer.Name + " 发送 " + TShock.Utils.ItemTag(itemToTransfer) + "！");
        targetPlayer.SendSuccessMessage("成功从 " + plr.Name + " 收到 " + TShock.Utils.ItemTag(itemToTransfer) + "！");
        plr.SetData("PendingItemDrop", false);
        
    }
}