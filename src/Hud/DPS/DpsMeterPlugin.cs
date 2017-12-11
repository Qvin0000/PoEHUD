using PoeHUD.Controllers;
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
        private double[] damageMemory;
        private int damageMemoryIndex;
        private int maxDps;
        readonly Coroutine dpsCoroutine;
        int dps;
        int sumHp;
        public DpsMeterPlugin(GameController gameController, Graphics graphics, DpsMeterSettings settings)
            : base(gameController, graphics, settings)
        {
            GameController.Area.OnAreaChange += area =>
            {
                maxDps = 0;
                damageMemory = new double[1000/GameController.Performance.DpsUpdateTime];
                lastMonsters.Clear();
            };
            damageMemory = new double[1000/GameController.Performance.DpsUpdateTime];

          
            dpsCoroutine = (new Coroutine(() => { 
                    Monsters.Clear();
                    damageMemoryIndex++;
                    if (damageMemoryIndex >= damageMemory.Length)
                    {
                        damageMemoryIndex = 0;
                    }
                    damageMemory[damageMemoryIndex] = CalculateDps(); 
                    double sum = 0;
                    foreach (var d in damageMemory)
                        sum += d;
                    dps = (int)sum;
                    maxDps = Math.Max(dps, maxDps);
                    if(settings.ShowInformationAround)
                        sumHp = Monsters.Sum(pair => pair.Value);
                }, new WaitTime(GameController.Performance.DpsUpdateTime), nameof(DpsMeterPlugin), "Calculate DPS"){Priority = CoroutinePriority.High})
                .AutoRestart(GameController.CoroutineRunner).Run();
        }
        public override void Render()
        {
            try
            {
                base.Render();
                if (!Settings.Enable || WinApi.IsKeyDown(Keys.F10) ||
                    !Settings.ShowInTown && GameController.Area.CurrentArea.IsTown ||
                    !Settings.ShowInTown && GameController.Area.CurrentArea.IsHideout)
                {
                    dpsCoroutine.Pause();
                    return;
                }
                dpsCoroutine.Resume();

                Vector2 position = StartDrawPointFunc();
                string dpsText = dps + " dps" + Environment.NewLine;
                string peakText = maxDps + " top dps " + Environment.NewLine;
                string monsterAround = "";
                string monsterAroundHp = "";
                if (Settings.ShowInformationAround)
                {
                 monsterAround = Monsters.Count + " monsters" + Environment.NewLine;
                 monsterAroundHp = sumHp + " hp" + Environment.NewLine;
                }
                Size2 dpsSize = Graphics.DrawText(dpsText, Settings.DpsTextSize, position, Settings.DpsFontColor, FontDrawFlags.Right);
                Size2 peakSize = Graphics.DrawText(peakText, Settings.PeakDpsTextSize, position.Translate(0, dpsSize.Height), Settings.PeakFontColor,FontDrawFlags.Right);
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

        private double CalculateDps()
        {
                int totalDamage = 0;
            foreach (EntityWrapper monster in GameController.Entities.ToArray())
            {
                if(monster ==null) continue;
                if (monster.HasComponent<Monster>() && monster.IsHostile)
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
                                totalDamage += lastHP - hp;
                            }
                        }
                        lastMonsters[monster.Id] = hp;
                        if(hp>0)
                        Monsters[monster.Id] = hp;
                    }
                }
            }

            return totalDamage < 0 ? 0 : totalDamage;
        }
    }
}