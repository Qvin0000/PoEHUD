namespace PoeHUD.Poe.RemoteMemoryObjects
{
    public class DiagnosticElement : RemoteMemoryObject
    {
        public long DiagnosticArray =>
            Game.Performance.ReadMemWithCache(M.ReadLong, Address + 0x0, Game.Performance.skipTicksRender, 50);
        public float CurrValue => M.ReadFloat(DiagnosticArray + 0x13C);
        public int X => Game.Performance.ReadMemWithCache(M.ReadInt, Address + 0x8, Game.Performance.skipTicksRender, 100);
        public int Y => Game.Performance.ReadMemWithCache(M.ReadInt, Address + 0xC, Game.Performance.skipTicksRender, 100);
        public int Width => Game.Performance.ReadMemWithCache(M.ReadInt, Address + 0x10, Game.Performance.skipTicksRender, 100);
        public int Height => Game.Performance.ReadMemWithCache(M.ReadInt, Address + 0x14, Game.Performance.skipTicksRender, 100);
    }
}