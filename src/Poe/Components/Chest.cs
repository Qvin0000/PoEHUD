using PoeHUD.Controllers;

namespace PoeHUD.Poe.Components
{
    public class Chest : Component
    {
        private bool _isOpened;

        public bool IsOpened =>
            Game.Performance.ReadMemWithCache(M.ReadByte, Address + 0x40, Game.Performance.meanLatency, 100) == 1;

        private bool? _isStrongbox;
        public bool IsStrongbox
        {
            get
            {
                if (_isStrongbox == null)
                {
                    _isStrongbox = Address != 0 && M.ReadInt(Address + 0x60) != 0;
                }
                return (bool) _isStrongbox;
            }
        }
    }
}