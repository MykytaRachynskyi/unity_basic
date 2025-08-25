using System.Collections.Generic;
using Basic.Singleton;

namespace Basic
{
    public abstract class Database<T> : Singleton<T>
        where T : Database<T>
    {
        public abstract IEnumerable<IConfig> Configs { get; }

        public void GetNames(List<string> list)
        {
            if (list == null)
            {
                return;
            }

            foreach (var config in Configs)
            {
                list.Add(config.DEBUG_Name);
            }
        }
    }
}
