using UnityEngine;
using UnityEngine.UI;

namespace Basic.UI
{
    [AddComponentMenu("UI (Canvas)/Basic Image", 10)]
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public class BasicImage : Image
    {
        [SerializeField]
        private PaletteTintMode _mode = PaletteTintMode.Local;

        [SerializeField]
        private Color _localColor = Color.white;

        [SerializeField, Range(0f, 1f)]
        private float _alpha = 1f;

        [SerializeField]
        private ColorPalette _palette;

        [SerializeField]
        private GUID _colorGuid;

        public PaletteTintMode Mode => _mode;

        public Color LocalColor => _localColor;

        public float Alpha => _alpha;

        public ColorPalette Palette => _palette;

        public GUID ColorGuid => _colorGuid;

        public override Color color
        {
            get => ResolveColor();
            set
            {
                if (_mode == PaletteTintMode.Palette)
                    return;

                _localColor = value;
                base.color = value;
            }
        }

        public void ApplyResolvedColor()
        {
            base.color = ResolveColor();
        }

        public void RefreshRegistration()
        {
            PaletteColorRegistry.Unregister(this);

            if (_mode == PaletteTintMode.Palette && isActiveAndEnabled)
                PaletteColorRegistry.Register(this, _palette, _colorGuid);
        }

        public void SetMode(PaletteTintMode mode)
        {
            _mode = mode;
            ApplyResolvedColor();
            RefreshRegistration();
        }

        public void SetLocalColor(Color color)
        {
            _localColor = color;
            if (_mode == PaletteTintMode.Local)
                ApplyResolvedColor();
        }

        public void SetAlpha(float alpha)
        {
            _alpha = Mathf.Clamp01(alpha);
            if (_mode == PaletteTintMode.Palette)
                ApplyResolvedColor();
        }

        public void SetPalette(ColorPalette palette)
        {
            _palette = palette;
            ApplyResolvedColor();
            RefreshRegistration();
        }

        public void SetPaletteColor(GUID guid)
        {
            _colorGuid = guid;
            ApplyResolvedColor();
            RefreshRegistration();
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            ApplyResolvedColor();
            RefreshRegistration();
        }

        protected override void OnDisable()
        {
            PaletteColorRegistry.Unregister(this);
            base.OnDisable();
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            ApplyResolvedColor();
            RefreshRegistration();
        }
#endif

        private Color ResolveColor()
        {
            if (_mode == PaletteTintMode.Local)
                return _localColor;

            if (
                _palette != null
                && _colorGuid != default
                && _palette.TryGetColor(_colorGuid, out var paletteRgb)
            )
                return new Color(paletteRgb.r, paletteRgb.g, paletteRgb.b, _alpha);

            return _localColor;
        }
    }
}
