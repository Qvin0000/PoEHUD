using PoeHUD.Models.Enums;
using System.Collections.Generic;


namespace PoeHUD.Poe.Components
{
    public class ObjectMagicProperties : Component
    {
        private MonsterRarity _rarity = MonsterRarity.White;
        private bool setted = false;
        public MonsterRarity Rarity
        {
            get
            {
                if (!setted && Address != 0)
                {
                    _rarity = (MonsterRarity)M.ReadInt(Address + 0x7C);
                    if ((int) _rarity >= 0 && (int) _rarity <= 10)
                    {
                        setted = true;
                    }
                }
                return _rarity;
            }
        }

        public List<string> Mods
        {
            get
            {
                if (Address == 0)
                {
                    return new List<string>();
                }
                long begin = M.ReadLong(Address + 0x98);
                long end = M.ReadLong(Address + 0xA0);
                var list = new List<string>();
                if (begin == 0 || end == 0)
                {
                    return list;
                }
                for (long i = begin; i < end; i += 0x28)
                {
                    string mod = M.ReadStringU(M.ReadLong(i + 0x20, 0));
                    list.Add(mod);
                }
                return list;
            }
        }
    }
}