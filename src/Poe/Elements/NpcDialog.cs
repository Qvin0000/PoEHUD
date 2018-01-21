using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms.VisualStyles;

namespace PoeHUD.Poe.Elements
{
    public class NpcDialog : Element
    {
        public IEnumerable<NpcString> NpcStrings => Children[0].Children[2].Children.Where(x => x.ChildCount > 0)
            .Select(x => x.Children[0].AsObject<NpcString>());

    }

    public class NpcString : Element
    {
        //Some element have wrong pointer
        public string Name => M.ReadStringU(M.ReadLong(Address+0x690,0x648));
        public long Lentgh => M.ReadLong(Address + 0x690, 0x658);
    }
}
