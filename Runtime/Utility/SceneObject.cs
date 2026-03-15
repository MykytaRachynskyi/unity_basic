using UnityEngine;

namespace Basic
{
    public class SceneObject
    {
        public static T[] FindAll<T>()
            where T : Object =>
            Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        public static T FindOne<T>(bool expectOne = true)
            where T : Object
        {
            var objs = Object.FindObjectsByType<T>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None
            );
            if (objs.Length == 0)
            {
                return null;
            }
            else if (objs.Length > 1)
            {
                if (expectOne)
                {
                    Log.Error(
                        $"Found {objs.Length} instances of object {typeof(T).Name}, while expected 1!"
                    );
                }
                for (int i = 0; i < objs.Length; ++i)
                {
                    if (objs[i])
                    {
                        return objs[i];
                    }
                    else
                    {
                        Log.Error($"Found null instance of object {typeof(T).Name}");
                    }
                }
                return null;
            }
            else
            {
                if (!objs[0])
                {
                    Log.Error($"Found null instance of object {typeof(T).Name}");
                }
                return objs[0];
            }
        }
    }
}
