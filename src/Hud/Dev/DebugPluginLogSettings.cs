using PoeHUD.Hud.Settings;

namespace PoeHUD.Hud.Dev
{
    public class DebugPluginLogSettings:SettingsBase
    {
        public DebugPluginLogSettings()
        {
            Enable = true;
            
        }

        public bool ShowWindow { get; set; }
    }
}