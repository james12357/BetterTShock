namespace BetterTShock.Managers;

public class GiftManager
{
    private readonly Plugin _plugin;

    public GiftManager(Plugin plugin)
    {
        _plugin = plugin;
    }
    
    public void HandleGiftCommand(CommandArgs args)
    {
        TSPlayer plr = args.Player as TSPlayer;
        if (!plr.GetData<bool>("Bonded"))
        {
            plr.SendErrorMessage("你没有绑定到玩家！");
            return;
        }

        if (plr.GetData<DropIntent>("DropIntent") == DropIntent.Gifting)
        {
            plr.SetData("DropIntent", DropIntent.None);
            plr.SendSuccessMessage("发送通道已关闭。");
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
        plr.SetData("DropIntent", DropIntent.Gifting);
        plr.SendSuccessMessage("成功激活给 " + targetPlayer.Name + " 的发送通道！将物品丢出即可发送。");
        targetPlayer.SendSuccessMessage(plr.Name + " 想给你送个礼物。");
    }
    
    public void HandleGiftDrop(GetDataHandlers.ItemDropEventArgs args)
    {
        if (args.Player == null) return;
        TSPlayer plr = args.Player;
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
        plr.SetData("DropIntent", DropIntent.None);
        
    }
}