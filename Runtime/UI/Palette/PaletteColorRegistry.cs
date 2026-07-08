using System;
using System.Collections.Generic;

namespace Basic.UI
{
    internal static class PaletteColorRegistry
    {
        private readonly struct RegistryKey : IEquatable<RegistryKey>
        {
            public readonly int PaletteInstanceId;
            public readonly GUID ColorGuid;

            public RegistryKey(int paletteInstanceId, GUID colorGuid)
            {
                PaletteInstanceId = paletteInstanceId;
                ColorGuid = colorGuid;
            }

            public bool Equals(RegistryKey other) =>
                PaletteInstanceId == other.PaletteInstanceId && ColorGuid == other.ColorGuid;

            public override bool Equals(object obj) => obj is RegistryKey other && Equals(other);

            public override int GetHashCode() => HashCode.Combine(PaletteInstanceId, ColorGuid);
        }

        private static readonly Dictionary<RegistryKey, List<BasicImage>> Consumers = new();
        private static readonly Dictionary<BasicImage, RegistryKey> ConsumerKeys = new();

        public static void Register(BasicImage consumer, ColorPalette palette, GUID guid)
        {
            Unregister(consumer);

            if (consumer == null || palette == null || guid == default)
                return;

            var key = new RegistryKey(palette.GetInstanceID(), guid);
            if (!Consumers.TryGetValue(key, out var list))
            {
                list = new List<BasicImage>();
                Consumers[key] = list;
            }

            list.Add(consumer);
            ConsumerKeys[consumer] = key;
        }

        public static void Unregister(BasicImage consumer)
        {
            if (consumer == null || !ConsumerKeys.TryGetValue(consumer, out var key))
                return;

            ConsumerKeys.Remove(consumer);

            if (!Consumers.TryGetValue(key, out var list))
                return;

            list.Remove(consumer);
            if (list.Count == 0)
                Consumers.Remove(key);
        }

        public static void Notify(ColorPalette palette, GUID guid)
        {
            if (palette == null || guid == default)
                return;

            var key = new RegistryKey(palette.GetInstanceID(), guid);
            if (!Consumers.TryGetValue(key, out var list))
                return;

            for (var i = list.Count - 1; i >= 0; i--)
            {
                var consumer = list[i];
                if (consumer != null)
                    consumer.ApplyResolvedColor();
            }
        }
    }
}
