using System.Collections.Generic;

namespace Basic.UI
{
    [System.Serializable]
    public class PaletteEntryConfigID : GUIDBasedConfigID
    {
        [System.NonSerialized]
        private ColorPalette _owner;

        public void Bind(ColorPalette owner) => _owner = owner;

        public override void GetNames(List<string> list) => _owner?.GetNames(list);

        public override int GUIDToIndex(GUID guid) => _owner?.GUIDToIndex(guid) ?? -1;

        public override GUID IndexToGUID(int newIndex) => _owner?.IndexToGUID(newIndex) ?? default;

        public override IConfig IndexToConfig(int newIndex) => _owner?.IndexToConfig(newIndex);
    }
}
