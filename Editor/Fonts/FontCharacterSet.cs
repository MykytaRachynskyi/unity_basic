using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Basic.UnityEditorTools
{
    /// <summary>
    /// Reads Unicode coverage from .ttf/.otf cmap tables and builds a TMP-pasteable character dump.
    /// </summary>
    public static class FontCharacterSet
    {
        public static bool IsSupportedFontFilePath(string path)
        {
            if (string.IsNullOrEmpty(path))
                return false;

            var extension = Path.GetExtension(path);
            return extension.Equals(".ttf", StringComparison.OrdinalIgnoreCase)
                   || extension.Equals(".otf", StringComparison.OrdinalIgnoreCase);
        }

        public static string BuildDumpString(IEnumerable<uint> codepoints)
        {
            if (codepoints == null)
                return string.Empty;

            var unique = new SortedSet<uint>();
            foreach (var codepoint in codepoints)
            {
                if (codepoint == 0)
                    continue;
                unique.Add(codepoint);
            }

            var builder = new StringBuilder(unique.Count);
            foreach (var codepoint in unique)
            {
                if (codepoint > 0x10FFFF || (codepoint >= 0xD800 && codepoint <= 0xDFFF))
                    continue;

                builder.Append(char.ConvertFromUtf32((int)codepoint));
            }

            return builder.ToString();
        }

        public static string DumpFromFontData(byte[] fontData)
        {
            return DumpFromFontData(fontData, out _);
        }

        public static string DumpFromFontData(byte[] fontData, out int characterCount)
        {
            var dump = BuildDumpString(ReadUnicodeCodepoints(fontData));
            characterCount = CountCodepoints(dump);
            return dump;
        }

        public static int CountCodepoints(string value)
        {
            if (string.IsNullOrEmpty(value))
                return 0;

            var count = 0;
            for (var i = 0; i < value.Length; i++)
            {
                count++;
                if (char.IsHighSurrogate(value[i]) && i + 1 < value.Length && char.IsLowSurrogate(value[i + 1]))
                    i++;
            }

            return count;
        }

        public static IReadOnlyList<uint> ReadUnicodeCodepoints(byte[] fontData)
        {
            if (fontData == null || fontData.Length < 12)
                throw new InvalidDataException("Font data is empty or too small to be an sfnt font.");

            var cmapOffset = FindTableOffset(fontData, 0x636D6170); // 'cmap'
            if (cmapOffset < 0)
                throw new InvalidDataException("Font has no cmap table.");

            var codepoints = new HashSet<uint>();
            ReadCmapTable(fontData, cmapOffset, codepoints);

            var sorted = new List<uint>(codepoints.Count);
            foreach (var codepoint in codepoints)
                sorted.Add(codepoint);
            sorted.Sort();
            return sorted;
        }

        private static int FindTableOffset(byte[] data, uint tag)
        {
            var numTables = ReadUInt16(data, 4);
            const int tableDirectoryStart = 12;
            for (var i = 0; i < numTables; i++)
            {
                var recordOffset = tableDirectoryStart + i * 16;
                if (recordOffset + 16 > data.Length)
                    break;

                var tableTag = ReadUInt32(data, recordOffset);
                if (tableTag != tag)
                    continue;

                return (int)ReadUInt32(data, recordOffset + 8);
            }

            return -1;
        }

        private static void ReadCmapTable(byte[] data, int cmapOffset, HashSet<uint> codepoints)
        {
            if (cmapOffset + 4 > data.Length)
                throw new InvalidDataException("cmap table is truncated.");

            var numTables = ReadUInt16(data, cmapOffset + 2);
            for (var i = 0; i < numTables; i++)
            {
                var recordOffset = cmapOffset + 4 + i * 8;
                if (recordOffset + 8 > data.Length)
                    break;

                var platformId = ReadUInt16(data, recordOffset);
                var encodingId = ReadUInt16(data, recordOffset + 2);
                if (!IsUnicodeEncoding(platformId, encodingId))
                    continue;

                var subtableOffset = cmapOffset + (int)ReadUInt32(data, recordOffset + 4);
                ReadCmapSubtable(data, subtableOffset, codepoints);
            }
        }

        private static bool IsUnicodeEncoding(ushort platformId, ushort encodingId)
        {
            if (platformId == 0)
                return true;

            // Windows Unicode BMP / full repertoire
            return platformId == 3 && (encodingId == 1 || encodingId == 10);
        }

        private static void ReadCmapSubtable(byte[] data, int offset, HashSet<uint> codepoints)
        {
            if (offset + 2 > data.Length)
                return;

            var format = ReadUInt16(data, offset);
            switch (format)
            {
                case 4:
                    ReadFormat4(data, offset, codepoints);
                    break;
                case 12:
                    ReadFormat12(data, offset, codepoints);
                    break;
            }
        }

        private static void ReadFormat4(byte[] data, int offset, HashSet<uint> codepoints)
        {
            if (offset + 14 > data.Length)
                return;

            var length = ReadUInt16(data, offset + 2);
            if (length < 14 || offset + length > data.Length)
                return;

            var segCountX2 = ReadUInt16(data, offset + 6);
            var segCount = segCountX2 / 2;
            var endCodesOffset = offset + 14;
            var startCodesOffset = endCodesOffset + segCountX2 + 2; // + reservedPad
            var idDeltasOffset = startCodesOffset + segCountX2;
            var idRangeOffsetsOffset = idDeltasOffset + segCountX2;

            if (idRangeOffsetsOffset + segCountX2 > offset + length)
                return;

            for (var i = 0; i < segCount; i++)
            {
                var endCode = ReadUInt16(data, endCodesOffset + i * 2);
                var startCode = ReadUInt16(data, startCodesOffset + i * 2);
                var idDelta = ReadUInt16(data, idDeltasOffset + i * 2);
                var idRangeOffset = ReadUInt16(data, idRangeOffsetsOffset + i * 2);

                for (var code = startCode; code <= endCode; code++)
                {
                    ushort glyphId;
                    if (idRangeOffset == 0)
                    {
                        glyphId = (ushort)((code + idDelta) & 0xFFFF);
                    }
                    else
                    {
                        // glyphId = *(idRangeOffset/2 + (code - startCode) + &idRangeOffset)
                        var glyphIndexOffset = idRangeOffsetsOffset
                                               + i * 2
                                               + idRangeOffset
                                               + (code - startCode) * 2;
                        if (glyphIndexOffset + 2 > offset + length)
                            break;

                        glyphId = ReadUInt16(data, glyphIndexOffset);
                        if (glyphId != 0)
                            glyphId = (ushort)((glyphId + idDelta) & 0xFFFF);
                    }

                    if (glyphId != 0)
                        codepoints.Add(code);

                    if (code == 0xFFFF)
                        break;
                }
            }
        }

        private static void ReadFormat12(byte[] data, int offset, HashSet<uint> codepoints)
        {
            if (offset + 16 > data.Length)
                return;

            var length = ReadUInt32(data, offset + 4);
            if (length < 16 || offset + (int)length > data.Length)
                return;

            var numGroups = ReadUInt32(data, offset + 12);
            var groupsOffset = offset + 16;
            for (var i = 0; i < numGroups; i++)
            {
                var groupOffset = groupsOffset + i * 12;
                if (groupOffset + 12 > offset + (int)length)
                    break;

                var startCharCode = ReadUInt32(data, groupOffset);
                var endCharCode = ReadUInt32(data, groupOffset + 4);
                var startGlyphId = ReadUInt32(data, groupOffset + 8);

                for (var code = startCharCode; code <= endCharCode; code++)
                {
                    var glyphId = startGlyphId + (code - startCharCode);
                    if (glyphId != 0)
                        codepoints.Add(code);

                    if (code == uint.MaxValue)
                        break;
                }
            }
        }

        private static ushort ReadUInt16(byte[] data, int offset)
        {
            return (ushort)((data[offset] << 8) | data[offset + 1]);
        }

        private static uint ReadUInt32(byte[] data, int offset)
        {
            return ((uint)data[offset] << 24)
                   | ((uint)data[offset + 1] << 16)
                   | ((uint)data[offset + 2] << 8)
                   | data[offset + 3];
        }
    }
}
