namespace PoeHUD.Poe.Components
{
    public class WorldItem : Component
    {
        private Entity _itemEntity;
        public Entity ItemEntity
        {
            get
            {
                if(_itemEntity==null)
                    _itemEntity = Address != 0 ? ReadObject<Entity>(Address + 0x28) : GetObject<Entity>(0);
                return _itemEntity;
            }
        }
    }
}