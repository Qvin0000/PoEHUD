using SharpDX;

namespace PoeHUD.Poe.Components
{
    public class Positioned : Component
    {
        public int GridX => Game.Performance.ReadMemWithCache(M.ReadInt, Address + 0x20, Game.Performance.skipTicksRender);
          //  Address != 0 ? M.ReadInt(Address + 0x20) : 0;
        public int GridY => Game.Performance.ReadMemWithCache(M.ReadInt, Address + 0x24, Game.Performance.skipTicksRender);
            //Address != 0 ? M.ReadInt(Address + 0x24) : 0;
/*        public float X => Address != 0 ? M.ReadFloat(Address + 0x2c) : 0f;
        public float Y => Address != 0 ? M.ReadFloat(Address + 0x30) : 0f;*/

        public Vector2 GridPos => new Vector2(GridX, GridY);
        
    }
}