using System.Security.Cryptography.X509Certificates;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace PoeHUD.Poe.Components
{
    public class Pathfinding : Component
    {
        public int TargetGridX => Address != 0 ? M.ReadInt(Address + 0x28) : 0;
        public int TargetGridY => Address != 0 ? M.ReadInt(Address + 0x2C) : 0;

        public int X1 => Address != 0 ? M.ReadInt(Address + 0x30) : 0;
        public int Y1 => Address != 0 ? M.ReadInt(Address + 0x34) : 0;






        public bool Moving => Address != 0 && M.ReadByte(Address + 0x4AC) == 1;

        public int XTarget => Address != 0 ? M.ReadInt(Address + 0x4B0) : 0;
        public int YTarget => Address != 0 ? M.ReadInt(Address + 0x4B4) : 0;
        public int PathPointCollision => Address != 0 ? M.ReadInt(Address + 0x4C0) : 0;
        public bool ClickOnWhat => Address != 0 && M.ReadInt(Address + 0x474) == 1;
        public TypeClick TypeClickOn => (TypeClick)(Address != 0 ? M.ReadInt(Address + 0x474) : 0);
        public float TimeWithoutMove => Address != 0 ? M.ReadInt(Address + 0x4C4) : 0;

        public enum TypeClick : int
        {

        };
    }
}
