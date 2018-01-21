using PoeHUD.Controllers;
using PoeHUD.DebugPlug;
using PoeHUD.Framework;
using PoeHUD.Framework.Helpers;
using PoeHUD.Hud.AdvancedTooltip;
using PoeHUD.Hud.Dps;
using PoeHUD.Hud.Health;
using PoeHUD.Hud.Icons;
using PoeHUD.Hud.Interfaces;
using PoeHUD.Hud.KillCounter;
using PoeHUD.Hud.Loot;
using PoeHUD.Hud.Menu;
using PoeHUD.Hud.PluginExtension;
using PoeHUD.Hud.Preload;
using PoeHUD.Hud.Settings;
using PoeHUD.Hud.Trackers;
using PoeHUD.Hud.XpRate;
using PoeHUD.Models.Enums;
using PoeHUD.Poe;
using SharpDX;
using SharpDX.Windows;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Color = System.Drawing.Color;
using Graphics2D = PoeHUD.Hud.UI.Graphics;
using Rectangle = System.Drawing.Rectangle;
using PoeHUD.Hud.Dev;

namespace PoeHUD.Hud
{
    internal sealed class ExternalOverlay : RenderForm
    {
        private readonly SettingsHub settings;
        private readonly GameController gameController;
        private readonly Memory _memory;
        private readonly IntPtr gameHandle;
        private readonly List<IPlugin> plugins = new List<IPlugin>();
        private Graphics2D graphics;

        public ExternalOverlay(Memory memory)
        {
            _memory = memory;
            settings = SettingsHub.Load();
            gameController = new GameController(_memory,settings.PerformanceSettings);
#if DEBUG
            Debug();
#endif
            gameHandle = gameController.Window.Process.MainWindowHandle;
            SuspendLayout();
            Text = MathHepler.GetRandomWord(MathHepler.Randomizer.Next(7) + 5);
            TransparencyKey = Color.Transparent;
            BackColor = Color.Black;
            FormBorderStyle = FormBorderStyle.None;
            ShowIcon = false;
            TopMost = true;
            ResumeLayout(false);
            (new Coroutine(() =>
            {
                if (!_memory.IsInvalid())
                {
                    Rectangle gameSize = WinApi.GetClientRectangle(gameHandle);
                    Bounds = gameSize;
                }
            }, new WaitTime(250), nameof(ExternalOverlay), "Check Game Window Size")
            {
                Priority = CoroutinePriority.Critical
            }).AutoRestart(gameController.CoroutineRunnerParallel).RunParallel();
            (new Coroutine(() => {  if (_memory.IsInvalid())
            {
                graphics.Dispose();
                Close();
            }}, new WaitTime(500), nameof(ExternalOverlay), "Check Game State")
            {
                Priority = CoroutinePriority.Critical
            }).AutoRestart(gameController.CoroutineRunnerParallel).RunParallel();
            Load += OnLoad;
        }
       

        private IEnumerable<MapIcon> GatherMapIcons()
        {
            IEnumerable<IPluginWithMapIcons> pluginsWithIcons = plugins.OfType<IPluginWithMapIcons>();
            return pluginsWithIcons.SelectMany(iconSource => iconSource.GetIcons());
        }

        private Vector2 GetLeftCornerMap()
        {
            var ingameState = gameController.Game.IngameState;
            RectangleF clientRect = ingameState.IngameUi.Map.SmallMinimap.GetClientRect();
            var diagnosticElement = ingameState.LatencyRectangle;
            switch (ingameState.DiagnosticInfoType)
            {
                case DiagnosticInfoType.Short:
                    clientRect.X = diagnosticElement.X + 30;
                    break;

                case DiagnosticInfoType.Full:
                    clientRect.Y = diagnosticElement.Y + diagnosticElement.Height + 5;
                    var fpsRectangle = ingameState.FPSRectangle;
                    clientRect.X = fpsRectangle.X + fpsRectangle.Width + 6;
                    break;
            }
            return new Vector2(clientRect.X - 5, clientRect.Y + 5);
        }

        private Vector2 _getUnderCornerMap;
        private float nextUpdateGetUnderCornerMap;
        private Vector2 GetUnderCornerMap()
        {
            if (gameController.Game.MainTimer.ElapsedMilliseconds > nextUpdateGetUnderCornerMap)
            {
                nextUpdateGetUnderCornerMap = gameController.Game.Performance.GetWaitTime(gameController.Game.Performance.updateIngameState);
                const int EPSILON = 1;
                Element questPanel = gameController.Game.IngameState.IngameUi.QuestTracker;
                Element gemPanel = gameController.Game.IngameState.IngameUi.GemLvlUpPanel;
                RectangleF questPanelRect = questPanel.GetClientRect();
                RectangleF gemPanelRect = gemPanel.GetClientRect();
                RectangleF clientRect = gameController.Game.IngameState.IngameUi.Map.SmallMinimap.GetClientRect();
                if (gemPanel.IsVisible && Math.Abs(gemPanelRect.Right - clientRect.Right) < EPSILON)
                {
                    // gem panel is visible, add its height
                    clientRect.Height += gemPanelRect.Height;
                }
                if (questPanel.IsVisible && Math.Abs(gemPanelRect.Right - clientRect.Right) < EPSILON)
                {
                    // quest panel is visible, add its height
                    clientRect.Height += questPanelRect.Height;
                }

                 _getUnderCornerMap = new Vector2(clientRect.X + clientRect.Width, clientRect.Y + clientRect.Height + 10);
            }
            return _getUnderCornerMap;
        }

        private void OnClosing(object sender, FormClosingEventArgs e)
        {
            SettingsHub.Save(settings);
            plugins.ForEach(plugin => plugin.Dispose());
            graphics.Dispose();
        }

        private void OnDeactivate(object sender, EventArgs e)
        {
            BringToFront();
        }

        private async void OnLoad(object sender, EventArgs e)
        {
            Bounds = WinApi.GetClientRectangle(gameHandle);
            WinApi.EnableTransparent(Handle, Bounds);
            graphics = new Graphics2D(this, Bounds.Width, Bounds.Height);
            plugins.Add(new HealthBarPlugin(gameController, graphics, settings.HealthBarSettings));
            plugins.Add(new MinimapPlugin(gameController, graphics, GatherMapIcons, settings.MapIconsSettings));
            plugins.Add(new LargeMapPlugin(gameController, graphics, GatherMapIcons, settings.MapIconsSettings));
            plugins.Add(new MonsterTracker(gameController, graphics, settings.MonsterTrackerSettings));
            plugins.Add(new PoiTracker(gameController, graphics, settings.PoiTrackerSettings));

            var leftPanel = new PluginPanel(GetLeftCornerMap);
            leftPanel.AddChildren(new XpRatePlugin(gameController, graphics, settings.XpRateSettings, settings));
            leftPanel.AddChildren(new PreloadAlertPlugin(gameController, graphics, settings.PreloadAlertSettings, settings));
            leftPanel.AddChildren(new KillCounterPlugin(gameController, graphics, settings.KillCounterSettings));
            leftPanel.AddChildren(new DpsMeterPlugin(gameController, graphics, settings.DpsMeterSettings));
            leftPanel.AddChildren(new DebugPlugin(gameController, graphics, new DebugPluginSettings(), settings));

            var horizontalPanel = new PluginPanel(Direction.Left);
            leftPanel.AddChildren(horizontalPanel);
            plugins.AddRange(leftPanel.GetPlugins());

            var underPanel = new PluginPanel(GetUnderCornerMap);
            underPanel.AddChildren(new ItemAlertPlugin(gameController, graphics, settings.ItemAlertSettings, settings));
            plugins.AddRange(underPanel.GetPlugins());

            plugins.Add(new AdvancedTooltipPlugin(gameController, graphics, settings.AdvancedTooltipSettings, settings));
            plugins.Add(new DebugTree(gameController,graphics,settings.DebugTreeSettings));
            plugins.Add(new DebugInformation(gameController,graphics,settings.DebugInformationSettings));
            plugins.Add(new DebugPluginLog(gameController,graphics,settings.DebugPluginLogSettings));
            plugins.Add(new MenuPlugin(gameController, graphics, settings));
            plugins.Add(new PluginExtensionPlugin(gameController, graphics));//Should be after MenuPlugin

            Deactivate += OnDeactivate;
            FormClosing += OnClosing;
            graphics.Render += () => plugins.ForEach(x => x.Render());
            gameController.Clear += graphics.Clear;
            gameController.Render += graphics.TryRender;
            await Task.Run(() => gameController.WhileLoop());
        }

        private void Debug()
        {
                StringBuilder sb = new StringBuilder();

                sb.Append("AddressOfProcess: " + _memory.AddressOfProcess.ToString("X"));
                sb.Append(System.Environment.NewLine);

                sb.Append("GameController full: " + (_memory.offsets.Base + _memory.AddressOfProcess).ToString("X"));
                sb.Append(System.Environment.NewLine);

                sb.Append("GameController: " + (_memory.offsets.Base + _memory.AddressOfProcess).ToString("X"));
                sb.Append(System.Environment.NewLine);

                sb.Append("TheGame: " + gameController.Game.Address.ToString("X"));
                sb.Append(System.Environment.NewLine);

                sb.Append("IngameState: " + gameController.Game.IngameState.Address.ToString("X"));
                sb.Append(System.Environment.NewLine);


                sb.Append("IngameData: " + gameController.Game.IngameState.Data.Address.ToString("X"));
                sb.Append(System.Environment.NewLine);

                sb.Append("IngameUi: " + gameController.Game.IngameState.IngameUi.Address.ToString("X"));
                sb.Append(System.Environment.NewLine);

                sb.Append("UIRoot: " + gameController.Game.IngameState.UIRoot.Address.ToString("X"));
                sb.Append(System.Environment.NewLine);

                sb.Append("ServerData: " + gameController.Game.IngameState.ServerData.Address.ToString("X"));
                sb.Append(System.Environment.NewLine);


                sb.Append("GetInventoryZone: " + _memory.ReadLong(gameController.Game.IngameState.IngameUi.InventoryPanel.Address + Poe.Element.OffsetBuffers + 0x42c).ToString("X"));
                sb.Append(System.Environment.NewLine);

                sb.Append("Area Addr: " + gameController.Game.IngameState.Data.CurrentArea.Address.ToString("X"));
                sb.Append(System.Environment.NewLine);

                sb.Append("Area Name: " + gameController.Game.IngameState.Data.CurrentArea.Name);
                sb.Append(System.Environment.NewLine);


                sb.Append("Area change: " + _memory.ReadInt(_memory.offsets.AreaChangeCount + _memory.AddressOfProcess));
                sb.Append(System.Environment.NewLine);
                sb.Append(System.Environment.NewLine);

                sb.Append(_memory.DebugStr);

                File.WriteAllText("__BaseOffsets.txt", sb.ToString());
        }
       
    }
}