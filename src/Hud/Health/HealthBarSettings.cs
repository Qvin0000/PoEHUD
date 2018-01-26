using PoeHUD.Hud.Settings;

namespace PoeHUD.Hud.Health
{
    public sealed class HealthBarSettings : SettingsBase
    {
        public HealthBarSettings()
        {
            Enable = false;
            ShowInTown = false;
            ShowES = true;
            ShowMana = true;
            ShowIncrements = true;
            ShowEnemies = true;
            Players = new UnitSettings(0x008000ff, 0);
            Minions = new UnitSettings(0x90ee90ff, 0);
            NormalEnemy = new UnitSettings(0xff0000ff, 0, 0x66ff66ff, false);
            MagicEnemy = new UnitSettings(0xff0000ff, 0x8888ffff, 0x66ff99ff, false);
            RareEnemy = new UnitSettings(0xff0000ff, 0xffff77ff, 0x66ff99ff, false);
            UniqueEnemy = new UnitSettings(0xff0000ff, 0xffa500ff, 0x66ff99ff, false);
            ShowDebuffPanel = false;
            DebuffPanelIconSize = new RangeNode<int>(20, 15, 40);
            X = new RangeNode<float>(0, -200, 200);
            Y = new RangeNode<float>(0, -200, 500);
            NewStyle = false;
            ZAll = new RangeNode<int>(55,-200,200);
        }

        public ToggleNode ShowInTown { get; set; }
        public ToggleNode ShowES { get; set; }
        public ToggleNode ShowMana { get; set; }
        public ToggleNode ShowIncrements { get; set; }
        public ToggleNode ShowEnemies { get; set; }
        public UnitSettings Players { get; set; }
        public UnitSettings Minions { get; set; }
        public UnitSettings NormalEnemy { get; set; }
        public UnitSettings MagicEnemy { get; set; }
        public UnitSettings RareEnemy { get; set; }
        public UnitSettings UniqueEnemy { get; set; }
        public ToggleNode ShowDebuffPanel { get; set; }
        public RangeNode<int> DebuffPanelIconSize { get; set; }
        public RangeNode<float> X { get; set; }
        public RangeNode<float> Y { get; set; }
        public ToggleNode NewStyle { get; set; }
        public RangeNode<int> ZAll { get; set; }
    }
}