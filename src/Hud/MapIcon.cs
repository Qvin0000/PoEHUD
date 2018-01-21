using PoeHUD.Models;
using PoeHUD.Poe.Components;
using SharpDX;
using System;
using System.Linq;
using PoeHUD.Controllers;

namespace PoeHUD.Hud
{
    public class CreatureMapIcon : MapIcon
    {
        public CreatureMapIcon(EntityWrapper entityWrapper, string hudTexture, Func<bool> show, float iconSize)
            : base(entityWrapper, new HudTexture(hudTexture), show, iconSize)
        { }
 
        public override bool IsVisible()
        {
            return base.IsVisible() && EntityWrapper.IsAlive;
        }
    }

    public class ChestMapIcon : MapIcon
    {
        public ChestMapIcon(EntityWrapper entityWrapper, HudTexture hudTexture, Func<bool> show, float iconSize)
            : base(entityWrapper, hudTexture, show, iconSize)
        { }

        public override bool IsEntityStillValid()
        {
          //  return EntityWrapper.Path[0] == 'M' && !EntityWrapper.GetComponent<Chest>().IsOpened && EntityWrapper.IsInList;
            return EntityWrapper.IsValid && !EntityWrapper.GetComponent<Chest>().IsOpened;
        }
    }

    public class MapIcon
    {
        private readonly Func<bool> show;

        public MapIcon(EntityWrapper entityWrapper, HudTexture hudTexture, Func<bool> show, float iconSize = 10f)
        {
            EntityWrapper = entityWrapper;
            TextureIcon = hudTexture;
            this.show = show;
            Size = iconSize;
        }

        public float? SizeOfLargeIcon { get; set; }
        public EntityWrapper EntityWrapper { get; }
        public HudTexture TextureIcon { get; private set; }
        public float Size { get; private set; }
        public Vector2 WorldPosition => EntityWrapper.GetComponent<Positioned>().GridPos;

        public static Vector2 DeltaInWorldToMinimapDelta(Vector2 delta, double diag, float scale, float deltaZ = 0)
        {
            const float CAMERA_ANGLE = 42 * MathUtil.Pi / 180;
            // Values according to 40 degree rotation of cartesian coordiantes, still doesn't seem right but closer
            var cos = (float)(diag * Math.Cos(CAMERA_ANGLE) / scale);
            var sin = (float)(diag * Math.Sin(CAMERA_ANGLE) / scale); // possible to use cos so angle = nearly 45 degrees
            // 2D rotation formulas not correct, but it's what appears to work?
            return new Vector2((delta.X - delta.Y) * cos, deltaZ - (delta.X + delta.Y) * sin);
        }
        
        private static string hide = "-gray";

        private static string[] hiddenTextures = new[]
        {
            "ms-red.png",
            "ms-blue.png",
            "ms-yellow.png",
            "ms-purple.png",
            "ms-red-gray.png",
            "ms-blue-gray.png",
            "ms-yellow-gray.png",
            "ms-purple-gray.png"
            
        };
        private float WaitRender;
        public void Hidden()
        {
            if (GameController.Instance.Game.MainTimer.ElapsedMilliseconds < WaitRender) return;
            WaitRender = GameController.Instance.Performance.GetWaitTime(GameController.Instance.Performance.meanLatency,75);
            if (!hiddenTextures.Any(x => x == TextureIcon.FileName)) return;
            var life = EntityWrapper.GetComponent<Life>();
            if (life.HasBuff2("hidden_monster"))
            {
                if (!TextureIcon.FileName.Contains(hide))
                {
                    TextureIcon = new HudTexture(TextureIcon.FileName.Replace(".png",$"{hide}.png"));
                    return;
                }
                
            }
            else
            TextureIcon = new HudTexture(TextureIcon.FileName.Replace(hide, String.Empty));
        }
        public virtual bool IsEntityStillValid()
        {
          // return EntityWrapper.Path[0] == 'M' && EntityWrapper.IsInList;
            return EntityWrapper.IsValid;
        }

        public virtual bool IsVisible()
        {
            return show();
        }
    }
}