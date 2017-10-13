using Newtonsoft.Json;
using PoeHUD.Hud.Settings;

namespace PoeHUD.Hud.Performance
{
    public sealed class PerformanceSettings : SettingsBase
    {
        public PerformanceSettings()
        {
            Enable = true;
            UpdateDataLimit = new RangeNode<int>(60, 10, 200);
            RenderLimit = new RangeNode<int>(60, 10, 200);
        }


        public RangeNode<int> UpdateDataLimit { get; set; }

        public RangeNode<int> RenderLimit { get; set; }

        [JsonIgnore]
        public int DataSkip => 1000 / UpdateDataLimit;

        [JsonIgnore]
        public int RenderSkip => 1000 / RenderLimit;
    }
}
