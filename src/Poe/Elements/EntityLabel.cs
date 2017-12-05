namespace PoeHUD.Poe.Elements
{
    public class EntityLabel : Element
    {
        public int Len
        {
            get
            {
                int LabelLen = M.ReadInt(Address + 0xC28);
                return LabelLen <= 0 || LabelLen > 256 ? 0 : LabelLen;
            }
        }

        public string Text
        {
            get
            {
                var LabelLen = Len;
                if (LabelLen <= 0 || LabelLen > 256)
                {
                    return "";
                }
                return LabelLen >= 8 ? M.ReadStringU(M.ReadLong(Address + 0xC18), LabelLen * 2) : M.ReadStringU(Address + 0xC18, LabelLen * 2);
            }
        }
    }
}