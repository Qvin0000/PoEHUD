using System.Runtime.Remoting.Activation;

namespace PoeHUD.Poe.Components
{
    public class SkillGem : Component
    {
        public int Level => M.ReadInt(Address+0x24);
    }
}