using PoeHUD.Models;
using PoeHUD.Poe.Components;
using System;
using System.Linq.Expressions;
using System.Numerics;
using PoeHUD.Controllers;
using PoeHUD.Hud.Dev;
using Vector2 = SharpDX.Vector2;
using Vector3 = SharpDX.Vector3;
using Vector4 = System.Numerics.Vector4;

namespace PoeHUD.Poe.RemoteMemoryObjects
{
    public class Camera : RemoteMemoryObject
    {
   
        public int Width => Game.Performance.ReadMemWithCache(M.ReadInt, Address + 0x4, Game.Performance.skipTicksRender);


        public int Height => Game.Performance.ReadMemWithCache(M.ReadInt, Address + 0x8, Game.Performance.skipTicksRender);


        public float ZFar => M.ReadFloat(Address + 0x204);
        public Vector3 Position => new Vector3(M.ReadFloat(Address + 0x15C), M.ReadFloat(Address + 0x160), M.ReadFloat(Address + 0x164));

        //cameraarray 0x17c
        private byte[] _readBytes;
        private float _lastTimeUpdateMatrix;
        byte[] GetMatrix()
        {
            if (Game.Performance.timer > _lastTimeUpdateMatrix)
            {
                _lastTimeUpdateMatrix =  Game.Performance.GetWaitTime(Game.Performance.skipTicksRender*0.66f);
                _readBytes = M.ReadBytes(Address + 0xE4, 0x40);
            }

            return _readBytes;
        }
        
        private static Vector2 oldplayerCord;
        public unsafe Vector2 WorldToScreen(Vector3 vec3, EntityWrapper entityWrapper)
        {
            Entity localPlayer = Game.IngameState.Data.LocalPlayer;
            var isplayer = localPlayer.Address == entityWrapper.Address;// && localPlayer.IsValid;
            bool isMoving = false;
            if (isplayer)
            {
                isMoving = entityWrapper.GetComponent<Actor>().isMoving;
            }
            var playerMoving = isplayer && isMoving;
            float x, y;
           
            fixed (byte* numRef = GetMatrix())
            {
                Matrix4x4 matrix = *(Matrix4x4*)numRef;
                Vector4 cord = *(Vector4*)&vec3;
                cord.W = 1;
                cord = Vector4.Transform(cord, matrix);
                cord = Vector4.Divide(cord, cord.W);
                x = (cord.X + 1.0f) * 0.5f * Width;
                y = (1.0f - cord.Y) * 0.5f * Height;
            }
            var resultCord = new Vector2(x, y);
            if (playerMoving)
            {
                if (Math.Abs(oldplayerCord.X - resultCord.X) < 40 || (Math.Abs(oldplayerCord.X - resultCord.Y) < 40))
                    resultCord = oldplayerCord;
                else
                    oldplayerCord = resultCord;
            }
            else if (isplayer)
            {
                oldplayerCord = resultCord;
            }
            return resultCord;
        }
    }
}