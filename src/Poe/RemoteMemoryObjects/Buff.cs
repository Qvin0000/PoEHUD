namespace PoeHUD.Poe.RemoteMemoryObjects
{
    public class Buff : RemoteMemoryObject
    {
        private string _name;
        public string Name
        {
            get
            {
                if(_name==null)
                _name =M.ReadStringU(M.ReadLong(Address + 8, 0));
                return _name;
            }
        }

        public byte Charges => M.ReadByte(Address + 0x2C);
        //public int SkillId => M.ReadInt(Address + 0x5C); // I think this is part of another structure referenced in a pointer at 0x58
        public float MaxTime => M.ReadFloat(Address + 0x10); // infinity for auras and always on buff
        public float Timer => M.ReadFloat(Address + 0x14); // timeleft
    }
}