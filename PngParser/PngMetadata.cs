using System.Text;
using System.IO.Hashing;
using System.Buffers.Binary;


namespace PngParser;

public static class PngMetadata
{
    /// <summary>
    /// Encodes a tEXt chunk with the given keyword and content.
    /// </summary>
    public static Chunk TextEncode(string keyword, string content, string chunkName = "tEXt")
    {
        keyword ??= string.Empty;
        content ??= string.Empty;

        if (content.Length > 0 && (!IsLatin1(keyword) || !IsLatin1(content)))
            throw new Exception("Only Latin-1 characters are permitted in PNG tEXt chunks. Consider base64 encoding and/or zEXt compression.");

        if (keyword.Length >= 80)
            throw new Exception($"Keyword \"{keyword}\" exceeds the 79-character limit imposed by the PNG specification.");

        var totalSize = keyword.Length + content.Length + 1;
        var output = new byte[totalSize];
        var idx = 0;

        foreach (var ch in keyword)
        {
            if (ch == 0)
                throw new Exception("0x00 character is not permitted in tEXt keywords.");

            output[idx++] = (byte)ch;
        }

        output[idx++] = 0; // Null separator

        foreach (var ch in content)
        {
            if (ch == 0)
                throw new Exception("0x00 character is not permitted in tEXt content.");

            output[idx++] = (byte)ch;
        }

        return new Chunk { Name = chunkName, Data = output };
    }

    /// <summary>
    /// Decodes a tEXt chunk and returns the keyword and text.
    /// </summary>
    public static (string Keyword, string Text) TextDecode(Chunk chunk)
    {
        var data = chunk.Data;
        var naming = true;
        var keyword = new StringBuilder();
        var text = new StringBuilder();

        foreach (var code in data)
        {
            if (naming)
            {
                if (code != 0)
                    keyword.Append((char)code);
                else
                    naming = false;
            }
            else
            {
                if (code != 0)
                    text.Append((char)code);
                else
                    throw new Exception("Invalid NULL character found. 0x00 character is not permitted in tEXt content.");
            }
        }

        return (keyword.ToString(), text.ToString());
    }

    /// <summary>
    /// Extracts PNG chunks from the given data.
    /// </summary>
    public static List<Chunk> ExtractChunks(byte[] data)
    {
        if (!IsValidPngHeader(data))
            throw new Exception("Invalid .png file header.");

        var chunks = new List<Chunk>();
        var idx = 8;
        var ended = false;

        while (idx < data.Length)
        {
            if (idx + 8 > data.Length)
                throw new Exception("Unexpected end of data while reading chunk length and type.");

            var length = BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(idx));
            idx += 4;

            var name = Encoding.ASCII.GetString(data, idx, 4);
            idx += 4;

            if (name == "IEND")
            {
                ended = true;
                chunks.Add(new Chunk { Name = name, Data = [] });
                break;
            }

            if (idx + length > data.Length)
                throw new Exception("Unexpected end of data while reading chunk data.");

            var chunkData = new byte[length];
            Array.Copy(data, idx, chunkData, 0, length);
            idx += (int)length;

            if (idx + 4 > data.Length)
                throw new Exception("Unexpected end of data while reading chunk CRC.");

            var crcActual = BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(idx));

            idx += 4;

            var crcChunk = new byte[4 + length];
            Encoding.ASCII.GetBytes(name, 0, 4, crcChunk, 0);
            Array.Copy(chunkData, 0, crcChunk, 4, length);

            var crcExpected = Crc32.HashToUInt32(crcChunk);

            if (crcActual != crcExpected)
                throw new Exception($"CRC values for {name} header do not match; PNG file is likely corrupted.");

            chunks.Add(new Chunk { Name = name, Data = chunkData });
        }

        if (!ended)
            throw new Exception(".png file ended prematurely: no IEND header was found.");

        return chunks;
    }

    /// <summary>
    /// Encodes PNG chunks into a byte array.
    /// </summary>
    public static byte[] EncodeChunks(List<Chunk> chunks)
    {
        var totalSize = 8; // PNG header size

        foreach (var chunk in chunks)
            totalSize += 12 + chunk.Data.Length; // Length + Chunk Type + Data + CRC

        var output = new byte[totalSize];
        var idx = 0;

        // Write PNG header
        WritePngHeader(output, ref idx);

        foreach (var chunk in chunks)
        {
            BinaryPrimitives.WriteUInt32BigEndian(output.AsSpan(idx), (uint)chunk.Data.Length);
            //WriteUInt32(output, idx, (uint)chunk.Data.Length);
            idx += 4;

            var nameBytes = Encoding.ASCII.GetBytes(chunk.Name);
            Array.Copy(nameBytes, 0, output, idx, 4);
            idx += 4;

            Array.Copy(chunk.Data, 0, output, idx, chunk.Data.Length);
            idx += chunk.Data.Length;

            var crcData = new byte[4 + chunk.Data.Length];
            Array.Copy(nameBytes, 0, crcData, 0, 4);
            Array.Copy(chunk.Data, 0, crcData, 4, chunk.Data.Length);
            var crc = Crc32.HashToUInt32(crcData);

            BinaryPrimitives.WriteUInt32BigEndian(output.AsSpan(idx), crc);
            //WriteUInt32(output, idx, crc);
            idx += 4;
        }

        return output;
    }

    /// <summary>
    /// Reads PNG metadata (tEXt and pHYs chunks).
    /// </summary>
    public static (Dictionary<string, string> TextData, PhysChunkData? PhysData) ReadMetadata(byte[] buffer)
    {
        var textData = new Dictionary<string, string>();
        PhysChunkData? physData = null;
        var chunks = ExtractChunks(buffer);

        foreach (var chunk in chunks)
        {
            switch (chunk.Name)
            {
                case "tEXt":
                    var (keyword, text) = TextDecode(chunk);
                    textData[keyword] = text;
                    break;
                case "pHYs":
                    physData = new PhysChunkData
                    {
                        X = BinaryPrimitives.ReadUInt32BigEndian(chunk.Data.AsSpan(0)),
                        Y = BinaryPrimitives.ReadUInt32BigEndian(chunk.Data.AsSpan(4)),
                        Unit = chunk.Data[8]
                    };
                    break;
            }
        }

        return (textData, physData);
    }

    /// <summary>
    /// Writes PNG metadata into a buffer.
    /// </summary>
    public static byte[] WriteMetadata(byte[] buffer, Dictionary<string, string>? textData = null, PhysChunkData? physData = null, bool clearMetadata = false)
    {
        var chunks = ExtractChunks(buffer);
        InsertMetadata(chunks, textData, physData, clearMetadata);
        return EncodeChunks(chunks);
    }

    /// <summary>
    /// Inserts metadata into the list of PNG chunks.
    /// </summary>
    public static void InsertMetadata(List<Chunk> chunks, Dictionary<string, string>? textData, PhysChunkData? physData, bool clearMetadata)
    {
        if (clearMetadata)
            chunks.RemoveAll(chunk => chunk.Name != "IHDR" && chunk.Name != "IDAT" && chunk.Name != "IEND");

        if (textData != null)
        {
            foreach (var kvp in textData)
            {
                var textChunk = TextEncode(kvp.Key, kvp.Value);
                var iendIndex = chunks.FindIndex(chunk => chunk.Name == "IEND");
                if (iendIndex >= 0)
                    chunks.Insert(iendIndex, textChunk);
                else
                    chunks.Add(textChunk); // If IEND not found, append at the end
            }
        }

        if (physData != null)
        {
            var data = new byte[9];
            BinaryPrimitives.WriteUInt32BigEndian(data.AsSpan(0), physData.X);
            BinaryPrimitives.WriteUInt32BigEndian(data.AsSpan(4), physData.Y);
            data[8] = physData.Unit;

            var existingIndex = chunks.FindIndex(chunk => chunk.Name == "pHYs");
            var physChunk = new Chunk { Name = "pHYs", Data = data };

            if (existingIndex >= 0)
                chunks[existingIndex] = physChunk;
            else
            {
                var ihdrIndex = chunks.FindIndex(chunk => chunk.Name == "IHDR");
                if (ihdrIndex >= 0)
                    chunks.Insert(ihdrIndex + 1, physChunk); // After IHDR
                else
                    chunks.Insert(0, physChunk); // If IHDR not found, insert at the beginning
            }
        }
    }

    #region Helper Methods

    private static bool IsLatin1(string s) => s.All(c => c <= 0xFF);

    private static bool IsValidPngHeader(byte[] data)
    {
        var pngSignature = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
        return data.Length >= 8 && data.Take(8).SequenceEqual(pngSignature);
    }

    private static void WritePngHeader(byte[] output, ref int idx)
    {
        var pngHeader = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
        Array.Copy(pngHeader, 0, output, idx, pngHeader.Length);
        idx += pngHeader.Length;
    }
    #endregion
}

