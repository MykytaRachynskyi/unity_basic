namespace Basic
{
    public interface IConfig
    {
        public GUID GUID { get; set; }
        public GUIDBasedConfigID ConfigID { get; }
        public string DEBUG_Name { get; }

        public void EDITOR_SetGUID(GUID guid)
        {
            GUID = guid;
        }
    }
}
