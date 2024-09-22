using System.Collections.Generic;

using PngParser;

namespace YourApplication
{
    class Program
    {
        static void Main()
        {
            // Read the PNG file
            var png = new PngMetadata("input.png");
            // Create a new tEXt chunk
            var textChunk = new Dictionary<string, string>{
                {"Author" , "John Doe"},
                {"Description" , "Test Description"}
            };

            // Insert or replace the tEXt chunk
            png.UpdateTextChunks(textChunk);

            // Create a new pHYs chunk
            var physChunk = new Chunk
            {
                Name = "pHYs",
                Data = PngUtilities.PhysEncodeData(2835, 2835, (byte)ResolutionUnits.Meters)
            };

            // Insert or replace the pHYs chunk
            png.InsertOrReplaceChunk(physChunk);

            // Write the modified chunks back to PNG data
            png.Save();
        }
    }
}

