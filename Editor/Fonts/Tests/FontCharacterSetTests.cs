using System.Collections.Generic;
using NUnit.Framework;

namespace Basic.UnityEditorTools.Tests
{
    public class FontCharacterSetTests
    {
        [Test]
        public void IsSupportedFontFilePath_AcceptsTtfAndOtf_RejectsOthers()
        {
            Assert.That(FontCharacterSet.IsSupportedFontFilePath("Fonts/A.ttf"), Is.True);
            Assert.That(FontCharacterSet.IsSupportedFontFilePath("Fonts/A.TTF"), Is.True);
            Assert.That(FontCharacterSet.IsSupportedFontFilePath("Fonts/A.otf"), Is.True);
            Assert.That(FontCharacterSet.IsSupportedFontFilePath("Fonts/A.ttc"), Is.False);
            Assert.That(FontCharacterSet.IsSupportedFontFilePath("Fonts/A.png"), Is.False);
            Assert.That(FontCharacterSet.IsSupportedFontFilePath(""), Is.False);
            Assert.That(FontCharacterSet.IsSupportedFontFilePath(null), Is.False);
        }

        [Test]
        public void BuildDumpString_DropsNull_Sorts_Dedupes()
        {
            var codepoints = new uint[] { 0x42, 0x00, 0x41, 0x41, 0x20 };
            Assert.That(FontCharacterSet.BuildDumpString(codepoints), Is.EqualTo(" AB"));
        }

        [Test]
        public void BuildDumpString_SupportsSupplementaryPlaneAsUtf16()
        {
            // U+1F600 😀
            var dump = FontCharacterSet.BuildDumpString(new uint[] { 0x1F600 });
            Assert.That(dump, Is.EqualTo(char.ConvertFromUtf32(0x1F600)));
        }

        [Test]
        public void DumpFromFontData_ReadsCmap_DropsNull_Ascending()
        {
            var fontData = MinimalSfntFont.WithFormat4Codepoints(0x0000, 0x0020, 0x0041, 0x0042);
            Assert.That(FontCharacterSet.DumpFromFontData(fontData), Is.EqualTo(" AB"));
        }

        [Test]
        public void DumpFromFontData_UnionsFormat12Supplementary()
        {
            var fontData = MinimalSfntFont.WithFormat12Codepoints(0x1F600);
            Assert.That(FontCharacterSet.DumpFromFontData(fontData), Is.EqualTo(char.ConvertFromUtf32(0x1F600)));
        }
    }

    /// <summary>
    /// Tiny sfnt container with only a cmap table — enough for <see cref="FontCharacterSet"/> tests.
    /// </summary>
    internal static class MinimalSfntFont
    {
        public static byte[] WithFormat4Codepoints(params uint[] codepoints)
        {
            var cmap = BuildFormat4Cmap(codepoints);
            return BuildSfntWithCmap(cmap);
        }

        public static byte[] WithFormat12Codepoints(params uint[] codepoints)
        {
            var cmap = BuildFormat12Cmap(codepoints);
            return BuildSfntWithCmap(cmap);
        }

        private static byte[] BuildSfntWithCmap(byte[] cmapBody)
        {
            const int offsetTableSize = 12;
            const int tableRecordSize = 16;
            const int numTables = 1;
            var cmapOffset = offsetTableSize + tableRecordSize * numTables;

            var bytes = new List<byte>(cmapOffset + cmapBody.Length);
            WriteUInt32(bytes, 0x00010000); // scaler type
            WriteUInt16(bytes, numTables);
            WriteUInt16(bytes, 16); // searchRange
            WriteUInt16(bytes, 0); // entrySelector
            WriteUInt16(bytes, 0); // rangeShift

            WriteUInt32(bytes, 0x636D6170); // 'cmap'
            WriteUInt32(bytes, 0); // checksum
            WriteUInt32(bytes, (uint)cmapOffset);
            WriteUInt32(bytes, (uint)cmapBody.Length);
            bytes.AddRange(cmapBody);
            return bytes.ToArray();
        }

        private static byte[] BuildFormat4Cmap(uint[] codepoints)
        {
            // One segment per BMP codepoint, plus required 0xFFFF sentinel segment.
            var sorted = new SortedSet<ushort>();
            foreach (var cp in codepoints)
            {
                if (cp > 0xFFFF)
                    continue;
                sorted.Add((ushort)cp);
            }

            var segCount = sorted.Count + 1;
            var endCodes = new List<ushort>();
            var startCodes = new List<ushort>();
            var idDeltas = new List<ushort>();
            ushort glyphId = 1;
            foreach (var cp in sorted)
            {
                endCodes.Add(cp);
                startCodes.Add(cp);
                idDeltas.Add(UncheckedUShort(glyphId - cp));
                glyphId++;
            }

            endCodes.Add(0xFFFF);
            startCodes.Add(0xFFFF);
            idDeltas.Add(1); // (0xFFFF + 1) & 0xFFFF == 0 → missing glyph sentinel

            var idRangeOffsets = new ushort[segCount]; // all zero → use idDelta

            var subtable = new List<byte>();
            WriteUInt16(subtable, 4); // format
            var lengthPos = subtable.Count;
            WriteUInt16(subtable, 0); // length placeholder
            WriteUInt16(subtable, 0); // language
            WriteUInt16(subtable, (ushort)(segCount * 2));
            WriteUInt16(subtable, 2); // searchRange (unused by our reader)
            WriteUInt16(subtable, 0);
            WriteUInt16(subtable, (ushort)(segCount * 2 - 2));

            foreach (var v in endCodes)
                WriteUInt16(subtable, v);
            WriteUInt16(subtable, 0); // reservedPad
            foreach (var v in startCodes)
                WriteUInt16(subtable, v);
            foreach (var v in idDeltas)
                WriteUInt16(subtable, v);
            foreach (var v in idRangeOffsets)
                WriteUInt16(subtable, v);

            SetUInt16(subtable, lengthPos, (ushort)subtable.Count);

            return WrapCmapEncodingRecords(subtable.ToArray(), platformId: 3, encodingId: 1);
        }

        private static byte[] BuildFormat12Cmap(uint[] codepoints)
        {
            var sorted = new SortedSet<uint>(codepoints);
            var groups = new List<(uint start, uint end, uint startGlyph)>();
            uint glyphId = 1;
            foreach (var cp in sorted)
            {
                groups.Add((cp, cp, glyphId));
                glyphId++;
            }

            var subtable = new List<byte>();
            WriteUInt16(subtable, 12);
            WriteUInt16(subtable, 0); // reserved
            var lengthPos = subtable.Count;
            WriteUInt32(subtable, 0); // length placeholder
            WriteUInt32(subtable, 0); // language
            WriteUInt32(subtable, (uint)groups.Count);
            foreach (var g in groups)
            {
                WriteUInt32(subtable, g.start);
                WriteUInt32(subtable, g.end);
                WriteUInt32(subtable, g.startGlyph);
            }

            SetUInt32(subtable, lengthPos, (uint)subtable.Count);

            return WrapCmapEncodingRecords(subtable.ToArray(), platformId: 3, encodingId: 10);
        }

        private static byte[] WrapCmapEncodingRecords(byte[] subtable, ushort platformId, ushort encodingId)
        {
            const int headerSize = 4;
            const int recordSize = 8;
            var encodingOffset = headerSize + recordSize;

            var cmap = new List<byte>();
            WriteUInt16(cmap, 0); // version
            WriteUInt16(cmap, 1); // numTables
            WriteUInt16(cmap, platformId);
            WriteUInt16(cmap, encodingId);
            WriteUInt32(cmap, (uint)encodingOffset);
            cmap.AddRange(subtable);
            return cmap.ToArray();
        }

        private static ushort UncheckedUShort(int value) => (ushort)(value & 0xFFFF);

        private static void WriteUInt16(List<byte> bytes, ushort value)
        {
            bytes.Add((byte)(value >> 8));
            bytes.Add((byte)(value & 0xFF));
        }

        private static void WriteUInt32(List<byte> bytes, uint value)
        {
            bytes.Add((byte)((value >> 24) & 0xFF));
            bytes.Add((byte)((value >> 16) & 0xFF));
            bytes.Add((byte)((value >> 8) & 0xFF));
            bytes.Add((byte)(value & 0xFF));
        }

        private static void SetUInt16(List<byte> bytes, int index, ushort value)
        {
            bytes[index] = (byte)(value >> 8);
            bytes[index + 1] = (byte)(value & 0xFF);
        }

        private static void SetUInt32(List<byte> bytes, int index, uint value)
        {
            bytes[index] = (byte)((value >> 24) & 0xFF);
            bytes[index + 1] = (byte)((value >> 16) & 0xFF);
            bytes[index + 2] = (byte)((value >> 8) & 0xFF);
            bytes[index + 3] = (byte)(value & 0xFF);
        }
    }
}
