using System.Diagnostics;
using PoeHUD.Framework;
using PoeHUD.Hud.Performance;

namespace PoeHUD.Poe.RemoteMemoryObjects
{
    public class TheGame : RemoteMemoryObject
    {
        public readonly Stopwatch MainTimer;
        public readonly Performance Performance;
        public TheGame(Memory m,Performance performance)
        {
            M = m;
            Address = m.ReadLong(Offsets.Base + m.AddressOfProcess, 0x8, 0xf8);//0xC40
            Game = this;
            MainTimer = Stopwatch.StartNew();
            Performance = performance;
        }
        public IngameState IngameState => Game.Performance.Cache.Enable && Game.Performance.Cache.IngameState != null
            ? Game.Performance.Cache.IngameState
            : (Game.Performance.Cache.Enable
                ? Game.Performance.Cache.IngameState =
                    IngameStateReal 
                : IngameStateReal);

        private IngameState IngameStateReal => ReadObject<IngameState>(Address + 0x38);

        public int AreaChangeCount => M.ReadInt(M.AddressOfProcess + Offsets.AreaChangeCount);
        public bool IsGameLoading => M.ReadInt(M.AddressOfProcess + Offsets.isLoadingScreenOffset) == 1;
    }
}