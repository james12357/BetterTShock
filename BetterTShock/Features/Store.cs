using BetterTShock;
// ReSharper disable InconsistentNaming
namespace BetterTShock.Features;

public class Store
{
    private readonly Plugin _plugin;
    
    public Store(Plugin plugin)
    {
        _plugin = plugin;
    }

    public void HandleStoreCommand(CommandArgs args)
    {
        TSPlayer plr = args.Player;
        if (plr == TSPlayer.Server)
        {
            plr.SendErrorMessage("服务器控制台上不能执行此操作。");
            return;
        }

        Item itemOnHand = plr.TPlayer.inventory[plr.TPlayer.selectedItem];
        if (plr.GetData<DropIntent>("DropIntent") == DropIntent.Selling)
        {
            plr.SetData("DropIntent", DropIntent.None);
            plr.SendSuccessMessage("售卖通道已关闭。");
            return;
        }
        
        
        plr.SetData("DropIntent", DropIntent.Selling);
        plr.SendSuccessMessage("丢出物品即可快速售卖！");
    }

    public void HandlePlayerDropForSell(object? sender, GetDataHandlers.ItemDropEventArgs args)
    {
        if (args.Player == null) return;
        args.Handled = true;
        TSPlayer plr = args.Player;
        plr.SetData("DropIntent", DropIntent.None);
        Item itemToSell = new Item();
        itemToSell.SetDefaults(args.Type);
        itemToSell.prefix = args.Prefix;
        itemToSell.stack = args.Stacks;
        int value = itemToSell.GetStoreValue();
        if (value == 0)
        {
            plr.SendErrorMessage("东西不值钱。");
            return;
        }
        Snippets.Coinage result = Snippets.ConvertCopperToCoinage(value);
        // 游戏内有专门的钱币槽，所以不用看有没有空位置
        // 不知道有没有更好的方法，构造里面好像传不了这两个参
        plr.GiveItem(71, result.Copper);
        Item CopperItem = new Item();
        CopperItem.type = 71;
        CopperItem.stack = result.Copper;
        
        plr.GiveItem(72, result.Silver);
        Item SilverItem = new Item();
        SilverItem.type = 72;
        SilverItem.stack = result.Silver;
        
        plr.GiveItem(73, result.Gold);
        Item GoldItem = new Item();
        GoldItem.type = 73;
        GoldItem.stack = result.Gold;
        
        plr.GiveItem(74, result.Platinum);
        Item PlatinumItem = new Item();
        PlatinumItem.type = 74;
        PlatinumItem.stack = result.Platinum;
        var ItemTag = TShock.Utils.ItemTag;
        plr.SendSuccessMessage($"已售卖 {ItemTag(itemToSell)}！获得 {ItemTag(PlatinumItem)}，{ItemTag(GoldItem)}" +
                               $"，{ItemTag(SilverItem)} 和 {ItemTag(CopperItem)}。");
        plr.SetData("DropIntent", DropIntent.None);
    }
}