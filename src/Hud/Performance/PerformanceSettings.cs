using PoeHUD.Hud.Settings;

namespace PoeHUD.Hud.Performance
{
    public sealed class  PerformanceSettings: SettingsBase
    {


        public PerformanceSettings()
        {
            Enable = true;
           
            UpdateEntityDataLimit = new RangeNode<int>(25,10,200);
            UpdateAreaLimit = new RangeNode<int>(100,25,1000);
            UpdateIngemeStateLimit = new RangeNode<int>(100,25,1000);
            IterCoroutinePerLoop = new RangeNode<int>(3,1,20);
            RenderLimit = new RangeNode<int>(60, 10,200);
            LoopLimit = new RangeNode<int>(1, 1,300);
            ParallelCoroutineLimit = new RangeNode<int>(3, 1,300);
            Cache = new ToggleNode(true);
        }


        public RangeNode<int> UpdateEntityDataLimit { get; set; }
        public RangeNode<int> IterCoroutinePerLoop { get; set; }
        public RangeNode<int> UpdateIngemeStateLimit { get; set; }
        public RangeNode<int> UpdateAreaLimit { get; set; }
        public RangeNode<int> RenderLimit { get; set; }
        public RangeNode<int> LoopLimit { get; set; }
        public RangeNode<int> ParallelCoroutineLimit { get; set; }
        public ToggleNode Cache { get; set; }
        
    }
}
