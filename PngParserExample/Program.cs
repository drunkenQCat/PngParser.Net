using System;
using System.Collections.Generic;
using System.IO;
using PngParser;

namespace PngParserExample;

class Program
{
    static void Main()
    {
        var pngData = File.ReadAllBytes("input.png");

        // Read metadata
        var (textData, physData) = PngMetadata.ReadMetadata(pngData);

        foreach (var kvp in textData)
            Console.WriteLine($"Keyword: {kvp.Key}, Text: {kvp.Value}");

        if (physData != null)
            Console.WriteLine($"pHYs Data - X: {physData.X}, Y: {physData.Y}, Unit: {physData.Unit}");

        // Modify metadata
        var newTextData = new Dictionary<string, string>
        {
            { "Author", "John Doe" },
            { "Description", "Sample PNG image" }
        };

        var newPhysData = new PhysChunkData
        {
            X = 2835,
            Y = 2835,
            Unit = (byte)ResolutionUnits.Meters
        };

        var newPngData = PngMetadata.WriteMetadata(pngData, newTextData, newPhysData, clearMetadata: true);
        File.WriteAllBytes("output.png", newPngData);
    }
}

