using System.Collections.Generic;
using Basic.Randomness;
using UnityEngine;

namespace Basic
{
	public static class ExtensionMethods
	{
		public static void Shuffle<T>(this IList<T> list, System.Random random)
		{
			var n = list.Count;
			for (var i = n - 1; i > 0; i--)
			{
				var j = random.Next(i + 1);
				(list[i], list[j]) = (list[j], list[i]);
			}
		}

		public static void Shuffle<T>(this IList<T> list, IRandomnessService random)
		{
			var n = list.Count;
			for (var i = n - 1; i > 0; i--)
			{
				var j = random.NextInt(0, i + 1);
				(list[i], list[j]) = (list[j], list[i]);
			}
		}
	}
}