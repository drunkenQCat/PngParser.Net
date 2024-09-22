using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;


using System.Buffers.Binary;
using System.IO.Hashing;
using System.Text;

namespace PngParser
{
    public static class PngMetadata
    {
        /// <summary>
        /// Reads all chunks from the PNG data.
        /// </summary>
        public static List<Chunk> ReadChunks(byte[] buffer)
        {
            return ExtractChunks(buffer);
        }

        /// <summary>
        /// Writes the list of chunks back into PNG data.
        /// </summary>
        public static byte[] WriteChunks(List<Chunk> chunks)
        {
            return EncodeChunks(chunks);
        }
        
        /// <summary>
        /// Adds or updates textual chunks (tEXt, iTXt, zTXt) based on the given dictionary.
        /// </summary>
        /// <param name="chunks">List of existing chunks.</param>
        /// <param name="metadata">Dictionary where keys are keywords and values are the corresponding text.</param>
        /// <param name="chunkType">The type of textual chunk to write ("tEXt", "iTXt", or "zTXt").</param>
        public static void AddOrUpdateTextChunksFromDictionary(List<Chunk> chunks, Dictionary<string, string> metadata, string chunkType = "tEXt")
        {
            if (metadata == null || !metadata.Any())
                throw new ArgumentException("Metadata dictionary is empty or null.");

            foreach (var kvp in metadata)
            {
                string keyword = kvp.Key;
                string text = kvp.Value;

                Chunk textChunk;

                // Create the chunk based on the chunk type
                switch (chunkType)
                {
                    case "iTXt":
                        textChunk = new Chunk
                        {
                            Name = "iTXt",
                            Data = PngUtilities.ITXtEncodeData(keyword, text)
                        };
                        break;
                    case "zTXt":
                        textChunk = new Chunk
                        {
                            Name = "zTXt",
                            Data = PngUtilities.ZTxtEncodeData(keyword, text)
                        };
                        break;
                    case "tEXt":
                    default:
                        textChunk = new Chunk
                        {
                            Name = "tEXt",
                            Data = PngUtilities.TextEncodeData(keyword, text)
                        };
                        break;
                }

                // Add or update the textual chunk
                AddOrUpdateTextChunk(chunks, textChunk);
            }
        }
        
        /// <summary>
        /// Adds or updates a textual chunk (tEXt, iTXt, zTXt) based on the keyword.
        /// </summary>
        public static void AddOrUpdateTextChunk(List<Chunk> chunks, Chunk newChunk)
        {
            if (!IsTextChunk(newChunk.Name))
                throw new InvalidOperationException($"AddOrUpdateTextChunk method is only for textual chunks (tEXt, iTXt, zTXt).");

            // Extract the keyword from the newChunk
            var newKeyword = ExtractKeyword(newChunk);

            // Find existing textual chunk with the same keyword
            for (int i = 0; i < chunks.Count; i++)
            {
                var chunk = chunks[i];
                if (IsTextChunk(chunk.Name))
                {
                    var keyword = ExtractKeyword(chunk);
                    if (keyword == newKeyword)
                    {
                        // Replace the existing chunk
                        chunks[i] = newChunk;
                        return;
                    }
                }
            }

            // If not found, add the new textual chunk
            AddChunk(chunks, newChunk);
        }

        /// <summary>
        /// Adds a multi-instance chunk to the list.
        /// </summary>
        public static void AddChunk(List<Chunk> chunks, Chunk newChunk)
        {
            if (!IsMultiInstanceChunk(newChunk.Name))
                throw new InvalidOperationException($"Use InsertOrReplaceChunk method for single-instance chunks like {newChunk.Name}.");

            // Insert before IEND chunk
            var iendIndex = chunks.FindIndex(c => c.Name == "IEND");
            if (iendIndex >= 0)
                chunks.Insert(iendIndex, newChunk);
            else
                chunks.Add(newChunk); // If IEND not found, append at the end
        }
        
        /// <summary>
        /// Inserts or replaces a single-instance chunk in the list.
        /// </summary>
        public static void InsertOrReplaceChunk(List<Chunk> chunks, Chunk newChunk)
        {
            if (IsMultiInstanceChunk(newChunk.Name))
                throw new InvalidOperationException($"Use AddOrUpdateTextChunk or AddChunk method for multi-instance chunks like {newChunk.Name}.");

            var index = chunks.FindIndex(c => c.Name == newChunk.Name);

            if (index >= 0)
            {
                // Replace existing chunk
                chunks[index] = newChunk;
            }
            else
            {
                // Insert after IHDR and before IDAT or before IEND if IDAT not found
                InsertChunkAfterIHDR(chunks, newChunk);
            }
        }
        
        
        /// <summary>
        /// Determines if a chunk type is allowed to have multiple instances.
        /// </summary>
        private static bool IsMultiInstanceChunk(string chunkName)
        {
            return chunkName == "tEXt" || chunkName == "iTXt" || chunkName == "zTXt";
            // Add other multi-instance chunk types if needed
        }

        /// <summary>
        /// Determines if a chunk is a textual chunk (tEXt, iTXt, zTXt).
        /// </summary>
        private static bool IsTextChunk(string chunkName)
        {
            return chunkName == "tEXt" || chunkName == "iTXt" || chunkName == "zTXt";
        }

        /// <summary>
        /// Extracts the keyword from a textual chunk.
        /// </summary>
        private static string ExtractKeyword(Chunk chunk)
        {
            switch (chunk.Name)
            {
                case "tEXt":
                    var (keyword, _) = PngUtilities.TextDecode(chunk);
                    return keyword;

                case "iTXt":
                    var (iKeyword, _, _, _, _) = PngUtilities.ITXtDecode(chunk);
                    return iKeyword;

                case "zTXt":
                    var (zKeyword, _) = PngUtilities.ZTxtDecode(chunk);
                    return zKeyword;

                default:
                    throw new InvalidOperationException($"Cannot extract keyword from non-textual chunk {chunk.Name}.");
            }
        }

        /// <summary>
        /// Inserts a chunk after the IHDR chunk.
        /// </summary>
        private static void InsertChunkAfterIHDR(List<Chunk> chunks, Chunk newChunk)
        {
            var ihdrIndex = chunks.FindIndex(c => c.Name == "IHDR");
            if (ihdrIndex >= 0)
            {
                chunks.Insert(ihdrIndex + 1, newChunk);
            }
            else
            {
                // If IHDR is not found, insert at the beginning
                chunks.Insert(0, newChunk);
            }
        }

        /// <summary>
        /// Reads all textual chunks (tEXt, iTXt, zTXt) as a dictionary.
        /// </summary>
        /// <param name="chunks">List of PNG chunks.</param>
        /// <returns>Dictionary where keys are keywords and values are the corresponding text.</returns>
        public static Dictionary<string, string> ReadTextChunksAsDictionary(List<Chunk> chunks)
        {
            var metadata = new Dictionary<string, string>();

            foreach (var chunk in chunks.Where(chunk => IsTextChunk(chunk.Name)))
            {
                string keyword;
                string text;

                switch (chunk.Name)
                {
                    case "tEXt":
                        (keyword, text) = PngUtilities.TextDecode(chunk);
                        break;
                    case "iTXt":
                        (keyword, _, _, _, text) = PngUtilities.ITXtDecode(chunk);
                        break;
                    case "zTXt":
                        (keyword, text) = PngUtilities.ZTxtDecode(chunk);
                        break;
                    default:
                        continue;
                }

                // Add the keyword and text to the dictionary
                metadata[keyword] = text;
            }

            return metadata;
        }
        /// <summary>
        /// Removes a chunk from the list by name.
        /// </summary>
        public static void RemoveChunk(List<Chunk> chunks, string chunkName)
        {
            chunks.RemoveAll(c => c.Name == chunkName);
        }

        /// <summary>
        /// Extracts PNG chunks from the given data.
        /// </summary>
        private static List<Chunk> ExtractChunks(byte[] data)
        {
            if (!IsValidPngHeader(data))
                throw new Exception("Invalid PNG file header.");

            var chunks = new List<Chunk>();
            var idx = 8; // Skip PNG signature

            while (idx < data.Length)
            {
                if (idx + 8 > data.Length)
                    throw new Exception("Unexpected end of data while reading chunk length and type.");

                // Read chunk length (4 bytes, big-endian)
                var length = BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(idx));
                idx += 4;

                // Read chunk type/name (4 bytes)
                var name = Encoding.ASCII.GetString(data, idx, 4);
                idx += 4;

                // Read chunk data
                if (idx + length > data.Length)
                    throw new Exception($"Unexpected end of data while reading chunk {name}.");

                var chunkData = data.AsSpan(idx, (int)length).ToArray();
                idx += (int)length;

                // Read CRC (4 bytes)
                if (idx + 4 > data.Length)
                    throw new Exception("Unexpected end of data while reading chunk CRC.");

                var crcActual = BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(idx));
                idx += 4;

                // Verify CRC
                var crcData = new byte[4 + length];
                Encoding.ASCII.GetBytes(name, 0, 4, crcData, 0);
                chunkData.CopyTo(crcData.AsSpan(4));

                var crcExpected = ComputeCrc32(crcData);

                if (crcActual != crcExpected)
                    throw new Exception($"CRC mismatch for chunk {name}. File may be corrupted.");

                chunks.Add(new Chunk { Name = name, Data = chunkData });
            }

            return chunks;
        }

        /// <summary>
        /// Encodes a list of chunks into PNG data.
        /// </summary>
        private static byte[] EncodeChunks(List<Chunk> chunks)
        {
            int totalSize = 8; // PNG signature

            foreach (var chunk in chunks)
                totalSize += 12 + chunk.Data.Length; // Length (4) + Type (4) + Data + CRC (4)

            var output = new byte[totalSize];
            var idx = 0;

            // Write PNG signature
            WritePngHeader(output, ref idx);

            // Write chunks
            foreach (var chunk in chunks)
            {
                // Write chunk length
                BinaryPrimitives.WriteUInt32BigEndian(output.AsSpan(idx), (uint)chunk.Data.Length);
                idx += 4;

                // Write chunk type
                var nameBytes = Encoding.ASCII.GetBytes(chunk.Name);
                nameBytes.CopyTo(output, idx);
                idx += 4;

                // Write chunk data
                chunk.Data.CopyTo(output, idx);
                idx += chunk.Data.Length;

                // Compute and write CRC
                var crcData = new byte[4 + chunk.Data.Length];
                nameBytes.CopyTo(crcData, 0);
                chunk.Data.CopyTo(crcData, 4);
                var crc = ComputeCrc32(crcData);

                BinaryPrimitives.WriteUInt32BigEndian(output.AsSpan(idx), crc);
                idx += 4;
            }

            return output;
        }

        /// <summary>
        /// Computes the CRC-32 checksum for the given data.
        /// </summary>
        private static uint ComputeCrc32(byte[] data)
        {
            uint crc = Crc32.HashToUInt32(data);
            return crc;
        }

        #region Helper Methods

        private static bool IsValidPngHeader(byte[] data)
        {
            var pngSignature = new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 };
            if (data.Length < pngSignature.Length)
                return false;

            return data.AsSpan(0, pngSignature.Length).SequenceEqual(pngSignature);
        }

        private static void WritePngHeader(byte[] output, ref int idx)
        {
            var pngSignature = new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 };
            pngSignature.CopyTo(output.AsSpan(idx));
            idx += pngSignature.Length;
        }

        #endregion
    }
}

