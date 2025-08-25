using System.Collections.Generic;
using UnityEngine;

namespace Basic
{
    public struct GUID
    {
        public long FirstHalf;
        public long SecondHalf;
    }

    public abstract class GUIDBasedConfigID
    {
        [SerializeField]
        protected GUID _guid;
        public GUID GUID => _guid;
        public abstract void GetNames(List<string> list);
    }
}
