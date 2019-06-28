using PoeHUD.EntitiesCache.CachedEntities;

namespace PoeHUD.Poe.Elements.InventoryElements
{
    public class NormalInventoryItem : Element
    {
        public virtual int InventPosX => M.ReadInt(Address + 0x390);
        //public ServerInventory.InventSlotItem ServerInventoryItem => ReadObject<ServerInventory.InventSlotItem>(Address + 0x18);//should work but it doesnt(. Offset is 0x18
        public virtual int InventPosY => M.ReadInt(Address + 0x394);
        public virtual int ItemWidth => M.ReadInt(Address + 0x398);
        public virtual int ItemHeight => M.ReadInt(Address + 0x39c);
        public ItemEntity Item => new ItemEntity(M.ReadLong(Address + 0x388));
        public ToolTipType ToolTipType => ToolTipType.InventoryItem;
        public Element ToolTip => ReadObject<Element>(Address + 0xB20);
       // Element already have it
       // public bool IsHighlighted => M.ReadByte(Address + 0x958) > 0;
    }
}
