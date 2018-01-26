using PoeHUD.Poe.RemoteMemoryObjects;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PoeHUD.Poe.Components
{
    //TODO SOME CACHE RESERVED
    public class Life : Component
    {
        private float _maxhptimer;
        public int MaxHP => Address!=0 ?  Game.Performance.ReadMemWithCache(M.ReadInt, Address + 0x50, Game.Performance.meanLatency,100) :1;
        public int CurHP => Address!=0 ? Game.Performance.ReadMemWithCache(M.ReadInt, Address + 0x54, Game.Performance.meanLatency, 25) : 1;
        public int ReservedFlatHP =>Address!=0 ?
            Game.Performance.ReadMemWithCache(M.ReadInt, Address + 0x5C, Game.Performance.meanLatency,200) :0;
        public int ReservedPercentHP =>Address!=0 ?
            Game.Performance.ReadMemWithCache(M.ReadInt, Address + 0x60, Game.Performance.meanLatency,200) :0;
        public int MaxMana =>Address!=0 ? Game.Performance.ReadMemWithCache(M.ReadInt, Address + 0x88, Game.Performance.meanLatency,100):1;
        public int CurMana => Address!=0 ?Game.Performance.ReadMemWithCache(M.ReadInt, Address + 0x8C, Game.Performance.meanLatency,25):1;
        public int ReservedFlatMana =>Address!=0 ?
            Game.Performance.ReadMemWithCache(M.ReadInt, Address + 0x94, Game.Performance.meanLatency,200):0;
        public int ReservedPercentMana =>Address!=0 ?
            Game.Performance.ReadMemWithCache(M.ReadInt, Address + 0x98, Game.Performance.meanLatency,200):0;
        public int MaxES => Address!=0 ?Game.Performance.ReadMemWithCache(M.ReadInt, Address + 0xB8, Game.Performance.meanLatency,100):0;
        public int CurES => Address!=0 ?Game.Performance.ReadMemWithCache(M.ReadInt, Address + 0xBC, Game.Performance.meanLatency,25):0;
        public float HPPercentage => CurHP / (float)(MaxHP - ReservedFlatHP - Math.Round(ReservedPercentHP * 0.01 * MaxHP));
        public float MPPercentage => CurMana / (float)(MaxMana - ReservedFlatMana - Math.Round(ReservedPercentMana * 0.01 * MaxMana));
        public float ESPercentage
        {
            get
            {
                if (MaxES != 0)
                {
                    return CurES / (float)MaxES;
                }
                return 0f;
            }
        }
        private long BuffStart => M.ReadLong(Address + 0xE8);
        private long BuffEnd => M.ReadLong(Address + 0xF0);
        //public bool CorpseUsable => M.ReadBytes(Address + 0x238, 1)[0] == 1; // Total guess, didn't verify

        private long MaxBuffCount => 512; // Randomly bumping to 512 from 32 buffs... no idea what real value is.
        public List<Buff> Buffs
        {
            get
            {
                var list = new List<Buff>();
                long start = BuffStart;
                long end = BuffEnd;
                long length = BuffEnd - BuffStart;
                if (length <= 0 || length >= MaxBuffCount * 8) // * 8 as we buff pointer takes 8 bytes.
                    return list;
                byte[] buffPointers = M.ReadBytes(start, (int)length);
                Buff tmp;
                for (int i = 0; i < length; i += 8)
                {
                    tmp = ReadObject<Buff>(BitConverter.ToInt64(buffPointers, i) + 0x08);
                    list.Add(tmp);
                }
                return list;
            }
        }

        public bool HasBuff(string buff)
        {
            long start = BuffStart;
            long end = BuffEnd;
            long length = end - start;
            if (length <= 0 || length >= MaxBuffCount * 8)
                return false;
            byte[] buffPointers = M.ReadBytes(start, (int)length);
            Buff tmp;
            for (int i = 0; i < length; i+=8)
            {
                tmp = ReadObject<Buff>(BitConverter.ToInt64(buffPointers, i) + 0x08);
                if (tmp.Name == buff)
                    return true;

            }
            return false;
        }
        
        Dictionary<long,Buff> cacheBuffs = new Dictionary<long, Buff>();
        public List<Buff> Buffs2
        {
            get
            {
                var temp = new Dictionary<long, Buff>();
                long startBuff = Game.Performance.ReadMemWithCache(M.ReadLong,Address + 0xE8,Game.Performance.meanLatency,100);
                long endBuff = Game.Performance.ReadMemWithCache(M.ReadLong,Address + 0xF0,Game.Performance.meanLatency,15);
                int count = (int)(endBuff - startBuff) / 8;
                if (count <= 0 || count >= MaxBuffCount * 8)
                {
                    return temp.Values.ToList();
                }
                var bytes = M.ReadBytes(startBuff, (int) (endBuff - startBuff));
                for (int i = 0; i < bytes.Length; i+=8)
                {
                    var addr = BitConverter.ToInt64(bytes, i);
                    if(addr==0)continue;
                    if (cacheBuffs.ContainsKey(addr))
                    {
                        temp[addr] = cacheBuffs[addr];
                        continue;
                    }
                    temp[addr] =(ReadObject<Buff>(addr+8));
                }
                cacheBuffs = temp;
                return cacheBuffs.Values.ToList();
            }
        }


        public bool HasBuff2(string buff)
        {
            return Buffs2.Exists(x => x.Name == buff);
        }
    }
}