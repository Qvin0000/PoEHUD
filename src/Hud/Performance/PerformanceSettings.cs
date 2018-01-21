using System.Windows.Forms;
using PoeHUD.Hud.Settings;

namespace PoeHUD.Hud.Performance
{
    public sealed class  PerformanceSettings: SettingsBase
    {


        public PerformanceSettings()
        {
            Enable = true;
           
            UpdateEntityDataLimit = new RangeNode<int>(25,5,200);
            UpdateAreaLimit = new RangeNode<int>(100,25,1000);
            UpdateIngemeStateLimit = new RangeNode<int>(100,25,1000);
            IterCoroutinePerLoop = new RangeNode<int>(3,1,20);
            RenderLimit = new RangeNode<int>(60, 10,200);
            DynamicRender = true;
            DynamicDataUpdate = true;
            LoopLimit = new RangeNode<int>(5, 1,300);
            ParallelCoroutineLimit = new RangeNode<int>(3, 1,300);
            DpsUpdateTime = new RangeNode<int>(200, 20,600);
            Cache = new ToggleNode(true);
            AlwaysForeground = new ToggleNode(false);
            ItemAlertUpdateTime = 50;
            ItemAlertLimitWork = 250;
            MinDynamicFps = new RangeNode<int>(15,5,60);
            LittleCache = true;
        }


        public RangeNode<int> UpdateEntityDataLimit { get; set; }
        public ToggleNode DynamicDataUpdate { get; set; }
        public RangeNode<int> IterCoroutinePerLoop { get; set; }
        public RangeNode<int> UpdateIngemeStateLimit { get; set; }
        public RangeNode<int> UpdateAreaLimit { get; set; }
        public RangeNode<int> RenderLimit { get; set; }
        public ToggleNode DynamicRender { get; set; }
        public RangeNode<int> LoopLimit { get; set; }
        public RangeNode<int> ParallelCoroutineLimit { get; set; }
        public RangeNode<int> DpsUpdateTime { get; set; }
        public int ItemAlertUpdateTime { get; set; }
        public int ItemAlertLimitWork { get; set; }
        public ToggleNode Cache { get; set; }
        public ToggleNode AlwaysForeground { get; set; }
        public RangeNode<int> MinDynamicFps { get; set; }
        public ToggleNode LittleCache { get; set; }
    }
}
