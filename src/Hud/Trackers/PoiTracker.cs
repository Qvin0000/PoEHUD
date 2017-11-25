using PoeHUD.Controllers;
using PoeHUD.Hud.UI;
using PoeHUD.Models;
using PoeHUD.Poe.Components;
using System.Collections.Generic;

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

        public PoiTracker(GameController gameController, Graphics graphics, PoiTrackerSettings settings)
            : base(gameController, graphics, settings)
        { }

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
                    if (e.Path.Contains("Arcanist"))
                    {
                        return new ChestMapIcon(e, new HudTexture("arcanist.png",
                                e.GetComponent<ObjectMagicProperties>().Rarity), () => Settings.Strongboxes,
                            Settings.StrongboxesIcon);
                    }
                    if (e.Path.Contains("Divine"))
                    {
                        return new ChestMapIcon(e, new HudTexture("diviner.png",
                                e.GetComponent<ObjectMagicProperties>().Rarity), () => Settings.Strongboxes,
                            Settings.StrongboxesIcon);
                    }
                    if (e.Path.Contains("Armory"))
                    {
                        return new ChestMapIcon(e, new HudTexture("armory.png",
                                e.GetComponent<ObjectMagicProperties>().Rarity), () => Settings.Strongboxes,
                            Settings.StrongboxesIcon);
                    }
                    if (e.Path.Contains("Large") || e.Path.Contains("Ornate"))
                    {
                        return new ChestMapIcon(e, new HudTexture("large.png",
                                e.GetComponent<ObjectMagicProperties>().Rarity), () => Settings.Strongboxes,
                            Settings.StrongboxesIcon);
                    }
                    if (e.Path.Contains("Artisan"))
                    {
                        return new ChestMapIcon(e, new HudTexture("artisan.png",
                                e.GetComponent<ObjectMagicProperties>().Rarity), () => Settings.Strongboxes,
                            Settings.StrongboxesIcon);
                    }
                    if (e.Path.Contains("Blacksm"))
                    {
                        return new ChestMapIcon(e, new HudTexture("blacksmith.png",
                                e.GetComponent<ObjectMagicProperties>().Rarity), () => Settings.Strongboxes,
                            Settings.StrongboxesIcon);
                    }
                    if (e.Path.Contains("Gemcutter"))
                    {
                        return new ChestMapIcon(e, new HudTexture("gemcutter.png",
                                e.GetComponent<ObjectMagicProperties>().Rarity), () => Settings.Strongboxes,
                            Settings.StrongboxesIcon);
                    }
                    return new ChestMapIcon(e, new HudTexture("strongbox.png",
                            e.GetComponent<ObjectMagicProperties>().Rarity), () => Settings.Strongboxes,
                        Settings.StrongboxesIcon);
                }
                return new ChestMapIcon(e, new HudTexture("chest.png"), () => Settings.Chests, Settings.ChestsIcon);
            }
            return null;
        }
    }
}