using System;
using System.Linq;
using System.Buffers.Binary;
using System.IO.Compression;
using System.Text;

namespace PngParser
{
    public static class PngUtilities
    {
        /// <summary>
        /// Encodes tEXt chunk data.
        /// </summary>
        public static byte[] TextEncodeData(string keyword, string text)
        {
            keyword = keyword ?? string.Empty;
            text = text ?? string.Empty;

            if (keyword.Length == 0 || keyword.Length > 79)
                throw new ArgumentException("Keyword must be between 1 and 79 characters long.");

            if (keyword.Contains('\0') || text.Contains('\0'))
                throw new ArgumentException("Keyword and text cannot contain null characters.");

            byte[] keywordBytes = Encoding.ASCII.GetBytes(keyword);
            byte[] textBytes = Encoding.ASCII.GetBytes(text);

            byte[] data = new byte[keywordBytes.Length + 1 + textBytes.Length];
            Array.Copy(keywordBytes, 0, data, 0, keywordBytes.Length);
            data[keywordBytes.Length] = 0; // Null separator
            Array.Copy(textBytes, 0, data, keywordBytes.Length + 1, textBytes.Length);

            return data;
        }

        /// <summary>
        /// Decodes tEXt chunk data.
        /// </summary>
        public static (string Keyword, string Text) TextDecode(Chunk chunk)
        {
            var data = chunk.Data;
            int idx = 0;

            // Find null separator
            while (idx < data.Length && data[idx] != 0)
                idx++;

            if (idx >= data.Length)
                throw new Exception("Invalid tEXt chunk data.");

            string keyword = Encoding.ASCII.GetString(data, 0, idx);
            string text = Encoding.ASCII.GetString(data, idx + 1, data.Length - idx - 1);

            return (keyword, text);
        }

        /// <summary>
        /// Encodes pHYs chunk data.
        /// </summary>
        public static byte[] PhysEncodeData(uint xPixelsPerUnit, uint yPixelsPerUnit, byte unitSpecifier)
        {
            var data = new byte[9];
            BinaryPrimitives.WriteUInt32BigEndian(data.AsSpan(0, 4), xPixelsPerUnit);
            BinaryPrimitives.WriteUInt32BigEndian(data.AsSpan(4, 4), yPixelsPerUnit);
            data[8] = unitSpecifier;
            return data;
        }

        /// <summary>
        /// Decodes pHYs chunk data.
        /// </summary>
        public static PhysChunkData PhysDecodeData(Chunk chunk)
        {
            var data = chunk.Data;
            if (data.Length != 9)
                throw new Exception("Invalid pHYs chunk data.");

            var x = BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(0, 4));
            var y = BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(4, 4));
            var unit = data[8];

            return new PhysChunkData { X = x, Y = y, Unit = unit };
        }
        
        
        /// <summary>
        /// Encodes iTXt chunk data.
        /// </summary>
        public static byte[] ITXtEncodeData(string keyword, string text, bool compressed = false, string languageTag = "", string translatedKeyword = "")
        {
            if (string.IsNullOrEmpty(keyword))
                throw new ArgumentException("Keyword must not be empty.");

            if (keyword.Contains('\0'))
                throw new ArgumentException("Keyword cannot contain null characters.");

            byte compressionFlag = compressed ? (byte)1 : (byte)0;
            byte compressionMethod = 0; // Deflate

            byte[] keywordBytes = Encoding.UTF8.GetBytes(keyword);
            byte[] languageTagBytes = Encoding.ASCII.GetBytes(languageTag);
            byte[] translatedKeywordBytes = Encoding.UTF8.GetBytes(translatedKeyword);
            byte[] textBytes = Encoding.UTF8.GetBytes(text);

            if (compressed)
            {
                textBytes = CompressData(textBytes);
            }

            // Build the data
            using (var ms = new System.IO.MemoryStream())
            {
                ms.Write(keywordBytes, 0, keywordBytes.Length);
                ms.WriteByte(0); // Null separator
                ms.WriteByte(compressionFlag);
                ms.WriteByte(compressionMethod);
                ms.Write(languageTagBytes, 0, languageTagBytes.Length);
                ms.WriteByte(0); // Null separator
                ms.Write(translatedKeywordBytes, 0, translatedKeywordBytes.Length);
                ms.WriteByte(0); // Null separator
                ms.Write(textBytes, 0, textBytes.Length);

                return ms.ToArray();
            }
        }

        /// <summary>
        /// Decodes iTXt chunk data.
        /// </summary>
        public static (string Keyword, bool Compressed, string LanguageTag, string TranslatedKeyword, string Text) ITXtDecode(Chunk chunk)
        {
            var data = chunk.Data;
            int idx = 0;

            // Read keyword
            var keyword = ReadString(data, ref idx, Encoding.UTF8);
            if (idx >= data.Length)
                throw new Exception("Invalid iTXt chunk data.");

            // Read compression flag
            byte compressionFlag = data[idx++];
            bool compressed = compressionFlag != 0;

            // Read compression method
            byte compressionMethod = data[idx++];
            if (compressionMethod != 0)
                throw new Exception("Unsupported compression method in iTXt chunk.");

            // Read language tag
            var languageTag = ReadString(data, ref idx, Encoding.ASCII);

            // Read translated keyword
            var translatedKeyword = ReadString(data, ref idx, Encoding.UTF8);

            // Read text
            var textBytes = new byte[data.Length - idx];
            Array.Copy(data, idx, textBytes, 0, textBytes.Length);

            if (compressed)
            {
                textBytes = DecompressData(textBytes);
            }

            var text = Encoding.UTF8.GetString(textBytes);

            return (keyword, compressed, languageTag, translatedKeyword, text);
        }

        /// <summary>
        /// Encodes zTXt chunk data.
        /// </summary>
        public static byte[] ZTxtEncodeData(string keyword, string text)
        {
            if (string.IsNullOrEmpty(keyword))
                throw new ArgumentException("Keyword must not be empty.");

            if (keyword.Contains('\0'))
                throw new ArgumentException("Keyword cannot contain null characters.");

            byte[] keywordBytes = Encoding.ASCII.GetBytes(keyword);
            byte compressionMethod = 0; // Deflate

            byte[] textBytes = Encoding.ASCII.GetBytes(text);
            textBytes = CompressData(textBytes);

            // Build the data
            using (var ms = new System.IO.MemoryStream())
            {
                ms.Write(keywordBytes, 0, keywordBytes.Length);
                ms.WriteByte(0); // Null separator
                ms.WriteByte(compressionMethod);
                ms.Write(textBytes, 0, textBytes.Length);

                return ms.ToArray();
            }
        }

        /// <summary>
        /// Decodes zTXt chunk data.
        /// </summary>
        public static (string Keyword, string Text) ZTxtDecode(Chunk chunk)
        {
            var data = chunk.Data;
            int idx = 0;

            // Read keyword
            var keyword = ReadString(data, ref idx, Encoding.ASCII);
            if (idx >= data.Length)
                throw new Exception("Invalid zTXt chunk data.");

            // Read compression method
            byte compressionMethod = data[idx++];
            if (compressionMethod != 0)
                throw new Exception("Unsupported compression method in zTXt chunk.");

            // Read compressed text
            var compressedTextBytes = new byte[data.Length - idx];
            Array.Copy(data, idx, compressedTextBytes, 0, compressedTextBytes.Length);

            var textBytes = DecompressData(compressedTextBytes);
            var text = Encoding.ASCII.GetString(textBytes);

            return (keyword, text);
        }

        // Helper methods
        private static string ReadString(byte[] data, ref int idx, Encoding encoding)
        {
            int startIdx = idx;
            while (idx < data.Length && data[idx] != 0)
                idx++;

            if (idx >= data.Length)
                throw new Exception("Invalid chunk data: missing null terminator.");

            var str = encoding.GetString(data, startIdx, idx - startIdx);
            idx++; // Skip null terminator
            return str;
        }

        private static byte[] CompressData(byte[] data)
        {
            using (var ms = new System.IO.MemoryStream())
            {
                using (var deflateStream = new DeflateStream(ms, CompressionLevel.Optimal, leaveOpen: true))
                {
                    deflateStream.Write(data, 0, data.Length);
                }
                return ms.ToArray();
            }
        }

        private static byte[] DecompressData(byte[] data)
        {
            using (var compressedStream = new System.IO.MemoryStream(data))
            using (var deflateStream = new DeflateStream(compressedStream, CompressionMode.Decompress))
            using (var resultStream = new System.IO.MemoryStream())
            {
                deflateStream.CopyTo(resultStream);
                return resultStream.ToArray();
            }
        }
    }
}

