using System.Collections;
using Basic.Coroutines;
using Basic.Utility;
using UnityEngine;

namespace Basic
{
    public static class CoroutineUtility
    {
        private const string COROUTINE_UTILITY_GO_NAME = "CoroutineUtility";

        private static CoroutineUtilityBehaviour _monoBehaviour;
        private static CoroutineUtilityBehaviour MonoBehaviour
        {
            get
            {
                if (_monoBehaviour == null)
                {
                    var go = new GameObject(COROUTINE_UTILITY_GO_NAME);
                    go.AddComponent<DDOL>();
                    _monoBehaviour = go.AddComponent<CoroutineUtilityBehaviour>();
                }

                return _monoBehaviour;
            }
        }

        public static void InvokeAfterFrameDelay(System.Action callback, int frames) =>
            StartRoutine(InvokeAfterFrameDelayRoutine(callback, frames));

        public static void InvokeAfterTimeDelay(System.Action callback, float delay) =>
            StartRoutine(InvokeAfterTimeDelayRoutine(callback, delay));

        public static Coroutine StartRoutine(IEnumerator routine) =>
            MonoBehaviour.StartCoroutine(routine);

        public static void StopRoutine(Coroutine routine) => MonoBehaviour.StopCoroutine(routine);

        private static IEnumerator InvokeAfterFrameDelayRoutine(System.Action callback, int frames)
        {
            while (frames > 0)
            {
                --frames;
                yield return null;
            }

            callback?.Invoke();
        }

        private static IEnumerator InvokeAfterTimeDelayRoutine(System.Action callback, float delay)
        {
            while (delay > 0f)
            {
                delay -= Time.deltaTime;
                yield return null;
            }

            callback?.Invoke();
        }
    }
}
