using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using ImGuiNET;
using PoeHUD.Controllers;
using PoeHUD.DebugPlug;
using PoeHUD.Framework;
using PoeHUD.Framework.Helpers;
using PoeHUD.Hud.UI;
using PoeHUD.Models;
using PoeHUD.Models.Interfaces;
using PoeHUD.Poe;
using SharpDX;
using Vector2 = System.Numerics.Vector2;
using Vector4 = System.Numerics.Vector4;
namespace PoeHUD.Hud.Dev
{
    public class DebugTree : Plugin<DebugTreeSettings>
    {
        private readonly GameController _gameController;
        private readonly Graphics _graphics;
        private readonly DebugTreeSettings _settings;
        private Random rnd;
        Coroutine coroutineRndColor;

        public static DebugTree Instance = null;
        public DebugTree(GameController gameController, Graphics graphics, DebugTreeSettings settings) : base(
            gameController, graphics, settings)
        {
            Instance = this;
            _gameController = gameController;
            _graphics = graphics;
            _settings = settings;
            rnd = new Random((int)Runner.Instance.sw.ElapsedTicks);
            coroutineRndColor =
            (new Coroutine(() => { clr = new Color(rnd.Next(255), rnd.Next(255), rnd.Next(255), 255); }, 200,
                nameof(DebugTree), "Random Color")).Run();
            objectForDebug.Add(("GameController", gameController));
            objectForDebug.Add(("GameController.Game", gameController.Game));
            objectForDebug.Add(("IngameUi", gameController.Game.IngameState.IngameUi));
            objectForDebug.Add(("UIRoot", gameController.Game.IngameState.UIRoot));
        }

        private List<RectangleF> rectForDebug = new List<RectangleF>();
        Color clr = Color.Pink;
        bool settingsShowWindow;
        private bool enableDebugHover = false;
        private long uniqueIndex = 0;
        List<(string name, object obj)> objectForDebug = new List<(string name, object obj)>();

        public void AddToDebug(string name, object o)
        {
            if (objectForDebug.Any(x => x.name == name))
                return;
            objectForDebug.Add((name, o));

        }
        public override void Render()
        {
            if (_settings.ShowWindow)
            {
                uniqueIndex = 0;
                settingsShowWindow = _settings.ShowWindow;
                if (rectForDebug.Count == 0)
                    coroutineRndColor.Stop();
                else
                    coroutineRndColor.Resume();


                foreach (var rectangleF in rectForDebug)
                {
                    Graphics.DrawFrame(rectangleF, 2, clr);
                }

                ImGui.SetNextWindowPos(new Vector2(100, 100), SetCondition.Appearing);
                ImGui.SetNextWindowSize(new Vector2(600, 600), SetCondition.Appearing);

                ImGui.BeginWindow("DebugTree", ref settingsShowWindow, WindowFlags.NoCollapse);
                _settings.ShowWindow = settingsShowWindow;
                if (ImGui.Button("Clear##base"))
                {
                    rectForDebug.Clear();
                }

                ImGui.SameLine();
                ImGui.Checkbox("F1 for debug hover", ref enableDebugHover);
                if (enableDebugHover && WinApi.IsKeyDown(Keys.F1))
                {
                    var uihover = _gameController.Game.IngameState.UIHover;
                    var formattable = $"Hover: {uihover.ToString()} {uihover.Address}";
                    if (objectForDebug.Any(x => x.name.Contains(formattable)))
                    {
                        var findIndex = objectForDebug.FindIndex(x => x.name.Contains(formattable));
                        objectForDebug[findIndex] = (formattable + "^", uihover);
                    }
                    else
                    {

                        objectForDebug.Add((formattable, uihover));
                    }


                }


                for (int i = 0; i < objectForDebug.Count; i++)
                {
                    if (ImGui.TreeNode($"{objectForDebug[i].name}"))
                    {
                        DebugForImgui(objectForDebug[i].obj);

                        ImGui.TreePop();
                    }
                }

                ImGui.EndWindow();
            }
            else
            {
                coroutineRndColor.Stop();
            }
        }

        private void DebugForImgui(object obj)
        {
            var flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

            try
            {
                if (obj is IEntity)
                {
                    Dictionary<string, long> comp = new Dictionary<string, long>();
                    object ro;
                    if (obj is EntityWrapper)
                    {
                        ro = (EntityWrapper)obj;
                        comp = ((EntityWrapper)obj).InternalEntity.GetComponents();
                    }
                    else
                    {
                        ro = (Entity)obj;
                        comp = ((Entity)obj).GetComponents();
                    }
                    if (ImGui.TreeNode($"Components {comp.Count} ##{ro.GetHashCode()}"))
                    {
                        MethodInfo method;
                        if (ro is EntityWrapper)
                            method = typeof(EntityWrapper).GetMethod("GetComponent");
                        else
                            method = typeof(Entity).GetMethod("GetComponent");


                        foreach (var c in comp)
                        {
                            ImGui.Text(c.Key, new System.Numerics.Vector4(1, 0.412f, 0.706f, 1));
                            ImGui.SameLine();
                            var type = Type.GetType(
                                "PoeHUD.Poe.Components." + c.Key +
                                ", PoeHUD, Version=6.3.9600.0, Culture=neutral, PublicKeyToken=null");
                            if (type == null)
                            {
                                ImGui.Text(" - undefiend",
                                    new System.Numerics.Vector4(1, 0.412f, 0.706f, 1));
                                continue;
                            }
                            var generic = method.MakeGenericMethod(type);
                            var g = generic.Invoke(ro, null);
                            if (ImGui.TreeNode($"##{ro.GetHashCode()}{c.Key.GetHashCode()}"))
                            {
                                uniqueIndex++;
                                if (ImGui.Button($"Debug this##{uniqueIndex}"))
                                {
                                    var formattableString = $"{obj.ToString()}->{c.Key}";
                                    if (objectForDebug.Any(x => x.name.Contains(formattableString)))
                                    {
                                        var findIndex = objectForDebug.FindIndex(x => x.name.Contains(formattableString));
                                        objectForDebug[findIndex] = (formattableString + "^", g);
                                    }
                                    else
                                        objectForDebug.Add((formattableString, g));
                                }
                                DebugForImgui(g);
                                ImGui.TreePop();
                            }
                        }
                        ImGui.TreePop();
                    }
                }


                if (obj is Element)
                {
                    ImGui.SameLine();
                    uniqueIndex++;
                    if (ImGui.Button($"Draw this##{uniqueIndex}"))
                    {
                        var el = (Element)obj;

                        rectForDebug.Add(el.GetClientRect());
                    }
                    ImGui.SameLine();

                    uniqueIndex++;
                    if (ImGui.Button($"Clear##from draw this{uniqueIndex}"))
                    {
                        rectForDebug.Clear();
                    }
                }

                var oProp = obj.GetType().GetProperties(flags).Where(x => x.GetIndexParameters().Length == 0);
                DebugImGuiFields(obj);
                oProp = oProp.OrderBy(x => x.Name).ToList();
                foreach (var propertyInfo in oProp)
                    if (propertyInfo.GetValue(obj, null).GetType().IsPrimitive ||
                        propertyInfo.GetValue(obj, null) is decimal ||
                        propertyInfo.GetValue(obj, null) is string ||
                        propertyInfo.GetValue(obj, null) is TimeSpan ||
                        propertyInfo.GetValue(obj, null) is Enum
                    )
                    {
                        ImGui.Text($"{propertyInfo.Name}: ");
                        ImGui.SameLine();
                        var o = propertyInfo.GetValue(obj, null);
                        if (propertyInfo.Name.Contains("Address"))
                            o = Convert.ToInt64(o).ToString("X");
                        ImGui.Text($"{o}", new System.Numerics.Vector4(1, 0.647f, 0, 1));
                        if (propertyInfo.Name.Contains("Address"))
                        {
                            ImGui.SameLine();
                            if (ImGui.SmallButton($"Copy##{o}"))
                            {
                                ImGuiNative.igSetClipboardText(o.ToString());
                            }
                        }
                    }
                    else
                    {
                        var label = propertyInfo.ToString();
                        var o = propertyInfo.GetValue(obj, null);
                        if (o == null)
                        {
                            ImGui.Text("Null");
                            continue;
                        }
                        if (label.Contains("Framework") || label.Contains("Offsets"))
                            continue;

                        if (!propertyInfo.PropertyType.GetInterfaces().Contains(typeof(IEnumerable)))
                        {
                            if (ImGui.TreeNode(label))
                            {
                                uniqueIndex++;
                                if (ImGui.Button($"Debug this##{uniqueIndex}"))
                                {
                                    var formattable = $"{label}->{o}";
                                    if (objectForDebug.Any(x => x.name.Contains(formattable)))
                                    {
                                        var findIndex = objectForDebug.FindIndex(x => x.name.Contains(formattable));
                                        objectForDebug[findIndex] = (formattable + "^", o);
                                    }
                                    else
                                    {

                                        objectForDebug.Add((formattable, o));
                                    }
                                }


                                DebugForImgui(o);

                                ImGui.TreePop();
                            }
                        }
                        if (propertyInfo.PropertyType.GetInterfaces().Contains(typeof(IEnumerable)))
                        {
                            ImGui.Text($"{propertyInfo.GetValue(obj, null).ToString()}",
                                new System.Numerics.Vector4(0.486f, 0.988f, 0, 1));
                            var i = 0;

                            var enumerable = (IEnumerable)o;
                            var items = enumerable as IList<object> ?? enumerable.Cast<object>().ToList();
                            uniqueIndex++;
                            if (ImGui.Button($"Draw Childs##{uniqueIndex}"))
                            {
                                var tempi = 0;
                                foreach (var item in items)
                                {
                                    var el = (Element)item;

                                    rectForDebug.Add(el.GetClientRect());
                                    tempi++;
                                    if (tempi > 1000) break;
                                }
                            }
                            ImGui.SameLine();
                            uniqueIndex++;
                            if (ImGui.Button($"Draw Childs for Childs##{uniqueIndex}"))
                            {
                                DrawChilds(items);
                            }
                            ImGui.SameLine();
                            uniqueIndex++;
                            if (ImGui.Button($"Draw Childs for Childs Only Visible##{uniqueIndex}"))
                            {
                                DrawChilds(items, true);
                            }
                            ImGui.SameLine();
                            uniqueIndex++;
                            if (ImGui.Button($"Clear##from draw childs##{uniqueIndex}"))
                            {
                                rectForDebug.Clear();
                            }
                            foreach (var item in items)
                            {
                                if (item == null)
                                    continue;
                                if (i > 500)
                                {
                                    break;
                                }
                                ImGui.Text($"{item.ToString()}", new System.Numerics.Vector4(0.486f, 0.988f, 0, 1));
                                ImGui.SameLine();
                                if (ImGui.TreeNode($" #{i} ##{item.ToString()}  {item.GetHashCode()}"))
                                {
                                    DebugForImgui(item);
                                    ImGui.TreePop();
                                }
                                i++;
                            }
                        }
                    }
            }
            catch (Exception e)
            {
                DebugPlugin.LogMsg($"Debug Tree: {e.Message}", 1);
            }
        }

        private void DrawChilds(object obj, bool OnlyVisible = false)
        {
            var flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

            if (obj is IEnumerable)
            {

                var tempi = 0;
                foreach (var item in (IEnumerable)obj)
                {
                    var el = (Element)item;

                    if (OnlyVisible)
                    {
                        if (!el.IsVisible) continue;
                    }
                    rectForDebug.Add(el.GetClientRect());
                    tempi++;
                    if (tempi > 1000) break;
                    var oProp = item.GetType().GetProperties(flags).Where(x => x.GetIndexParameters().Length == 0);
                    foreach (var propertyInfo in oProp)
                    {
                        DrawChilds(propertyInfo.GetValue(item, null));
                    }
                }
            }
            else
            {
                if (obj is Element)
                {
                    var el = (Element)obj;

                    rectForDebug.Add(el.GetClientRect());
                }
            }

        }



        void DebugImGuiFields(object obj)
        {
            var flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
            var fields = obj.GetType().GetFields(flags);

            foreach (var fieldInfo in fields)
            {
                {
                    ImGui.PushStyleColor(ColorTarget.Text, new Vector4(0.529f, 0.808f, 0.922f, 1));
                    ImGui.Text($"{fieldInfo.Name} -=> {fieldInfo.GetValue(obj)}");
                    ImGui.PopStyleColor();
                }
            }
        }
    }
}