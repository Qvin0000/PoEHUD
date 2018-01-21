using System.Reflection.Emit;
using PoeHUD.Hud.Settings;

namespace PoeHUD.Hud.Dev
{
    public class DebugTreeSettings:SettingsBase
    {
        public DebugTreeSettings()
        {
            Enable = true;
            
        }

        public bool ShowWindow { get; set; }
    }
}