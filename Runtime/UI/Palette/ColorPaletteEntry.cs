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

        public GUID GUID => ConfigID.GUID;

        public GUIDBasedConfigID ConfigID => _configId;

        public string DEBUG_Name => string.IsNullOrEmpty(_name) ? "Unnamed" : _name;

        internal void BindOwner(ColorPalette owner) => _configId.Bind(owner);
    }
}
