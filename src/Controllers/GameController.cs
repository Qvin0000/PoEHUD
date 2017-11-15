using System;
using PoeHUD.Framework;
using PoeHUD.Models;
using PoeHUD.Poe.Components;
using PoeHUD.Poe.RemoteMemoryObjects;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using PoeHUD.Framework.Helpers;
using PoeHUD.Hud.Performance;


namespace PoeHUD.Controllers
{
    public class GameController
    {
        public static GameController Instance;

        public GameController(Memory memory)
        {
            Instance = this;
            Memory = memory;
            Area = new AreaController(this);
            EntityListWrapper = new EntityListWrapper(this);
            Window = new GameWindow(memory.Process);
            Game = new TheGame(memory);
            Files = new FsController(memory);
            CoroutineRunner = Runner.Instance;
            InGameCache = InGame;
            IsForeGroundCache = WinApi.IsForegroundWindow(Window.Process.MainWindowHandle);
        }

        public EntityListWrapper EntityListWrapper { get; }
        public GameWindow Window { get; private set; }
        public TheGame Game { get; }
        public AreaController Area { get; }

        public Memory Memory { get; private set; }

        public IEnumerable<EntityWrapper> Entities => EntityListWrapper.Entities;

        public EntityWrapper Player => EntityListWrapper.Player;
        public bool InGameCache { get; private set; }
        public bool InGame => Game.IngameState.InGame;
        public bool AutoResume { get; set; }
        public FsController Files { get; private set; }
        public bool IsForeGroundCache { get; private set; }


        public List<EntityWrapper> GetAllPlayerMinions() =>
            Entities.Where(x => x.HasComponent<Player>()).SelectMany(c => c.Minions).ToList();

        public Action Render;
        public Action Clear;
        public Dictionary<string, float> DebugInformation = new Dictionary<string, float>();
        public readonly Runner CoroutineRunner;
        public PerformanceSettings Performance;
        public void WhileLoop()
        {
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
            int deltaError = 500;

            if (Performance != null)
            {
                loopLimit = Performance.LoopLimit;
                skipTicksRender = 1000f / Performance.RenderLimit.Value;
            }

            var updateArea = (new Coroutine(() => { Area.RefreshState(); }, 100, nameof(GameController), "Update area") { Priority = CoroutinePriority.High }).Run();

            var updateEntity = (new Coroutine(() => { EntityListWrapper.RefreshState(); }, 50, nameof(GameController), "Update Entity") { Priority = CoroutinePriority.High }).Run();

            void Action()
            {
                InGameCache = InGame;
                IsForeGroundCache = WinApi.IsForegroundWindow(Window.Process.MainWindowHandle);
                var allCoroutines = CoroutineRunner.Coroutines;
                if (!InGameCache || !IsForeGroundCache)
                {
                    Clear.SafeInvoke();
                    CoroutineRunner.StopCoroutines(allCoroutines);
                    AutoResume = true;
                }
                else
                {
                    if (AutoResume)
                    {
                        CoroutineRunner.ResumeCoroutines(allCoroutines);
                        AutoResume = false;
                    }
                }
                if (Performance != null)
                {
                    skipTicksRender = 1000f / Performance.RenderLimit.Value;
                    loopLimit = (int)(200 + Math.Pow(Performance.LoopLimit, 2));
                    updateEntity.TimeoutForAction = 1000 / Performance.UpdateDataLimit.Value;
                }
                if (nextRenderTick - sw.ElapsedMilliseconds > deltaError || nextRenderTick - sw.ElapsedMilliseconds < deltaError)
                {
                    nextRenderTick = sw.ElapsedMilliseconds;
                }
            }

            var updateCoroutine = new Coroutine(Action, 250, nameof(GameController), "$#Main#$") { Priority = CoroutinePriority.Critical };
            updateCoroutine = CoroutineRunner.Run(updateCoroutine);



            sw.Restart();
            while (true)
            {
                if (!InGameCache)
                {
                    Thread.Sleep(100);
                }

                var startFrameTime = sw.Elapsed.TotalMilliseconds;

                if (CoroutineRunner.IsRunning)
                {
                    fpsCoroutine++;
                    CoroutineRunner.Update();
                }

                if (sw.Elapsed.TotalMilliseconds > nextRenderTick && InGameCache && IsForeGroundCache)
                {
                    Render.SafeInvoke();
                    nextRenderTick += skipTicksRender;
                    fpsRender++;
                }


                if (sw.ElapsedMilliseconds > tickEverySecond)
                {
                    DebugInformation["FpsLoop"] = fpsLoop;
                    DebugInformation["FpsRender"] = fpsRender;
                    DebugInformation["FpsCoroutine"] = fpsCoroutine;
                    DebugInformation["Looplimit"] = loopLimit;
                    DebugInformation["ElapsedSeconds"] = sw.Elapsed.Seconds;
                    fpsLoop = 0;
                    fpsRender = 0;
                    fpsCoroutine = 0;
                    tickEverySecond += 1000;
                }
                fpsLoop++;
                DebugInformation["ElapsedMilliseconds"] = sw.ElapsedMilliseconds;
                DebugInformation["DeltaTimeMs"] = (float)(sw.Elapsed.TotalMilliseconds - startFrameTime);


                if (fpsLoop > loopLimit)
                {
                    Thread.Sleep(1);
                }
            }
        }
    }
}