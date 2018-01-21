namespace PoeHUD.Poe.Elements
{
    public class EntityLabel : Element
    {
        public int Length
        {
            get
            {
                int LabelLen = M.ReadInt(Address + 0xC50);
                if (LabelLen <= 0 || LabelLen > 256)
                {
                    return 0;
                }
                return LabelLen;
            }
        }

        private string _text;
        public string Text
        {
            get
            {
                if (_text == null)
                {
                    var LabelLen = Length;
                    if (LabelLen <= 0 || LabelLen > 256)
                    {
                        return "";
                    }
                    _text = LabelLen >= 8 ? M.ReadStringU(M.ReadLong(Address + 0xC40), LabelLen * 2) : M.ReadStringU(Address + 0xC40, LabelLen * 2);
                }
                return _text;
            }
        }
        
        
        
    }
}