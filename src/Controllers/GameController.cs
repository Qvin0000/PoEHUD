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

        public GameController(Memory memory, PerformanceSettings settingsHub)
        {
            if (_instance != null) return;
            Instance = this;
            Performance = new Performance(this,settingsHub);
            Memory = memory;
            CoroutineRunner = new Runner("Main Coroutine");
            CoroutineRunnerParallel = new Runner("Parallel Coroutine");
            Area = new AreaController(this);
            Game = new TheGame(memory,Performance);
            EntityListWrapper = new EntityListWrapper(this);
            Window = new GameWindow(memory.Process,this);
            Files = new FsController(memory);
            InGame = Game.IngameState.InGame;
            IsForeGroundCache = WinApi.IsForegroundWindow(Window.Process.MainWindowHandle);
        }

        public EntityListWrapper EntityListWrapper { get; }
        public GameWindow Window { get; private set; }
        public TheGame Game { get; }
        public AreaController Area { get; }
        public Memory Memory { get; private set; }
        public IEnumerable<EntityWrapper> Entities => EntityListWrapper.Entities;
        public readonly Dictionary<string,SettingsBase> pluginsSettings = new Dictionary<string, SettingsBase>();
        
        public EntityWrapper Player => EntityListWrapper.Player;
        public Dictionary<Models.Enums.PlayerStats, int> PlayerStats => EntityListWrapper.PlayerStats;
        public bool InGame { get; private set; }
        public bool IsLoading { get; private set; }
        public bool AutoResume { get; set; }
        public FsController Files { get; private set; }
        public bool IsForeGroundCache { get; private set; }
        public Action Render;
        public Action Clear;
        public ConcurrentDictionary<string, float> DebugInformation = new ConcurrentDictionary<string, float>();
        public readonly Runner CoroutineRunner;
        public readonly Runner CoroutineRunnerParallel;
        public readonly Performance Performance;
        public static  long RenderCount { get; private set; }
        
        private int offsetRenderGraph = 0;
        private int offDelta = 0;
        public void WhileLoop()
        {
            Task.Run(ParallelCoroutineRunner);
            Performance.Initialise();
            DebugInformation["FpsLoop"] = 0;
            DebugInformation["FpsRender"] = 0;
            DebugInformation["FpsCoroutine"] = 0;
            DebugInformation["ElapsedMilliseconds"] = 0;
            var sw = Stopwatch.StartNew();
            float nextRenderTick = sw.ElapsedMilliseconds;
            var tickEverySecond = sw.ElapsedMilliseconds;
            
            int fpsLoop = 0;
            int fpsRender = 0;
            int fpsCoroutine = 0;
            int deltaError = 500;

            ControlCoroutinesInPlugin();

            var updateCoroutine = new Coroutine(MainCoroutineAction, 250, nameof(GameController) ,"$#Main#$") {Priority = CoroutinePriority.Critical};
            var updateArea = (new Coroutine(() => { Area.RefreshState(); }, Performance.updateAreaLimit, nameof(GameController),"Update area") {Priority = CoroutinePriority.High});
            var updateGameState = (new Coroutine(() => { 
               InGame = Game.IngameState.InGame;
                IsForeGroundCache = Performance.Settings.AlwaysForeground || WinApi.IsForegroundWindow(Window.Process.MainWindowHandle);
               Performance.Cache.ForceUpdateWindowCache();
               IsLoading = Game.IsGameLoading;
               Performance.CollectData(Game.IngameState.CurFps,Game.IngameState.CurLatency);
            }, Performance.updateIngameState, nameof(GameController), "Update Game State"){Priority = CoroutinePriority.Critical}).Run();
                
               
            
                
               
                
            
            updateArea.AutoRestart(CoroutineRunner).Run();
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
                        catch(Exception e){DebugPlugin.LogMsg($"Coroutine error: {e.Message}",6);}
                    }
                }
               

                if (sw.Elapsed.TotalMilliseconds >= nextRenderTick && InGame && IsForeGroundCache && !IsLoading)
                {
                    Render.SafeInvoke();
                    nextRenderTick += Performance.skipTicksRender;
                    fpsRender++;
                    RenderCount++;
                    var deltaRender = (float) (sw.Elapsed.TotalMilliseconds - startFrameTime);
                    DebugInformation["DeltaRender"] = deltaRender;
                    Performance.DeltaGraph[offDelta] = deltaRender;
                    offDelta++;
                    if (offDelta >= Performance.DeltaGraph.Length) offDelta = 0;
                }
                

                if (sw.ElapsedMilliseconds >= tickEverySecond)
                {
                    Performance.CalculateMeanDelta();
                    DebugInformation["FpsLoop"] = fpsLoop;
                    DebugInformation["FpsRender"] = fpsRender;
                    Performance.RenderGraph[offsetRenderGraph] = fpsRender;
                    offsetRenderGraph++;
                    if (offsetRenderGraph >= Performance.RenderGraph.Length) offsetRenderGraph = 0;
                    DebugInformation["FpsCoroutine"] = fpsCoroutine;
                    DebugInformation["Looplimit"] = Performance.loopLimit ;
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
                    EntityListWrapper.UpdateCondition();
                }
                fpsLoop++;
                DebugInformation["ElapsedMilliseconds"] = sw.ElapsedMilliseconds;
                DebugInformation["DeltaTimeMs"] = (float) (sw.Elapsed.TotalMilliseconds - startFrameTime);


                if (fpsLoop >= Performance.loopLimit )
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
                            DebugPlugin.LogMsg($"Coroutine error: {e.Message}",6);
                        }
                    }
                    else
                    {
                        await Task.Delay(100);
                    }
                parallelFps++;
                if (parallelFps >= Performance.Settings.ParallelCoroutineLimit)
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