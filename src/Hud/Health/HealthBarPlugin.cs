using Newtonsoft.Json;
using PoeHUD.Controllers;
using PoeHUD.Framework;
using PoeHUD.Framework.Helpers;
using PoeHUD.Models;
using PoeHUD.Poe.Components;
using PoeHUD.Poe.RemoteMemoryObjects;
using SharpDX;
using SharpDX.Direct3D9;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using ImGuiNET;
using PoeHUD.DebugPlug;
using Color = SharpDX.Color;
using Graphics = PoeHUD.Hud.UI.Graphics;
using RectangleF = SharpDX.RectangleF;

namespace PoeHUD.Hud.Health
{
    public class HealthBarPlugin : Plugin<HealthBarSettings>
    {
        private readonly Dictionary<CreatureType, List<HealthBar>> healthBars;
        private readonly DebuffPanelConfig debuffPanelConfig;

        public HealthBarPlugin(GameController gameController, Graphics graphics, HealthBarSettings settings)
            : base(gameController, graphics, settings)
        {
            CreatureType[] types = Enum.GetValues(typeof(CreatureType)).Cast<CreatureType>().ToArray();
            healthBars = new Dictionary<CreatureType, List<HealthBar>>(types.Length);
            foreach (CreatureType type in types)
            {
                healthBars.Add(type, new List<HealthBar>());
            }

            string json = File.ReadAllText("config/debuffPanel.json");
            debuffPanelConfig = JsonConvert.DeserializeObject<DebuffPanelConfig>(json);
            (new Coroutine(() =>
            { _spriteHp++;
             _spriteMp++;
             _spriteEs++;
                if (_spriteHp >= _spriteCount)
                    _spriteHp = 0;
                if (_spriteMp >= _spriteCount)
                    _spriteMp = 0;
                if (_spriteEs >= _spriteCount)
                    _spriteEs = 0;
                
            }, new WaitTime(40), nameof(HealthBar), "spriteHp")).AutoRestart(GameController.CoroutineRunner).Run();
            var ui = gameController.Game.IngameState.IngameUi;
            
            (new Coroutine(() => {
                    foreach (var healthBar in healthBars)
                {
                    healthBar.Value.RemoveAll(hp => !hp.Entity.IsValid);
                }
                    
                    leftOpen = ui.OpenRightPanel.IsVisible;
                    rightOpen = ui.OpenLeftPanel.IsVisible;
                 

                }, new WaitRender(10), nameof(HealthBarPlugin), "RemoveAll"))
                .AutoRestart(GameController.CoroutineRunner).Run();
            spritePlayerV = 1f / _spriteCount;
            spriteMonsterV = 1f / _monsterSpriteCount;
        }
       
        private bool leftOpen = false;
        private bool rightOpen = false;
        private int _spriteHp = 0;
        private int _spriteEs = 24;
        private int _spriteMp = 0;
        private float _spriteCount = 60f;
        private float _monsterSpriteCount = 35f;
        private float spritePlayerV = 0;
        private float spriteMonsterV = 0;
        List<PlayerBarRenderData> _playersBarRenderData = new List<PlayerBarRenderData>();
        public override void Render()
        {
            try
            {
                if (!Settings.Enable || WinApi.IsKeyDown(Keys.F10) || !GameController.InGame ||
                !Settings.ShowInTown && GameController.Area.CurrentArea.IsTown ||
                !Settings.ShowInTown && GameController.Area.CurrentArea.IsHideout)
                { return; }

                if (leftOpen&&rightOpen) return;
                RectangleF windowRectangle = GameController.Window.GetWindowRectangle();
                var windowSize = new Size2F(windowRectangle.Width / 2560, windowRectangle.Height / 1600);

                Camera camera = GameController.Game.IngameState.Camera;
                Func<HealthBar, bool> showHealthBar = x => x.IsShow(Settings.ShowEnemies);
                //Not Parallel better for performance
                //Parallel.ForEach(healthBars, x => x.Value.RemoveAll(hp => !hp.Entity.IsValid));
                foreach (HealthBar healthBar in healthBars.SelectMany(x => x.Value).Where(hp => showHealthBar(hp) && hp.Entity.IsAlive))
                {
                    Vector3 worldCoords = healthBar.Entity.Pos;
                    Vector2 mobScreenCoords;
                    if (healthBar.Type == CreatureType.Player)
                        mobScreenCoords = camera.WorldToScreen(worldCoords.Translate(0, 0, -170), healthBar.Entity);
                    else
                        mobScreenCoords = camera.WorldToScreen(worldCoords.Translate(0, 0, Settings.ZEnemy), healthBar.Entity);
                    if (mobScreenCoords != new Vector2())
                    {
                        float scaledWidth = healthBar.Settings.Width * windowSize.Width;
                        float scaledHeight = healthBar.Settings.Height * windowSize.Height;
                        Color color = healthBar.Settings.Color;
                        float hpPercent = healthBar.Life.HPPercentage;
                        float esPercent = healthBar.Life.ESPercentage;
                        float hpWidth = hpPercent * scaledWidth;
                        float esWidth = esPercent * scaledWidth;
                        var bg = new RectangleF(mobScreenCoords.X - scaledWidth / 2, mobScreenCoords.Y - scaledHeight / 2, scaledWidth, scaledHeight);
                        if (healthBar.Type == CreatureType.Player && Settings.NewStyle)
                        {
                            bg.X += Settings.X;
                            bg.Y += Settings.Y;
                        }
                        var windowRect = GameController.Window.GetWindowRectangle();
                        var fixNotFullscreen = new RectangleF(windowRect.X + bg.X, windowRect.Y + bg.Y, bg.Width, bg.Height);
                        if (!windowRect.Intersects(fixNotFullscreen))
                            continue;
                        if (hpPercent <= 0.1f)
                        {
                            color = healthBar.Settings.Under10Percent;
                        }
                        bg.Y = DrawFlatLifeAmount(healthBar.Life, hpPercent, healthBar.Settings, bg);
                        var yPosition = DrawFlatESAmount(healthBar, bg);
                        yPosition = DrawDebuffPanel(new Vector2(bg.Left, yPosition), healthBar, healthBar.Life);
                        ShowDps(healthBar, new Vector2(bg.Center.X, yPosition));
                        if (healthBar.Type == CreatureType.Player && Settings.NewStyle)
                        {
                            var playerBarRenderData = new PlayerBarRenderData();
                            var info = healthBar.Life;
                            var unreserved = (info.MaxMana - info.ReservedFlatMana-
                                             (info.MaxMana * info.ReservedPercentMana * 0.01f));
                            var manaPercent = (info.CurMana) /(unreserved);
                            playerBarRenderData.bgPlayeRectangleF = new RectangleF(bg.X, bg.Y, 2.5f*bg.Width, bg.Height);
                            playerBarRenderData.hpPlayer = new RectangleF(bg.X, bg.Y, 2.5f*bg.Width * hpPercent, bg.Height);
                            playerBarRenderData.hpPlayerSprite = new RectangleF(0, _spriteHp/_spriteCount, 1f*hpPercent, 1/_spriteCount);
                            if (Settings.ShowES)
                            {
                                if (esPercent > 1) esPercent = 1;
                                playerBarRenderData.esPLayer = new RectangleF(bg.X,bg.Y + (bg.Height) / 2f, 2.5f * bg.Width * esPercent,bg.Height / 2f);
                                playerBarRenderData.esPlayerSprite = new RectangleF(0, _spriteEs / _spriteCount, 1f * esPercent, spritePlayerV);
                            }
                            if (Settings.ShowMana)
                            {
                                playerBarRenderData.manaPLayer = new RectangleF(bg.X, bg.Y + bg.Height, 2.5f * bg.Width * manaPercent,
                                    bg.Height / 2f);
                                playerBarRenderData.manaPlayerSprite = new RectangleF(0, _spriteMp / _spriteCount, 1f * manaPercent,
                                    spritePlayerV);
                            }
                            float hpPlusEsPercent = hpPercent;
                            if (Settings.ShowES)
                            {
                                hpPlusEsPercent = (info.CurES + info.CurHP) /
                                      (info.MaxES + (info.MaxHP - info.ReservedFlatHP -
                                                     (info.MaxHP * info.ReservedPercentHP * 0.01f)));
                            }
                            DrawPercents(healthBar.Settings,hpPlusEsPercent , new RectangleF(bg.X-(1.4f*bg.Width),bg.Y+6,bg.Width,bg.Height));
                            _playersBarRenderData.Add(playerBarRenderData);
                            continue;
                        }
                        DrawPercents(healthBar.Settings, hpPercent, bg);
                        Graphics.DrawImage("circlebg.png",bg,Color.Black);
                        var hpMonsterSpriteIndex = (float)Math.Round(34f*hpPercent)/35f;
                        
                        var folder = "newBar/";
                        
                        switch (healthBar.Type)
                        {
                            case CreatureType.Normal:
                                color = Color.Red;
                                Graphics.DrawImage("hpMonsterSprite.png",bg,new RectangleF(0,hpMonsterSpriteIndex,1f,spriteMonsterV),color);
                                break;
                            case CreatureType.Magic:
                                color = Color.LightBlue;
                                Graphics.DrawImage("hpMonsterSpriteMagic.png",bg,new RectangleF(0,hpMonsterSpriteIndex,1f,spriteMonsterV),color);
                                break;
                            case CreatureType.Rare:
                                color = Color.Yellow;
                                Graphics.DrawImage("hpMonsterSpriteRare.png",bg,new RectangleF(0,hpMonsterSpriteIndex,1f,spriteMonsterV),color);
                                break;
                           case CreatureType.Unique:
                               color = Color.White;
                               Graphics.DrawImage("hpMonsterSpriteUnique.png",bg,new RectangleF(0,hpMonsterSpriteIndex,1f,spriteMonsterV),color);
                               break;
                        }
                        if (esPercent > 0.2f)
                        {
                            hpMonsterSpriteIndex = (float)Math.Round(34f*esPercent)/35f;
                            color = Color.Aqua;
                            Graphics.DrawImage("esMonster.png",bg,new RectangleF(0,hpMonsterSpriteIndex,1f,spriteMonsterV));
                        }
                      /*  var hpIndex = Math.Round( hpPercent * 100/ 2.94f).ToString("0000");
                        var esIndex = Math.Round( esPercent * 100 / 2.94f).ToString("0000");
                        Graphics.DrawImage(folder+"circle"+hpIndex+".png",bg,color);
                        Graphics.DrawImage("newBarEs/"+"circle"+esIndex +".png",bg,Color.Aqua);*/
                        //DrawBackground(color, healthBar.Settings.Outline, bg, hpWidth, esWidth);
                    }
                }

                if (Settings.NewStyle && !leftOpen)                                    
                {
                    foreach (var playerBarRenderData in _playersBarRenderData)
                    {
                        Graphics.DrawImage($"bgQ.png",
                            playerBarRenderData.bgPlayeRectangleF, Color.Black);
                        Graphics.DrawImage($"hpQ.png",
                            playerBarRenderData.hpPlayer,
                            playerBarRenderData.hpPlayerSprite, Color.Red);
                        if (Settings.ShowES)
                        {
                            Graphics.DrawImage($"esQ.png",
                                playerBarRenderData.esPLayer,
                                playerBarRenderData.esPlayerSprite,
                                Color.White);
                        }
                        if (Settings.ShowMana)
                        {
                            Graphics.DrawImage($"manaQ.png",
                                playerBarRenderData.manaPLayer,
                                playerBarRenderData.manaPlayerSprite, Color.Aqua);
                        }
                    }
                    _playersBarRenderData.Clear();
                } 
                
            }
            catch
            {
                // do nothing
            }
        }

        private float asds = 0;
        private void ShowDps(HealthBar healthBar, Vector2 point)
        {
            if (!healthBar.Settings.ShowFloatingCombatDamage)
            {
                return;
            }
            const int MARGIN_TOP = 2;
            const int LAST_DAMAGE_ADD_SIZE = 7;
            var fontSize = healthBar.Settings.FloatingCombatTextSize + LAST_DAMAGE_ADD_SIZE;
            var textHeight = Graphics.MeasureText("100500", fontSize).Height;

            healthBar.DpsRefresh();

            point = point.Translate(0, -textHeight - MARGIN_TOP);
            int i = 0;
            foreach (var dps in healthBar.DpsQueue)
            {
                i++;
                var damageColor = healthBar.Settings.FloatingCombatDamageColor;
                var sign = string.Empty;
                if (dps > 0)
                {
                    damageColor = healthBar.Settings.FloatingCombatHealColor;
                    sign = "+";
                }

                string dpsText = $"{sign}{dps}";
                Graphics.DrawText(dpsText, fontSize, point, Color.Black, FontDrawFlags.Center);
                point = point.Translate(0, -Graphics.DrawText(dpsText, fontSize,
                    point.Translate(1, 0), damageColor, FontDrawFlags.Center).Height - MARGIN_TOP);
                if (i == 1)
                {
                    fontSize -= LAST_DAMAGE_ADD_SIZE;
                }
            }
            healthBar.DpsDequeue();
        }

        private float DrawDebuffPanel(Vector2 startPoint, HealthBar healthBar, Life life)
        {
            var startY = startPoint.Y;
            if (!Settings.ShowDebuffPanel)
            {
                return startY;
            }
            var buffs = life.Buffs;
            if (buffs.Count > 0)
            {
                var isHostile = healthBar.Entity.IsHostile;
                int debuffTable = 0;
                foreach (var buff in buffs)
                {
                    var buffName = buff.Name;
                    if (HasDebuff(debuffPanelConfig.Bleeding, buffName, isHostile) ||
                    HasDebuff(debuffPanelConfig.Corruption, buffName, isHostile))
                        debuffTable |= 1;
                    else if (HasDebuff(debuffPanelConfig.Poisoned, buffName, isHostile))
                        debuffTable |= 2;
                    else if (HasDebuff(debuffPanelConfig.Chilled, buffName, isHostile) ||
                             HasDebuff(debuffPanelConfig.Frozen, buffName, isHostile))
                        debuffTable |= 4;
                    else if (HasDebuff(debuffPanelConfig.Burning, buffName, isHostile))
                        debuffTable |= 8;
                    else if (HasDebuff(debuffPanelConfig.Shocked, buffName, isHostile))
                        debuffTable |= 16;
                    else if (HasDebuff(debuffPanelConfig.WeakenedSlowed, buffName, isHostile))
                        debuffTable |= 32;
                }
                if (debuffTable > 0)
                {
                    startY -= Settings.DebuffPanelIconSize + 2;
                    var startX = startPoint.X;
                    DrawAllDebuff(debuffTable, startX, startY);
                }
            }
            return startY;
        }

        private void DrawAllDebuff(int debuffTable, float startX, float startY)
        {
            startX += DrawDebuff(() => (debuffTable & 1) == 1, startX, startY, 0, 4);
            startX += DrawDebuff(() => (debuffTable & 2) == 2, startX, startY, 1, 4);
            startX += DrawDebuff(() => (debuffTable & 4) == 4, startX, startY, 2);
            startX += DrawDebuff(() => (debuffTable & 8) == 8, startX, startY, 3, 4.5f);
            startX += DrawDebuff(() => (debuffTable & 16) == 16, startX, startY, 4, 5);
            DrawDebuff(() => (debuffTable & 32) == 32, startX, startY, 5);
        }

        private bool HasDebuff(Dictionary<string, int> dictionary, string buffName, bool isHostile)
        {
            int filterId;
            if (dictionary.TryGetValue(buffName, out filterId))
            {
                return filterId == 0 || isHostile == (filterId == 1);
            }
            return false;
        }

        private float DrawDebuff(Func<bool> predicate, float startX, float startY, int index, float marginFix = 0f)
        {
            if (predicate())
            {
                var size = Settings.DebuffPanelIconSize;
                const float ICON_COUNT = 6;
                float oneIconWidth = 1.0f / ICON_COUNT;
                if (marginFix > 0)
                    marginFix = oneIconWidth / marginFix;
                Graphics.DrawImage("debuff_panel.png", new RectangleF(startX, startY, size, size),
                    new RectangleF(index / ICON_COUNT + marginFix, 0, oneIconWidth - marginFix, 1f), Color.White);
                return size - 1.2f * size * marginFix * ICON_COUNT;
            }
            return 0;
        }

        protected override void OnEntityAdded(EntityWrapper entity)
        {
            var healthbarSettings = new HealthBar(entity, Settings);
            if (healthbarSettings.IsValid)
            {
                healthBars[healthbarSettings.Type].Add(healthbarSettings);
            }
        }

        private void DrawBackground(Color color, Color outline, RectangleF bg, float hpWidth, float esWidth)
        {
            if (outline != Color.Black)
            {
                Graphics.DrawFrame(bg, 2, outline);
            }
            string healthBar = Settings.ShowIncrements ? "healthbar_increment.png" : "healthbar.png";
            Graphics.DrawImage("healthbar_bg.png", bg, color);
            var hpRectangle = new RectangleF(bg.X, bg.Y, hpWidth, bg.Height);
            Graphics.DrawImage(healthBar, hpRectangle, color, hpWidth * 10 / bg.Width);
            if (Settings.ShowES)
            {
                bg.Width = esWidth;
                Graphics.DrawImage("esbar.png", bg);
            }
        }

        private float DrawFlatLifeAmount(Life life, float hpPercent,
            UnitSettings settings, RectangleF bg)
        {
            if (!settings.ShowHealthText)
            {
                return bg.Y;
            }

            string curHp = ConvertHelper.ToShorten(life.CurHP);
            string maxHp = ConvertHelper.ToShorten(life.MaxHP);
            string text = $"{curHp}/{maxHp}";
            Color color = hpPercent <= 0.1f ? settings.HealthTextColorUnder10Percent : settings.HealthTextColor;
            var position = new Vector2(bg.X + bg.Width / 2, bg.Y);
            Size2 size = Graphics.DrawText(text, settings.TextSize, position, color, FontDrawFlags.Center);
            return (int)bg.Y + (size.Height - bg.Height) / 2;
        }

        private float DrawFlatESAmount(HealthBar healthBar, RectangleF bg)
        {
            if (!healthBar.Settings.ShowHealthText || healthBar.Life.MaxES == 0)
            {
                return bg.Y;
            }

            string curES = ConvertHelper.ToShorten(healthBar.Life.CurES);
            string maxES = ConvertHelper.ToShorten(healthBar.Life.MaxES);
            string text = $"{curES}/{maxES}";
            Color color = healthBar.Settings.HealthTextColor;
            var position = new Vector2(bg.X + bg.Width / 2, bg.Y - 12);
            Size2 size = Graphics.DrawText(text, healthBar.Settings.TextSize, position, color, FontDrawFlags.Center);
            return (int)bg.Y + (size.Height - bg.Height) / 2 - 10;
        }

        private void DrawPercents(UnitSettings settings, float hpPercent, RectangleF bg)
        {
            if (settings.ShowPercents)
            {
                string text = Convert.ToString((int)(hpPercent * 100));
                var position = new Vector2(bg.X + bg.Width + 4, bg.Y);
                Graphics.DrawText(text, settings.TextSize, position, settings.PercentTextColor);
            }
        }

        class PlayerBarRenderData
        {
            public RectangleF bgPlayeRectangleF { get; set; }
            public RectangleF hpPlayer { get; set; }                                  
            public RectangleF hpPlayerSprite { get; set; }                              
            public RectangleF esPLayer { get; set; }                                  
            public RectangleF esPlayerSprite { get; set; }                                 
            public RectangleF manaPLayer { get; set; }                                
            public RectangleF manaPlayerSprite { get; set; }  
        }
    }
}
