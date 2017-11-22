using System.Collections;

namespace PoeHUD.Framework.Helpers
{
    public static class CoroutineExtension
    {
        public static Coroutine Run(this Coroutine coroutine) => Runner.Instance.Run(coroutine);

        public static Coroutine Run(this IEnumerator iEnumeratorCor, string owner, string name = null) => Runner.Instance.Run(iEnumeratorCor, owner, name);

        public static bool Done(this Coroutine coroutine) => Runner.Instance.Done(coroutine);

        public static Coroutine GetCopy(this Coroutine coroutine) => coroutine.GetCopy(coroutine);

        public static Coroutine AutoRestart(this Coroutine coroutine)
        {
            Runner.Instance.AddToAutoupdate(coroutine);
            return coroutine;
        }

    }
}