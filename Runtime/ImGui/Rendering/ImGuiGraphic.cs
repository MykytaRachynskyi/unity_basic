using Basic.ImGui.Layout;
using UnityEngine;
using UnityEngine.UI;

namespace Basic.ImGui.Rendering
{
    [AddComponentMenu("UI/ImGui Graphic")]
    [ExecuteAlways]
    public sealed class ImGuiGraphic : MaskableGraphic
    {
        readonly BatchBuilder _batchBuilder = new();

        ImGuiContext _context;
        IFontRegistry _fontRegistry;
        RenderFrame _frame;
        RenderBackendContext _backendContext;
        bool _hasFrame;
        Mesh _uploadMesh;

        public int LastVertexCount => _batchBuilder.VertexCount;
        public int LastBatchCount => _batchBuilder.BatchCount;
        public int LastTriangleCount => _batchBuilder.TriangleCount;

        public void Configure(ImGuiContext context, IFontRegistry fontRegistry = null)
        {
            _context = context;
            _fontRegistry = fontRegistry;
        }

        public void SetFrame(RenderFrame frame, RenderBackendContext backendContext)
        {
            _frame = frame;
            _backendContext = backendContext;
            _hasFrame = true;
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vertexHelper)
        {
            vertexHelper.Clear();
            if (!_hasFrame || !_frame.Commands.IsCreated || _frame.Commands.Length <= 0)
            {
                return;
            }

            if (_uploadMesh == null)
            {
                _uploadMesh = new Mesh { name = "ImGuiUpload" };
                _uploadMesh.MarkDynamic();
            }

            _batchBuilder.Build(
                _frame.Commands,
                _frame.Strings,
                _fontRegistry,
                material,
                _frame.LayoutDimensions,
                _backendContext.FlipY,
                _uploadMesh);

            vertexHelper.FillMesh(_uploadMesh);
        }

        protected override void OnDestroy()
        {
            _batchBuilder.Dispose();
            if (_uploadMesh != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(_uploadMesh);
                }
                else
                {
                    DestroyImmediate(_uploadMesh);
                }
            }

            base.OnDestroy();
        }
    }
}
