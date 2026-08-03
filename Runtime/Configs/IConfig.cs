namespace Basic
{
    public interface IConfig
    {
        GUID GUID { get; }
        GUIDBasedConfigID ConfigID { get; }
        string DEBUG_Name { get; }

#if UNITY_EDITOR
        void EDITOR_SetGUID(GUID guid);
#endif
    }
}
