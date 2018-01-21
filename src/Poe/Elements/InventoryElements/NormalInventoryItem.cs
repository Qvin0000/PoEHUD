namespace PoeHUD.Poe.Elements
{
    public class NormalInventoryItem : Element
    {
        public virtual int InventPosX => M.ReadInt(Address + 0xb68);
        public virtual int InventPosY => M.ReadInt(Address + 0xb6C);

        private Entity _item;
        public Entity Item
        {
            get
            {
                if(_item==null)
                    _item = ReadObject<Entity>(Address + 0xB60);
                return _item;
            }
        }

        public ToolTipType toolTipType => ToolTipType.InventoryItem;
        public Element ToolTip => ReadObject<Element>(Address + 0xB10);

    }
}
