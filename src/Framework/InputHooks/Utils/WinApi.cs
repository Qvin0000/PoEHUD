using System.Runtime.InteropServices;

namespace PoeHUD.Framework.InputHooks.Utils
{
    public class WinApi
    {
        [DllImport("user32.dll")]
        public static extern bool BlockInput(bool fBlockIt);
    }
}