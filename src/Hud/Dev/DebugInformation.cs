using System.Linq;
using System.Numerics;
using ImGuiNET;
using PoeHUD.Controllers;
using PoeHUD.Framework;
using PoeHUD.Hud.UI;
using PoeHUD.Models;

namespace PoeHUD.Hud.Dev
{
    public class DebugInformation : Plugin<DebugInformationSettings>
    {
        private readonly GameController _gameController;
        private readonly Graphics _graphics;
        private readonly DebugInformationSettings _settings;

        public DebugInformation(GameController gameController, Graphics graphics, DebugInformationSettings settings) : base(gameController, graphics, settings)
        {
            _gameController = gameController;
            _graphics = graphics;
            _settings = settings;

        }
        bool settingsShowWindow;
        private bool editTime;
        private int coroutinePerLoopIter;
        public override void Render()
        {


            if (_settings.ShowWindow)
            {
                ImGui.SetNextWindowPos(new Vector2(100, 100), SetCondition.Appearing);
                ImGui.SetNextWindowSize(new Vector2(1000, 600), SetCondition.Appearing);

                settingsShowWindow = _settings.ShowWindow;
                ImGui.BeginWindow("DebugInformation", ref settingsShowWindow, WindowFlags.NoCollapse);
                _settings.ShowWindow = settingsShowWindow;
                ImGui.Text($"Coroutines:{_gameController.CoroutineRunner.Count}");
                ImGui.SameLine(); ImGui.Text($" Added: {_gameController.CoroutineRunner.CountAddCoroutines} Attempted to add an existing: {_gameController.CoroutineRunner.CountFalseAddCoroutines}");
                ImGui.Text($"Coroutines Working: {_gameController.CoroutineRunner.WorkingCoroutines.Count()}");
                ImGui.Separator();
                coroutinePerLoopIter = _gameController.CoroutineRunner.RunPerLoopIter;
                ImGui.Text($"Run coroutines per loop iteration: {_gameController.CoroutineRunner.RunPerLoopIter}"); ImGui.SameLine();
                ImGui.SliderInt("##CoroutineRunPerLoop", ref coroutinePerLoopIter, 1, 15, coroutinePerLoopIter.ToString());
                _gameController.CoroutineRunner.RunPerLoopIter = coroutinePerLoopIter;
                ImGui.Separator();
                ImGui.Columns(10, "CoroutineTable", true);
                ImGui.Text("Name"); ImGui.NextColumn();
                ImGui.Text("Owner"); ImGui.NextColumn();
                ImGui.Text("Ticks"); ImGui.NextColumn();
                ImGui.Text("Started"); ImGui.NextColumn();
                ImGui.Text("Timeout ms"); ImGui.SameLine();
                ImGui.Checkbox("##timeout", ref editTime); ImGui.NextColumn();
                ImGui.Text("DoWork"); ImGui.NextColumn();
                ImGui.Text("AutoResume"); ImGui.NextColumn();
                ImGui.Text("Done"); ImGui.NextColumn();
                ImGui.Text("Priority"); ImGui.NextColumn();
                ImGui.Text($"DO"); ImGui.NextColumn();
                var coroutines = _gameController.CoroutineRunner.Coroutines.OrderByDescending(x => x.Priority).ToList();
                for (int i = 0; i < coroutines.Count(); i++)
                {
                    ImGui.Text($"{coroutines[i].Name}"); ImGui.NextColumn();
                    ImGui.Text($"{coroutines[i].Owner}"); ImGui.NextColumn();
                    ImGui.Text($"{coroutines[i].Ticks}"); ImGui.NextColumn();
                    ImGui.Text($"{coroutines[i].Started}"); ImGui.NextColumn();
                    if (editTime)
                    {
                        int tempDrag = coroutines[i].TimeoutForAction;
                        ImGui.DragInt($"Edit{coroutines[i].Name}", ref tempDrag, 1, 0, 5000, tempDrag.ToString());
                        coroutines[i].TimeoutForAction = tempDrag;
                    }
                    else
                    {
                        ImGui.Text($"{coroutines[i].TimeoutForAction}");
                    }


                    ImGui.NextColumn();
                    ImGui.Text($"{coroutines[i].DoWork}"); ImGui.NextColumn();
                    ImGui.Text($"{coroutines[i].AutoResume}"); ImGui.NextColumn();
                    ImGui.Text($"{coroutines[i].IsDone}"); ImGui.NextColumn();
                    ImGui.Text($"{coroutines[i].Priority}"); ImGui.NextColumn();
                    if (coroutines[i].DoWork)
                    {
                        if (ImGui.Button($"Stop##{coroutines[i].Name}"))
                        {
                            coroutines[i].Stop();
                        }
                    }
                    else
                    {
                        if (ImGui.Button($"Start##{coroutines[i].Name}"))
                        {
                            coroutines[i].Resume();
                        }
                    }
                    ImGui.SameLine();
                    if (ImGui.Button($"Done##{coroutines[i].Name}"))
                    {
                        coroutines[i].Done();
                    }
                    ImGui.NextColumn();
                }
                ImGui.Columns(1, "", false);
                ImGui.Separator();
                ImGui.Columns(2, "DebugInformation##GC", false);

                foreach (var DI in _gameController.DebugInformation)
                {
                    ImGui.Text($"{DI.Key}"); ImGui.NextColumn();
                    ImGui.Text($"{DI.Value}"); ImGui.NextColumn();
                }
                ImGui.Columns(1, "", false);
                ImGui.Separator();
                if (ImGui.CollapsingHeader($"Finished Coroutine {_gameController.CoroutineRunner.FinishedCoroutineCount}", TreeNodeFlags.CollapsingHeader))
                {
                    ImGui.Separator();
                    ImGui.Columns(5, "CoroutineTableFinished", true);
                    ImGui.Text("Name"); ImGui.NextColumn();
                    ImGui.Text("Owner"); ImGui.NextColumn();
                    ImGui.Text("Ticks"); ImGui.NextColumn();
                    ImGui.Text("Started"); ImGui.NextColumn();
                    ImGui.Text("End"); ImGui.NextColumn();
                    var finishedCoroutines = _gameController.CoroutineRunner.FinishedCoroutines.ToList();
                    for (int i = 0; i < finishedCoroutines.Count(); i++)
                    {
                        ImGui.Text($"{finishedCoroutines[i].Name}"); ImGui.NextColumn();
                        ImGui.Text($"{finishedCoroutines[i].Owner}"); ImGui.NextColumn();
                        ImGui.Text($"{finishedCoroutines[i].Ticks}"); ImGui.NextColumn();
                        ImGui.Text($"{finishedCoroutines[i].Started}"); ImGui.NextColumn();
                        ImGui.Text($"{finishedCoroutines[i].End}"); ImGui.NextColumn();

                    }
                }
                ImGui.Columns(1, "", false);
                ImGui.Separator();
                if (ImGui.CollapsingHeader($"Auto Restart Coroutine {_gameController.CoroutineRunner.AutorestartCoroutines.Count()}", TreeNodeFlags.CollapsingHeader))
                {
                    ImGui.Separator();
                    ImGui.Columns(5, "AutorestartCoroutinesTable", true);
                    ImGui.Text("Name"); ImGui.NextColumn();
                    ImGui.Text("Owner"); ImGui.NextColumn();
                    ImGui.Text("Timeout ms"); ImGui.NextColumn();
                    ImGui.Text("DoWork"); ImGui.NextColumn();
                    ImGui.Text("Priority"); ImGui.NextColumn();
                    var autorestartCoroutines = _gameController.CoroutineRunner.AutorestartCoroutines;
                    foreach (var autorestartCoroutine in autorestartCoroutines)

                    {
                        ImGui.Text($"{autorestartCoroutine.Name}"); ImGui.NextColumn();
                        ImGui.Text($"{autorestartCoroutine.Owner}"); ImGui.NextColumn();
                        ImGui.Text($"{autorestartCoroutine.TimeoutForAction}"); ImGui.NextColumn();
                        ImGui.Text($"{autorestartCoroutine.DoWork}"); ImGui.NextColumn();
                        ImGui.Text($"{autorestartCoroutine.Priority}"); ImGui.NextColumn();

                    }
                }
                ImGui.Columns(1, "", false);
                ImGui.Separator();
                ImGui.EndWindow();

            }

        }
    }
}