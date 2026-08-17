using Basic.ImGui.Layout;
using UnityEngine;

namespace Basic.ImGui.Rendering
{
    public interface ITextureRegistry
    {
        bool TryGetTexture(TextureId textureId, out Texture texture);
    }
}
