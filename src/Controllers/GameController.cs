using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using PoeHUD.DebugPlug;
using PoeHUD.Framework;
using PoeHUD.Framework.Helpers;
using PoeHUD.Hud.Performance;
using PoeHUD.Hud.Settings;
using PoeHUD.Models;
using PoeHUD.Poe.RemoteMemoryObjects;
namespace PoeHUD.Controllers
{
    public class GameController
    {
        private static GameController _instance;
        public static GameController Instance
        {
            get => _instance;
            private set
            {
                if(_instance==null)
                _instance = value;
            }
        }

        public GameController(Memory memory)
        {
            if (_instance != null) return;
            Instance = this;
            Memory = memory;
            CoroutineRunner = new Runner("Main Coroutine");
            CoroutineRunnerParallel = new Runner("Parallel Coroutine");
            Area = new AreaController(this);
            EntityListWrapper = new EntityListWrapper(this);
            Window = new GameWindow(memory.Process);
            Cache = new Cache();
            Game = new TheGame(memory);
            Files = new FsController(memory);
            
            InGame = InGameReal;
            IsForeGroundCache = WinApi.IsForegroundWindow(Window.Process.MainWindowHandle);
          
            
            MainTimer = Stopwatch.StartNew();
        }

        public EntityListWrapper EntityListWrapper { get; }
        public GameWindow Window { get; private set; }
        public TheGame Game { get; }
        public AreaController Area { get; }
        public Cache Cache { get; private set; } 
        public Memory Memory { get; private set; }
        public Stopwatch MainTimer { get; }
        public IEnumerable<EntityWrapper> Entities => EntityListWrapper.Entities;
        public readonly Dictionary<string,SettingsBase> pluginsSettings = new Dictionary<string, SettingsBase>();
        
        public EntityWrapper Player => EntityListWrapper.Player;
        public bool InGame { get; private set; }
        public bool IsLoading { get; private set; }
        bool InGameReal => Game.IngameState.InGame;
        public bool AutoResume { get; set; }
        public FsController Files { get; private set; }
        public bool IsForeGroundCache { get; private set; }
        public Action Render;
        public Action Clear;
        public ConcurrentDictionary<string, float> DebugInformation = new ConcurrentDictionary<string, float>();
        public readonly Runner CoroutineRunner;
        public readonly Runner CoroutineRunnerParallel;
        public PerformanceSettings Performance;
        public  long RenderCount { get; private set; }
        public float[] RenderGraph = new float[121];
        public float[] DeltaGraph = new float[100];
        private int offsetRenderGraph = 0;
        private int offDelta = 0;
        public void WhileLoop()
        {
            Task.Run(ParallelCoroutineRunner);
            DebugInformation["FpsLoop"] = 0;
            DebugInformation["FpsRender"] = 0;
            DebugInformation["FpsCoroutine"] = 0;
            DebugInformation["ElapsedMilliseconds"] = 0;
            var sw = Stopwatch.StartNew();
            float nextRenderTick = sw.ElapsedMilliseconds;
            var tickEverySecond = sw.ElapsedMilliseconds;
            var skipTicksRender = 0f;
            int fpsLoop = 0;
            int fpsRender = 0;
            int fpsCoroutine = 0;
            float updateRate = 1f / 60f;
            float loopLimit = 1;
            int updateAreaLimit = 100;
            int updateEntityLimit = 50;
            int updateIngameState = 100;
            int deltaError = 500;

            ControlCoroutinesInPlugin();

            var updateCoroutine = new Coroutine(MainCoroutineAction, 250, nameof(GameController) ,"$#Main#$") {Priority = CoroutinePriority.Critical};
            var updateArea = (new Coroutine(() => { Area.RefreshState(); }, updateAreaLimit, nameof(GameController),"Update area") {Priority = CoroutinePriority.High});
            var updateEntity = (new Coroutine(() => { EntityListWrapper.RefreshState(); }, updateEntityLimit,nameof(GameController), "Update Entity"){Priority = CoroutinePriority.High});
           var updateGameState = (new Coroutine(() => { 
               InGame = InGameReal;
                IsForeGroundCache = Performance.AlwaysForeground || WinApi.IsForegroundWindow(Window.Process.MainWindowHandle);
               Cache.ForceUpdateWindowCache();
               IsLoading = Game.IsGameLoading;
           }, updateIngameState, nameof(GameController), "Update Game State"){Priority = CoroutinePriority.Critical}).Run();
            
            if (Performance != null)
            {
                loopLimit = Performance.LoopLimit;
                skipTicksRender = 1000f/Performance.RenderLimit;
                Cache.Enable = Performance.Cache;
                updateAreaLimit = Performance.UpdateAreaLimit;
                updateEntityLimit = Performance.UpdateEntityDataLimit;
                updateIngameState = Performance.UpdateIngemeStateLimit;
                DeltaGraph = new float[Performance.RenderLimit*5];
                Performance.UpdateEntityDataLimit.OnValueChanged += () =>
                {
                    CoroutineRunner.Coroutines.Concat(CoroutineRunnerParallel.Coroutines).FirstOrDefault(x => x.Name == "Update Entity")
                        ?.UpdateCondtion(new WaitTime(1000 / Performance.UpdateEntityDataLimit.Value));
                };
                Performance.RenderLimit.OnValueChanged += () =>
                {
                    skipTicksRender = 1000f / Performance.RenderLimit; 
                    DeltaGraph = new float[Performance.RenderLimit*5];
                };
                Performance.LoopLimit.OnValueChanged += () =>{loopLimit = (int) (300 + Performance.LoopLimit);};
                Performance.UpdateAreaLimit.OnValueChanged += () => {      CoroutineRunner.Coroutines.Concat(CoroutineRunnerParallel.Coroutines).FirstOrDefault(x => x.Name == "Update Entity")
                    ?.UpdateCondtion(new WaitTime(Performance.UpdateAreaLimit));};
                Performance.UpdateIngemeStateLimit.OnValueChanged += () =>{     CoroutineRunner.Coroutines.Concat(CoroutineRunnerParallel.Coroutines).FirstOrDefault(x => x.Name == "Update Entity")
                    ?.UpdateCondtion(new WaitTime(Performance.UpdateIngemeStateLimit));};
                Performance.Cache.OnValueChanged += () => { Cache.Enable = Performance.Cache; };
                Performance.DpsUpdateTime.OnValueChanged += () =>
                {
                    CoroutineRunner.Coroutines.Concat(CoroutineRunnerParallel.Coroutines).FirstOrDefault(x=>x.Name == "Calculate DPS")?.UpdateCondtion(new WaitTime(Performance.DpsUpdateTime));
                };
            }
            updateArea.AutoRestart(CoroutineRunner).Run();
            //Sometimes parallel maybe unstable need testing
            if (Performance?.ParallelEntityUpdate)
                updateEntity.AutoRestart(CoroutineRunnerParallel).RunParallel();
            else
                updateEntity.AutoRestart(CoroutineRunner).Run();
            sw.Restart();
            CoroutineRunnerParallel.RunPerLoopIter = 1;
            updateCoroutine.Run();
            while (true)
            {
                if (!InGame)
                {
                    Thread.Sleep(50);
                }

                var startFrameTime = sw.Elapsed.TotalMilliseconds;

                for (int j = 0; j < CoroutineRunner.RunPerLoopIter; j++)
                {
                    if (CoroutineRunner.IsRunning)
                    {
                        fpsCoroutine++;
                        try
                        {
                            CoroutineRunner.Update();
                        }
                        catch(Exception e){DebugPlugin.LogMsg($"{e.Message}",1);}
                    }
                }
               

                if (sw.Elapsed.TotalMilliseconds >= nextRenderTick && InGame && IsForeGroundCache && !IsLoading)
                {
                    Render.SafeInvoke();
                    nextRenderTick += skipTicksRender;
                    fpsRender++;
                    RenderCount++;
                    var deltaRender = (float) (sw.Elapsed.TotalMilliseconds - startFrameTime);
                    DebugInformation["DeltaRender"] = deltaRender;
                    DeltaGraph[offDelta] = deltaRender;
                    offDelta++;
                    if (offDelta >= DeltaGraph.Length) offDelta = 0;
                }
                

                if (sw.ElapsedMilliseconds >= tickEverySecond)
                {
                    DebugInformation["FpsLoop"] = fpsLoop;
                    DebugInformation["FpsRender"] = fpsRender;
                    RenderGraph[offsetRenderGraph] = fpsRender;
                    offsetRenderGraph++;
                    if (offsetRenderGraph >= RenderGraph.Length) offsetRenderGraph = 0;
                    DebugInformation["FpsCoroutine"] = fpsCoroutine;
                    DebugInformation["Looplimit"] = loopLimit;
                    DebugInformation["ElapsedSeconds"] = sw.Elapsed.Seconds;
                    DebugInformation["RenderCount"] = RenderCount;
                    fpsLoop = 0;
                    fpsRender = 0;
                    fpsCoroutine = 0;
                    tickEverySecond += 1000;
                    if (nextRenderTick - sw.ElapsedMilliseconds > deltaError || nextRenderTick - sw.ElapsedMilliseconds < deltaError)
                    {
                        nextRenderTick = sw.ElapsedMilliseconds;
                    }
                    foreach (var autorestartCoroutine in CoroutineRunner.AutorestartCoroutines)
                    {
                        if(!CoroutineRunner.HasName(autorestartCoroutine.Name))
                            autorestartCoroutine.GetCopy().Run();
                    }
                    foreach (var autorestartCoroutine in CoroutineRunnerParallel.AutorestartCoroutines)
                    {
                        if(!CoroutineRunnerParallel.HasName(autorestartCoroutine.Name))
                            autorestartCoroutine.GetCopy().RunParallel();
                    }
                }
                fpsLoop++;
                DebugInformation["ElapsedMilliseconds"] = sw.ElapsedMilliseconds;
                DebugInformation["DeltaTimeMs"] = (float) (sw.Elapsed.TotalMilliseconds - startFrameTime);


                if (fpsLoop >= loopLimit)
                {
                    Thread.Sleep(1);
                }
            }
        }

        private void ControlCoroutinesInPlugin()
        {
              foreach (var setting in pluginsSettings)
                        {
                            setting.Value.Enable.OnValueChanged += () =>
                            {
                                var coroutines = CoroutineRunner.Coroutines.Where(x=>x.Owner == setting.Key).Concat(CoroutineRunnerParallel.Coroutines.Where(x=>x.Owner == setting.Key)).ToList();
                                foreach (var coroutine in coroutines)
                                {
                                    if (setting.Value.Enable)
                                    {
                                        if (coroutine.AutoResume)
                                        {
                                            coroutine.Resume();
                                        }
                                    }
                                    else
                                    {
                                        coroutine.Pause();
                                    }
                                }
                            };
                        }
        }

        async Task ParallelCoroutineRunner()
        {
            var parallelFps = 0;
            while (true)
            {
                    if (CoroutineRunnerParallel.IsRunning)
                    {
                        try
                        {
                            CoroutineRunnerParallel.Update();
                        }
                        catch (Exception e)
                        {
                            DebugPlugin.LogMsg($"{e.Message}", 10);
                        }
                    }
                    else
                    {
                        await Task.Delay(100);
                    }
                parallelFps++;
                if (parallelFps >= Performance.ParallelCoroutineLimit)
                {
                    await Task.Delay(1);
                    DebugInformation["Parallel Coroutine FPS"] = parallelFps;
                    parallelFps = 0;
                }
            }
        }
        
        void MainCoroutineAction()
        {
                
            var coroutines = CoroutineRunner.Coroutines.Concat(CoroutineRunnerParallel.Coroutines).ToList();
            if (!InGame || !IsForeGroundCache || IsLoading)
            {
                Clear.SafeInvoke();
                foreach (var cor in coroutines)
                {
                    cor.Pause();
                }
                AutoResume = true;
            }
            else
            {
                if (AutoResume)
                {
                    foreach (var coroutine in coroutines)
                    {
                        if (pluginsSettings.TryGetValue(coroutine.Owner, out var result))
                        {
                            if(result.Enable && coroutine.AutoResume)
                                coroutine.Resume();
                            else
                                continue;
                        }
                        if(coroutine.AutoResume)
                            coroutine.Resume();
                    }
                    AutoResume = false;
                }
                foreach (var coroutine in coroutines)
                {

                    if (pluginsSettings.TryGetValue(coroutine.Owner, out var result) && !result.Enable)
                    {
                        coroutine.Pause();
                    }
                }
            }
        }
    }
}