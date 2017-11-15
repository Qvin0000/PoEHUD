using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PoeHUD.Poe.Elements
{
    public class VendorSellElement : Element
    {
        public Element Accept => Children[3].Children[5];
        public Element Cancel => Children[3].Children[6];
    }
}
