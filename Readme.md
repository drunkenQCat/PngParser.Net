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

### Reading and Writing Metadata

#### Initialize the `PngMetadata` Class

```csharp
using PngParser;

class Program
{
    static void Main()
    {
        // Initialize PngMetadata by reading an existing PNG file
        var pngMetadata = new PngMetadata("input.png");

        // Read textual metadata from the PNG file
        var metadata = pngMetadata.ReadTextChunks();

        // Display the metadata
        foreach (var kvp in metadata)
        {
            Console.WriteLine($"Keyword: {kvp.Key}, Text: {kvp.Value}");
        }

        // Update metadata by adding or modifying textual chunks
        var newMetadata = new Dictionary<string, string>
        {
            { "Author", "Jane Smith" },
            { "Description", "Test Image with updated metadata" }
        };

        pngMetadata.UpdateTextChunks(newMetadata);

        // Save the modified PNG file
        pngMetadata.Save();
    }
}
```
### Removing a Specific Chunk

You can remove a chunk (e.g., a `tEXt` chunk) from the PNG file using the `RemoveChunk` method:

```csharp
var pngMetadata = new PngMetadata("input.png");

// Remove all tEXt chunks
pngMetadata.RemoveChunk("tEXt");

// Save the modified PNG
pngMetadata.Save("output_without_text_chunks.png");
```

### Inserting or Replacing a Chunk

If you need to insert or replace a chunk in the PNG file (e.g., a `pHYs` chunk), you can use the `InsertOrReplaceChunk` method:

```csharp
var pngMetadata = new PngMetadata("input.png");

var physChunk = new Chunk
{
    Name = "pHYs",
    Data = PngUtilities.PhysEncodeData(2835, 2835, (byte)ResolutionUnits.Meters)
};

// Insert or replace the pHYs chunk
pngMetadata.InsertOrReplaceChunk(physChunk);

// Save the modified PNG
pngMetadata.Save("output_with_phys.png");
```
### API Reference

#### **PngMetadata**: Represents a PNG image and provides methods to modify its metadata.

- **`PngMetadata(string path)`**:  
  Initializes the `PngMetadata` class by loading a PNG file from the given file path.

- **`Dictionary<string, string> ReadTextChunks()`**:  
  Reads all textual chunks (`tEXt`, `iTXt`, `zTXt`) and returns them as a dictionary.

- **`void UpdateTextChunks(Dictionary<string, string> metadata, string chunkType = "tEXt")`**:  
  Adds or updates textual chunks based on the provided metadata dictionary. The `chunkType` parameter specifies the type of chunk (`tEXt`, `iTXt`, `zTXt`).

- **`void InsertOrReplaceChunk(Chunk newChunk)`**:  
  Inserts or replaces a single-instance chunk (e.g., `pHYs`).

- **`void RemoveChunk(string chunkName)`**:  
  Removes all chunks with the specified name.

- **`void Save()`**:  
  Saves the modified PNG back to the original file path.

- **`void Save(string path)`**:  
  Saves the modified PNG to a specified file path.

### Chunk Structure

- **`Chunk`**: Represents a PNG chunk.
  - **`Name`**: The chunk name (e.g., `"tEXt"`, `"IHDR"`, `"IDAT"`, etc.).
  - **`Data`**: The chunk data as a byte array.
## Compatibility

PngParser.Net targets **.NET 7** and higher, compatible to .Net Standard 2.0.

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
