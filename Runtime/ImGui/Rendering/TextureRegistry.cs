using System.Collections.Generic;
using Basic.ImGui.Layout;
using UnityEngine;

namespace Basic.ImGui.Rendering
{
    public sealed class TextureRegistry : ITextureRegistry
    {
        readonly Dictionary<uint, Texture> _textures = new Dictionary<uint, Texture>();

        public void Register(TextureId textureId, Texture texture)
        {
            if (texture != null)
            {
                _textures[textureId.Value] = texture;
            }
        }

        public bool TryGetTexture(TextureId textureId, out Texture texture) =>
            _textures.TryGetValue(textureId.Value, out texture);
    }
}
