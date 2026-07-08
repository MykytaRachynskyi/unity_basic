using UnityEngine;

namespace Basic.UI
{
    [System.Serializable]
    public class ColorPaletteEntry : IConfig
    {
        [SerializeField]
        private string _name = "New Color";

        [SerializeField]
        private Color _color = Color.white;

        [SerializeField]
        private PaletteEntryConfigID _configId = new();

        public string Name
        {
            get => _name;
            set => _name = value;
        }

        public Color Color
        {
            get => _color;
            set => _color = value;
        }

        public GUID GUID
        {
            get => _configId.GUID;
            set => _configId.EDITOR_SetGUID(value);
        }

        public GUIDBasedConfigID ConfigID => _configId;

        public string DEBUG_Name => string.IsNullOrEmpty(_name) ? "Unnamed" : _name;

        public void EDITOR_SetGUID(GUID guid) => _configId.EDITOR_SetGUID(guid);

        internal void BindOwner(ColorPalette owner) => _configId.Bind(owner);
    }
}
