﻿using PoeHUD.Controllers;
using PoeHUD.Framework;
using PoeHUD.Framework.Helpers;
using PoeHUD.Hud.UI;
using PoeHUD.Models;
using PoeHUD.Poe.Components;
using SharpDX;
using SharpDX.Direct3D9;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using PoeHUD.DebugPlug;

namespace PoeHUD.Hud.Dps
{
    public class DpsMeterPlugin : SizedPlugin<DpsMeterSettings>
    {
        private readonly Dictionary<long, int> lastMonsters = new Dictionary<long, int>();
        private readonly Dictionary<long, int> Monsters = new Dictionary<long, int>();
        private double MaxDps; 
        private double CurrentDps; 
        private double CurrentDmg; 
        private double[] damageMemory;
        private int damageMemoryIndex;
        readonly Coroutine dpsCoroutine;
        int sumHp;
        public DpsMeterPlugin(GameController gameController, Graphics graphics, DpsMeterSettings settings)
            : base(gameController, graphics, settings)
        {
            Settings.ClearNode.OnPressed += Clear; 
            GameController.Area.OnAreaChange += area =>
            {
                Clear();
            };
            damageMemory = new double[10];

          
            dpsCoroutine = (new Coroutine(() => { 
                    Monsters.Clear();
                    damageMemoryIndex++;
                    if (damageMemoryIndex >= damageMemory.Length)
                    {
                        damageMemoryIndex = 0;
                    }
                    var curDmg = CalculateDps(Settings.CalcAOE); 
                    damageMemory[damageMemoryIndex] = curDmg; 
                    if (curDmg > 0) 
                    { 
                        CurrentDmg = curDmg; 
                        CurrentDps = damageMemory.Sum(); 
                        MaxDps = Math.Max(CurrentDps, MaxDps); 
                    } 
                    if(settings.ShowInformationAround)
                        sumHp = Monsters.Sum(pair => pair.Value);
                }, new WaitTime(100), nameof(DpsMeterPlugin), "Calculate DPS"){Priority = CoroutinePriority.High})
                .AutoRestart(GameController.CoroutineRunner).Run();
        }

        private void Clear()
        {
            damageMemory = new double[1000/GameController.Performance.DpsUpdateTime];
            MaxDps = 0;
            CurrentDps = 0;
            CurrentDmg = 0;
            lastMonsters.Clear();

        }

        public override void Render()
        {
            try
            {
                base.Render();
                if (!Settings.Enable ||
                    !Settings.ShowInTown && GameController.Area.CurrentArea.IsTown ||
                    !Settings.ShowInTown && GameController.Area.CurrentArea.IsHideout)
                {
                    dpsCoroutine.Pause();
                    return;
                }
                dpsCoroutine.Resume();

                Vector2 position = StartDrawPointFunc();
                string monsterAround = "";
                string monsterAroundHp = "";
                if (Settings.ShowInformationAround)
                {
                 monsterAround = Monsters.Count + " monsters" + Environment.NewLine;
                 monsterAroundHp = sumHp + " hp" + Environment.NewLine;
                }
                Size2 dpsSize = Graphics.DrawText(CurrentDmg + " dps", Settings.DpsTextSize, position, Settings.DpsFontColor, FontDrawFlags.Right); 
                Size2 peakSize = Graphics.DrawText(MaxDps + " top dps", Settings.PeakDpsTextSize, position.Translate(0, dpsSize.Height), Settings.PeakFontColor, FontDrawFlags.Right);
                int height = dpsSize.Height + peakSize.Height;
                int width = Math.Max(1, dpsSize.Width);
                if (Settings.ShowInformationAround)
                {
                    Size2 monsterAroundSize = Graphics.DrawText(monsterAround, Settings.DpsTextSize,
                        position.Translate(0, peakSize.Height+dpsSize.Height), Settings.PeakFontColor, FontDrawFlags.Right);
                    Size2 monsterAroundHpSize = Graphics.DrawText(monsterAroundHp, Settings.DpsTextSize,
                        position.Translate(0, monsterAroundSize.Height+peakSize.Height+dpsSize.Height), Settings.PeakFontColor, FontDrawFlags.Right);
                    height += monsterAroundSize.Height + monsterAroundHpSize.Height;
                    width = Math.Max(width, monsterAroundSize.Width);
                    width = Math.Max(width, monsterAroundHpSize.Width);
                }
               
                
             
                var bounds = new RectangleF(position.X - 5 - width - 41, position.Y - 5, width + 50, height + 10);

                Graphics.DrawImage("preload-start.png", bounds, Settings.BackgroundColor);
                Graphics.DrawImage("preload-end.png", bounds, Settings.BackgroundColor);

                Size = bounds.Size;
                Margin = new Vector2(0, 5);
            }
            catch
            {
                // do nothing
            }
        }

        private double CalculateDps(bool aoe)
        {
            int totalDamage = 0;
            foreach (EntityWrapper monster in GameController.Entities.Where(x => x.HasComponent<Monster>() && x.IsHostile))
            {
                var life = monster.GetComponent<Life>();
                int hp = monster.IsAlive ? life.CurHP + life.CurES : 0;
                if (hp > -1000000 && hp < 10000000)
                {
                    int lastHP;
                    if (lastMonsters.TryGetValue(monster.Id, out lastHP))
                    {
                        if (lastHP != hp)
                        {
                            if (aoe)
                                totalDamage += lastHP - hp;
                            else
                                totalDamage = Math.Max(totalDamage, lastHP - hp);
                        }
                    }
                    lastMonsters[monster.Id] = hp;
                    if (hp > 0)
                        Monsters[monster.Id] = hp;
                }
            }
            return totalDamage < 0 ? 0 : totalDamage;
        }
    }
    
}