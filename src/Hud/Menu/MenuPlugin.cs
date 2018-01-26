using PoeHUD.Controllers;
using PoeHUD.Framework;
using PoeHUD.Framework.InputHooks;
using PoeHUD.Hud.Health;
using PoeHUD.Hud.Loot;
using PoeHUD.Hud.Settings;
using PoeHUD.Hud.UI;
using SharpDX;
using System;
using System.Linq;
using System.Windows.Forms;
using System.Reflection;
using ImGuiNET;
using PoeHUD.Hud.Dev;
using Vector2 = SharpDX.Vector2;
using Vector4 = System.Numerics.Vector4;

namespace PoeHUD.Hud.Menu
{
    public class MenuPlugin : Plugin<MenuSettings>
    {
        private readonly SettingsHub settingsHub;
        private readonly Action<MouseInfo> onMouseDown, onMouseUp, onMouseMove;
        private bool holdKey;
        public static RootButton MenuRootButton;
        public static event Action<RootButton> eInitMenu = delegate { };//For spawning the menu in external plugins
        public static bool MenuStatus { get; private set; }
        // Use this event if you want your mouse clicks to be handled by poehud and does not passed to the game {Stridemann}
        public static Func<MouseEventID, Vector2, bool> ExternalMouseClick = delegate { return false; };
        // Use this event if you want your mouse clicks to be handled by poehud and passed to the game {Stridemann}
        public static Action<MouseEventID, Vector2> eMouseEvent = delegate {  };
    

        public MenuPlugin(GameController gameController, Graphics graphics, SettingsHub settingsHub)
            : base(gameController, graphics, settingsHub.MenuSettings)
        {
            this.settingsHub = settingsHub;
            MenuStatus = Settings.Enable.Value;
            CreateMenu();
            MouseHook.MouseDown += onMouseDown = info => info.Handled = OnMouseEvent(MouseEventID.LeftButtonDown, info.Position);
            MouseHook.MouseUp += onMouseUp = info => info.Handled = OnMouseEvent(MouseEventID.LeftButtonUp, info.Position);
            MouseHook.MouseMove += onMouseMove = info => info.Handled = OnMouseEvent(MouseEventID.MouseMove, info.Position);
        }
        private bool logOpened;
        public bool performanceSetting;
        public bool mainSetting;
        private void RenderMenuBarImgui()
        {
            ImGui.SetNextWindowSize(new System.Numerics.Vector2(800,600),Condition.FirstUseEver);
            ImGui.BeginWindow("Settings");
            {
                ImGui.Text("Trying parse old menu.");
                foreach (var setting in SettingsHub.SettingsBases)
                {
                    var split = setting.ToString().Split('.');
                    if (ImGui.CollapsingHeader(split.Last(),TreeNodeFlags.Framed))
                    {
                         
                        MenuParser(setting);
                    }
                }

                foreach (var basePlugin in GameController.pluginsSettings)
                {
                  var setting =   basePlugin.Value;
                    var split = basePlugin.Key;
                    if (ImGui.CollapsingHeader(split,TreeNodeFlags.Framed))
                    {
                         
                        MenuParser(setting);
                    }
                }
                
            }
            ImGui.EndWindow();
            ImGui.BeginMainMenuBar();
            if (ImGui.BeginMenu("Settings"))
            {
               ImGui.Text("Trying parse old menu, working checkboxs and sliders");
                 foreach (var setting in SettingsHub.SettingsBases)
                 {
                     var split = setting.ToString().Split('.');
                     if (ImGui.CollapsingHeader(split.Last(),TreeNodeFlags.Framed))
                     {
                         
                         MenuParser(setting);
                     }
                 }
                ImGui.EndMenu();
            }
            if (ImGui.BeginMenu("For dev"))
            {
                if (ImGui.MenuItem("Debug Tree",null,settingsHub.DebugTreeSettings.ShowWindow,true))
                {
                    settingsHub.DebugTreeSettings.ShowWindow = !settingsHub.DebugTreeSettings.ShowWindow;
                }
                if (ImGui.MenuItem("Debug Information",null,settingsHub.DebugInformationSettings.ShowWindow,true))
                {
                    settingsHub.DebugInformationSettings.ShowWindow = !settingsHub.DebugInformationSettings.ShowWindow;
                }
                if (ImGui.MenuItem("DebugPlugin Log", null, settingsHub.DebugPluginLogSettings.ShowWindow, true))
                {
                    settingsHub.DebugPluginLogSettings.ShowWindow = !settingsHub.DebugPluginLogSettings.ShowWindow;
                }
                if (ImGui.MenuItem("Another Debugs", null, settingsHub.DebugPluginLogSettings.ShowWindow, true))
                {
                    DebugTree.Instance.anotherDebugWindow = !DebugTree.Instance.anotherDebugWindow;
                }
                ImGui.EndMenu();
            }
            ImGui.EndMainMenuBar();
        }

        private int index = 0;
        void MenuParser(object obj)
        {
            var flags = BindingFlags.Public |  BindingFlags.Instance;
            var oProp = obj.GetType().GetProperties(flags).Where(x => x.GetIndexParameters().Length == 0);

            foreach (var propertyInfo in oProp)
            {
                index++;
                    var o = Convert.ChangeType(propertyInfo.GetValue(obj, null), propertyInfo.PropertyType);
                    if (propertyInfo.PropertyType == typeof(ToggleNode))
                    {
                        var toggleNode = ((ToggleNode)o);
                        if (toggleNode == null) continue;
                        var toggleNodeValue = toggleNode.Value;
                        ImGui.Text($"{propertyInfo.Name}");
                        ImGui.SameLine();
                        ImGui.Checkbox(
                            $"##{propertyInfo.ReflectedType}{propertyInfo.Name}{toggleNode.GetHashCode()}",
                            ref toggleNodeValue);
                        toggleNode.Value = toggleNodeValue;
                    }
                    else if (propertyInfo.PropertyType == typeof(RangeNode<int>))
                    {
                        var rangeNode = ((RangeNode<int>)o);
                        if (rangeNode == null) continue;
                        var rangeNodeValue = rangeNode.Value;
                        ImGui.Text($"{propertyInfo.Name}"); 
                        ImGui.SameLine();
                        ImGui.SliderInt($"##{propertyInfo.ReflectedType}{propertyInfo.Name}{rangeNode.GetHashCode()}", ref rangeNodeValue, rangeNode.Min, rangeNode.Max, null);
                        rangeNode.Value = rangeNodeValue;
                    }
                    else if (propertyInfo.PropertyType==typeof(RangeNode<float>))
                    {
                        var rangeNode = ((RangeNode<float>)o);
                        if (rangeNode == null) continue;
                        var rangeNodeValue = rangeNode.Value;
                        ImGui.Text($"{propertyInfo.Name}"); 
                        ImGui.SameLine();
                        ImGui.SliderFloat($"##{propertyInfo.ReflectedType}{propertyInfo.Name}{rangeNode.GetHashCode()}", ref rangeNodeValue, rangeNode.Min, rangeNode.Max, null,1f);
                        rangeNode.Value = rangeNodeValue;
                    }
                    else if (propertyInfo.PropertyType == typeof(ColorNode))
                    {
                        var colorNode = (ColorNode) o;
                        if(colorNode==null) continue;
                        var colorNodeValue = colorNode.Value;
                        ImGui.Text($"{propertyInfo.Name}");
                        ImGui.SameLine();
                        //TODO: Fix color
                        Vector4 color4 = new Vector4(colorNodeValue.R/255f,colorNodeValue.G/255f,colorNodeValue.B/255f,colorNodeValue.A/255f);
                        ImGui.ColorEdit4($"##{propertyInfo.ReflectedType}{propertyInfo.Name}{colorNode.GetHashCode()}",ref color4, ColorEditFlags.Default);
                        colorNode.Value = new Color(color4.X,color4.Y,color4.Z,color4.W);
                    }
                    else if (propertyInfo.PropertyType == typeof(UnitSettings))
                    {
                        var unit = (UnitSettings) o;
                        if(unit==null) continue;
                        if (ImGui.CollapsingHeader(propertyInfo.Name,TreeNodeFlags.Framed))
                        {
                         
                            MenuParser(unit);

                        }
                    }
                    else
                    {
                        ImGui.Text($"{propertyInfo.Name} : {o} " +
                                   $"/ PropertyType: {propertyInfo.PropertyType} ");
                    }
                  
                }
        }
        

        public override void Dispose()
        {
            MouseHook.MouseDown -= onMouseDown;
            MouseHook.MouseUp -= onMouseUp;
            MouseHook.MouseMove -= onMouseMove;
        }

        public override void Render()
        {
            try
            {
                if (!holdKey && WinApi.IsKeyDown(Keys.F12))
                {
                    holdKey = true;
                    Settings.Enable.Value = !Settings.Enable.Value;
                    MenuStatus = Settings.Enable.Value;
                    SettingsHub.Save(settingsHub);
                }
                else if (holdKey && !WinApi.IsKeyDown(Keys.F12))
                {
                    holdKey = false;
                }

                if (Settings.Enable)
                {
                    RenderMenuBarImgui();
                    MenuRootButton.Render(Graphics, Settings);
                }
            }
            catch
            {
                // do nothing
            }
        }

        public static MenuItem AddChild(MenuItem parent, string text, ToggleNode node, string key = null, Func<MenuItem, bool> hide = null)
        {
            var item = new ToggleButton(parent, text, node, key, hide);
            parent.AddChild(item);
            return item;
        }

        public static MenuItem AddChild(MenuItem parent, string text)
        {
            var item = new SimpleMenu(parent, text);
            parent.AddChild(item);
            return item;
        }

        public static void AddChild(MenuItem parent, FileNode path)
        {
            var item = new FileButton(path);
            parent.AddChild(item);
        }

        public static MenuItem AddChild(MenuItem parent, string text, ColorNode node)
        {
            var item = new ColorButton(text, node);
            parent.AddChild(item);
            return item;
        }

        public static MenuItem AddChild<T>(MenuItem parent, string text, RangeNode<T> node) where T : struct
        {
            var item = new Picker<T>(text, node);
            parent.AddChild(item);
            return item;
        }

        public static MenuItem AddChild(MenuItem parent, string text, HotkeyNode node)
        {
            var item = new HotkeyButton(text, node);
            parent.AddChild(item);
            return item;
        }

        public static MenuItem AddChild(MenuItem parent, string text, ButtonNode node)
        {
            var item = new ButtonButton(text, node);
            parent.AddChild(item);
            return item;
        }

        public static ListButton AddChild(MenuItem parent, string text, ListNode node)
        {
            var item = new ListButton(text, node);
            parent.AddChild(item);
            //item.StartInitItems();//Can't be in ListButton.ctor coz first of all ListButton menu should be added to childs to initialise it's own bounds.
            node.SettingsListButton = item;
            return item;
        }

        private void CreateMenu()
        {
            MenuRootButton = new RootButton(new Vector2(Settings.X, Settings.Y));
            MenuRootButton.eOnClose += delegate { SettingsHub.Save(settingsHub); };
            // Health bars
            HealthBarSettings healthBarPlugin = settingsHub.HealthBarSettings;
            MenuItem healthMenu = AddChild(MenuRootButton, "Health bars", healthBarPlugin.Enable);
            MenuItem playersMenu = AddChild(healthMenu, "Players", healthBarPlugin.Players.Enable);
            MenuItem enemiesMenu = AddChild(healthMenu, "Enemies", healthBarPlugin.ShowEnemies);
            MenuItem minionsMenu = AddChild(healthMenu, "Minions", healthBarPlugin.Minions.Enable);
            AddChild(healthMenu, "Show nrg shield", healthBarPlugin.ShowES);
            AddChild(healthMenu, "Show in town", healthBarPlugin.ShowInTown);
            MenuItem debuffPanelMenu = AddChild(healthMenu, "Show debuffs", healthBarPlugin.ShowDebuffPanel);
            AddChild(debuffPanelMenu, "Icon size", healthBarPlugin.DebuffPanelIconSize);
            AddChild(playersMenu, "Print percents", healthBarPlugin.Players.ShowPercents);
            AddChild(playersMenu, "Print health text", healthBarPlugin.Players.ShowHealthText);
            AddChild(playersMenu, "Width", healthBarPlugin.Players.Width);
            AddChild(playersMenu, "Height", healthBarPlugin.Players.Height);
            AddChild(playersMenu, "New Style", healthBarPlugin.NewStyle);
            AddChild(playersMenu, "Show Mana", healthBarPlugin.ShowMana);
            AddChild(playersMenu, "X", healthBarPlugin.X);
            AddChild(playersMenu, "Y", healthBarPlugin.Y);
            MenuItem playerDPSMenu = AddChild(playersMenu, "Floating combat text", healthBarPlugin.Players.ShowFloatingCombatDamage);
            AddChild(playerDPSMenu, "Text size", healthBarPlugin.Players.FloatingCombatTextSize);
            AddChild(playerDPSMenu, "Damage Color", healthBarPlugin.Players.FloatingCombatDamageColor);
            AddChild(playerDPSMenu, "Heal Color", healthBarPlugin.Players.FloatingCombatHealColor);
            AddChild(playerDPSMenu, "Number of lines", healthBarPlugin.Players.FloatingCombatStackSize);
            AddChild(minionsMenu, "Print percents", healthBarPlugin.Minions.ShowPercents);
            AddChild(minionsMenu, "Print health text", healthBarPlugin.Minions.ShowHealthText);
            AddChild(minionsMenu, "Width", healthBarPlugin.Minions.Width);
            AddChild(minionsMenu, "Height", healthBarPlugin.Minions.Height);
            MenuItem minionsDPSMenu = AddChild(minionsMenu, "Floating combat text", healthBarPlugin.Minions.ShowFloatingCombatDamage);
            AddChild(minionsDPSMenu, "Text size", healthBarPlugin.Minions.FloatingCombatTextSize);
            AddChild(minionsDPSMenu, "Damage color", healthBarPlugin.Minions.FloatingCombatDamageColor);
            AddChild(minionsDPSMenu, "Heal color", healthBarPlugin.Minions.FloatingCombatHealColor);
            AddChild(minionsDPSMenu, "Number of lines", healthBarPlugin.Minions.FloatingCombatStackSize);
            MenuItem whiteEnemyMenu = AddChild(enemiesMenu, "White", healthBarPlugin.NormalEnemy.Enable);
            AddChild(whiteEnemyMenu, "Print percents", healthBarPlugin.NormalEnemy.ShowPercents);
            AddChild(whiteEnemyMenu, "Print health text", healthBarPlugin.NormalEnemy.ShowHealthText);
            AddChild(whiteEnemyMenu, "Width", healthBarPlugin.NormalEnemy.Width);
            AddChild(whiteEnemyMenu, "Height", healthBarPlugin.NormalEnemy.Height);
            MenuItem whiteEnemyDPSMenu = AddChild(whiteEnemyMenu, "Floating combat text", healthBarPlugin.NormalEnemy.ShowFloatingCombatDamage);
            AddChild(whiteEnemyDPSMenu, "Text size", healthBarPlugin.NormalEnemy.FloatingCombatTextSize);
            AddChild(whiteEnemyDPSMenu, "Damage color", healthBarPlugin.NormalEnemy.FloatingCombatDamageColor);
            AddChild(whiteEnemyDPSMenu, "Heal color", healthBarPlugin.NormalEnemy.FloatingCombatHealColor);
            AddChild(whiteEnemyDPSMenu, "Number of lines", healthBarPlugin.NormalEnemy.FloatingCombatStackSize);
            MenuItem magicEnemyMenu = AddChild(enemiesMenu, "Magic", healthBarPlugin.MagicEnemy.Enable);
            AddChild(magicEnemyMenu, "Print percents", healthBarPlugin.MagicEnemy.ShowPercents);
            AddChild(magicEnemyMenu, "Print health text", healthBarPlugin.MagicEnemy.ShowHealthText);
            AddChild(magicEnemyMenu, "Width", healthBarPlugin.MagicEnemy.Width);
            AddChild(magicEnemyMenu, "Height", healthBarPlugin.MagicEnemy.Height);
            MenuItem magicEnemyDPSMenu = AddChild(magicEnemyMenu, "Floating combat text", healthBarPlugin.MagicEnemy.ShowFloatingCombatDamage);
            AddChild(magicEnemyDPSMenu, "Text size", healthBarPlugin.MagicEnemy.FloatingCombatTextSize);
            AddChild(magicEnemyDPSMenu, "Damage color", healthBarPlugin.MagicEnemy.FloatingCombatDamageColor);
            AddChild(magicEnemyDPSMenu, "Heal color", healthBarPlugin.MagicEnemy.FloatingCombatHealColor);
            AddChild(magicEnemyDPSMenu, "Number of lines", healthBarPlugin.MagicEnemy.FloatingCombatStackSize);
            MenuItem rareEnemyMenu = AddChild(enemiesMenu, "Rare", healthBarPlugin.RareEnemy.Enable);
            AddChild(rareEnemyMenu, "Print percents", healthBarPlugin.RareEnemy.ShowPercents);
            AddChild(rareEnemyMenu, "Print health text", healthBarPlugin.RareEnemy.ShowHealthText);
            AddChild(rareEnemyMenu, "Width", healthBarPlugin.RareEnemy.Width);
            AddChild(rareEnemyMenu, "Height", healthBarPlugin.RareEnemy.Height);
            MenuItem rareEnemyDPSMenu = AddChild(rareEnemyMenu, "Floating combat text", healthBarPlugin.RareEnemy.ShowFloatingCombatDamage);
            AddChild(rareEnemyDPSMenu, "Text size", healthBarPlugin.RareEnemy.FloatingCombatTextSize);
            AddChild(rareEnemyDPSMenu, "Damage color", healthBarPlugin.RareEnemy.FloatingCombatDamageColor);
            AddChild(rareEnemyDPSMenu, "Heal color", healthBarPlugin.RareEnemy.FloatingCombatHealColor);
            AddChild(rareEnemyDPSMenu, "Number of lines", healthBarPlugin.RareEnemy.FloatingCombatStackSize);
            MenuItem uniquesEnemyMenu = AddChild(enemiesMenu, "Uniques", healthBarPlugin.UniqueEnemy.Enable);
            AddChild(uniquesEnemyMenu, "Print percents", healthBarPlugin.UniqueEnemy.ShowPercents);
            AddChild(uniquesEnemyMenu, "Print health text", healthBarPlugin.UniqueEnemy.ShowHealthText);
            AddChild(uniquesEnemyMenu, "Width", healthBarPlugin.UniqueEnemy.Width);
            AddChild(uniquesEnemyMenu, "Height", healthBarPlugin.UniqueEnemy.Height);
            MenuItem uniqueEnemyDPSMenu = AddChild(uniquesEnemyMenu, "Floating combat text", healthBarPlugin.UniqueEnemy.ShowFloatingCombatDamage);
            AddChild(uniqueEnemyDPSMenu, "Text size", healthBarPlugin.UniqueEnemy.FloatingCombatTextSize);
            AddChild(uniqueEnemyDPSMenu, "Damage color", healthBarPlugin.UniqueEnemy.FloatingCombatDamageColor);
            AddChild(uniqueEnemyDPSMenu, "Heal color", healthBarPlugin.UniqueEnemy.FloatingCombatHealColor);
            AddChild(uniqueEnemyDPSMenu, "Number of lines", healthBarPlugin.UniqueEnemy.FloatingCombatStackSize);

            // Xph Display
            MenuItem xpRateMenu = AddChild(MenuRootButton, "Xph & area", settingsHub.XpRateSettings.Enable, "F10");
            AddChild(xpRateMenu, "Only area name", settingsHub.XpRateSettings.OnlyAreaName);
            AddChild(xpRateMenu, "Show in town", settingsHub.XpRateSettings.ShowInTown);
            AddChild(xpRateMenu, "Font size", settingsHub.XpRateSettings.TextSize);
            AddChild(xpRateMenu, "Fps font color", settingsHub.XpRateSettings.FpsTextColor);
            AddChild(xpRateMenu, "Xph font color", settingsHub.XpRateSettings.XphTextColor);
            AddChild(xpRateMenu, "Area font color", settingsHub.PreloadAlertSettings.AreaTextColor);
            AddChild(xpRateMenu, "Time left color", settingsHub.XpRateSettings.TimeLeftColor);
            AddChild(xpRateMenu, "Timer font color", settingsHub.XpRateSettings.TimerTextColor);
            AddChild(xpRateMenu, "Latency font color", settingsHub.XpRateSettings.LatencyTextColor);
            AddChild(xpRateMenu, "Background color", settingsHub.XpRateSettings.BackgroundColor);
            AddChild(xpRateMenu, "Show latency", settingsHub.XpRateSettings.ShowLatency);
            AddChild(xpRateMenu, "Show fps", settingsHub.XpRateSettings.ShowFps);

            // Item Alert
            MenuItem itemAlertMenu = AddChild(MenuRootButton, "Item alert", settingsHub.ItemAlertSettings.Enable);
            var itemAlertStaticMenuList = new[] { "Alternative", "Item tooltips", "Play sound", "Show text", "Hide others", "Show border", "Dim Others" };
            MenuItem alternative = AddChild(itemAlertMenu, itemAlertStaticMenuList[0],
                settingsHub.ItemAlertSettings.Alternative, null, y => itemAlertStaticMenuList.All(x => x != (y as ToggleButton)?.Name));
            AddChild(alternative, settingsHub.ItemAlertSettings.FilePath);
            AddChild(alternative, "With border", settingsHub.ItemAlertSettings.WithBorder);
            AddChild(alternative, "With sound", settingsHub.ItemAlertSettings.WithSound);
            MenuItem tooltipMenu = AddChild(itemAlertMenu, itemAlertStaticMenuList[1], settingsHub.AdvancedTooltipSettings.Enable);
            MenuItem itemLevelMenu = AddChild(tooltipMenu, "Item level", settingsHub.AdvancedTooltipSettings.ItemLevel.Enable);
            AddChild(itemLevelMenu, "Text size", settingsHub.AdvancedTooltipSettings.ItemLevel.TextSize);
            AddChild(itemLevelMenu, "Text color", settingsHub.AdvancedTooltipSettings.ItemLevel.TextColor);
            AddChild(itemLevelMenu, "Background color", settingsHub.AdvancedTooltipSettings.ItemLevel.BackgroundColor);
            MenuItem itemModsMenu = AddChild(tooltipMenu, "Item mods", settingsHub.AdvancedTooltipSettings.ItemMods.Enable, "F9");
            AddChild(itemModsMenu, "Mods size", settingsHub.AdvancedTooltipSettings.ItemMods.ModTextSize);
            AddChild(itemModsMenu, "Tier 1 color", settingsHub.AdvancedTooltipSettings.ItemMods.T1Color);
            AddChild(itemModsMenu, "Tier 2 color", settingsHub.AdvancedTooltipSettings.ItemMods.T2Color);
            AddChild(itemModsMenu, "Tier 3 color", settingsHub.AdvancedTooltipSettings.ItemMods.T3Color);
            AddChild(itemModsMenu, "Suffix color", settingsHub.AdvancedTooltipSettings.ItemMods.SuffixColor);
            AddChild(itemModsMenu, "Prefix color", settingsHub.AdvancedTooltipSettings.ItemMods.PrefixColor);
            MenuItem weaponDpsMenu = AddChild(tooltipMenu, "Weapon Dps", settingsHub.AdvancedTooltipSettings.WeaponDps.Enable);
            var damageColors = AddChild(weaponDpsMenu, "Damage colors", settingsHub.AdvancedTooltipSettings.WeaponDps.Enable);
            AddChild(damageColors, "Cold damage", settingsHub.AdvancedTooltipSettings.WeaponDps.DmgColdColor);
            AddChild(damageColors, "Fire damage", settingsHub.AdvancedTooltipSettings.WeaponDps.DmgFireColor);
            AddChild(damageColors, "Lightning damage", settingsHub.AdvancedTooltipSettings.WeaponDps.DmgLightningColor);
            AddChild(damageColors, "Chaos damage", settingsHub.AdvancedTooltipSettings.WeaponDps.DmgChaosColor);
            AddChild(damageColors, "Physical damage", settingsHub.AdvancedTooltipSettings.WeaponDps.pDamageColor);
            AddChild(damageColors, "Elemental damage", settingsHub.AdvancedTooltipSettings.WeaponDps.eDamageColor);
            AddChild(weaponDpsMenu, "Text color", settingsHub.AdvancedTooltipSettings.WeaponDps.TextColor);
            AddChild(weaponDpsMenu, "Dps size", settingsHub.AdvancedTooltipSettings.WeaponDps.DpsTextSize);
            AddChild(weaponDpsMenu, "Dps text size", settingsHub.AdvancedTooltipSettings.WeaponDps.DpsNameTextSize);
            AddChild(weaponDpsMenu, "Background color", settingsHub.AdvancedTooltipSettings.WeaponDps.BackgroundColor);
            MenuItem itemSound = AddChild(itemAlertMenu, itemAlertStaticMenuList[2], settingsHub.ItemAlertSettings.PlaySound);
            AddChild(itemSound, "Sound volume", settingsHub.ItemAlertSettings.SoundVolume);
            MenuItem alertTextMenu = AddChild(itemAlertMenu, itemAlertStaticMenuList[3], settingsHub.ItemAlertSettings.ShowText);
            AddChild(alertTextMenu, "Text size", settingsHub.ItemAlertSettings.TextSize);
            AddChild(itemAlertMenu, itemAlertStaticMenuList[4], settingsHub.ItemAlertSettings.HideOthers);
            ItemAlertSettings dimOtherSettings = settingsHub.ItemAlertSettings;
            MenuItem dimOtherMenu = AddChild(itemAlertMenu, itemAlertStaticMenuList[6], dimOtherSettings.DimOtherByPercentToggle);
            AddChild(dimOtherMenu, "Dim Others By %", dimOtherSettings.DimOtherByPercent);
            BorderSettings borderSettings = settingsHub.ItemAlertSettings.BorderSettings;
            MenuItem showBorderMenu = AddChild(itemAlertMenu, itemAlertStaticMenuList[5], borderSettings.Enable);
            AddChild(showBorderMenu, "Border width", borderSettings.BorderWidth);
            AddChild(showBorderMenu, "Border color:", borderSettings.BorderColor);
            AddChild(showBorderMenu, "Can't pick up :", borderSettings.CantPickUpBorderColor);
            AddChild(showBorderMenu, "Not my item :", borderSettings.NotMyItemBorderColor);
            AddChild(showBorderMenu, "Show timer", borderSettings.ShowTimer);
            AddChild(showBorderMenu, "Timer text size", borderSettings.TimerTextSize);
            AddChild(itemAlertMenu, "Uniques", settingsHub.ItemAlertSettings.Uniques);
            AddChild(itemAlertMenu, "Rares", settingsHub.ItemAlertSettings.Rares);
            AddChild(itemAlertMenu, "Currency", settingsHub.ItemAlertSettings.Currency);
            AddChild(itemAlertMenu, "Maps", settingsHub.ItemAlertSettings.Maps);
            AddChild(itemAlertMenu, "RGB", settingsHub.ItemAlertSettings.Rgb);
            AddChild(itemAlertMenu, "Crafting bases", settingsHub.ItemAlertSettings.Crafting);
            AddChild(itemAlertMenu, "Divination cards", settingsHub.ItemAlertSettings.DivinationCards);
            AddChild(itemAlertMenu, "Jewels", settingsHub.ItemAlertSettings.Jewels);
            AddChild(itemAlertMenu, "Talisman", settingsHub.ItemAlertSettings.Talisman);
            QualityItemsSettings qualityItemsSettings = settingsHub.ItemAlertSettings.QualityItems;
            MenuItem qualityMenu = AddChild(itemAlertMenu, "Show quality items", qualityItemsSettings.Enable);
            MenuItem qualityWeaponMenu = AddChild(qualityMenu, "Weapons", qualityItemsSettings.Weapon.Enable);
            AddChild(qualityWeaponMenu, "Min. quality", qualityItemsSettings.Weapon.MinQuality);
            MenuItem qualityArmourMenu = AddChild(qualityMenu, "Armours", qualityItemsSettings.Armour.Enable);
            AddChild(qualityArmourMenu, "Min. quality", qualityItemsSettings.Armour.MinQuality);
            MenuItem qualityFlaskMenu = AddChild(qualityMenu, "Flasks", qualityItemsSettings.Flask.Enable);
            AddChild(qualityFlaskMenu, "Min. quality", qualityItemsSettings.Flask.MinQuality);
            MenuItem qualitySkillGemMenu = AddChild(qualityMenu, "Skill gems", qualityItemsSettings.SkillGem.Enable);
            AddChild(qualitySkillGemMenu, "Min. quality", qualityItemsSettings.SkillGem.MinQuality);

            // Preload Alert
            var preloadMenu = AddChild(MenuRootButton, "Preload alert", settingsHub.PreloadAlertSettings.Enable, "F5");
            var masters = AddChild(preloadMenu, "Masters", settingsHub.PreloadAlertSettings.Masters);
            AddChild(masters, "Zana", settingsHub.PreloadAlertSettings.MasterZana);
            AddChild(masters, "Tora", settingsHub.PreloadAlertSettings.MasterTora);
            AddChild(masters, "Haku", settingsHub.PreloadAlertSettings.MasterHaku);
            AddChild(masters, "Vorici", settingsHub.PreloadAlertSettings.MasterVorici);
            AddChild(masters, "Elreon", settingsHub.PreloadAlertSettings.MasterElreon);
            AddChild(masters, "Vagan", settingsHub.PreloadAlertSettings.MasterVagan);
            AddChild(masters, "Catarina", settingsHub.PreloadAlertSettings.MasterCatarina);
            AddChild(masters, "Krillson", settingsHub.PreloadAlertSettings.MasterKrillson);

            var exiles = AddChild(preloadMenu, "Exiles", settingsHub.PreloadAlertSettings.Exiles);
            AddChild(exiles, "Orra Greengate", settingsHub.PreloadAlertSettings.OrraGreengate);
            AddChild(exiles, "Thena Moga", settingsHub.PreloadAlertSettings.ThenaMoga);
            AddChild(exiles, "Antalie Napora", settingsHub.PreloadAlertSettings.AntalieNapora);
            AddChild(exiles, "Torr Olgosso", settingsHub.PreloadAlertSettings.TorrOlgosso);
            AddChild(exiles, "Armios Bell", settingsHub.PreloadAlertSettings.ArmiosBell);
            AddChild(exiles, "Zacharie Desmarais", settingsHub.PreloadAlertSettings.ZacharieDesmarais);
            AddChild(exiles, "Minara Anenima", settingsHub.PreloadAlertSettings.MinaraAnenima);
            AddChild(exiles, "Igna Phoenix", settingsHub.PreloadAlertSettings.IgnaPhoenix);
            AddChild(exiles, "Jonah Unchained", settingsHub.PreloadAlertSettings.JonahUnchained);
            AddChild(exiles, "Damoi Tui", settingsHub.PreloadAlertSettings.DamoiTui);
            AddChild(exiles, "Xandro Blooddrinker", settingsHub.PreloadAlertSettings.XandroBlooddrinker);
            AddChild(exiles, "Vickas Giantbone", settingsHub.PreloadAlertSettings.VickasGiantbone);
            AddChild(exiles, "Eoin Greyfur", settingsHub.PreloadAlertSettings.EoinGreyfur);
            AddChild(exiles, "Tinevin Highdove", settingsHub.PreloadAlertSettings.TinevinHighdove);
            AddChild(exiles, "Magnus Stonethorn", settingsHub.PreloadAlertSettings.MagnusStonethorn);
            AddChild(exiles, "Ion Darkshroud", settingsHub.PreloadAlertSettings.IonDarkshroud);
            AddChild(exiles, "Ash Lessard", settingsHub.PreloadAlertSettings.AshLessard);
            AddChild(exiles, "Wilorin Demontamer", settingsHub.PreloadAlertSettings.WilorinDemontamer);
            AddChild(exiles, "Augustina Solaria", settingsHub.PreloadAlertSettings.AugustinaSolaria);
            AddChild(exiles, "Dena Lorenni", settingsHub.PreloadAlertSettings.DenaLorenni);
            AddChild(exiles, "Vanth Agiel", settingsHub.PreloadAlertSettings.VanthAgiel);
            AddChild(exiles, "Lael Furia", settingsHub.PreloadAlertSettings.LaelFuria);
            AddChild(exiles, "OyraOna", settingsHub.PreloadAlertSettings.OyraOna);
            AddChild(exiles, "BoltBrownfur", settingsHub.PreloadAlertSettings.BoltBrownfur);
            AddChild(exiles, "AilentiaRac", settingsHub.PreloadAlertSettings.AilentiaRac);
            AddChild(exiles, "UlyssesMorvant", settingsHub.PreloadAlertSettings.UlyssesMorvant);
            AddChild(exiles, "AurelioVoidsinger", settingsHub.PreloadAlertSettings.AurelioVoidsinger);

            var strongboxes = AddChild(preloadMenu, "Strongboxes", settingsHub.PreloadAlertSettings.Strongboxes);
            AddChild(strongboxes, "Arcanist", settingsHub.PreloadAlertSettings.ArcanistStrongbox);
            AddChild(strongboxes, "Artisan", settingsHub.PreloadAlertSettings.ArtisanStrongbox);
            AddChild(strongboxes, "Cartographer", settingsHub.PreloadAlertSettings.CartographerStrongbox);
            AddChild(strongboxes, "Diviner", settingsHub.PreloadAlertSettings.DivinerStrongbox);
            AddChild(strongboxes, "Gemcutter", settingsHub.PreloadAlertSettings.GemcutterStrongbox);
            AddChild(strongboxes, "Jeweller", settingsHub.PreloadAlertSettings.JewellerStrongbox);
            AddChild(strongboxes, "Blacksmith", settingsHub.PreloadAlertSettings.BlacksmithStrongbox);
            AddChild(strongboxes, "Armourer", settingsHub.PreloadAlertSettings.ArmourerStrongbox);
            AddChild(strongboxes, "Ornate", settingsHub.PreloadAlertSettings.OrnateStrongbox);
            AddChild(strongboxes, "Large", settingsHub.PreloadAlertSettings.LargeStrongbox);
            AddChild(strongboxes, "Perandus", settingsHub.PreloadAlertSettings.PerandusStrongbox);
            AddChild(strongboxes, "Kaom", settingsHub.PreloadAlertSettings.KaomStrongbox);
            AddChild(strongboxes, "Malachai", settingsHub.PreloadAlertSettings.MalachaiStrongbox);
            AddChild(strongboxes, "Epic", settingsHub.PreloadAlertSettings.EpicStrongbox);
            AddChild(strongboxes, "Simple", settingsHub.PreloadAlertSettings.SimpleStrongbox);

            var essences = AddChild(preloadMenu, "Essences", settingsHub.PreloadAlertSettings.Essence);
            AddChild(essences, "Remnant of Corruption", settingsHub.PreloadAlertSettings.RemnantOfCorruption);
            AddChild(essences, "Essence of Anger", settingsHub.PreloadAlertSettings.EssenceOfAnger);
            AddChild(essences, "Essence of Anguish", settingsHub.PreloadAlertSettings.EssenceOfAnguish);
            AddChild(essences, "Essence of Contempt", settingsHub.PreloadAlertSettings.EssenceOfContempt);
            AddChild(essences, "Essence of Delirium", settingsHub.PreloadAlertSettings.EssenceOfDelirium);
            AddChild(essences, "Essence of Doubt", settingsHub.PreloadAlertSettings.EssenceOfDoubt);
            AddChild(essences, "Essence of Dread", settingsHub.PreloadAlertSettings.EssenceOfDread);
            AddChild(essences, "Essence of Envy", settingsHub.PreloadAlertSettings.EssenceOfEnvy);
            AddChild(essences, "Essence of Fear", settingsHub.PreloadAlertSettings.EssenceOfFear);
            AddChild(essences, "Essence of Greed", settingsHub.PreloadAlertSettings.EssenceOfGreed);
            AddChild(essences, "Essence of Hatred", settingsHub.PreloadAlertSettings.EssenceOfHatred);
            AddChild(essences, "Essence of Horror", settingsHub.PreloadAlertSettings.EssenceOfHorror);
            AddChild(essences, "Essence of Hysteria", settingsHub.PreloadAlertSettings.EssenceOfHysteria);
            AddChild(essences, "Essence of Insanity", settingsHub.PreloadAlertSettings.EssenceOfInsanity);
            AddChild(essences, "Essence of Loathing", settingsHub.PreloadAlertSettings.EssenceOfLoathing);
            AddChild(essences, "Essence of Misery", settingsHub.PreloadAlertSettings.EssenceOfMisery);
            AddChild(essences, "Essence of Rage", settingsHub.PreloadAlertSettings.EssenceOfRage);
            AddChild(essences, "Essence of Scorn", settingsHub.PreloadAlertSettings.EssenceOfScorn);
            AddChild(essences, "Essence of Sorrow", settingsHub.PreloadAlertSettings.EssenceOfSorrow);
            AddChild(essences, "Essence of Spite", settingsHub.PreloadAlertSettings.EssenceOfSpite);
            AddChild(essences, "Essence of Suffering", settingsHub.PreloadAlertSettings.EssenceOfSuffering);
            AddChild(essences, "Essence of Torment", settingsHub.PreloadAlertSettings.EssenceOfTorment);
            AddChild(essences, "Essence of Woe", settingsHub.PreloadAlertSettings.EssenceOfWoe);
            AddChild(essences, "Essence of Wrath", settingsHub.PreloadAlertSettings.EssenceOfWrath);
            AddChild(essences, "Essence of Zeal", settingsHub.PreloadAlertSettings.EssenceOfZeal);

            var perandus = AddChild(preloadMenu, "Perandus Chests", settingsHub.PreloadAlertSettings.PerandusBoxes);
            AddChild(perandus, "Cadiro Trader", settingsHub.PreloadAlertSettings.CadiroTrader);
            AddChild(perandus, "Perandus Chest", settingsHub.PreloadAlertSettings.PerandusChestStandard);
            AddChild(perandus, "Perandus Cache", settingsHub.PreloadAlertSettings.PerandusChestRarity);
            AddChild(perandus, "Perandus Hoard", settingsHub.PreloadAlertSettings.PerandusChestQuantity);
            AddChild(perandus, "Perandus Coffer", settingsHub.PreloadAlertSettings.PerandusChestCoins);
            AddChild(perandus, "Perandus Jewellery", settingsHub.PreloadAlertSettings.PerandusChestJewellery);
            AddChild(perandus, "Perandus Safe", settingsHub.PreloadAlertSettings.PerandusChestGems);
            AddChild(perandus, "Perandus Treasury", settingsHub.PreloadAlertSettings.PerandusChestCurrency);
            AddChild(perandus, "Perandus Wardrobe", settingsHub.PreloadAlertSettings.PerandusChestInventory);
            AddChild(perandus, "Perandus Catalogue", settingsHub.PreloadAlertSettings.PerandusChestDivinationCards);
            AddChild(perandus, "Perandus Trove", settingsHub.PreloadAlertSettings.PerandusChestKeepersOfTheTrove);
            AddChild(perandus, "Perandus Locker", settingsHub.PreloadAlertSettings.PerandusChestUniqueItem);
            AddChild(perandus, "Perandus Archive", settingsHub.PreloadAlertSettings.PerandusChestMaps);
            AddChild(perandus, "Perandus Tackle Box", settingsHub.PreloadAlertSettings.PerandusChestFishing);
            AddChild(perandus, "Cadiro's Locker", settingsHub.PreloadAlertSettings.PerandusManorUniqueChest);
            AddChild(perandus, "Cadiro's Treasury", settingsHub.PreloadAlertSettings.PerandusManorCurrencyChest);
            AddChild(perandus, "Cadiro's Archive", settingsHub.PreloadAlertSettings.PerandusManorMapsChest);
            AddChild(perandus, "Cadiro's Jewellery", settingsHub.PreloadAlertSettings.PerandusManorJewelryChest);
            AddChild(perandus, "Cadiro's Catalogue", settingsHub.PreloadAlertSettings.PerandusManorDivinationCardsChest);
            AddChild(perandus, "Grand Perandus Vault", settingsHub.PreloadAlertSettings.PerandusManorLostTreasureChest);

            var corruptedMenu = AddChild(preloadMenu, "Corrupted Area", settingsHub.PreloadAlertSettings.CorruptedArea);
            AddChild(corruptedMenu, "Corrupted title", settingsHub.PreloadAlertSettings.CorruptedTitle, "F5");
            AddChild(corruptedMenu, "Corrupted color", settingsHub.PreloadAlertSettings.CorruptedAreaColor);
            AddChild(preloadMenu, "Background color", settingsHub.PreloadAlertSettings.BackgroundColor);
            AddChild(preloadMenu, "Font color", settingsHub.PreloadAlertSettings.DefaultTextColor);
            AddChild(preloadMenu, "Font size", settingsHub.PreloadAlertSettings.TextSize);

            // Monster alert
            MenuItem MonsterTrackerMenu = AddChild(MenuRootButton, "Monster alert", settingsHub.MonsterTrackerSettings.Enable);
            MenuItem alertSound = AddChild(MonsterTrackerMenu, "Sound warning", settingsHub.MonsterTrackerSettings.PlaySound);
            AddChild(alertSound, "Sound volume", settingsHub.MonsterTrackerSettings.SoundVolume);
            MenuItem warningTextMenu = AddChild(MonsterTrackerMenu, "Text warning", settingsHub.MonsterTrackerSettings.ShowText);
            AddChild(warningTextMenu, "Text size", settingsHub.MonsterTrackerSettings.TextSize);
            AddChild(warningTextMenu, "Default text color:", settingsHub.MonsterTrackerSettings.DefaultTextColor);
            AddChild(warningTextMenu, "Background color:", settingsHub.MonsterTrackerSettings.BackgroundColor);
            AddChild(warningTextMenu, "Position X", settingsHub.MonsterTrackerSettings.TextPositionX);
            AddChild(warningTextMenu, "Position Y", settingsHub.MonsterTrackerSettings.TextPositionY);

            // Monster Kills
            MenuItem showMonsterKillsMenu = AddChild(MenuRootButton, "Monster kills", settingsHub.KillCounterSettings.Enable);
            AddChild(showMonsterKillsMenu, "Show details", settingsHub.KillCounterSettings.ShowDetail);
            AddChild(showMonsterKillsMenu, "Show in town", settingsHub.KillCounterSettings.ShowInTown);
            AddChild(showMonsterKillsMenu, "Font color", settingsHub.KillCounterSettings.TextColor);
            AddChild(showMonsterKillsMenu, "Background color", settingsHub.KillCounterSettings.BackgroundColor);
            AddChild(showMonsterKillsMenu, "Label font size", settingsHub.KillCounterSettings.LabelTextSize);
            AddChild(showMonsterKillsMenu, "Kills font size", settingsHub.KillCounterSettings.KillsTextSize);

            // Dps options
            MenuItem showDpsMenu = AddChild(MenuRootButton, "Show dps", settingsHub.DpsMeterSettings.Enable);
            AddChild(showDpsMenu, "Show in town", settingsHub.DpsMeterSettings.ShowInTown);
            AddChild(showDpsMenu, "Dps font size", settingsHub.DpsMeterSettings.DpsTextSize);
            AddChild(showDpsMenu, "Top dps font size", settingsHub.DpsMeterSettings.PeakDpsTextSize);
            AddChild(showDpsMenu, "Background color", settingsHub.DpsMeterSettings.BackgroundColor);
            AddChild(showDpsMenu, "Dps font color", settingsHub.DpsMeterSettings.DpsFontColor);
            AddChild(showDpsMenu, "Top dps font color", settingsHub.DpsMeterSettings.PeakFontColor);
            AddChild(showDpsMenu, "Show AOE", settingsHub.DpsMeterSettings.CalcAOE); 
            AddChild(showDpsMenu, "Clear", settingsHub.DpsMeterSettings.ClearNode); 
            // Map icons
            MenuItem mapIconsMenu = AddChild(MenuRootButton, "Map icons", settingsHub.MapIconsSettings.Enable);
            MenuItem iconSizeMenu = AddChild(mapIconsMenu, "Icon sizes", settingsHub.MonsterTrackerSettings.ShowText);
            AddChild(iconSizeMenu, "White Mob Icon", settingsHub.MonsterTrackerSettings.WhiteMobIcon);
            AddChild(iconSizeMenu, "Magic Mob Icon", settingsHub.MonsterTrackerSettings.MagicMobIcon);
            AddChild(iconSizeMenu, "Rare Mob Icon", settingsHub.MonsterTrackerSettings.RareMobIcon);
            AddChild(iconSizeMenu, "Unique Mob Icon", settingsHub.MonsterTrackerSettings.UniqueMobIcon);
            AddChild(iconSizeMenu, "Minions Icon", settingsHub.MonsterTrackerSettings.MinionsIcon);
            AddChild(iconSizeMenu, "Masters Icon", settingsHub.PoiTrackerSettings.MastersIcon);
            AddChild(iconSizeMenu, "Chests Icon", settingsHub.PoiTrackerSettings.ChestsIcon);
            AddChild(iconSizeMenu, "Strongboxes Icon", settingsHub.PoiTrackerSettings.StrongboxesIcon);
            AddChild(iconSizeMenu, "PerandusChest Icon", settingsHub.PoiTrackerSettings.PerandusChestIcon);
            AddChild(iconSizeMenu, "BreachChest Icon", settingsHub.PoiTrackerSettings.BreachChestIcon);
            MenuItem itemLootIcon = AddChild(iconSizeMenu, "Item loot Icon", settingsHub.ItemAlertSettings.LootIcon);
            AddChild(iconSizeMenu, "Use border color for loot icon", settingsHub.ItemAlertSettings.LootIconBorderColor); // Adding a ToggleNode as a RangeNode child doesn't display it
            AddChild(mapIconsMenu, "Minimap icons", settingsHub.MapIconsSettings.IconsOnMinimap);
            AddChild(mapIconsMenu, "Large map icons", settingsHub.MapIconsSettings.IconsOnLargeMap);
            AddChild(mapIconsMenu, "Drop items", settingsHub.ItemAlertSettings.ShowItemOnMap);
            AddChild(mapIconsMenu, "Monsters", settingsHub.MonsterTrackerSettings.Monsters);
            AddChild(mapIconsMenu, "Minions", settingsHub.MonsterTrackerSettings.Minions);
            AddChild(mapIconsMenu, "Strongboxes", settingsHub.PoiTrackerSettings.Strongboxes);
            AddChild(mapIconsMenu, "Chests", settingsHub.PoiTrackerSettings.Chests);
            AddChild(mapIconsMenu, "Masters", settingsHub.PoiTrackerSettings.Masters);

            //Menu Settings
            var menuSettings = AddChild(MenuRootButton, "Menu settings", settingsHub.MenuSettings.ShowMenu, "F12");
            AddChild(menuSettings, "Menu font color", settingsHub.MenuSettings.MenuFontColor);
            AddChild(menuSettings, "Title font color", settingsHub.MenuSettings.TitleFontColor);
            AddChild(menuSettings, "Enabled color", settingsHub.MenuSettings.EnabledBoxColor);
            AddChild(menuSettings, "Disabled color", settingsHub.MenuSettings.DisabledBoxColor);
            AddChild(menuSettings, "Slider color", settingsHub.MenuSettings.SliderColor);
            AddChild(menuSettings, "Background color", settingsHub.MenuSettings.BackgroundColor);
            AddChild(menuSettings, "Menu font size", settingsHub.MenuSettings.MenuFontSize);
            AddChild(menuSettings, "Title font size", settingsHub.MenuSettings.TitleFontSize);
            AddChild(menuSettings, "Picker font size", settingsHub.MenuSettings.PickerFontSize);

            var performanceSettings = AddChild(MenuRootButton, "Performance", settingsHub.PerformanceSettings.Enable);
            AddChild(performanceSettings, "FPS Render limit", settingsHub.PerformanceSettings.RenderLimit);
            AddChild(performanceSettings, "FPS Update entity limit", settingsHub.PerformanceSettings.UpdateEntityDataLimit);
            AddChild(performanceSettings, "Update areachange every N ms", settingsHub.PerformanceSettings.UpdateAreaLimit);
            AddChild(performanceSettings, "Use cache for most data", settingsHub.PerformanceSettings.Cache);
            AddChild(performanceSettings, "Update ingame state every N ms", settingsHub.PerformanceSettings.UpdateIngemeStateLimit);
            AddChild(performanceSettings, "Always Foreground", settingsHub.PerformanceSettings.AlwaysForeground);
            eInitMenu(MenuRootButton);//Spawning the menu in external plugins
        }

        private bool OnMouseEvent(MouseEventID id, Point position)
        {
            Vector2 mousePosition = Vector2.Zero;
            bool result = false;

            if (GameController.Window.IsForeground())
            {
                mousePosition = GameController.Window.ScreenToClient(position.X, position.Y);
                eMouseEvent(id, mousePosition);
                if (Settings.Enable)
                {
                    //I dunno how to handle this in plugins. Seems there is only this way {Stridemann}
                    result = ExternalMouseClick(id, mousePosition);
                    return MenuRootButton.OnMouseEvent(id, mousePosition) || result;
                }
            }
            return result;
        }
    }
}