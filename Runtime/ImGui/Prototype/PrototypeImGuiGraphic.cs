using UnityEngine;
using UnityEngine.UI;

namespace Basic.ImGui.Prototype
{
    /// <summary>
    /// PROTOTYPE — CanvasRenderBackend stand-in: one MaskableGraphic, OnPopulateMesh batch upload.
    /// </summary>
    [AddComponentMenu("UI (Canvas)/PROTOTYPE ImGui Graphic")]
    [ExecuteAlways]
    public sealed class PrototypeImGuiGraphic : MaskableGraphic
    {
        readonly PrototypeBatchBuilder _batchBuilder = new();
        readonly VertexHelper _vertexHelper = new();

        PrototypeRenderCommand[] _commands = System.Array.Empty<PrototypeRenderCommand>();

        public int CommandCount => _commands.Length;
        public int LastVertexCount => _batchBuilder.VertexCount;
        public int LastBatchCount => _batchBuilder.BatchCount;

        public void SetCommands(PrototypeRenderCommand[] commands) => _commands = commands ?? System.Array.Empty<PrototypeRenderCommand>();

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            if (_commands.Length == 0)
            {
                vh.Clear();
                return;
            }

            if (_uploadMesh == null)
            {
                _uploadMesh = new Mesh { name = "PrototypeImGuiUpload" };
                _uploadMesh.MarkDynamic();
            }

            _batchBuilder.BuildFillMesh(_commands, _commands.Length, _uploadMesh);
            vh.Clear();
            vh.FillMesh(_uploadMesh);
        }

        Mesh _uploadMesh;

        public void PopulateMeshNow(Mesh mesh)
        {
            _batchBuilder.Build(_commands, _commands.Length, _vertexHelper);
            _vertexHelper.FillMesh(mesh);
            canvasRenderer.SetMesh(mesh);
        }
    }
}
