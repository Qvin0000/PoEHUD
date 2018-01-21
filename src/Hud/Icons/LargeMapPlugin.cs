using PoeHUD.Controllers;
using PoeHUD.Framework.Helpers;
using PoeHUD.Hud.UI;
using PoeHUD.Poe.Components;
using PoeHUD.Poe.RemoteMemoryObjects;
using SharpDX;
using System;
using System.Collections.Generic;
using System.Linq;
using PoeHUD.Framework;
using Map = PoeHUD.Poe.Elements.Map;

namespace PoeHUD.Hud.Icons
{
    public class LargeMapPlugin : Plugin<MapIconsSettings>
    {
        private readonly Func<IEnumerable<MapIcon>> getIcons;

        public LargeMapPlugin(GameController gameController, Graphics graphics, Func<IEnumerable<MapIcon>> gatherMapIcons,
            MapIconsSettings settings)
            : base(gameController, graphics, settings)
        {
            getIcons = gatherMapIcons;
            (new Coroutine(() =>
                {
                    Camera camera = GameController.Game.IngameState.Camera;
                    mapWindow = GameController.Game.IngameState.IngameUi.Map;
                    RectangleF mapRect = mapWindow.GetClientRect();
                    diag = (float)Math.Sqrt(camera.Width * camera.Width + camera.Height * camera.Height);
                    float k = camera.Width < 1024f ? 1120f : 1024f;
                    scale = k / camera.Height * camera.Width * 3f / 4f / mapWindow.LargeMapZoom;
            
                    screenCenter = new Vector2(mapRect.Width / 2, mapRect.Height / 2).Translate(0, -20) + new Vector2(mapRect.X, mapRect.Y)
                                   + new Vector2(mapWindow.LargeMapShiftX, mapWindow.LargeMapShiftY);
                }, new WaitTime(100), nameof(LargeMapPlugin), "MapIconOptimize"){Priority = CoroutinePriority.High})
                .AutoRestart(GameController.CoroutineRunner).Run();
        }

        private Map mapWindow;
        private float scale;
           private float diag;
        private Vector2 screenCenter;
        public override void Render()
        {
            try
            {
                if (!Settings.Enable || !GameController.InGame || !Settings.IconsOnLargeMap
                    || !GameController.Game.IngameState.IngameUi.Map.LargeMap.IsVisible)
                {
                    return;
                }


                if (GameController.Game.IngameState.IngameUi.AtlasPanel.IsVisible)
                    return;
                if (GameController.Game.IngameState.IngameUi.TreePanel.IsVisible)
                    return;

                Vector2 playerPos = GameController.Player.GetComponent<Positioned>().GridPos;
                float posZ = GameController.Player.GetComponent<Render>().Z;
                foreach (MapIcon icon in getIcons().Where(x => x.IsVisible()))
                {
                    float iconZ = icon.EntityWrapper.GetComponent<Render>().Z;
                    Vector2 point = screenCenter
                                    + MapIcon.DeltaInWorldToMinimapDelta(icon.WorldPosition - playerPos, diag, scale, (iconZ - posZ) / (9f / mapWindow.LargeMapZoom));

                    HudTexture texture = icon.TextureIcon;
                    float size = icon.Size * 2;//icon.SizeOfLargeIcon.GetValueOrDefault(icon.Size * 2);
                    texture.Draw(Graphics, new RectangleF(point.X - size / 2f, point.Y - size / 2f, size, size));
                }
            }
            catch
            {
                // do nothing
            }
        }
    }
}