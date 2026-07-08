using System.Collections.Generic;
using UnityEngine;

namespace Basic.UI
{
    [CreateAssetMenu(fileName = "ColorPalette", menuName = "Basic/UI/Color Palette")]
    public class ColorPalette : ScriptableObject
    {
        [SerializeField]
        private List<ColorPaletteEntry> _entries = new();

#if UNITY_EDITOR
        [System.NonSerialized]
        private Dictionary<GUID, Color> _lastKnownColors;
#endif

        public IReadOnlyList<ColorPaletteEntry> Entries => _entries;

        public bool TryGetColor(GUID guid, out Color rgb)
        {
            rgb = default;
            if (_entries == null || guid == default)
                return false;

            foreach (var entry in _entries)
            {
                if (entry.GUID == guid)
                {
                    rgb = entry.Color;
                    return true;
                }
            }

            return false;
        }

        public bool SetColor(GUID guid, Color rgb)
        {
            if (!TryGetEntry(guid, out var entry))
                return false;

            var newColor = new Color(rgb.r, rgb.g, rgb.b, entry.Color.a);
            if (entry.Color == newColor)
                return true;

            entry.Color = newColor;
            PaletteColorRegistry.Notify(this, guid);
            return true;
        }

        public void GetNames(IList<string> list)
        {
            if (list == null || _entries == null)
                return;

            foreach (var entry in _entries)
                list.Add(entry.DEBUG_Name);
        }

        public int GUIDToIndex(GUID guid)
        {
            if (_entries == null)
                return -1;

            for (var i = 0; i < _entries.Count; i++)
            {
                if (_entries[i].GUID == guid)
                    return i;
            }

            return -1;
        }

        public GUID IndexToGUID(int index)
        {
            if (_entries == null || index < 0 || index >= _entries.Count)
                return default;

            return _entries[index].GUID;
        }

        public string IndexToName(int index)
        {
            if (_entries == null || index < 0 || index >= _entries.Count)
                return null;

            return _entries[index].DEBUG_Name;
        }

        public IConfig IndexToConfig(int index)
        {
            if (_entries == null || index < 0 || index >= _entries.Count)
                return null;

            return _entries[index];
        }

        private bool TryGetEntry(GUID guid, out ColorPaletteEntry entry)
        {
            entry = null;
            if (_entries == null || guid == default)
                return false;

            foreach (var candidate in _entries)
            {
                if (candidate.GUID == guid)
                {
                    entry = candidate;
                    return true;
                }
            }

            return false;
        }

        private void OnValidate()
        {
            RepairGUIDs();
            BindEntries();
            DetectAndNotifyChanges();
        }

        private void RepairGUIDs()
        {
            if (_entries == null)
                return;

            var usedGuids = new HashSet<GUID>();
            var dirty = false;

            foreach (var entry in _entries)
            {
                if (entry.ConfigID.GUID == default || usedGuids.Contains(entry.ConfigID.GUID))
                {
                    var newGuid = GUID.Generate();
                    Log.Info(
                        $"Generating new ID for color {entry.DEBUG_Name} in {name}!\nWas:{entry.ConfigID.GUID}\nNew:{newGuid}"
                    );
                    entry.EDITOR_SetGUID(newGuid);
                    dirty = true;
                }

                usedGuids.Add(entry.ConfigID.GUID);
            }

#if UNITY_EDITOR
            if (dirty)
            {
                UnityEditor.EditorUtility.SetDirty(this);
                UnityEditor.AssetDatabase.SaveAssets();
            }
#endif
        }

        private void BindEntries()
        {
            if (_entries == null)
                return;

            foreach (var entry in _entries)
                entry.BindOwner(this);
        }

        private void DetectAndNotifyChanges()
        {
#if UNITY_EDITOR
            if (_entries == null)
                return;

            var hadCache = _lastKnownColors != null && _lastKnownColors.Count > 0;
            if (hadCache)
            {
                foreach (var entry in _entries)
                {
                    if (entry.GUID == default)
                        continue;

                    if (
                        !_lastKnownColors.TryGetValue(entry.GUID, out var oldColor)
                        || oldColor != entry.Color
                    )
                        PaletteColorRegistry.Notify(this, entry.GUID);
                }
            }

            CacheLastKnownColors();
#endif
        }

#if UNITY_EDITOR
        private void CacheLastKnownColors()
        {
            _lastKnownColors ??= new Dictionary<GUID, Color>();
            _lastKnownColors.Clear();

            if (_entries == null)
                return;

            foreach (var entry in _entries)
            {
                if (entry.GUID != default)
                    _lastKnownColors[entry.GUID] = entry.Color;
            }
        }
#endif
    }
}
