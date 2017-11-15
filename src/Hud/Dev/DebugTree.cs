using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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

namespace PoeHUD.Hud.Dev
{
    public class DebugTree : Plugin<DebugTreeSettings>
    {
        private readonly GameController _gameController;
        private readonly Graphics _graphics;
        private readonly DebugTreeSettings _settings;
        private Random rnd;
        Coroutine coroutineRndColor;

        public DebugTree(GameController gameController, Graphics graphics, DebugTreeSettings settings) : base(
            gameController, graphics, settings)
        {
            _gameController = gameController;
            _graphics = graphics;
            _settings = settings;
            rnd = new Random((int)Runner.Instance.sw.ElapsedTicks);

            coroutineRndColor =
            (new Coroutine(() => { clr = new Color(rnd.Next(255), rnd.Next(255), rnd.Next(255), 255); }, 200,
                nameof(DebugTree), "Random Color")).Run();
            objectForDebug.Add(("GameController", gameController));
            objectForDebug.Add(("GameController.Game", gameController.Game));
        }

        private RectangleF rectForDebug = RectangleF.Empty;
        Color clr = Color.Pink;
        bool settingsShowWindow;
        List<(string name, object obj)> objectForDebug = new List<(string name, object obj)>();

        public override void Render()
        {
            if (_settings.ShowWindow)
            {
                settingsShowWindow = _settings.ShowWindow;
                if (rectForDebug == RectangleF.Empty)
                    coroutineRndColor.Stop();
                else
                    coroutineRndColor.Resume();


                Graphics.DrawFrame(rectForDebug, 2, clr);
                ImGui.SetNextWindowPos(new Vector2(100, 100), SetCondition.Appearing);
                ImGui.SetNextWindowSize(new Vector2(600, 600), SetCondition.Appearing);

                ImGui.BeginWindow("DebugTree", ref settingsShowWindow, WindowFlags.NoCollapse);
                _settings.ShowWindow = settingsShowWindow;
                if (ImGui.Button("Clear"))
                {
                    rectForDebug = RectangleF.Empty;
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

                                if (ImGui.Button($"Debug this##{g.GetHashCode()}"))
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
                var oProp = obj.GetType().GetProperties(flags).Where(x => x.GetIndexParameters().Length == 0);
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
                                if (ImGui.Button($"Debug this##{label.GetHashCode()}{o.GetHashCode()}"))
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
                                if (o is Element || label.Contains("PoeHUD.Poe.Element"))
                                {
                                    ImGui.SameLine();
                                    if (ImGui.Button("Draw this##" + o.GetHashCode()))
                                    {
                                        var el = (Element)propertyInfo.GetValue(obj, null);

                                        rectForDebug = el.GetClientRect();
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
                            foreach (var item in (IEnumerable)o)
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
                                    if (item is Element && !(item is IEnumerable))
                                    {
                                        ImGui.PushStyleColor(ColorTarget.Button,
                                            new System.Numerics.Vector4(0.541f, 0.169f, 0.786f, 0.7f));
                                        ImGui.PushStyleColor(ColorTarget.ButtonHovered,
                                            new System.Numerics.Vector4(0.551f, 0.179f, 0.886f, 0.7f));
                                        ImGui.PushStyleColor(ColorTarget.ButtonActive,
                                            new System.Numerics.Vector4(0.561f, 0.189f, 0.986f, 0.7f));
                                        if (ImGui.Button("Draw this##" + item.GetHashCode()))
                                        {
                                            var el = (Element)item;
                                            rectForDebug = el.GetClientRect();
                                        }
                                        ImGui.PopStyleColor(3);
                                    }
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
    }
}