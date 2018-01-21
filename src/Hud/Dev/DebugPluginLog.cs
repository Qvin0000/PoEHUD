using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using ImGuiNET;
using PoeHUD.Controllers;
using PoeHUD.DebugPlug;
using PoeHUD.Hud.Interfaces;
using PoeHUD.Hud.UI;

namespace PoeHUD.Hud.Dev
{
    public class DebugPluginLog:Plugin<DebugPluginLogSettings>
    {
        private readonly GameController _gameController;
        private readonly Graphics _graphics;
        private readonly DebugPluginLogSettings _settings;

        public DebugPluginLog(GameController gameController, Graphics graphics, DebugPluginLogSettings settings) : base(gameController, graphics, settings)
        {
            _gameController = gameController;
            _graphics = graphics;
            _settings = settings;
        }
        bool settingsShowWindow;
        List<(string message,Vector4 color)> msg = new List<(string,Vector4)>();
        private bool pause = false;
        public override void Render()
        {
            
            if (_settings.ShowWindow)
            {
                settingsShowWindow = _settings.ShowWindow;
                ImGui.SetNextWindowSize(new Vector2(500,500),Condition.Appearing );
                ImGui.SetNextWindowPos(new Vector2(200,200),Condition.Appearing,new Vector2(1,0));
                ImGui.BeginWindow("DebugPlugin Log", ref settingsShowWindow, WindowFlags.Default);
                _settings.ShowWindow = settingsShowWindow;
                if ( ImGui.Button("Clear##Debuglog"))
                {
                    msg.Clear();
                }
                if ( ImGui.Button("Clear All##Debuglog"))
                {
                    DebugPlugin.QueueDebugMessages.Clear();
                }
                ImGui.SameLine();
                if (pause)
                {
                    if ( ImGui.Button("Resume##Debuglog"))
                    {
                        pause = false;
                    }
                }
                else
                {
                    if ( ImGui.Button("Pause##Debuglog"))
                    {
                        pause = true;
                    }
                }
                ImGui.SameLine();
                if (ImGui.Button($"Copy##Debuglog"))
                {
                    var strbuilder = new StringBuilder();
                    for (int i = 0; i < msg.Count; i++)
                    {
                       
                            strbuilder.Append(Encoding.UTF8.GetString(Encoding.UTF8.GetBytes(msg[i].message)) + Environment.NewLine);
                        
                    }
                    ImGuiNative.igSetClipboardText(strbuilder.ToString());
                }
                if (!pause && DebugPlugin.QueueDebugMessages.Count>0)
                {
                    var queuemsg = DebugPlugin.QueueDebugMessages.Dequeue();
                  
                 
                    msg.Add(($"{queuemsg.time.ToLongTimeString()}   {queuemsg.message}",new Vector4(queuemsg.color.R,queuemsg.color.G,queuemsg.color.B,1)));
                }
                ImGui.Separator();
                ImGui.BeginChild("ChildDebugPLuginLog", new Vector2(0,0), false, WindowFlags.HorizontalScrollbar);
                for (int i = 0; i < msg.Count; i++)
                {
                    ImGui.Text($"{msg[i].message}",msg[i].color);
                }
                ImGui.EndChild();
                ImGui.EndWindow();
            }
        }
    }
}