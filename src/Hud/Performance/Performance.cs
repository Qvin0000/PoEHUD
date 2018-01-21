using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Sockets;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Antlr4.Runtime.Tree.Xpath;
using PoeHUD.Controllers;
using PoeHUD.Framework;
using PoeHUD.Framework.Helpers;
using PoeHUD.Models;

namespace PoeHUD.Hud.Performance
{
    public class Performance
    {
        private readonly GameController _gameController;
        public readonly PerformanceSettings Settings;
        public readonly Cache Cache;
        public Performance(GameController gameController, PerformanceSettings performanceSettings)
        {
            _gameController = gameController;
            Settings = performanceSettings;
            Cache = new Cache(gameController);
            GameFps = new float[SizeArray];
            Latency = new float[SizeArray];
        }
        
        public float[] RenderGraph = new float[121];
        public float[] DeltaGraph = new float[300];
        public float[] GameFps;
        public float[] Latency;
        private int SizeArray = 200;
       
        public int updateAreaLimit = 100;
        public int updateIngameState = 100;
        public int loopLimit;
        public float skipTicksRender;
        public int timeUpdateEntity;
        public int meanRender;
        public int meanDelta;
        public int meanLatency;
        public int meanGameFps;
        private Coroutine timerCoroutine;
        public long timer;
        private bool SettingLittleCache;
        public void Initialise()
        {
            timer = _gameController.Game.MainTimer.ElapsedMilliseconds;
            skipTicksRender = 1000f/Settings.RenderLimit;
            timeUpdateEntity =  (int) 1000f / Settings.UpdateEntityDataLimit;
            Cache.Enable = Settings.Cache;
            updateAreaLimit = Settings.UpdateAreaLimit;
            updateIngameState = Settings.UpdateIngemeStateLimit;
            for (int i = 0; i < RenderGraph.Length; i++)
            {
                RenderGraph[i] = Settings.RenderLimit * 0.8f;
            }
            for (int i = 0; i < DeltaGraph.Length; i++)
            {
                DeltaGraph[i] = skipTicksRender;
            }
            for (int i = 0; i < GameFps.Length; i++)
            {
                GameFps[i] = 60;
            }
            for (int i = 0; i < Latency.Length; i++)
            {
                Latency[i] = 15;
            }
            Settings.UpdateEntityDataLimit.OnValueChanged += () =>
            {
                _gameController.CoroutineRunner.Coroutines.Concat(_gameController.CoroutineRunnerParallel.Coroutines).FirstOrDefault(x => x.Name == "Update Entity")
                    ?.UpdateCondtion(new WaitTime(1000 / Settings.UpdateEntityDataLimit.Value));
            };
            
            Settings.LoopLimit.OnValueChanged += () =>{loopLimit = (int) (300 + Settings.LoopLimit);};
            Settings.UpdateAreaLimit.OnValueChanged += () => {      _gameController.CoroutineRunner.Coroutines.Concat(_gameController.CoroutineRunnerParallel.Coroutines).FirstOrDefault(x => x.Name == "Update Entity")
                ?.UpdateCondtion(new WaitTime(Settings.UpdateAreaLimit));};
            Settings.UpdateIngemeStateLimit.OnValueChanged += () =>{     _gameController.CoroutineRunner.Coroutines.Concat(_gameController.CoroutineRunnerParallel.Coroutines).FirstOrDefault(x => x.Name == "Update Entity")
                ?.UpdateCondtion(new WaitTime(Settings.UpdateIngemeStateLimit));};
            Settings.Cache.OnValueChanged += () => { Cache.Enable = Settings.Cache; };
            Settings.DpsUpdateTime.OnValueChanged += () =>
            {
                _gameController.CoroutineRunner.Coroutines.Concat(_gameController.CoroutineRunnerParallel.Coroutines).FirstOrDefault(x=>x.Name == "Calculate DPS")?.UpdateCondtion(new WaitTime(Settings.DpsUpdateTime));
            };
            timerCoroutine =
                (new Coroutine(() =>
                    {
                        timer = _gameController.Game.MainTimer.ElapsedMilliseconds;
                        SettingLittleCache = Settings.LittleCache;
                    }, new WaitTime(5),
                    nameof(Performance), "Timer updater"){Priority = CoroutinePriority.Critical}).AutoRestart(_gameController.CoroutineRunner)
                .Run();
        }

       public void CalculateMeanDelta()
       {

           meanRender = (int) MeanArray(RenderGraph, (int) (RenderGraph.Length *0.15f), (int) (RenderGraph.Length*0.85f));
           meanDelta = (int) MeanArray(DeltaGraph, (int) (DeltaGraph.Length *0.15f), (int) (DeltaGraph.Length*0.85f));
            meanLatency = (int) MeanArray(Latency, (int) (Latency.Length*0.15f), (int) (Latency.Length*0.85f));
           meanGameFps = (int) MeanArray(GameFps, (int) (Latency.Length *0.10f), (int) (Latency.Length*0.65f));
            if (Settings.DynamicRender)
            {
                if (meanGameFps <= Settings.MinDynamicFps)
                    meanGameFps = Settings.MinDynamicFps;
                if (meanGameFps < Settings.RenderLimit)
                    skipTicksRender = 1000f / (meanGameFps * 0.8f);
                else
                    skipTicksRender = 1000f / Settings.RenderLimit;
            }
            else
            {
                skipTicksRender = 1000f / Settings.RenderLimit;
            }
            if (Settings.DynamicDataUpdate) 
            {
                if (meanLatency >= 1000f / Settings.UpdateEntityDataLimit)
                    timeUpdateEntity = meanLatency;
                else
                    timeUpdateEntity = (int) 1000f / Settings.UpdateEntityDataLimit;
            }
            else
            {
                timeUpdateEntity = (int) 1000f / Settings.UpdateEntityDataLimit;
            }

           if (Settings.LittleCache)
           {
               timerCoroutine.Resume();
           }
           else
           {
               timerCoroutine.Pause();
           }
        }

        private float MeanArray(float[] data, int skip,int take)
        {
            var sortedData = data.ToArray();
            Array.Sort(sortedData);
            var meanData = sortedData.Skip(skip).Take(take).Average();
            return meanData;
        }

        private int _offsets = 0;
        public void CollectData(float fps, float latency)
        {
            GameFps[_offsets] = fps;
            Latency[_offsets] = latency;
            _offsets++;
            if (_offsets >= GameFps.Length)
                _offsets = 0;
        }

        public T ReadMemWithCache<T>(Func<long, T> read, long address, float time, float minWait = 0)
        {
            if (!SettingLittleCache)
            {
                return read(address);
            }
            T result;
            if (Cache.LittleCacheTime.TryGetValue(address, out var resultTime))
            {
                if (timer > resultTime)
                {
                    Cache.LittleCacheTime[address] = GetWaitTime(time);
                    result = read(address);
                    Cache.LittleCache[address] = result;

                }
                else
                {

                    result = (T)Cache.LittleCache[address];
                }
            }
            else
            {
                result = read(address);
                Cache.LittleCacheTime[address] = GetWaitTime(time);
                Cache.LittleCache[address] = result;
            }

            return result;
        }
        
        public float GetWaitTime(float wait, float minWait =0)
        {
            if (wait < minWait)
            {
                return timer + minWait;
            }
            return timer + wait;
        }
    }
}