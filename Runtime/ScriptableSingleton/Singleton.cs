using UnityEngine;

namespace Basic.Singleton
{
	public abstract class Singleton : ScriptableObject { }

	public abstract class Singleton<T> : Singleton
		where T : Singleton<T>
	{
		private static T _instance;

		public static T Instance
		{
			get
			{
				if (_instance == null)
				{
					if (Application.isEditor) { }
					else { }
				}

				return _instance;
			}
		}
	}
}
