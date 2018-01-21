using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using ImGuiNET;
using PoeHUD.Controllers;
using PoeHUD.DebugPlug;
using PoeHUD.Framework;
using PoeHUD.Framework.Helpers;
using PoeHUD.Hud.AdvancedTooltip;
using PoeHUD.Hud.UI;
using PoeHUD.Models;
using PoeHUD.Models.Enums;
using PoeHUD.Models.Interfaces;
using PoeHUD.Poe;
using PoeHUD.Poe.Components;
using PoeHUD.Poe.EntityComponents;
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
        public static DebugTree Instance;
        private Random rnd;
        Coroutine coroutineRndColor;
       
        public DebugTree(GameController gameController, Graphics graphics, DebugTreeSettings settings) : base(
            gameController, graphics, settings)
        {
            Instance = this;
            _gameController = gameController;
            _graphics = graphics;
            _settings = settings;
            rnd = new Random((int) gameController.Game.MainTimer.ElapsedMilliseconds);
            
             coroutineRndColor =
             (new Coroutine(() => { clr = new Color(rnd.Next(255), rnd.Next(255), rnd.Next(255), 255); }, new WaitTime(200), 
                 nameof(DebugTree), "Random Color")).Run();
           objectForDebug.Add(("GameController", gameController));
           objectForDebug.Add(("GameController.Game", gameController.Game));
           objectForDebug.Add(("Memory", gameController.Memory));
           objectForDebug.Add(("Offsets", gameController.Memory.offsets));
           objectForDebug.Add(("IngameUi",gameController.Game.IngameState.IngameUi));
           objectForDebug.Add(("UIRoot",gameController.Game.IngameState.UIRoot));
           objectForDebug.Add(("Player",gameController.Player));
           objectForDebug.Add(("PlayerInventory",   gameController.Game.IngameState.IngameUi.InventoryPanel[InventoryIndex.PlayerInventory]));
           objectForDebug.Add(("Amulet",   gameController.Game.IngameState.IngameUi.InventoryPanel[InventoryIndex.Amulet]));
           objectForDebug.Add(("Flask",   gameController.Game.IngameState.IngameUi.InventoryPanel[InventoryIndex.Flask]));
           objectForDebug.Add(("Belt",   gameController.Game.IngameState.IngameUi.InventoryPanel[InventoryIndex.Belt]));
           objectForDebug.Add(("LRing",   gameController.Game.IngameState.IngameUi.InventoryPanel[InventoryIndex.LRing]));

            void DrawGroupTest(IEnumerable<IGrouping<long, Entity>> fortest, string name)
            {
                foreach (var tt in fortest)
                {
                    if (ImGui.TreeNode(tt.Key.ToString()))
                    {
                        foreach (var entity in tt)
                        {
                            if (ImGui.TreeNode($"{entity.Path}##{entity.Id}"))
                            {
                                DebugForImgui(entity);
                                ImGui.TreePop();
                            }
                        }
                        ImGui.TreePop();
                    }
                }
            }
            anotherDebugWindowDelegates["Debug Tree-> Group Entity Test1"] = () =>
            {
                var test = GameController.Game.IngameState.Data.EntityList.Entities.GroupBy(x => x.TestTypeId);
                DrawGroupTest(test, "Group Entity Test1");
            };
            anotherDebugWindowDelegates["Debug Tree-> Group Entity Test2"] = () =>
            {
                var test2 = GameController.Game.IngameState.Data.EntityList.Entities.GroupBy(x => x.TestTypeId2);
                DrawGroupTest(test2, "Group Entity Test2");
            };
            anotherDebugWindowDelegates["Debug Tree-> Group Entity Test3"] = () =>
            {
                var test3 = GameController.Game.IngameState.Data.EntityList.Entities.GroupBy(x => x.TestTypeId3);
                DrawGroupTest(test3, "Group Entity Test3");
            };
            anotherDebugWindowDelegates["Debug Tree-> Group Entity Test4"] = () =>
            {
                var test4 = GameController.Game.IngameState.Data.EntityList.Entities.GroupBy(x => x.TestTypeId4);
                DrawGroupTest(test4, "Group Entity Test4");
            };

            anotherDebugWindowDelegates["All Inventory Items"] = () =>
            {

               // var items = GameController.Entities.Where(x=>x.HasComponent<WorldItem>()).Select(x => x.GetComponent<WorldItem>().ItemEntity);
                if (!GameController.Game.IngameState.IngameUi.InventoryPanel.IsVisibleLocal) return;
                var items = GameController.Game.IngameState.IngameUi.InventoryPanel[InventoryIndex.PlayerInventory].VisibleInventoryItems.Select(x=>x.Item);
                foreach (var item in items)
                {
                   if(ImGui.TreeNode($"{item.Path}##{item.Address}"))
                    {
                        var it = new ItemDebug(item);
                        RoyalBlueText("ItemDebug");
                        DebugImguiSimpleProp(it);
                        ImGui.Separator();
                        
                        RoyalBlueText("BaseItemType");
                        DebugImguiSimpleProp(it.BaseItemType);
                        ImGui.Separator();
                        
                        RoyalBlueText("Mods");
                        DebugImguiSimpleProp(it.mods);
                        ImGui.Separator();
                        
                        RoyalBlueText("List-> ItemMods");
                   
                        foreach (var mod in it.mods.ItemMods)
                        {
                            DebugForImgui(mod);                            
                        }
                      
                        ImGui.Separator();
                        RoyalBlueText("Mods#2");
                        var test = it.mods.ItemMods.Select(itemS => new ModValue(itemS, GameController.Files,
                            it.mods.ItemLevel, GameController.Files.BaseItemTypes.Translate(item.Path))).ToList();
                        var index = 0;
                        foreach (var modValue in test)
                        {
                            ImGui.Text($"MOD VALUE #:{index}");
                            DebugImguiSimpleProp(modValue);
                            DebugForImgui(modValue.Record);
                            index++;
                        }
                        ImGui.Separator();
                        RoyalBlueText("ItemStats");
                        DebugImguiSimpleProp(it.mods.ItemStats);
                        ImGui.Separator();
                        
                        RoyalBlueText("Sockets");
                        DebugImguiSimpleProp(it.sockets);
                        ImGui.Separator();
                        
                        RoyalBlueText("Rarity");
                        DebugImguiSimpleProp(it.itemRarity);
                        ImGui.TreePop();
                    }
                }
                
            };

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
            {
                var findIndex = objectForDebug.FindIndex(x=>x.name==name);
                objectForDebug.RemoveAt(findIndex);
            }
            objectForDebug.Add((name, o));
            
        }


        public bool anotherDebugWindow = false;
        public ConcurrentDictionary<string, Action> anotherDebugWindowDelegates = new ConcurrentDictionary<string, Action>();
        public override void Render()
        {
            if (anotherDebugWindow)
            {
                AnotherDebugWindowDraw();
            }
            if (_settings.ShowWindow)
            {  
                uniqueIndex = 0;
                settingsShowWindow = _settings.ShowWindow;
                if (rectForDebug.Count == 0)
                    coroutineRndColor.Pause();
                else
                    coroutineRndColor.Resume();

                
                foreach (var rectangleF in rectForDebug)
                {
                    Graphics.DrawFrame(rectangleF, 2, clr);
                }
                
                ImGui.SetNextWindowPos(new Vector2(100, 100), Condition.Appearing,new Vector2(1,0));
                ImGui.SetNextWindowSize(new Vector2(600, 600), Condition.Appearing);

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
                        var findIndex = objectForDebug.FindIndex(x=>x.name.Contains(formattable));
                        objectForDebug[findIndex] = (formattable + "^", uihover);
                    }
                    else
                    {
                                        
                        objectForDebug.Add((formattable,uihover));
                    }
                    
     
                }

                if (ImGui.TreeNode("Buttons for another Debugs"))
                {
                    ButtonsAnotherDebug();
                    ImGui.TreePop();
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
                coroutineRndColor.Pause();
            }
        }

        private void ButtonsAnotherDebug()
        {
          
         
            if (ImGui.Button("DebugCurrent Stash"))
            {
                if (_gameController.Game.IngameState.IngameUi.OpenLeftPanel.IsVisible)
                {
                    if (_gameController.Game.IngameState.ServerData.StashPanel?.VisibleStash != null)
                    {
                        AddToDebug("Current Stash",_gameController.Game.IngameState.ServerData.StashPanel.VisibleStash);
                    }
                }
            }
               
      
            if (ImGui.Button("Another Debug"))
            {
                anotherDebugWindow = !anotherDebugWindow;
            }
          
            if (ImGui.Button("Debug items entity inventory"))
            {
                var entities = GameController.Game.IngameState.IngameUi.InventoryPanel[InventoryIndex.PlayerInventory].VisibleInventoryItems.Select(x=>x.Item).ToList();
                var test = new Tuple<string,List<Entity>>("asd",entities);
                AddToDebug("Inventory items",test);
            }
           
            if (ImGui.Button("Debug modvalue items"))
            {
                var entites = GameController.Game.IngameState.IngameUi.InventoryPanel[InventoryIndex.PlayerInventory].VisibleInventoryItems.Select(x=>x.Item).ToList();
                List<(Entity, List<ModValue>)> tupleEnt = entites.Select(x => (x, x.GetComponent<Mods>().ItemMods.Select(y=>new ModValue(y,GameController.Files,x.GetComponent<Mods>().ItemLevel,GameController.Files.BaseItemTypes.Translate(x.Path))).ToList())).ToList();
                var test = new  Tuple<string, List<(Entity, List<ModValue>)>>("DebugValueMod",tupleEnt);
                AddToDebug("Debug modvalue items",test);
            }
        }

        private bool[] _windows = new bool[1024];
        private void AnotherDebugWindowDraw()
        {
            try
            {
                var index = 0;
                foreach (var action in anotherDebugWindowDelegates)
                {
                    if (action.Value != null)
                    {
                        ImGui.BeginWindow("Another debug");
                        ImGui.Checkbox(action.Key, ref _windows[index]);
                        if (_windows[index])
                        {
                            ImGui.BeginWindow(action.Key);
                            action.Value.SafeInvoke();
                            ImGui.EndWindow();
                        }
                        ImGui.EndWindow();

                    }
                    index++;
                }
            }
            catch (Exception e)
            {
                DebugPlugin.LogMsg(e.Message);
            }
        }

        private void DebugForImgui(object obj)
        {
            var flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
            
            try
            {
                if (obj is IEntity )
                {
                    Dictionary<string, long> comp = new Dictionary<string, long>();
                    object ro;
                    if (obj is EntityWrapper)
                    {
                        ro = (EntityWrapper) obj;
                        comp = ((EntityWrapper) obj).InternalEntity.GetComponents();
                    }
                    else
                    {
                         ro = (Entity) obj ;
                        comp = ((Entity) obj).GetComponents();
                    }
                    if (ImGui.TreeNode($"Components {comp.Count} ##{ro.GetHashCode()}"))
                    {
                        MethodInfo method;
                        if(ro is EntityWrapper)
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
                                type = Type.GetType(
                                    "PoeHUD.Poe.EntityComponents." + c.Key +
                                    ", PoeHUD, Version=6.3.9600.0, Culture=neutral, PublicKeyToken=null");
                            }
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
                                        var findIndex = objectForDebug.FindIndex(x=>x.name.Contains(formattableString));
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
                        var el = (Element) obj;

                        rectForDebug.Add(el.GetClientRect());
                    }
                    ImGui.SameLine();
                    
                    uniqueIndex++;
                    if (ImGui.Button($"Clear##from draw this{uniqueIndex}"))
                    {
                        rectForDebug.Clear();
                    }
                    ImGui.SameLine();
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
                                        var findIndex = objectForDebug.FindIndex(x=>x.name.Contains(formattable));
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

                            var enumerable = (IEnumerable) o;
                            var items = enumerable as IList<object> ?? enumerable.Cast<object>().ToList();
                            uniqueIndex++;
                            if (ImGui.Button($"Draw Childs##{uniqueIndex}"))
                            {
                                var tempi = 0;
                                foreach (var item in items)
                                {
                                    var el = (Element) item;

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
                                DrawChilds(items,true);
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
                foreach (var item in (IEnumerable) obj)
                {
                    var el = (Element) item;

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
                    var el = (Element) obj;

                    rectForDebug.Add(el.GetClientRect());
                }
            }

        }

       

        void DebugImGuiFields(object obj) 
        { 
            var flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance; 
            var fields = obj.GetType().GetFields(flags); 
 
            foreach (var fieldInfo in fields){ 
                { 
                    ImGui.PushStyleColor(ColorTarget.Text,new Vector4(0.529f, 0.808f, 0.922f,1)); 
                    ImGui.Text($"{fieldInfo.Name} -=> {fieldInfo.GetValue(obj)}"); 
                    ImGui.PopStyleColor(); 
                } 
            } 
        }


        void DebugImguiSimpleProp(object obj)
        {
            var flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance; 
            var oProp = obj.GetType().GetProperties(flags).Where(x => x.GetIndexParameters().Length == 0);
            oProp = oProp.OrderBy(x => x.Name).ToList();
            foreach (var propertyInfo in oProp)
                if (propertyInfo.GetValue(obj, null).GetType().IsPrimitive ||
                    propertyInfo.GetValue(obj, null) is decimal ||
                    propertyInfo.GetValue(obj, null) is string ||
                    propertyInfo.GetValue(obj, null) is TimeSpan ||
                    propertyInfo.GetValue(obj, null) is Enum
                )
                {
                    ImGui.PushStyleColor(ColorTarget.Text,new Vector4(1.000f, 0.843f, 0.000f,1)); 
                    ImGui.Text($"{propertyInfo.Name}: ");
                    ImGui.PopStyleColor(); 
                    ImGui.SameLine();
                    var o = propertyInfo.GetValue(obj, null);
                    ImGui.PushStyleColor(ColorTarget.Text,new Vector4(0f, 1f, 0f,1)); 
                    ImGui.Text(o.ToString());
                    ImGui.PopStyleColor(); 
                }
        }

        void RoyalBlueText(string text)
        {
            ImGui.PushStyleColor(ColorTarget.Text,new Vector4(0.255f, 0.412f, 0.88f,1)); 
            ImGui.Text(text);
            ImGui.PopStyleColor(); 
        }
        
    }


    public class ItemDebug
    {
        public BaseItemType BaseItemType { get; }
        public string basename { get; }
        public string className { get; }
        public Base itemBase { get; }
        public int width { get; }
        public int height { get; }
        public Mods mods { get; }
        public bool isSkillHGem { get; }
        public bool isMap { get; }
        public bool isShapedMap { get; }
        public ItemRarity itemRarity { get; }
        public int quality { get; }
        public string text { get; }
        public Sockets sockets { get; }
        public List<string> socketGroup { get; }
        public int numberOfSockets { get; }
        public int largestLinkSize { get; }
        public string path { get; }
        
        public ItemDebug(IEntity entity)
        {
            if (entity == null)
                return;
     
            
            BaseItemType = GameController.Instance.Files.BaseItemTypes.Translate(entity.Path);
            if (BaseItemType == null)
                return;
            
            basename = BaseItemType.BaseName;
            var dropLevel = BaseItemType.DropLevel;
            Models.ItemClass tmp;
            
            if (GameController.Instance.Files.itemClasses.contents.TryGetValue(BaseItemType.ClassName, out tmp))
                className = tmp.ClassName;
            else
                className = BaseItemType.ClassName;
           
            itemBase = entity.GetComponent<Base>();
           
            width = BaseItemType.Width;
            
            height = BaseItemType.Height;
            
            mods = entity.GetComponent<Mods>();
            isSkillHGem = entity.HasComponent<SkillGem>();
        
            isMap = entity.HasComponent<Map>();
            isShapedMap = itemBase.Name.Contains("Shaped") && isMap;
            itemRarity = mods.ItemRarity;
            quality = 0;
            if (entity.HasComponent<Quality>()) { quality = entity.GetComponent<Quality>().ItemQuality; }
            
            text = string.Concat(quality > 0 ? "Superior " : string.Empty, basename);
            
            sockets = entity.GetComponent<Sockets>();
           
            numberOfSockets = sockets.NumberOfSockets;
            
            largestLinkSize = sockets.LargestLinkSize;
            
            socketGroup = sockets.SocketGroup;
           
            path = entity.Path;
        }
    }
}