using System.Collections.Generic;
using UnityEngine;

namespace Basic
{
    public static class ExtensionMethods
    {
        public static void Shuffle<T>(this IList<T> list, System.Random random)
        {
            int n = list.Count;
            for (int i = n - 1; i > 0; i--)
            {
                int j = random.Next(i + 1);

                T temp = list[i];
                list[i] = list[j];
                list[j] = temp;
            }
        }
    }
}
