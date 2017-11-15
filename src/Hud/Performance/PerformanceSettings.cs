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
            LoopLimit = new RangeNode<int>(1, 1, 300);
        }


        public RangeNode<int> UpdateDataLimit { get; set; }

        public RangeNode<int> RenderLimit { get; set; }
        public RangeNode<int> LoopLimit { get; set; }

    }
}
