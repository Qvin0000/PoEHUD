using PoeHUD.Poe.Elements;
using System.Collections.Generic;

namespace PoeHUD.Poe.RemoteMemoryObjects
{
    public class IngameUIElements : RemoteMemoryObject
    {
        public Element FlaskPanel => ReadObjectAt<Element>(0xA30);
        public SkillBarElement SkillBar => ReadObjectAt<SkillBarElement>(0xB48);
        public SkillBarElement HiddenSkillBar => ReadObjectAt<SkillBarElement>(0xB50);
        public Element QuestTracker => ReadObjectAt<Element>(0xBF8);
        public Element OpenLeftPanel => ReadObjectAt<Element>(0xC38);
        public Element OpenRightPanel => ReadObjectAt<Element>(0xC40);
        public InventoryElement InventoryPanel => ReadObjectAt<InventoryElement>(0xC70);
        public Element TreePanel => ReadObjectAt<Element>(0xCA0);
        public Element AtlasPanel => ReadObjectAt<Element>(0xCA8);
        public NpcDialog NpcDialog => ReadObjectAt<NpcDialog>(0xD68);
        public VendorSellElement VendorPanel => ReadObjectAt<VendorSellElement>(0xD88);
        public Map Map => ReadObjectAt<Map>(0xCF8);
        public IEnumerable<ItemsOnGroundLabelElement> ItemsOnGroundLabels
        {
            get
            {
                var itemsOnGroundLabelRoot = ReadObjectAt<ItemsOnGroundLabelElement>(0xD00);
                return itemsOnGroundLabelRoot.Children;
            }
        }
        public Element GemLvlUpPanel => ReadObjectAt<Element>(0xF00);
        public ItemOnGroundTooltip ItemOnGroundTooltip => ReadObjectAt<ItemOnGroundTooltip>(0xF68);
        public Element Cursor => ReadObjectAt<Element>(0xB40);
    }
}

