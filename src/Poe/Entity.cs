using System;
using PoeHUD.Models.Interfaces;
using System.Collections.Generic;

namespace PoeHUD.Poe
{
    public sealed class Entity : RemoteMemoryObject, IEntity
    {
        private long _componentLookup = -1;
        private long _componentList =-1;
        private long ComponentLookup =>  _componentLookup==-1 ?_componentLookup= M.ReadLong(Address, 0x48, 0x30) : _componentLookup;
        private long ComponentList => _componentList==-1 ? _componentList = M.ReadLong(Address + 0x8) : _componentList;
        private string _path;
        private int readingPathAttempts = 0;
        public string Path
        {
            get
            {
                if(string.IsNullOrEmpty(_path) && readingPathAttempts<5)
                {
                    _path = M.ReadStringU(M.ReadLong(Address, 0x20),(int) M.ReadLong(Address,0x30)*2);
                    readingPathAttempts++;
                }
                return _path;
            }
        }

       
   

        private bool _isValid; 
        public bool IsValid 
        {
            get
            {
                Experimental();
                return _isValid;
            }
        }

        private long? _id;
        public long Id => (long) (_id ?? (_id = ((long)M.ReadInt(Address + 0x40) << 32 ^ Address)));
        public int InventoryId => M.ReadInt(Address + 0x58);

        public long TestTypeId => M.ReadLong(Address, 0x30);
        public long TestTypeId2 => M.ReadLong(Address, 0x38);
        public long TestTypeId3 => M.ReadLong(Address, 0x48,0x38);
        public long TestTypeId4 => M.ReadInt(Address, 0x54);
      
        private float _nextUpdateTime;
        void Experimental()
        {
            if (Game.MainTimer.ElapsedMilliseconds > _nextUpdateTime)
            {
                _nextUpdateTime = Game.Performance.GetWaitTime(Game.Performance.meanLatency,33);
                _isValid = M.ReadInt(Address, 0x20, 0) == 0x65004D;
            }
        }
        /// <summary>
        /// 0x65004D = "Me"(4 bytes) from word Metadata
        /// </summary>
        private bool? _isHostile;
        public bool IsHostile => (bool) (_isHostile ?? (_isHostile = ((M.ReadByte(M.ReadLong(Address + 0x50) + 0x130) & 1) == 0)));

        public bool HasComponent<T>() where T : Component, new()
        {
            long addr;
            return HasComponent<T>(out addr);
        }

        readonly Dictionary<string,Component> _cacheComponents = new Dictionary<string, Component>();
        public bool HasComponent<T>(out long addr) where T : Component, new()
        {
            string name = typeof(T).Name;
            if (_cacheComponents.ContainsKey(name))
            {
                addr = _cacheComponents[name].Address;
                return true;
            }
            long componentLookup = ComponentLookup;
            addr = M.ReadLong(componentLookup);
            int i = 0;
            while (!M.ReadString(M.ReadLong(addr + 0x10)).Equals(name))
            {
                addr = M.ReadLong(addr);
                ++i;
                if (addr == componentLookup || addr == 0 || addr == -1 || i >= 200)
                    return false;
            }
            return true;
        }

        

        private bool HasComponent2<T>(out long addr) where T : Component, new()
        {
            string name = typeof(T).Name;
            long componentLookup = ComponentLookup;
            addr = M.ReadLong(componentLookup);
            int i = 0;
            while (!M.ReadString(M.ReadLong(addr + 0x10)).Equals(name))
            {
                addr = M.ReadLong(addr);
                ++i;
                if (addr == componentLookup || addr == 0 || addr == -1 || i >= 200)
                    return false;
            }
            return true;
        }

        public T GetComponent<T>() where T : Component, new()
        {
            string name = typeof(T).Name;
            if (_cacheComponents.ContainsKey(name))
            {
                return (T) _cacheComponents[name];
            }
            long addr;
            if (HasComponent<T>(out addr))
            {
                var readObject = ReadObject<T>(ComponentList + M.ReadInt(addr + 0x18) * 8);
                _cacheComponents[name] = readObject;
                return readObject;
            }
            else
                return GetObject<T>(0);
        }

        public Dictionary<string, long> GetComponents()
        {
            var dictionary = new Dictionary<string, long>();
            long componentLookup = ComponentLookup;
            // the first address is a base object that doesn't contain a component, so read the first component
            long addr = M.ReadLong(componentLookup);
            while (addr != componentLookup && addr != 0 && addr != -1)
            {
                string name = M.ReadString(M.ReadLong(addr + 0x10));
                string nameStart = name;
                if (!string.IsNullOrWhiteSpace(name))
                {
                    char[] arr = name.ToCharArray();
                    arr = Array.FindAll(arr, (c => (char.IsLetterOrDigit(c) || char.IsWhiteSpace(c) || c == '-')));
                    name = new string(arr);
                }
                if (String.IsNullOrWhiteSpace(name) || name != nameStart)
                {
                    break;
                }
                long componentAddress = M.ReadLong(ComponentList + M.ReadInt(addr + 0x18) * 8);
                if (!dictionary.ContainsKey(name) && !string.IsNullOrWhiteSpace(name))
                    dictionary.Add(name, componentAddress);
                addr = M.ReadLong(addr);
            }
            return dictionary;
        }

        /*
        public string DebugReadComponents()
        {
            string result = "";

            long cList = M.ReadLong(Address + 0x8);
            result += "ComponentList (EntytaAddr + 0x8): " + System.Environment.NewLine + cList.ToString("x") + System.Environment.NewLine + System.Environment.NewLine;

            result += "ComponentLookupRead: " + System.Environment.NewLine + ComponentLookup.ToString("x") + System.Environment.NewLine;

            long CL_read1 = M.ReadLong(Address);
            result += "CL_read1 (EntytaAddr + 0x0): " + System.Environment.NewLine + CL_read1.ToString("x") + System.Environment.NewLine + System.Environment.NewLine;

            long CL_read2 = M.ReadLong(CL_read1 + 0x48);
            result += "CL_read2 (CL_read1 + 0x48): " + System.Environment.NewLine + CL_read2.ToString("x") + System.Environment.NewLine + System.Environment.NewLine;

            long CL_read3 = M.ReadLong(CL_read2 + 0x30);
            result += "CL_read3 (CL_read2 + 0x30): " + System.Environment.NewLine + CL_read3.ToString("x") + System.Environment.NewLine + System.Environment.NewLine;

            long CL_read4 = M.ReadLong(CL_read3 + 0x0);
            result += ">LookUp  (CL_read3 + 0x0): " + System.Environment.NewLine + CL_read4.ToString("x") + System.Environment.NewLine + System.Environment.NewLine;


            result += "ReadingComponents: " + System.Environment.NewLine;


            var dictionary = new Dictionary<string, long>();

            long componentLookup = ComponentLookup;
            long addr = componentLookup;



            do
            {
                result += "addr: " + addr.ToString("x") + System.Environment.NewLine;

                result += "NamePchar at (addr + 0x10): " + (addr + 0x10).ToString("x") + System.Environment.NewLine;
                string name = M.ReadString(M.ReadLong(addr + 0x10));
                result += "name: " + name + System.Environment.NewLine;


                result += "componentAddress (ComponentList + M.ReadInt(addr + 0x18) * 8): " + (addr + 0x10).ToString("x") + System.Environment.NewLine;
                long componentAddress = M.ReadInt(ComponentList + M.ReadInt(addr + 0x18) * 8);
                result += $"({ComponentList} + M.ReadInt({(addr + 0x18).ToString("x")}) * 8)" + System.Environment.NewLine;

                result += $"({ComponentList} + {(M.ReadInt(addr + 0x18)).ToString("x")} * 8)" + System.Environment.NewLine;
                result += $"({ComponentList} + {((M.ReadInt(addr + 0x18)) * 8).ToString("x")})" + System.Environment.NewLine;

                result += "FinalComponentAddress: " + componentAddress.ToString("x") + System.Environment.NewLine;


                if (!dictionary.ContainsKey(name) && !string.IsNullOrWhiteSpace(name))
                {
                    dictionary.Add(name, componentAddress);
                    result += $"AddComponent: {name} : {componentAddress.ToString("x")}" + System.Environment.NewLine;
                }
                else
                {
                    result += $"SkipComponent: {name} : {componentAddress.ToString("x")}. Allready contains or emptyCompName" + System.Environment.NewLine;
                }

                addr = M.ReadLong(addr);
                result += System.Environment.NewLine;

            } while (addr != componentLookup && addr != 0 && addr != -1);

            return result;
        }
        */

        public override string ToString()
        {
            return Path;
        }
    }
}
