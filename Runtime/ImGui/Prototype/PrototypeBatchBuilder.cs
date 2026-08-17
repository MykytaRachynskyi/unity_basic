using UnityEngine;
using UnityEngine.UI;

namespace Basic.ImGui.Prototype
{
    /// <summary>
    /// PROTOTYPE — merges same-material rectangle commands into one VertexHelper upload.
    /// Mirrors the v1 BatchBuilder shape without layout-core dependencies.
    /// </summary>
    public sealed class PrototypeBatchBuilder
    {
        readonly UIVertex[] _scratch = new UIVertex[4];
        Vector3[] _meshVertices = System.Array.Empty<Vector3>();
        Color32[] _meshColors = System.Array.Empty<Color32>();
        ushort[] _meshIndices = System.Array.Empty<ushort>();

        public int CommandCount { get; private set; }
        public int VertexCount { get; private set; }
        public int TriangleCount { get; private set; }
        public int BatchCount { get; private set; }

        public void Build(PrototypeRenderCommand[] commands, int count, VertexHelper vertexHelper)
        {
            CommandCount = count;
            vertexHelper.Clear();

            if (count <= 0)
            {
                VertexCount = 0;
                TriangleCount = 0;
                BatchCount = 0;
                return;
            }

            for (var i = 0; i < count; i++)
                AddRect(vertexHelper, commands[i]);

            VertexCount = count * 4;
            TriangleCount = count * 2;
            BatchCount = 1;
        }

        public void BuildFillMesh(PrototypeRenderCommand[] commands, int count, Mesh mesh)
        {
            BuildToMesh(commands, count, mesh);
        }

        public void BuildToMesh(PrototypeRenderCommand[] commands, int count, Mesh mesh)
        {
            CommandCount = count;
            if (count <= 0)
            {
                mesh.Clear();
                VertexCount = 0;
                TriangleCount = 0;
                BatchCount = 0;
                return;
            }

            var vertexCount = count * 4;
            var indexCount = count * 6;
            EnsureMeshBuffers(vertexCount, indexCount);

            for (var i = 0; i < count; i++)
            {
                var cmd = commands[i];
                var baseVertex = i * 4;
                var x0 = cmd.X;
                var y0 = cmd.Y;
                var x1 = cmd.X + cmd.Width;
                var y1 = cmd.Y + cmd.Height;

                _meshVertices[baseVertex + 0] = new Vector3(x0, y0, 0f);
                _meshVertices[baseVertex + 1] = new Vector3(x0, y1, 0f);
                _meshVertices[baseVertex + 2] = new Vector3(x1, y1, 0f);
                _meshVertices[baseVertex + 3] = new Vector3(x1, y0, 0f);

                _meshColors[baseVertex + 0] = cmd.Color;
                _meshColors[baseVertex + 1] = cmd.Color;
                _meshColors[baseVertex + 2] = cmd.Color;
                _meshColors[baseVertex + 3] = cmd.Color;

                var baseIndex = i * 6;
                _meshIndices[baseIndex + 0] = (ushort)(baseVertex + 0);
                _meshIndices[baseIndex + 1] = (ushort)(baseVertex + 1);
                _meshIndices[baseIndex + 2] = (ushort)(baseVertex + 2);
                _meshIndices[baseIndex + 3] = (ushort)(baseVertex + 0);
                _meshIndices[baseIndex + 4] = (ushort)(baseVertex + 2);
                _meshIndices[baseIndex + 5] = (ushort)(baseVertex + 3);
            }

            mesh.Clear();
            mesh.vertices = _meshVertices;
            mesh.colors32 = _meshColors;
            mesh.SetIndices(_meshIndices, MeshTopology.Triangles, 0, false);

            VertexCount = vertexCount;
            TriangleCount = count * 2;
            BatchCount = 1;
        }

        void EnsureMeshBuffers(int vertexCount, int indexCount)
        {
            if (_meshVertices.Length != vertexCount)
                _meshVertices = new Vector3[vertexCount];
            if (_meshColors.Length != vertexCount)
                _meshColors = new Color32[vertexCount];
            if (_meshIndices.Length != indexCount)
                _meshIndices = new ushort[indexCount];
        }

        void AddRect(VertexHelper vertexHelper, PrototypeRenderCommand cmd)
        {
            var color = (Color32)cmd.Color;
            var x0 = cmd.X;
            var y0 = cmd.Y;
            var x1 = cmd.X + cmd.Width;
            var y1 = cmd.Y + cmd.Height;

            _scratch[0].position = new Vector3(x0, y0, 0f);
            _scratch[0].color = color;
            _scratch[1].position = new Vector3(x0, y1, 0f);
            _scratch[1].color = color;
            _scratch[2].position = new Vector3(x1, y1, 0f);
            _scratch[2].color = color;
            _scratch[3].position = new Vector3(x1, y0, 0f);
            _scratch[3].color = color;

            vertexHelper.AddUIVertexQuad(_scratch);
        }
    }
}
