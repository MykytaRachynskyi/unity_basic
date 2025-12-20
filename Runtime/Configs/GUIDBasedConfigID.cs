using System.Collections.Generic;
using UnityEngine;

namespace Basic
{
    [System.Serializable]
    public struct GUID
    {
        public long FirstHalf;
        public long SecondHalf;

        public static GUID Generate()
        {
            var guid = System.Guid.NewGuid();
            var bytes = guid.ToByteArray();
            long firstHalf = 0;
            long secondHalf = 0;
            for (int i = 0; i < 8; ++i)
            {
                firstHalf |= ((long)bytes[i]) << (i * 8);
            }
            for (int i = 0; i < 8; ++i)
            {
                secondHalf |= ((long)bytes[i + 8]) << (i * 8);
            }
            return new GUID { FirstHalf = firstHalf, SecondHalf = secondHalf };
        }

        public override readonly string ToString()
        {
            System.Span<byte> bytes = stackalloc byte[16];
            for (int i = 0; i < 8; ++i)
            {
                bytes[i] = (byte)(FirstHalf >> (i * 8));
            }
            for (int i = 0; i < 8; ++i)
            {
                bytes[i + 8] = (byte)(SecondHalf >> (i * 8));
            }
            var guid = new System.Guid(bytes);
            return guid.ToString();
        }

        public override readonly bool Equals(object obj) =>
            obj is GUID gUID && FirstHalf == gUID.FirstHalf && SecondHalf == gUID.SecondHalf;

        public override readonly int GetHashCode() =>
            System.HashCode.Combine(FirstHalf, SecondHalf);

        public static bool operator ==(GUID guid1, GUID guid2) =>
            guid1.FirstHalf == guid2.FirstHalf && guid1.SecondHalf == guid2.SecondHalf;

        public static bool operator !=(GUID guid1, GUID guid2) =>
            guid1.FirstHalf != guid2.FirstHalf || guid1.SecondHalf != guid2.SecondHalf;
    }

    [System.Serializable]
    public abstract class GUIDBasedConfigID
    {
        [SerializeField]
        protected GUID _guid;
        public GUID GUID => _guid;
        public abstract void GetNames(List<string> list);
        public abstract int GUIDToIndex(GUID guid);
        public abstract GUID IndexToGUID(int newIndex);
        public abstract IConfig IndexToConfig(int newIndex);

#if UNITY_EDITOR
        public void EDITOR_SetGUID(GUID guid) => _guid = guid;

#endif
    }
}
