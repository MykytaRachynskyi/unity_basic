using Basic.ImGui.Layout;
using UnityEngine;
using UnityEngine.UI;

namespace Basic.ImGui.Rendering
{
    [AddComponentMenu("UI/ImGui Graphic")]
    [ExecuteAlways]
    public sealed class ImGuiGraphic : MaskableGraphic
    {
        const AdditionalCanvasShaderChannels TmpShaderChannels = (AdditionalCanvasShaderChannels)25;

        readonly BatchBuilder _batchBuilder = new();

        ImGuiContext _context;
        IFontRegistry _fontRegistry;
        RenderFrame _frame;
        RenderBackendContext _backendContext;
        bool _hasFrame;
        Texture _fontAtlas;

        public int LastVertexCount => _batchBuilder.VertexCount;
        public int LastBatchCount => _batchBuilder.BatchCount;
        public int LastTriangleCount => _batchBuilder.TriangleCount;

        public override Texture mainTexture =>
            _fontAtlas != null
                ? _fontAtlas
                : material != null && material.mainTexture != null
                    ? material.mainTexture
                    : base.mainTexture;

        public void Configure(ImGuiContext context, IFontRegistry fontRegistry = null)
        {
            _context = context;
            _fontRegistry = fontRegistry;
            _fontAtlas = null;

            if (fontRegistry != null
                && fontRegistry.TryGetFont(FontId.Default, out var fontResources)
                && fontResources.Material != null)
            {
                material = fontResources.Material;
                _fontAtlas = fontResources.Atlas != null ? fontResources.Atlas : fontResources.Material.mainTexture;
                SetMaterialDirty();
            }

            EnsureCanvasShaderChannels();
        }

        public void SetFrame(RenderFrame frame, RenderBackendContext backendContext)
        {
            _frame = frame;
            _backendContext = backendContext;
            _hasFrame = true;
            SetVerticesDirty();
        }

        void EnsureCanvasShaderChannels()
        {
            var canvas = GetComponentInParent<Canvas>();
            if (canvas == null || (canvas.additionalShaderChannels & TmpShaderChannels) == TmpShaderChannels)
            {
                return;
            }

            canvas.additionalShaderChannels |= TmpShaderChannels;
        }

        protected override void OnPopulateMesh(VertexHelper vertexHelper)
        {
            if (!_hasFrame || !_frame.Commands.IsCreated || _frame.Commands.Length <= 0)
            {
                vertexHelper.Clear();
                return;
            }

            _batchBuilder.Build(
                _frame.Commands,
                _frame.Strings,
                _fontRegistry,
                material,
                _frame.LayoutDimensions,
                _backendContext.FlipY,
                rectTransform.rect,
                mapToLocalRect: true,
                vertexHelper);
        }

        protected override void OnDestroy()
        {
            _batchBuilder.Dispose();
            base.OnDestroy();
        }
    }
}
