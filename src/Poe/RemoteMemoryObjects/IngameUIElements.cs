using PoeHUD.Poe.Elements;
using System.Collections.Generic;
using System.Linq;

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
        //TODO need change to address + offests
        public EntityLabel LeagueLabel => (Map.Children[2].Children[0].Children[2]).AsObject<EntityLabel>();
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

        public List<EntityLabel> AllLabels => GetLabels();

        private List<EntityLabel> GetLabels()
        {
            Element labelsRoot = ReadObjectAt<Element>(0xD00);
            Element allLabels = labelsRoot.ReadObjectAt<Element>(0xA08);
            return allLabels.Children.Select(e => e.AsObject<EntityLabel>()).Where(entityLabel => entityLabel.Len > 0).ToList();
        }


        public Element PantheonPanel => ReadObjectAt<Element>(0xCC8);
        public Element WaypointPanel => ReadObjectAt<Element>(0xCD8);
        public Element CharInfoPanel => ReadObjectAt<Element>(0xCB0);
        public Element CadiroOfferPanel => ReadObjectAt<Element>(0xDA0);
        public Element CardTradePanel => ReadObjectAt<Element>(0xE08);
        public Element LabChoicePanel => ReadObjectAt<Element>(0xDD8);
        public Element TradePanel => ReadObjectAt<Element>(0xD90);
        public Element InstancePanel => ReadObjectAt<Element>(0xE48);
        public Element MapDevicePanel => ReadObjectAt<Element>(0xDC8);
        public StashElement StashPanel => ReadObjectAt<StashElement>(0xC78);

    }
}

