using System.Linq;
using System.Numerics;
using ImGuiNET;
using PoeHUD.Controllers;
using PoeHUD.DebugPlug;
using PoeHUD.Framework;
using PoeHUD.Framework.Helpers;
using PoeHUD.Hud.UI;

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
        public override void Render()
        {
            if (_settings.ShowWindow)
            {
                ImGui.SetNextWindowPos(new Vector2(100, 100), SetCondition.Appearing);
                ImGui.SetNextWindowSize(new Vector2(1000, 600), SetCondition.Appearing);

                settingsShowWindow = _settings.ShowWindow;
                ImGui.BeginWindow("DebugInformation", ref settingsShowWindow, WindowFlags.Default);
                _settings.ShowWindow = settingsShowWindow;
                ImGui.Text($"Coroutines:{Runner.Instance.Count}");
                ImGui.Text($"Coroutines Working: {Runner.Instance.WorkingCoroutines.Count()}");
                ImGui.Separator();
                ImGui.Columns(9, "CoroutineTable", true);
                ImGui.Text("Name"); ImGui.NextColumn();
                ImGui.Text("Owner"); ImGui.NextColumn();
                ImGui.Text("Ticks"); ImGui.NextColumn();
                ImGui.Text("Started"); ImGui.NextColumn();
                ImGui.Text("Timeout"); ImGui.NextColumn();
                ImGui.Text("DoWork"); ImGui.NextColumn();
                ImGui.Text("Done"); ImGui.NextColumn();
                ImGui.Text("Priority"); ImGui.NextColumn();
                ImGui.Text($"DO"); ImGui.NextColumn();
                var coroutines = Runner.Instance.Coroutines.ToList();
                for (int i = 0; i < coroutines.Count(); i++)
                {
                    ImGui.Text($"{coroutines[i].Name}"); ImGui.NextColumn();
                    ImGui.Text($"{coroutines[i].Owner}"); ImGui.NextColumn();
                    ImGui.Text($"{coroutines[i].Ticks}"); ImGui.NextColumn();
                    ImGui.Text($"{coroutines[i].Started}"); ImGui.NextColumn();
                    ImGui.Text($"{coroutines[i].TimeoutForAction}"); ImGui.NextColumn();
                    ImGui.Text($"{coroutines[i].DoWork}"); ImGui.NextColumn();
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

                ImGui.Separator();
                ImGui.Columns(2, "DebugInformation##GC", false);
                foreach (var DI in _gameController.DebugInformation)
                {
                    ImGui.Text($"{DI.Key}"); ImGui.NextColumn();
                    ImGui.Text($"{DI.Value}"); ImGui.NextColumn();
                }
                ImGui.Separator();
                if (ImGui.CollapsingHeader($"FinishedCoroutine {Runner.Instance.FinishedCoroutines.Count()}", TreeNodeFlags.CollapsingHeader))
                {
                    ImGui.Columns(5, "CoroutineTableFinished", true);
                    ImGui.Text("Name"); ImGui.NextColumn();
                    ImGui.Text("Owner"); ImGui.NextColumn();
                    ImGui.Text("Ticks"); ImGui.NextColumn();
                    ImGui.Text("Started"); ImGui.NextColumn();
                    ImGui.Text("End"); ImGui.NextColumn();
                    var finishedCoroutines = Runner.Instance.FinishedCoroutines.ToList();
                    for (int i = 0; i < finishedCoroutines.Count(); i++)
                    {
                        ImGui.Text($"{finishedCoroutines[i].Name}"); ImGui.NextColumn();
                        ImGui.Text($"{finishedCoroutines[i].Owner}"); ImGui.NextColumn();
                        ImGui.Text($"{finishedCoroutines[i].Ticks}"); ImGui.NextColumn();
                        ImGui.Text($"{finishedCoroutines[i].Started}"); ImGui.NextColumn();
                        ImGui.Text($"{finishedCoroutines[i].End}"); ImGui.NextColumn();

                    }
                }

                ImGui.EndWindow();

            }

        }
    }
}