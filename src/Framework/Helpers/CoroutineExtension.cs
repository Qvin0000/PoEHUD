using System.Collections;

namespace PoeHUD.Framework.Helpers
{
    public static class CoroutineExtension
    {
        public static Coroutine Run(this Coroutine cor) => Runner.Instance.Run(cor);

        public static Coroutine Run(this IEnumerator iEnumeratorCor, string owner) => Runner.Instance.Run(iEnumeratorCor, owner);

        public static bool Done(this Coroutine cor) => Runner.Instance.Done(cor);

    }
}