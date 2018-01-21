using PoeHUD.Controllers;
using PoeHUD.Models;

namespace PoeHUD.Poe.RemoteMemoryObjects
{
    public class IngameData : RemoteMemoryObject
    {
        public AreaTemplate CurrentArea => ReadObject<AreaTemplate>(Address + 0x28);
        public int CurrentAreaLevel => (int)M.ReadByte(Address + 0x40);
        public int CurrentAreaHash => M.ReadInt(Address + 0x60);
        public Entity LocalPlayer => Game.Performance.Cache.Enable && Game.Performance.Cache.LocalPlayer != null
            ? Game.Performance.Cache.LocalPlayer 
            : Game.Performance.Cache.Enable? Game.Performance.Cache.LocalPlayer=LocalPlayerReal: LocalPlayerReal;

        private Entity LocalPlayerReal => ReadObject<Entity>(Address + 0x1A8);
        public EntityList EntityList => GetObject<EntityList>(Address + 0x258);
    }
}