using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PoeHUD.Poe.Elements
{
   public class VendorSellElement:Element
   {
       public Element Accept => IsVisible ? Children[3].Children[5] : null;
       public Element Cancel => IsVisible ? Children[3].Children[6] : null;
   }
}
