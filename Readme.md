# PngParser

PngParser is a lightweight, pure C# library for reading and writing metadata in PNG images. It allows you to extract and modify tEXt and pHYs chunks within PNG files without relying on any external dependencies.

## Features

- **Extract Metadata**: Read tEXt and pHYs chunks from PNG images.
- **Modify Metadata**: Add, update, or remove tEXt and pHYs chunks.
- **Lightweight**: No external dependencies; pure C# implementation.
- **Cross-Platform**: Compatible with any .NET Standard 2.0 compliant framework.

## Installation

Install the package via NuGet:

```bash
Install-Package PngParser
```

Or via the .NET CLI:

```bash
dotnet add package PngParser
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

PngParser targets **.NET Standard 2.0**, making it compatible with:

- .NET Framework 4.6.1 and later
- .NET Core 2.0 and later
- Mono 5.4 and later
- Xamarin.iOS 10.14 and later
- Xamarin.Mac 3.8 and later
- Xamarin.Android 8.0 and later
- Universal Windows Platform 10.0.16299 and later
- Unity 2018.1 and later

## Building from Source

1. **Clone the Repository**:

   ```bash
   git clone https://github.com/yourusername/PngParser.git
   ```

2. **Build the Project**:

   Navigate to the project directory and build the library:

   ```bash
   cd PngParser
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

This project is licensed under the MIT License. See the [LICENSE](LICENSE) file for details.

## Acknowledgements

- The PNG file format specification and related documentation.
- Contributors to the open-source community who have provided guidance and inspiration.

