# PngParser.Net

PngParser.Net is a lightweight, pure C# library for reading and writing metadata in PNG images, translated from [hometlt/png-metadata](https://github.com/hometlt/png-metadata/tree/master). In current stage, as the original repo, it allows you to extract and modify tEXt and pHYs chunks within PNG files without relying on any external dependencies.



## Installation

Install the package via NuGet:

```bash
Install-Package PngParser.Net
```

Or via the .NET CLI:

```bash
dotnet add package PngParser.Net
```

## Usage

### Reading Metadata

```csharp
using System;
using System.IO;
using PngParser;

class Program
{
    static void Main()
    {
        byte[] pngData = File.ReadAllBytes("input.png");

        // Read metadata
        var (textData, physData) = PngMetadata.ReadMetadata(pngData);

        // Display tEXt metadata
        foreach (var kvp in textData)
            Console.WriteLine($"Keyword: {kvp.Key}, Text: {kvp.Value}");

        // Display pHYs metadata
        if (physData != null)
            Console.WriteLine($"pHYs Data - X: {physData.X}, Y: {physData.Y}, Unit: {physData.Unit}");
    }
}
```

### Writing Metadata

```csharp
using System;
using System.Collections.Generic;
using System.IO;
using PngParser;

class Program
{
    static void Main()
    {
        byte[] pngData = File.ReadAllBytes("input.png");

        // Prepare new metadata
        var newTextData = new Dictionary<string, string>
        {
            { "Title", "Sample Image" },
            { "Author", "Jane Doe" }
        };

        var newPhysData = new PhysChunkData
        {
            X = 3000,
            Y = 3000,
            Unit = (byte)ResolutionUnits.Meters
        };

        // Write new metadata
        byte[] newPngData = PngMetadata.WriteMetadata(pngData, newTextData, newPhysData, clearMetadata: true);

        // Save the modified PNG file
        File.WriteAllBytes("output.png", newPngData);
    }
}
```

## API Reference

### Classes and Methods

- **PngMetadata**: Static class providing methods to read and write PNG metadata.
  - `ReadMetadata(byte[] buffer)`: Reads tEXt and pHYs metadata from a PNG byte array.
  - `WriteMetadata(byte[] buffer, Dictionary<string, string>? textData = null, PhysChunkData? physData = null, bool clearMetadata = false)`: Writes tEXt and pHYs metadata to a PNG byte array.
  - `InsertMetadata(List<Chunk> chunks, Dictionary<string, string>? textData, PhysChunkData? physData, bool clearMetadata)`: Inserts metadata into PNG chunks.
  - `ExtractChunks(byte[] data)`: Extracts chunks from a PNG byte array.
  - `EncodeChunks(List<Chunk> chunks)`: Encodes PNG chunks into a byte array.

- **Chunk**: Represents a PNG chunk.
  - `string Name`: The name of the chunk (e.g., "IHDR", "tEXt").
  - `byte[] Data`: The data contained in the chunk.

- **PhysChunkData**: Represents the data in a pHYs chunk.
  - `uint X`: Pixels per unit in the X direction.
  - `uint Y`: Pixels per unit in the Y direction.
  - `byte Unit`: Unit specifier (0 for unknown, 1 for meters).

- **ResolutionUnits**: Enum representing resolution units.
  - `Undefined = 0`
  - `Meters = 1`
  - `Inches = 2`

## Compatibility

PngParser.Net targets **.NET 8** and higher, may make some work for compatible and migrate it to .Net Standard 2.0 if someone need it.

## Building from Source

1. **Clone the Repository**:

   ```bash
   git clone https://github.com/drunkenQCat/PngParser.Net.git
   ```

2. **Build the Project**:

   Navigate to the project directory and build the library:

   ```bash
   cd PngParser.Net
   dotnet build
   ```

3. **Run Tests**:

   If you have included unit tests, you can run them using:

   ```bash
   dotnet test
   ```

## Contributing

Contributions are welcome! Please open an issue or submit a pull request for any improvements or bug fixes.

## License

This project is licensed under the MIT License. See the [LICENSE](LICENSE.txt) file for details.

## Acknowledgements

- The PNG file format specification and related documentation:
  -  [PNG Specification: Chunk Specifications (w3.org)](https://www.w3.org/TR/PNG-Chunks.html)
  - [The Metadata in PNG files - Exiv2](https://dev.exiv2.org/projects/exiv2/wiki/The_Metadata_in_PNG_files)

- Contributors to the open-source community who have provided guidance and inspiration.

