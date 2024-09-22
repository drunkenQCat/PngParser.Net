using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

using PngParser;

namespace YourApplication
{
    class Program
    {
        static void Main()
        {
            // Read the PNG file
            byte[] pngData = File.ReadAllBytes("input.png");

            // Extract all chunks
            List<Chunk> chunks = PngMetadata.ReadChunks(pngData);

            // Create a new tEXt chunk
            var textChunk = new Chunk
            {
                Name = "tEXt",
                Data = PngUtilities.TextEncodeData("Author", "John Doe")
            };

            // Insert or replace the tEXt chunk
            PngMetadata.AddOrUpdateTextChunk(chunks, textChunk);

            // Create a new pHYs chunk
            var physChunk = new Chunk
            {
                Name = "pHYs",
                Data = PngUtilities.PhysEncodeData(2835, 2835, (byte)ResolutionUnits.Meters)
            };

            // Insert or replace the pHYs chunk
            PngMetadata.InsertOrReplaceChunk(chunks, physChunk);

            // Write the modified chunks back to PNG data
            byte[] newPngData = PngMetadata.WriteChunks(chunks);

            // Save the modified PNG file
            File.WriteAllBytes("output.png", newPngData);
        }
    }
}

