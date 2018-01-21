using System;
using System.Collections;
using System.Diagnostics;
using PoeHUD.Controllers;
using PoeHUD.Framework.Helpers;

namespace PoeHUD.Framework
{
    public class Coroutine
    {
        private readonly IEnumerator _enumerator;
        public bool IsDone { get; private set; }
        public string Name { get; set; }
        public string Owner { get; private set; }
        public bool DoWork { get; private set; }
        public bool AutoResume { get; set; } = true;
        public string TimeoutForAction { get;private set; }
        public long Ticks { get; private set; } = -1;
        public CoroutinePriority Priority { get; set; } =  CoroutinePriority.Normal;
        public DateTime Started { get; set; }
        public Action Action { get; private set; }
        public YieldBase Condition { get; private set; }
        
        public bool ThisIsSimple => Action != null;
       
        public Coroutine(Action action, YieldBase condition, string owner, string name = null, bool autoStart = true)
        {
            DoWork = autoStart;
            Started = DateTime.Now;
            switch (condition)
            {
                case WaitTime _:
                    TimeoutForAction = ((WaitTime) condition).Milliseconds.ToString();
                    break;
                case WaitRender _:
                    TimeoutForAction = ((WaitRender) condition).HowManyRenderCountWait.ToString();
                    break;
                case WaitFunction _:
                    TimeoutForAction = "Function -1";
                    break;
            }
            Owner = owner;
            Action = action;
            Condition = condition;
            IEnumerator CoroutineAction(Action a)
            {
                while (true)
                {
                    a?.Invoke();
                    Ticks++;
                    yield return Condition.GetEnumerator();
                }
            }
            Name = name ?? MathHepler.GetRandomWord(13);
            _enumerator = CoroutineAction(action);
        }

        public Coroutine(Action action, int waitMilliseconds, string owner, string name = null, bool autoStart = true) :
            this(action, new WaitTime(waitMilliseconds), owner, name, autoStart)
        {
            
        }


        public Coroutine(IEnumerator enumerator, string owner, string name = null, bool autoStart = true)
        {
            DoWork = autoStart;
            Started = DateTime.Now;
            TimeoutForAction = "Not simple -1";
            Name = name ?? MathHepler.GetRandomWord(13);
            Owner = owner;
            _enumerator = enumerator;
       
        }

        public IEnumerator Wait()
        {
            while (!IsDone)
            {
                yield return null;
            }
        }

        public void UpdateCondtion(YieldBase condition)
        {
            switch (condition)
            {
                case WaitTime _:
                    TimeoutForAction = ((WaitTime) condition).Milliseconds.ToString();
                    break;
                case WaitRender _:
                    TimeoutForAction = ((WaitRender) condition).HowManyRenderCountWait.ToString();
                    break;
                case WaitFunction _:
                    TimeoutForAction = "Function";
                    break;
            }
            Condition = condition;
        }

        public Coroutine GetCopy(Coroutine cor)
        {
            if (cor.ThisIsSimple)
            {
              return  (new Coroutine(cor.Action, cor.Condition, cor.Owner, cor.Name,cor.DoWork)
                  {Priority = cor.Priority, AutoResume =  cor.AutoResume, DoWork = cor.DoWork});
            }
            return (new Coroutine(cor.GetEnumerator(),cor.Owner,cor.Name,cor.DoWork)
                {Priority = cor.Priority, AutoResume = cor.AutoResume, DoWork = cor.DoWork});
        }
        public IEnumerator GetEnumerator() => _enumerator;
        public void UpdateTicks(int tick) => Ticks = tick;
        public void Resume()
        {
            DoWork = true;
        }

        public void Pause(bool force = false)
        {
            if (Priority == CoroutinePriority.Critical && !force) return;
            DoWork = false;
        }

        public bool Done(bool force = false)
        {
            if (Priority == CoroutinePriority.Critical) return false;
            return IsDone = true;
        }

        public bool MoveNext() => MoveNext(_enumerator);

        private bool MoveNext(IEnumerator enumerator) => !IsDone && (enumerator.Current is IEnumerator e && MoveNext(e) || enumerator.MoveNext());
    }


    public class WaitRender : YieldBase
    {
        private readonly long _howManyRenderCountWait;
        public long HowManyRenderCountWait => _howManyRenderCountWait;

        public WaitRender(long howManyRenderCountWait = 1)
        {
            _howManyRenderCountWait = howManyRenderCountWait;
            Current = GetEnumerator();
        }

        public sealed override IEnumerator GetEnumerator()
        {
            var wait = GameController.RenderCount +_howManyRenderCountWait;
            while (GameController.RenderCount < wait)
            {
                yield return null;
            }
        }
    }

    public class WaitFunction : YieldBase
    {
        public WaitFunction(Func<bool> fn)
        {
            while (fn())
            {
                Current = GetEnumerator();
            }
        }

        public sealed override IEnumerator GetEnumerator()
        {
            yield return null;
        }
    }

    public class WaitTime : YieldBase
    {
        private readonly int _milliseconds;
        public int Milliseconds => _milliseconds;

        public WaitTime(int milliseconds)
        {
            _milliseconds = milliseconds;
            Current = GetEnumerator();
        }


        public sealed override IEnumerator GetEnumerator()
        {
            var wait = sw.ElapsedMilliseconds + _milliseconds;
            while (sw.ElapsedMilliseconds < wait)
            {
                yield return null;
            }
        }
    }

    public abstract class YieldBase : IEnumerable, IEnumerator
    {
       protected static readonly Stopwatch sw = Stopwatch.StartNew();
        public bool MoveNext() 
        { 
            return Current != null && ((IEnumerator) Current).MoveNext(); 
        } 

        public void Reset()
        {
        }

        public object Current { get; protected set; }
        
        public abstract IEnumerator GetEnumerator();
    }

    public enum CoroutinePriority
    {
        Normal,
        High,
        Critical
    }
}
