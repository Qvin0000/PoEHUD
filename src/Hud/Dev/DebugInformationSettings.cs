using PoeHUD.Hud.Settings;

namespace PoeHUD.Hud.Dev
{
    public class DebugInformationSettings : SettingsBase
    {
        public DebugInformationSettings()
        {
            Enable = true;
        }

        public bool ShowWindow { get; set; }

    }
}