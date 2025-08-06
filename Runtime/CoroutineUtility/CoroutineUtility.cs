using System.Collections;
using Basic.Coroutines;
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
                    _monoBehaviour = go.AddComponent<CoroutineUtilityBehaviour>();
                }

                return _monoBehaviour;
            }
        }

        public static Coroutine StartRoutine(IEnumerator routine) =>
            MonoBehaviour.StartCoroutine(routine);

        public static void StopRoutine(Coroutine routine) => MonoBehaviour.StopCoroutine(routine);
    }
}
