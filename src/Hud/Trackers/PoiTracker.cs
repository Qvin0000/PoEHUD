using PoeHUD.Controllers;
using PoeHUD.Hud.UI;
using PoeHUD.Models;
using PoeHUD.Poe.Components;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using PoeHUD.DebugPlug;
using PoeHUD.Hud.Settings;
using SharpDX;

namespace PoeHUD.Hud.Trackers
{
    public class PoiTracker : PluginWithMapIcons<PoiTrackerSettings>
    {
        private static readonly List<string> masters = new List<string>
        {
            "Metadata/NPC/Missions/Wild/Dex",
            "Metadata/NPC/Missions/Wild/DexInt",
            "Metadata/NPC/Missions/Wild/Int",
            "Metadata/NPC/Missions/Wild/Str",
            "Metadata/NPC/Missions/Wild/StrDex",
            "Metadata/NPC/Missions/Wild/StrDexInt",
            "Metadata/NPC/Missions/Wild/StrInt"
        };

        private static readonly List<string> cadiro = new List<string>
        {
            "Metadata/NPC/League/Cadiro"
        };

        private static readonly List<string> perandus = new List<string>
        {
            "Metadata/Chests/PerandusChests/PerandusChestStandard",
            "Metadata/Chests/PerandusChests/PerandusChestRarity",
            "Metadata/Chests/PerandusChests/PerandusChestQuantity",
            "Metadata/Chests/PerandusChests/PerandusChestCoins",
            "Metadata/Chests/PerandusChests/PerandusChestJewellery",
            "Metadata/Chests/PerandusChests/PerandusChestGems",
            "Metadata/Chests/PerandusChests/PerandusChestCurrency",
            "Metadata/Chests/PerandusChests/PerandusChestInventory",
            "Metadata/Chests/PerandusChests/PerandusChestDivinationCards",
            "Metadata/Chests/PerandusChests/PerandusChestKeepersOfTheTrove",
            "Metadata/Chests/PerandusChests/PerandusChestUniqueItem",
            "Metadata/Chests/PerandusChests/PerandusChestMaps",
            "Metadata/Chests/PerandusChests/PerandusChestFishing",
            "Metadata/Chests/PerandusChests/PerandusManorUniqueChest",
            "Metadata/Chests/PerandusChests/PerandusManorCurrencyChest",
            "Metadata/Chests/PerandusChests/PerandusManorMapsChest",
            "Metadata/Chests/PerandusChests/PerandusManorJewelryChest",
            "Metadata/Chests/PerandusChests/PerandusManorDivinationCardsChest",
            "Metadata/Chests/PerandusChests/PerandusManorLostTreasureChest"
        };

        private static readonly Dictionary<string, string> strongboxes =
            new Dictionary<string, string>()
            {
                {"Metadata/Chests/StrongBoxes/Large","large.png"},
                {"Metadata/Chests/StrongBoxes/Strongbox","strongbox.png"},
                {"Metadata/Chests/StrongBoxes/Armory","armory.png"},
                {"Metadata/Chests/StrongBoxes/Arsenal","blacksmith.png"},
                {"Metadata/Chests/StrongBoxes/Artisan","artisan.png"},
                {"Metadata/Chests/StrongBoxes/Jeweller","jeweller.png"},
                {"Metadata/Chests/StrongBoxes/Cartographer","cartographer.png"},
                {"Metadata/Chests/StrongBoxes/CartographerLowMaps","cartographer.png"},
                {"Metadata/Chests/StrongBoxes/CartographerMidMaps","cartographer.png"},
                {"Metadata/Chests/StrongBoxes/CartographerHighMaps","cartographer.png"},
                {"Metadata/Chests/StrongBoxes/Ornate","large.png"},
                {"Metadata/Chests/StrongBoxes/Arcanist","arcanist.png"},
                {"Metadata/Chests/StrongBoxes/Gemcutter","gemcutter.png"},
                {"Metadata/Chests/StrongBoxes/StrongboxDivination","diviner.png"},
            };

        private readonly Dictionary<string,ManualPoiIcon> ManualPoiIcons;
        
        public PoiTracker(GameController gameController, Graphics graphics, PoiTrackerSettings settings)
            : base(gameController, graphics, settings)
        {
            if (!File.Exists("config/icons.json"))
            {
                File.WriteAllText("config/icons.json","");
            }
            else
            {
                var json = File.ReadAllText("config/icons.json");
                ManualPoiIcons = JsonConvert.DeserializeObject<Dictionary<string,ManualPoiIcon>>(json);
            }
           
          
        }

        public override void Render()
        {
            if (!Settings.Enable) { }
        }

        protected override void OnEntityAdded(EntityWrapper entity)
        {
            if (!Settings.Enable) { return; }

            MapIcon icon = GetMapIcon(entity);
            if (null != icon)
            {
                CurrentIcons[entity] = icon;
            }
        }

        
        private MapIcon GetMapIcon(EntityWrapper e)
        {
            if (e.HasComponent<NPC>() && masters.Contains(e.Path))
            {
                return new CreatureMapIcon(e, "ms-cyan.png", () => Settings.Masters, Settings.MastersIcon);
            }
            if (e.HasComponent<NPC>() && cadiro.Contains(e.Path))
            {
                return new CreatureMapIcon(e, "ms-green.png", () => Settings.Cadiro, Settings.CadiroIcon);
            }
            if (e.HasComponent<Chest>() && perandus.Contains(e.Path))
            {
                return new ChestMapIcon(e, new HudTexture("strongbox.png", Settings.PerandusChestColor), () => Settings.PerandusChest, Settings.PerandusChestIcon);
            }
            if (e.HasComponent<Chest>() && !e.GetComponent<Chest>().IsOpened)
            {
                if (e.Path.Contains("BreachChest"))
                {
                    return new ChestMapIcon(e, new HudTexture("strongbox.png", Settings.BreachChestColor), () => Settings.BreachChest, Settings.BreachChestIcon);
                }
                
                if (e.GetComponent<Chest>().IsStrongbox)
                {

                    if (strongboxes.TryGetValue(e.Path, out var texture))
                    {
                        return new ChestMapIcon(e, new HudTexture(texture,
                                e.GetComponent<ObjectMagicProperties>().Rarity), () => Settings.Strongboxes,
                            Settings.StrongboxesIcon);
                    }
                    return new ChestMapIcon(e, new HudTexture("strongbox.png",
                            e.GetComponent<ObjectMagicProperties>().Rarity), () => Settings.Strongboxes,
                        Settings.StrongboxesIcon);
                }
                return new ChestMapIcon(e, new HudTexture("chest.png"), () => Settings.Chests, Settings.ChestsIcon);
            }

            if (ManualPoiIcons.TryGetValue(e.Path, out var manualTexture))
            {
                return new MapIcon(e,new HudTexture(manualTexture.Icon,manualTexture.Color),(() => manualTexture.Show),manualTexture.Size);
            }
            return null;
        }
    }

    class ManualPoiIcon
    {
        public string Icon { get; set; }
        public Color Color { get; set; }
        public bool Show { get; set; }
        public int Size { get; set; }
    }
}